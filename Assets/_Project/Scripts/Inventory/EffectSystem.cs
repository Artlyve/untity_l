using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectFPS.Player;

namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Gère les effets de potions actifs sur le joueur.
    ///
    /// Ajouter ce composant sur le même GameObject que PlayerState.
    ///
    /// Effets supportés :
    ///   Vitesse   → +50% vitesse pendant 60s
    ///   Poison    → −20% vitesse pendant 10s + −50% PV immédiats (si lancé sur cible)
    ///   Géant     → +30% vitesse + visuel "géant" pendant 45s  (TODO visuel)
    ///   Invisible → cache le mesh du joueur pendant 30s          (TODO visuel)
    ///   Ouïe      → TODO (effet gameplay à définir)
    ///   Vie       → protection résurrection pendant 60s
    ///   Aveuglant → aveugle la cible pendant 45s                 (TODO overlay caméra)
    /// </summary>
    public class EffectSystem : MonoBehaviour
    {
        // ── État courant ──────────────────────────────────────────────────────────
        private readonly List<ActiveEffect> _effects  = new List<ActiveEffect>();
        private PlayerState                 _playerState;

        // ── Propriétés calculées en temps réel ───────────────────────────────────

        /// <summary>Multiplicateur de vitesse cumulé par les effets actifs.</summary>
        public float SpeedMultiplier
        {
            get
            {
                float m = 1f;
                foreach (var e in _effects)
                {
                    switch (e.Type)
                    {
                        case PotionType.Vitesse:  m *= 1.5f;  break;
                        case PotionType.Poison:   m *= 0.8f;  break;
                        case PotionType.Géant:    m *= 1.3f;  break;
                    }
                }
                return m;
            }
        }

        public bool IsInvisible        => HasEffect(PotionType.Invisible);
        public bool IsBlinded          => HasEffect(PotionType.Aveuglant);
        public bool HasReviveProtection => HasEffect(PotionType.Vie);

        /// <summary>Effets actifs (lecture seule pour le HUD).</summary>
        public IReadOnlyList<ActiveEffect> ActiveEffects => _effects;

        // ── Événements ────────────────────────────────────────────────────────────
        /// <summary>Déclenché à chaque ajout, retrait ou tick d'effet.</summary>
        public event Action OnEffectsChanged;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _playerState = GetComponent<PlayerState>();
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
                    Debug.Log($"[EffectSystem] Effet '{_effects[i].DisplayName}' expiré.");
                    _effects.RemoveAt(i);
                }
            }

            if (changed)
                OnEffectsChanged?.Invoke();
        }

        // ── API publique ──────────────────────────────────────────────────────────

        /// <summary>
        /// Applique l'effet d'une potion.
        /// selfApplied = true si le joueur l'utilise sur lui-même ([F]).
        /// selfApplied = false si elle lui est lancée dessus ([Q] d'un autre joueur).
        /// </summary>
        public void ApplyEffect(ItemData itemData)
        {
            if (itemData == null || itemData.Type != ItemType.Potion) return;

            var type = itemData.PotionSubType;

            Debug.Log($"[EffectSystem] Application de l'effet '{type}' sur {name}");

            switch (type)
            {
                case PotionType.Vitesse:
                    AddOrRefresh(type, 60f, "Vitesse", itemData.Icon);
                    break;

                case PotionType.Poison:
                    AddOrRefresh(type, 10f, "Poison", itemData.Icon);
                    // Dégâts immédiats : −50% PV
                    if (_playerState != null)
                    {
                        float dmg = _playerState.MaxHealth * 0.5f;
                        _playerState.TakeDamage(dmg);
                        Debug.Log($"[EffectSystem] Poison → dégâts immédiats {dmg} PV sur {name}");
                    }
                    break;

                case PotionType.Géant:
                    AddOrRefresh(type, 45f, "Géant", itemData.Icon);
                    // TODO : agrandir le modèle (transform.localScale)
                    break;

                case PotionType.Invisible:
                    AddOrRefresh(type, 30f, "Invisible", itemData.Icon);
                    // TODO : désactiver Renderer du body mesh
                    break;

                case PotionType.Ouïe:
                    AddOrRefresh(type, 60f, "Ouïe", itemData.Icon);
                    // TODO : effet gameplay ouïe
                    break;

                case PotionType.Vie:
                    AddOrRefresh(type, 60f, "Résurrection", itemData.Icon);
                    Debug.Log($"[EffectSystem] Protection résurrection active sur {name} (60s).");
                    break;

                case PotionType.Aveuglant:
                    AddOrRefresh(type, 45f, "Aveuglant", itemData.Icon);
                    // TODO : overlay noir sur la caméra de la cible
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
                Debug.Log($"[EffectSystem] {name} résurrection ! (potion Vie consommée, 50% PV restaurés)");
            }
            OnEffectsChanged?.Invoke();
            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private bool HasEffect(PotionType type)
        {
            foreach (var e in _effects)
                if (e.Type == type) return true;
            return false;
        }

        /// <summary>Ajoute un effet ou remet sa durée à zéro s'il est déjà actif.</summary>
        private void AddOrRefresh(PotionType type, float duration, string displayName, Sprite icon)
        {
            foreach (var e in _effects)
            {
                if (e.Type == type)
                {
                    e.TimeRemaining = duration;
                    Debug.Log($"[EffectSystem] Effet '{displayName}' rafraîchi ({duration}s).");
                    return;
                }
            }
            _effects.Add(new ActiveEffect(type, duration, displayName, icon));
            Debug.Log($"[EffectSystem] Nouvel effet '{displayName}' ajouté ({duration}s).");
        }

        private void RemoveEffect(PotionType type)
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
                if (_effects[i].Type == type) { _effects.RemoveAt(i); return; }
        }
    }
}
