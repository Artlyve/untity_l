using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectFPS.Player;
using ProjectFPS.Roles;
using ProjectFPS.Inventory;

namespace ProjectFPS.UI
{
    public class HUD : MonoBehaviour
    {
        [Header("Barre de vie")]
        [SerializeField] private Slider            healthBar;
        [SerializeField] private TextMeshProUGUI   healthText;

        [Header("Rôle actif")]
        [SerializeField] private TextMeshProUGUI   roleNameText;
        [SerializeField] private Image             roleIcon;

        [Header("Slots d'inventaire")]
        [SerializeField] private Transform         slotsContainer;
        [SerializeField] private GameObject        slotPrefab;
        [SerializeField] private Color             selectedSlotColor   = Color.yellow;
        [SerializeField] private Color             deselectedSlotColor = Color.white;

        [Header("Prompt d'interaction")]
        [SerializeField] private TextMeshProUGUI   interactionPrompt;

        [Header("Réticule")]
        [Tooltip("Image centrale du réticule. Laissez vide pour créer un réticule automatique.")]
        [SerializeField] private Image             reticle;
        [Tooltip("Couleur du réticule par défaut.")]
        [SerializeField] private Color             reticleColor        = Color.white;
        [Tooltip("Couleur quand un objet interactif est visé.")]
        [SerializeField] private Color             reticleHighlight    = new Color(1f, 0.85f, 0f);

        [Header("Ressources joueur")]
        [SerializeField] private TextMeshProUGUI   resourceText;

        [Header("Effets actifs")]
        [Tooltip("Conteneur des icônes d'effets (HorizontalLayoutGroup recommandé). Laissez vide pour créer automatiquement.")]
        [SerializeField] private Transform         effectsContainer;
        [Tooltip("Largeur/hauteur en pixels de chaque icône d'effet.")]
        [SerializeField] private float             effectIconSize      = 48f;

        [Header("Références joueur")]
        [SerializeField] private PlayerState       playerState;
        [SerializeField] private InventorySystem   inventorySystem;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private EffectSystem      effectSystem;

        // ── Internes ──────────────────────────────────────────────────────────────
        private Image[] _slotBackgrounds;
        private bool    _hasInteractable;

        // Pool d'icônes d'effets : associe ActiveEffect → entrée UI
        private readonly List<EffectIconEntry> _effectIcons = new List<EffectIconEntry>();

        private struct EffectIconEntry
        {
            public ActiveEffect Effect;
            public GameObject   Root;
            public Image        IconImage;
            public Image        CooldownFill;
            public TextMeshProUGUI TimerText;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);

            EnsureReticle();
            EnsureEffectsContainer();

            // Auto-résolution du EffectSystem si non assigné
            if (effectSystem == null)
                effectSystem = FindFirstObjectByType<EffectSystem>();
        }

        private void OnEnable()
        {
            if (playerState != null)
                playerState.OnHealthChanged += UpdateHealthBar;

            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged += UpdateInventorySlots;
                inventorySystem.OnResourceChanged  += UpdateResourceText;
            }

            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged += UpdateRoleDisplay;

            if (playerInteraction != null)
                playerInteraction.OnInteractionPrompt += UpdateInteractionPrompt;

