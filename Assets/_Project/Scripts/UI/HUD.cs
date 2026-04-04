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

        [Header("Ressources joueur")]
        [SerializeField] private TextMeshProUGUI   resourceText;   // optionnel — affiche les pts perso

        [Header("Références joueur")]
        [SerializeField] private PlayerState       playerState;
        [SerializeField] private InventorySystem   inventorySystem;
        [SerializeField] private PlayerInteraction playerInteraction;

        private Image[] _slotBackgrounds;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);
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

            // Nouvelle API : OnInteractionPrompt porte déjà le texte complet
            if (playerInteraction != null)
                playerInteraction.OnInteractionPrompt += UpdateInteractionPrompt;
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
        }

        private void Update()
        {
            UpdateSelectedSlotHighlight();
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
            if (interactionPrompt == null) return;

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

        private void UpdateResourceText(int personalResources)
        {
            if (resourceText != null)
                resourceText.text = $"Ressources : {personalResources}";
        }
    }
}
