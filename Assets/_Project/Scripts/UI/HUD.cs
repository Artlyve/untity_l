using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectFPS.Player;
using ProjectFPS.Roles;
using ProjectFPS.Inventory;

namespace ProjectFPS.UI
{
    /// <summary>
    /// Affiche en temps réel :
    ///   • Barre de vie + texte PV
    ///   • Nom du rôle actif
    ///   • Slots d'inventaire avec icônes
    ///   • Réticule (auto-créé si absent)
    ///   • Prompt d'interaction [E]
    ///   • Points de ressources personnels
    ///   • Effets actifs : icône + fill radial + compteur secondes
    ///
    /// Toutes les références joueur sont auto-trouvées dans la scène si non assignées
    /// dans l'Inspector (cherche via FindFirstObjectByType).
    /// </summary>
    public class HUD : MonoBehaviour
    {
        // ── Champs Inspector ──────────────────────────────────────────────────────

        [Header("Barre de vie")]
        [SerializeField] private Slider          healthBar;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Rôle actif")]
        [SerializeField] private TextMeshProUGUI roleNameText;
        [SerializeField] private Image           roleIcon;

        [Header("Slots d'inventaire")]
        [SerializeField] private Transform  slotsContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Color      selectedSlotColor   = Color.yellow;
        [SerializeField] private Color      deselectedSlotColor = Color.white;

        [Header("Prompt d'interaction")]
        [SerializeField] private TextMeshProUGUI interactionPrompt;

        [Header("Réticule")]
        [Tooltip("Laissez vide : un réticule est créé automatiquement.")]
        [SerializeField] private Image reticle;
        [SerializeField] private Color reticleColor     = Color.white;
        [SerializeField] private Color reticleHighlight = new Color(1f, 0.85f, 0f);

        [Header("Ressources")]
        [SerializeField] private TextMeshProUGUI resourceText;

        [Header("Effets actifs")]
        [Tooltip("Conteneur HLG des icônes d'effets. Laissez vide : auto-créé en bas à gauche.")]
        [SerializeField] private Transform effectsContainer;
        [SerializeField] private float     effectIconSize = 52f;

        [Header("Références joueur (auto-trouvées si vides)")]
        [SerializeField] private PlayerState       playerState;
        [SerializeField] private InventorySystem   inventorySystem;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private EffectSystem      effectSystem;

        // ── Internes ──────────────────────────────────────────────────────────────
        private Image[] _slotBackgrounds;
        private bool    _hasInteractable;
        private bool    _started;           // vrai après Start()

        private readonly List<EffectIconEntry> _effectIcons = new List<EffectIconEntry>();

        private struct EffectIconEntry
        {
            public ActiveEffect    Effect;
            public GameObject      Root;
            public Image           CooldownFill;
            public TextMeshProUGUI TimerText;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);

            EnsureReticle();
        }

        private void Start()
        {
            ResolveReferences();
            EnsureEffectsContainer();

            SubscribeEvents();
            _started = true;

            InitializeSlots();

            if (playerState != null)
                UpdateHealthBar(playerState.CurrentHealth, playerState.MaxHealth);

            if (RoleManager.Instance?.CurrentRole != null)
                UpdateRoleDisplay(RoleManager.Instance.CurrentRole);

            UpdateInventorySlots();
            UpdateResourceText(inventorySystem != null ? inventorySystem.PersonalResources : 0);
            RefreshEffectIcons();

            Debug.Log("[HUD] Démarrage — références :" +
                $" playerState={playerState?.name ?? "NULL"}," +
                $" inventory={inventorySystem?.name ?? "NULL"}," +
                $" effectSystem={effectSystem?.name ?? "NULL"}," +
                $" interaction={playerInteraction?.name ?? "NULL"}");
        }

        private void OnEnable()
        {
            if (_started) SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            UpdateSelectedSlotHighlight();
            UpdateEffectTimers();
        }

        // ── Résolution des références ─────────────────────────────────────────────