            if (effectSystem != null)
                effectSystem.OnEffectsChanged += RefreshEffectIcons;
        }

        private void OnDisable()
        {
            if (playerState != null)
                playerState.OnHealthChanged -= UpdateHealthBar;

            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged -= UpdateInventorySlots;
                inventorySystem.OnResourceChanged  -= UpdateResourceText;
            }

            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged -= UpdateRoleDisplay;

            if (playerInteraction != null)
                playerInteraction.OnInteractionPrompt -= UpdateInteractionPrompt;

            if (effectSystem != null)
                effectSystem.OnEffectsChanged -= RefreshEffectIcons;
        }

        private void Start()
        {
            InitializeSlots();

            if (playerState != null)
                UpdateHealthBar(playerState.CurrentHealth, playerState.MaxHealth);

            if (RoleManager.Instance?.CurrentRole != null)
                UpdateRoleDisplay(RoleManager.Instance.CurrentRole);

            UpdateInventorySlots();
            UpdateResourceText(inventorySystem != null ? inventorySystem.PersonalResources : 0);
            RefreshEffectIcons();
        }

        private void Update()
        {
            UpdateSelectedSlotHighlight();
            UpdateEffectTimers();
        }

        // ── Slots ─────────────────────────────────────────────────────────────────
        private void InitializeSlots()
        {
            if (slotsContainer == null || slotPrefab == null) return;

            foreach (Transform child in slotsContainer)
                Destroy(child.gameObject);

            int count       = inventorySystem != null ? inventorySystem.MaxSlots : 1;
            _slotBackgrounds = new Image[count];

            for (int i = 0; i < count; i++)
            {
                var slot = Instantiate(slotPrefab, slotsContainer);
                // Prefab attendu : Image racine (fond) + enfant "ItemIcon" (Image)
                _slotBackgrounds[i] = slot.GetComponent<Image>();
            }
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

            // Recréer les slots si le nombre change avec le rôle
            InitializeSlots();
        }

        private void UpdateInventorySlots()
        {
            if (_slotBackgrounds == null || inventorySystem == null) return;

            // Si le nombre de slots a changé (changement de rôle), recréer
            if (_slotBackgrounds.Length != inventorySystem.MaxSlots)
            {
                InitializeSlots();
                return;
            }

            for (int i = 0; i < _slotBackgrounds.Length; i++)
            {
                if (_slotBackgrounds[i] == null) continue;

                Transform iconTransform = _slotBackgrounds[i].transform.Find("ItemIcon");
                if (iconTransform == null) continue;

                Image    iconImage = iconTransform.GetComponent<Image>();
                if (iconImage == null) continue;

                ItemData item      = inventorySystem.GetItem(i);
                iconImage.sprite   = item?.Icon;
                iconImage.enabled  = item != null;
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

        /// <summary>
        /// Reçoit le texte de prompt complet depuis PlayerInteraction.OnInteractionPrompt.
        /// null = masquer, string = afficher tel quel.
        /// </summary>
        private void UpdateInteractionPrompt(string promptText)
        {
            _hasInteractable = promptText != null;

            if (interactionPrompt != null)
            {
                if (promptText != null)
                {
                    interactionPrompt.text = promptText;
                    interactionPrompt.gameObject.SetActive(true);
                }
                else
                {
                    interactionPrompt.gameObject.SetActive(false);
                }
            }

            // Réticule : change de couleur si un objet interactif est visé
            if (reticle != null)
                reticle.color = _hasInteractable ? reticleHighlight : reticleColor;
        }

        // ── Réticule ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Crée un réticule minimaliste (croix de 4 traits) si aucun n'est assigné.
        /// </summary>
        private void EnsureReticle()
        {
            if (reticle != null) return;

            // Conteneur centré
            var go = new GameObject("Reticle");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;

            // Point central (carré 4×4 px)
            reticle = CreateReticleElement(go.transform, "Dot", new Vector2(4, 4), Vector2.zero);

            // Quatre traits (haut, bas, gauche, droite)
            CreateReticleElement(go.transform, "Top",    new Vector2(2, 8),  new Vector2(0,  10));
            CreateReticleElement(go.transform, "Bottom", new Vector2(2, 8),  new Vector2(0, -10));
            CreateReticleElement(go.transform, "Left",   new Vector2(8, 2),  new Vector2(-10,  0));
            CreateReticleElement(go.transform, "Right",  new Vector2(8, 2),  new Vector2( 10,  0));
        }

        private Image CreateReticleElement(Transform parent, string name, Vector2 size, Vector2 offset)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = size;
            rt.anchoredPosition = offset;
            var img = go.AddComponent<Image>();
            img.color = reticleColor;
            return img;
        }

        private void UpdateResourceText(int personalResources)
        {
            if (resourceText != null)
                resourceText.text = $"Ressources : {personalResources}";
        }

        // ── Effets actifs ─────────────────────────────────────────────────────────

        /// <summary>
        /// Garantit l'existence d'un conteneur d'effets (HLG centré en bas-gauche du HUD).
        /// </summary>
        private void EnsureEffectsContainer()
        {
            if (effectsContainer != null) return;

            var go = new GameObject("EffectsContainer");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 0f);
            rt.pivot            = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(10f, 80f);
            rt.sizeDelta        = new Vector2(300f, effectIconSize + 4f);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing            = 4f;
            hlg.childAlignment     = TextAnchor.LowerLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            effectsContainer = go.transform;
        }

        /// <summary>
        /// Reconstruit les icônes d'effets depuis la liste active de l'EffectSystem.
        /// Appelé à chaque OnEffectsChanged.
        /// </summary>
        private void RefreshEffectIcons()
        {
            if (effectsContainer == null) return;

            var activeEffects = effectSystem != null
                ? effectSystem.ActiveEffects
                : (IReadOnlyList<ActiveEffect>)System.Array.Empty<ActiveEffect>();

            // Supprimer les icônes des effets qui ont expiré
            for (int i = _effectIcons.Count - 1; i >= 0; i--)
            {
                bool stillActive = false;
                foreach (var e in activeEffects)
                    if (e == _effectIcons[i].Effect) { stillActive = true; break; }

                if (!stillActive)
                {
                    if (_effectIcons[i].Root != null)
                        Destroy(_effectIcons[i].Root);
                    _effectIcons.RemoveAt(i);
                }
            }

            // Ajouter les icônes des nouveaux effets
            foreach (var effect in activeEffects)
            {
                bool exists = false;
                foreach (var entry in _effectIcons)
                    if (entry.Effect == effect) { exists = true; break; }

                if (!exists)
                    _effectIcons.Add(BuildEffectIcon(effect));
            }
        }

        /// <summary>
        /// Construit une icône d'effet : fond semi-transparent + icône sprite + timer texte.
        /// </summary>
        private EffectIconEntry BuildEffectIcon(ActiveEffect effect)
        {
            float sz = effectIconSize;

            // Conteneur racine
            var root = new GameObject($"Effect_{effect.DisplayName}");
            root.transform.SetParent(effectsContainer, false);
            var rootRT       = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(sz, sz);

            // Fond (noir semi-transparent)
            var bg = new GameObject("BG");
            bg.transform.SetParent(root.transform, false);
            var bgRT         = bg.AddComponent<RectTransform>();
            bgRT.anchorMin   = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta   = Vector2.zero;
            var bgImg        = bg.AddComponent<Image>();
            bgImg.color      = new Color(0f, 0f, 0f, 0.6f);

            // Remplissage cooldown (radial ou simple, selon disponibilité)
            var fill = new GameObject("CooldownFill");
            fill.transform.SetParent(root.transform, false);
            var fillRT       = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;
            var fillImg      = fill.AddComponent<Image>();
            fillImg.color    = new Color(1f, 1f, 1f, 0.25f);
            fillImg.type     = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Radial360;
            fillImg.fillOrigin = (int)Image.Origin360.Top;
            fillImg.fillClockwise = false;
            fillImg.fillAmount = 1f;

            // Icône de l'effet
            var icon = new GameObject("Icon");
            icon.transform.SetParent(root.transform, false);
            var iconRT       = icon.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.3f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.sizeDelta = Vector2.zero;
            var iconImg      = icon.AddComponent<Image>();
            iconImg.sprite   = effect.Icon;
            iconImg.preserveAspect = true;
            if (effect.Icon == null) iconImg.color = new Color(1f, 1f, 1f, 0.4f);

            // Timer texte (bas de l'icône)
            var timerGO = new GameObject("Timer");
            timerGO.transform.SetParent(root.transform, false);
            var timerRT        = timerGO.AddComponent<RectTransform>();
            timerRT.anchorMin  = new Vector2(0f, 0f);
            timerRT.anchorMax  = new Vector2(1f, 0.35f);
            timerRT.sizeDelta  = Vector2.zero;
            var timerTMP       = timerGO.AddComponent<TextMeshProUGUI>();
            timerTMP.alignment = TextAlignmentOptions.Center;
            timerTMP.fontSize  = sz * 0.22f;
            timerTMP.color     = Color.white;
            timerTMP.text      = $"{Mathf.CeilToInt(effect.TimeRemaining)}s";

            return new EffectIconEntry
            {
                Effect       = effect,
                Root         = root,
                IconImage    = iconImg,
                CooldownFill = fillImg,
                TimerText    = timerTMP,
            };
        }

        /// <summary>
        /// Mise à jour en temps réel des timers et du remplissage radial.
        /// Appelé dans Update().
        /// </summary>
        private void UpdateEffectTimers()
        {
            foreach (var entry in _effectIcons)
            {
                if (entry.Effect == null) continue;

                if (entry.CooldownFill != null)
                    entry.CooldownFill.fillAmount = entry.Effect.Progress;

                if (entry.TimerText != null)
                    entry.TimerText.text = $"{Mathf.CeilToInt(entry.Effect.TimeRemaining)}s";
            }
        }
    }
}
