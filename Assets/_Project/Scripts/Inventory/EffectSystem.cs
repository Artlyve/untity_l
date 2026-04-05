using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using ProjectFPS.Player;

namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Gère les effets de potions actifs sur le joueur.
    /// Ajouter sur le même GameObject que PlayerState.
    ///
    /// Effets implémentés :
    ///   Vitesse   → +50% vitesse (60s)
    ///   Poison    → −20% vitesse + −50% PV immédiats (10s)
    ///   Géant     → ×1.5 taille + ×1.3 vitesse (45s) → revert automatique
    ///   Invisible → body mesh en ShadowsOnly, invisible mais ombre gardée (30s)
    ///   Ouïe      → portée interaction ×2 + pulse de détection toutes les 3s (60s)
    ///   Vie       → protection résurrection : soigne 50% PV si mort (60s)
    ///   Aveuglant → overlay noir plein écran sur la caméra de la cible (45s)
    /// </summary>
    public class EffectSystem : MonoBehaviour
    {
        // ── Références ────────────────────────────────────────────────────────────
        [Header("Références visuelles")]
        [Tooltip("Root du mesh du joueur (pour Invisible). Null = auto-cherché parmi les enfants.")]
        [SerializeField] private Transform bodyRoot;

        [Header("Ouïe")]
        [Tooltip("Rayon de détection du pulse Ouïe (joueurs et items).")]
        [SerializeField] private float hearingRadius = 12f;
        [Tooltip("Intervalle en secondes entre chaque pulse Ouïe.")]
        [SerializeField] private float hearingPulseInterval = 3f;

        // ── État interne ──────────────────────────────────────────────────────────
        private readonly List<ActiveEffect> _effects = new List<ActiveEffect>();
        private PlayerState _playerState;
        private Camera      _cam;

        // Géant
        private bool    _isGiant;
        private Vector3 _originalScale;

        // Invisible
        private bool                _isInvisible;
        private readonly List<Renderer> _hiddenRenderers = new List<Renderer>();

        // Aveuglant
        private bool  _isBlinded;
        private Image _blindOverlay;

        // Ouïe
        private float _hearingPulseTimer;

        // ── Propriétés calculées ──────────────────────────────────────────────────

        /// <summary>Multiplicateur de vitesse cumulé de tous les effets actifs.</summary>
        public float SpeedMultiplier
        {
            get
            {
                float m = 1f;
                foreach (var e in _effects)
                {
                    switch (e.Type)
                    {
                        case PotionType.Vitesse: m *= 1.5f; break;
                        case PotionType.Poison:  m *= 0.8f; break;
                        case PotionType.Géant:   m *= 1.3f; break;
                    }
                }
                return m;
            }
        }

        public bool IsInvisible         => _isInvisible;
        public bool IsBlinded           => _isBlinded;
        public bool IsEnhancedHearing   => HasEffect(PotionType.Ouïe);
        public bool HasReviveProtection => HasEffect(PotionType.Vie);

        /// <summary>
        /// Multiplicateur de portée d'interaction.
        /// ×2 avec Ouïe active, ×1 sinon.
        /// Utilisé par PlayerInteraction.
        /// </summary>
        public float InteractionRangeMultiplier => IsEnhancedHearing ? 2f : 1f;

        /// <summary>Effets actifs exposés en lecture seule pour le HUD.</summary>
        public IReadOnlyList<ActiveEffect> ActiveEffects => _effects;

        /// <summary>Déclenché à chaque modification de la liste des effets.</summary>
        public event Action OnEffectsChanged;

        /// <summary>
        /// Déclenché par le pulse Ouïe (toutes les 3s quand actif).
        /// Paramètre : liste des descriptions des entités détectées à portée.
        /// Abonnez-vous pour afficher des indicateurs visuels.
        /// </summary>
        public event Action<List<string>> OnHearingPulse;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _playerState   = GetComponent<PlayerState>();
            _cam           = GetComponentInChildren<Camera>();
            _originalScale = transform.localScale;

            // Auto-cherche le body root si non assigné dans l'Inspector
            if (bodyRoot == null)
                bodyRoot = transform;
        }

        private void Update()
        {
            bool changed = false;

            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                _effects[i].TimeRemaining -= Time.deltaTime;
                changed = true;

                if (_effects[i].TimeRemaining <= 0f)
                {
                    string expiredName = _effects[i].DisplayName;
                    RevertVisualEffect(_effects[i].Type);
                    _effects.RemoveAt(i);
                    Debug.Log($"[EffectSystem] ⏱ Effet '{expiredName}' expiré sur {gameObject.name}.");
                }
            }

            if (changed)
                OnEffectsChanged?.Invoke();

            // ── Pulse Ouïe ───────────────────────────────────────────────────────
            if (IsEnhancedHearing)
            {
                _hearingPulseTimer -= Time.deltaTime;
                if (_hearingPulseTimer <= 0f)
                {
                    _hearingPulseTimer = hearingPulseInterval;
                    PerformHearingPulse();
                }
            }
            else
            {
                _hearingPulseTimer = 0f; // reset pour que le 1er pulse soit immédiat
            }
        }

        private void OnDestroy()
        {
            // Nettoie les effets visuels si l'objet est détruit
            RevertGiant();
            RevertInvisible();
            RevertBlind();
        }

        // ── API publique ──────────────────────────────────────────────────────────

        /// <summary>
        /// Applique l'effet d'une potion sur ce joueur.
        /// Appelé par PlayerInteraction ([F]) ou ItemWorldObject (lancé).
        /// </summary>
        public void ApplyEffect(ItemData itemData)
        {
            if (itemData == null || itemData.Type != ItemType.Potion) return;

            var type = itemData.PotionSubType;
            Debug.Log($"[EffectSystem] ▶ Application effet '{type}' sur {gameObject.name}");

            switch (type)
            {
                case PotionType.Vitesse:
                    AddOrRefresh(type, 60f, "Vitesse ⚡", itemData.Icon);
                    Debug.Log($"[EffectSystem] Vitesse : +50% vitesse pour 60s");
                    break;

                case PotionType.Poison:
                    AddOrRefresh(type, 10f, "Poison ☠", itemData.Icon);
                    if (_playerState != null)
                    {
                        float dmg = _playerState.MaxHealth * 0.5f;
                        _playerState.TakeDamage(dmg);
                        Debug.Log($"[EffectSystem] Poison : dégâts immédiats {dmg} PV sur {gameObject.name}");
                    }
                    break;

                case PotionType.Géant:
                    bool wasGiant = _isGiant;
                    AddOrRefresh(type, 45f, "Géant 🗿", itemData.Icon);
                    if (!wasGiant) ApplyGiant();
                    break;

                case PotionType.Invisible:
                    bool wasInvisible = _isInvisible;
                    AddOrRefresh(type, 30f, "Invisible 👻", itemData.Icon);
                    if (!wasInvisible) ApplyInvisible();
                    break;

                case PotionType.Ouïe:
                    AddOrRefresh(type, 60f, "Ouïe 👂", itemData.Icon);
                    _hearingPulseTimer = 0f; // déclenche le 1er pulse immédiatement
                    Debug.Log($"[EffectSystem] Ouïe : portée interaction ×2, détection ennemis/items toutes les {hearingPulseInterval}s.");
                    break;

                case PotionType.Vie:
                    AddOrRefresh(type, 60f, "Résurrection ❤", itemData.Icon);
                    Debug.Log($"[EffectSystem] Vie : protection résurrection active sur {gameObject.name} (60s).");
                    break;

                case PotionType.Aveuglant:
                    bool wasBlinded = _isBlinded;
                    AddOrRefresh(type, 45f, "Aveuglant 🌑", itemData.Icon);
                    if (!wasBlinded) ApplyBlind();
                    break;
            }

            OnEffectsChanged?.Invoke();
        }

        /// <summary>
        /// Consomme la protection de résurrection.
        /// Appelé par PlayerState quand le joueur mourrait.
        /// Retourne true si la résurrection a eu lieu.
        /// </summary>
        public bool TryRevive()
        {
            if (!HasReviveProtection) return false;

            RemoveEffect(PotionType.Vie);
            if (_playerState != null)
            {
                _playerState.Heal(_playerState.MaxHealth * 0.5f);
                Debug.Log($"[EffectSystem] ✨ {gameObject.name} résurrection ! (potion Vie consommée, 50% PV restaurés)");
            }
            OnEffectsChanged?.Invoke();
            return true;
        }

        // ── Effets visuels : Géant ────────────────────────────────────────────────

        private void ApplyGiant()
        {
            if (_isGiant) return;
            _isGiant       = true;
            _originalScale = transform.localScale;
            transform.localScale = _originalScale * 1.5f;
            Debug.Log($"[EffectSystem] Géant : taille ×1.5 appliquée sur {gameObject.name}");
        }

        private void RevertGiant()
        {
            if (!_isGiant) return;
            _isGiant             = false;
            transform.localScale = _originalScale;
            Debug.Log($"[EffectSystem] Géant : taille normale restaurée sur {gameObject.name}");
        }

        // ── Effets visuels : Invisible ────────────────────────────────────────────

        private void ApplyInvisible()
        {
            if (_isInvisible) return;
            _isInvisible = true;
            _hiddenRenderers.Clear();

            foreach (var r in bodyRoot.GetComponentsInChildren<Renderer>())
            {
                // On garde les renderers directement sous la caméra (armes FPS, bras visibles)
                if (_cam != null && r.transform.IsChildOf(_cam.transform)) continue;

                _hiddenRenderers.Add(r);
                // ShadowsOnly : objet invisible mais projette quand même son ombre
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }

            Debug.Log($"[EffectSystem] Invisible : {_hiddenRenderers.Count} renderer(s) masqué(s) sur {gameObject.name} (ombres gardées)");
        }

        private void RevertInvisible()
        {
            if (!_isInvisible) return;
            _isInvisible = false;

            foreach (var r in _hiddenRenderers)
                if (r != null) r.shadowCastingMode = ShadowCastingMode.On;

            _hiddenRenderers.Clear();
            Debug.Log($"[EffectSystem] Invisible : renderers restaurés sur {gameObject.name}");
        }

        // ── Effets visuels : Aveuglant ────────────────────────────────────────────

        private void ApplyBlind()
        {
            if (_isBlinded) return;
            _isBlinded = true;
            EnsureBlindOverlay();
            if (_blindOverlay != null)
                _blindOverlay.transform.parent.gameObject.SetActive(true);
            Debug.Log($"[EffectSystem] Aveuglant : overlay activé sur {gameObject.name}");
        }

        private void RevertBlind()
        {
            if (!_isBlinded) return;
            _isBlinded = false;
            if (_blindOverlay != null)
                _blindOverlay.transform.parent.gameObject.SetActive(false);
            Debug.Log($"[EffectSystem] Aveuglant : overlay désactivé sur {gameObject.name}");
        }

        /// <summary>Crée un Canvas fullscreen avec Image noire comme overlay.</summary>
        private void EnsureBlindOverlay()
        {
            if (_blindOverlay != null) return;

            // Canvas Screen Space Overlay (indépendant de la caméra)
            var canvasGO    = new GameObject("BlindCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas      = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.SetActive(false); // désactivé par défaut

            // Image noire plein écran
            var imgGO    = new GameObject("BlindImage");
            imgGO.transform.SetParent(canvasGO.transform, false);
            var rt       = imgGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            _blindOverlay       = imgGO.AddComponent<Image>();
            _blindOverlay.color = new Color(0f, 0f, 0f, 0.92f);

            Debug.Log($"[EffectSystem] BlindCanvas créé sur {gameObject.name}");
        }

        // ── Effet Ouïe : pulse de détection ──────────────────────────────────────

        /// <summary>
        /// Détecte joueurs et items dans le rayon hearingRadius.
        /// Déclenche OnHearingPulse avec la liste des entités trouvées.
        /// Extensible : abonnez-vous à OnHearingPulse pour afficher des indicateurs.
        /// </summary>
        private void PerformHearingPulse()
        {
            var detected = new List<string>();
            var cols = Physics.OverlapSphere(transform.position, hearingRadius);

            foreach (var c in cols)
            {
                // Ignore les colliders sur soi-même
                if (c.transform == transform || c.transform.IsChildOf(transform)) continue;

                // Joueur à portée
                var ps = c.GetComponent<PlayerState>() ?? c.GetComponentInParent<PlayerState>();
                if (ps != null)
                {
                    float dist = Vector3.Distance(transform.position, ps.transform.position);
                    string entry = $"Joueur '{ps.name}' à {dist:F1}m (PV: {Mathf.CeilToInt(ps.CurrentHealth)}/{Mathf.CeilToInt(ps.MaxHealth)})";
                    // Évite les doublons (plusieurs colliders sur le même objet)
                    if (!detected.Contains(entry)) detected.Add(entry);
                    continue;
                }

                // Item à portée
                var iwo = c.GetComponent<ItemWorldObject>() ?? c.GetComponentInParent<ItemWorldObject>();
                if (iwo != null && iwo.Data != null)
                {
                    float dist = Vector3.Distance(transform.position, iwo.transform.position);
                    string entry = $"Item '{iwo.Data.ItemName}' à {dist:F1}m";
                    if (!detected.Contains(entry)) detected.Add(entry);
                }
            }

            OnHearingPulse?.Invoke(detected);

            if (detected.Count > 0)
                Debug.Log($"[EffectSystem] 👂 Ouïe pulse sur {gameObject.name} : " + string.Join(" | ", detected));
            else
                Debug.Log($"[EffectSystem] 👂 Ouïe pulse sur {gameObject.name} : rien détecté dans {hearingRadius}m.");
        }

        // ── Dispatch revert à l'expiration ───────────────────────────────────────

        private void RevertVisualEffect(PotionType type)
        {
            switch (type)
            {
                case PotionType.Géant:    RevertGiant();     break;
                case PotionType.Invisible: RevertInvisible(); break;
                case PotionType.Aveuglant: RevertBlind();     break;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private bool HasEffect(PotionType type)
        {
            foreach (var e in _effects)
                if (e.Type == type) return true;
            return false;
        }

        /// <summary>Ajoute l'effet ou rafraîchit sa durée s'il est déjà actif.</summary>
        private void AddOrRefresh(PotionType type, float duration, string displayName, Sprite icon)
        {
            foreach (var e in _effects)
            {
                if (e.Type == type)
                {
                    e.TimeRemaining = duration;
                    Debug.Log($"[EffectSystem] ↺ Effet '{displayName}' rafraîchi ({duration}s) sur {gameObject.name}.");
                    return;
                }
            }
            _effects.Add(new ActiveEffect(type, duration, displayName, icon));
            Debug.Log($"[EffectSystem] ✚ Nouvel effet '{displayName}' ajouté ({duration}s) sur {gameObject.name}.");
        }

        private void RemoveEffect(PotionType type)
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (_effects[i].Type == type)
                {
                    RevertVisualEffect(type);
                    _effects.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