        private void ResolveReferences()
        {
#if UNITY_6000_0_OR_NEWER
            if (playerState     == null) playerState     = FindFirstObjectByType<PlayerState>();
            if (inventorySystem == null) inventorySystem = FindFirstObjectByType<InventorySystem>();
            if (playerInteraction==null) playerInteraction= FindFirstObjectByType<PlayerInteraction>();
            if (effectSystem    == null) effectSystem    = FindFirstObjectByType<EffectSystem>();
#else
            if (playerState     == null) playerState     = FindObjectOfType<PlayerState>();
            if (inventorySystem == null) inventorySystem = FindObjectOfType<InventorySystem>();
            if (playerInteraction==null) playerInteraction= FindObjectOfType<PlayerInteraction>();
            if (effectSystem    == null) effectSystem    = FindObjectOfType<EffectSystem>();
#endif

            if (playerState      == null) Debug.LogWarning("[HUD] PlayerState introuvable dans la scène !");
            if (inventorySystem  == null) Debug.LogWarning("[HUD] InventorySystem introuvable dans la scène !");
            if (effectSystem     == null) Debug.LogWarning("[HUD] EffectSystem introuvable dans la scène !");
        }

        // ── Abonnements événements ────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (playerState != null)
                playerState.OnHealthChanged += UpdateHealthBar;

            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged += OnInventoryChanged;
                inventorySystem.OnResourceChanged  += UpdateResourceText;
            }

            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged += UpdateRoleDisplay;

            if (playerInteraction != null)
                playerInteraction.OnInteractionPrompt += UpdateInteractionPrompt;

            if (effectSystem != null)
                effectSystem.OnEffectsChanged += RefreshEffectIcons;
        }

        private void UnsubscribeEvents()
        {
            if (playerState != null)
                playerState.OnHealthChanged -= UpdateHealthBar;

            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged -= OnInventoryChanged;
                inventorySystem.OnResourceChanged  -= UpdateResourceText;
            }

            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged -= UpdateRoleDisplay;

            if (playerInteraction != null)
                playerInteraction.OnInteractionPrompt -= UpdateInteractionPrompt;

            if (effectSystem != null)
                effectSystem.OnEffectsChanged -= RefreshEffectIcons;
        }

        // ── Slots d'inventaire ────────────────────────────────────────────────────

        private void InitializeSlots()
        {
            // Crée le conteneur si absent
            if (slotsContainer == null)
            {
                var go = new GameObject("SlotsContainer");
                go.transform.SetParent(transform, false);
                var rt          = go.AddComponent<RectTransform>();
                rt.anchorMin    = new Vector2(0.5f, 0f);
                rt.anchorMax    = new Vector2(0.5f, 0f);
                rt.pivot        = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 10f);
                rt.sizeDelta    = new Vector2(200f, 60f);
                var hlg         = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing     = 6f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childForceExpandWidth  = false;
                hlg.childForceExpandHeight = false;
                slotsContainer  = go.transform;
            }

            // Efface les anciens slots
            foreach (Transform child in slotsContainer)
                Destroy(child.gameObject);

            int count        = inventorySystem != null ? inventorySystem.MaxSlots : 1;
            _slotBackgrounds = new Image[count];

            for (int i = 0; i < count; i++)
            {
                GameObject slotGO;

                if (slotPrefab != null)
                {
                    slotGO = Instantiate(slotPrefab, slotsContainer);
                }
                else
                {
                    // Crée un slot par défaut si aucun prefab n'est assigné
                    slotGO = CreateDefaultSlot(slotsContainer);
                }

                _slotBackgrounds[i] = slotGO.GetComponent<Image>();
            }

            Debug.Log($"[HUD] Slots initialisés : {count} slot(s)");
        }

        /// <summary>Slot par défaut : fond gris foncé + icône blanche centrée.</summary>
        private GameObject CreateDefaultSlot(Transform parent)
        {
            // Fond
            var slotGO    = new GameObject("Slot");
            slotGO.transform.SetParent(parent, false);
            var slotRT    = slotGO.AddComponent<RectTransform>();
            slotRT.sizeDelta = new Vector2(54f, 54f);
            var slotImg   = slotGO.AddComponent<Image>();
            slotImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            // Icône
            var iconGO    = new GameObject("ItemIcon");
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconRT    = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin    = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax    = new Vector2(0.9f, 0.9f);
            iconRT.sizeDelta    = Vector2.zero;
            var iconImg   = iconGO.AddComponent<Image>();
            iconImg.color    = Color.white;
            iconImg.enabled  = false;       // caché tant que vide
            iconImg.preserveAspect = true;

            return slotGO;
        }

        // ── Callbacks ─────────────────────────────────────────────────────────────

        private void UpdateHealthBar(float current, float max)
        {
            if (healthBar != null)
                healthBar.value = max > 0f ? current / max : 0f;

            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void UpdateRoleDisplay(RoleData role)
        {
            if (role == null) return;

            if (roleNameText != null)
                roleNameText.text = role.RoleName;

            if (roleIcon != null)
            {
                roleIcon.sprite  = role.Icon;
                roleIcon.enabled = role.Icon != null;
            }

            Debug.Log($"[HUD] Rôle affiché → {role.RoleName}");
            InitializeSlots(); // le nombre de slots peut changer
        }

        private void OnInventoryChanged()
        {
            Debug.Log("[HUD] Inventaire modifié → mise à jour des slots");
            UpdateInventorySlots();
        }

        private void UpdateInventorySlots()
        {
            if (_slotBackgrounds == null || inventorySystem == null) return;

            if (_slotBackgrounds.Length != inventorySystem.MaxSlots)
            {
                InitializeSlots();
                return;
            }

            for (int i = 0; i < _slotBackgrounds.Length; i++)
            {
                if (_slotBackgrounds[i] == null) continue;

                Transform iconT = _slotBackgrounds[i].transform.Find("ItemIcon");
                if (iconT == null) continue;

                Image iconImg = iconT.GetComponent<Image>();
                if (iconImg == null) continue;

                ItemData item  = inventorySystem.GetItem(i);
                iconImg.sprite = item?.Icon;
                iconImg.color  = item != null ? Color.white : Color.clear;
                iconImg.enabled = item != null;
            }
        }

        private void UpdateSelectedSlotHighlight()
        {
            if (_slotBackgrounds == null || inventorySystem == null) return;

            for (int i = 0; i < _slotBackgrounds.Length; i++)
            {
                if (_slotBackgrounds[i] == null) continue;
                _slotBackgrounds[i].color = i == inventorySystem.SelectedSlot
                    ? selectedSlotColor
                    : deselectedSlotColor;
            }
        }

        private void UpdateInteractionPrompt(string promptText)
        {
            _hasInteractable = promptText != null;

            if (interactionPrompt != null)
            {
                interactionPrompt.text = promptText ?? "";
                interactionPrompt.gameObject.SetActive(promptText != null);
            }

            if (reticle != null)
                reticle.color = _hasInteractable ? reticleHighlight : reticleColor;
        }

        private void UpdateResourceText(int personal)
        {
            if (resourceText != null)
                resourceText.text = $"Ressources : {personal}";
        }

        // ── Réticule ──────────────────────────────────────────────────────────────

        private void EnsureReticle()
        {
            if (reticle != null) return;

            var go = new GameObject("Reticle");
            go.transform.SetParent(transform, false);
            var rt       = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;

            reticle = CreateReticleElement(go.transform, "Dot",    new Vector2(4, 4),  Vector2.zero);
            CreateReticleElement(go.transform, "Top",    new Vector2(2, 8),  new Vector2(0,  10));
            CreateReticleElement(go.transform, "Bottom", new Vector2(2, 8),  new Vector2(0, -10));
            CreateReticleElement(go.transform, "Left",   new Vector2(8, 2),  new Vector2(-10,  0));
            CreateReticleElement(go.transform, "Right",  new Vector2(8, 2),  new Vector2( 10,  0));
        }

        private Image CreateReticleElement(Transform parent, string n, Vector2 size, Vector2 offset)
        {
            var go       = new GameObject(n);
            go.transform.SetParent(parent, false);
            var rt       = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = size;
            rt.anchoredPosition = offset;
            var img     = go.AddComponent<Image>();
            img.color   = reticleColor;
            return img;
        }

        // ── Effets actifs ─────────────────────────────────────────────────────────

        private void EnsureEffectsContainer()
        {
            if (effectsContainer != null) return;

            var go       = new GameObject("EffectsContainer");
            go.transform.SetParent(transform, false);
            var rt       = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 0f);
            rt.pivot            = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(10f, 90f);
            rt.sizeDelta        = new Vector2(400f, effectIconSize + 8f);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 6f;
            hlg.childAlignment         = TextAnchor.LowerLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            effectsContainer = go.transform;
        }

        private void RefreshEffectIcons()
        {
            if (effectsContainer == null) return;

            IReadOnlyList<ActiveEffect> active = effectSystem != null
                ? effectSystem.ActiveEffects
                : (IReadOnlyList<ActiveEffect>)System.Array.Empty<ActiveEffect>();

            // Supprimer les icônes d'effets expirés
            for (int i = _effectIcons.Count - 1; i >= 0; i--)
            {
                bool stillActive = false;
                foreach (var e in active)
                    if (e == _effectIcons[i].Effect) { stillActive = true; break; }

                if (!stillActive)
                {
                    Debug.Log($"[HUD] Icône effet '{_effectIcons[i].Effect?.DisplayName}' supprimée.");
                    if (_effectIcons[i].Root != null) Destroy(_effectIcons[i].Root);
                    _effectIcons.RemoveAt(i);
                }
            }

            // Ajouter les icônes des nouveaux effets
            foreach (var effect in active)
            {
                bool exists = false;
                foreach (var entry in _effectIcons)
                    if (entry.Effect == effect) { exists = true; break; }

                if (!exists)
                {
                    Debug.Log($"[HUD] Icône effet '{effect.DisplayName}' ajoutée.");
                    _effectIcons.Add(BuildEffectIcon(effect));
                }
            }
        }

        private EffectIconEntry BuildEffectIcon(ActiveEffect effect)
        {
            float sz = effectIconSize;

            var root = new GameObject($"FX_{effect.DisplayName}");
            root.transform.SetParent(effectsContainer, false);
            var rootRT       = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(sz, sz);

            // Fond semi-transparent
            CreateChild<Image>(root.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero,
                img => img.color = new Color(0.05f, 0.05f, 0.05f, 0.75f));

            // Fill radial (countdown visuel)
            Image fill = null;
            CreateChild<Image>(root.transform, "Fill", Vector2.zero, Vector2.one, Vector2.zero, img =>
            {
                img.color         = new Color(1f, 1f, 1f, 0.2f);
                img.type          = Image.Type.Filled;
                img.fillMethod    = Image.FillMethod.Radial360;
                img.fillOrigin    = (int)Image.Origin360.Top;
                img.fillClockwise = false;
                img.fillAmount    = 1f;
                fill = img;
            });

            // Icône de l'effet (sprite)
            CreateChild<Image>(root.transform, "Icon",
                new Vector2(0.12f, 0.3f), new Vector2(0.88f, 0.92f), Vector2.zero, img =>
            {
                img.sprite         = effect.Icon;
                img.preserveAspect = true;
                img.color          = effect.Icon != null ? Color.white : new Color(1, 1, 1, 0.3f);
            });

            // Texte du timer (bas)
            TextMeshProUGUI timer = null;
            CreateChild<TextMeshProUGUI>(root.transform, "Timer",
                new Vector2(0f, 0f), new Vector2(1f, 0.32f), Vector2.zero, tmp =>
            {
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize  = sz * 0.22f;
                tmp.color     = Color.white;
                tmp.text      = $"{Mathf.CeilToInt(effect.TimeRemaining)}s";
                timer         = tmp;
            });

            return new EffectIconEntry
            {
                Effect       = effect,
                Root         = root,
                CooldownFill = fill,
                TimerText    = timer,
            };
        }

        private void UpdateEffectTimers()
        {
            foreach (var entry in _effectIcons)
            {
                if (entry.Effect == null) continue;
                if (entry.CooldownFill != null) entry.CooldownFill.fillAmount = entry.Effect.Progress;
                if (entry.TimerText    != null) entry.TimerText.text = $"{Mathf.CeilToInt(entry.Effect.TimeRemaining)}s";
            }
        }

        // ── Utilitaire de création UI ─────────────────────────────────────────────

        private static T CreateChild<T>(Transform parent, string n,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta,
            System.Action<T> setup = null) where T : Component
        {
            var go    = new GameObject(n);
            go.transform.SetParent(parent, false);
            var rt    = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            var comp  = go.AddComponent<T>();
            setup?.Invoke(comp);
            return comp;
        }
    }
}
