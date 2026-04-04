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
        // Couleurs de surbrillance du slot actif
        [SerializeField] private Color             selectedSlotColor   = Color.yellow;
        [SerializeField] private Color             deselectedSlotColor = Color.white;

        [Header("Prompt d'interaction")]
        [SerializeField] private TextMeshProUGUI   interactionPrompt;

        [Header("Références joueur")]
        [SerializeField] private PlayerState       playerState;
        [SerializeField] private InventorySystem   inventorySystem;
        [SerializeField] private PlayerInteraction playerInteraction;

        // Images de fond des slots (index = numéro de slot)
        private Image[] _slotBackgrounds;

        // ─── Lifecycle ──────────────────────────────────────────────────────────

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
                inventorySystem.OnInventoryChanged += UpdateInventorySlots;

            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged += UpdateRoleDisplay;

            if (playerInteraction != null)
                playerInteraction.OnLookAtPickup += UpdateInteractionPrompt;
        }

        private void OnDisable()
        {
            if (playerState != null)
                playerState.OnHealthChanged -= UpdateHealthBar;

            if (inventorySystem != null)
                inventorySystem.OnInventoryChanged -= UpdateInventorySlots;

            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged -= UpdateRoleDisplay;

            if (playerInteraction != null)
                playerInteraction.OnLookAtPickup -= UpdateInteractionPrompt;
        }

        private void Start()
        {
            InitializeSlots();

            // Peuplement initial depuis les états courants
            if (playerState != null)
                UpdateHealthBar(playerState.CurrentHealth, playerState.MaxHealth);

            if (RoleManager.Instance?.CurrentRole != null)
                UpdateRoleDisplay(RoleManager.Instance.CurrentRole);

            UpdateInventorySlots();
        }

        private void Update()
        {
            UpdateSelectedSlotHighlight();
        }

        // ─── Initialisation des slots ────────────────────────────────────────────

        // Crée les slots visuels selon le nombre de slots max de l'inventaire
        private void InitializeSlots()
        {
            if (slotsContainer == null || slotPrefab == null) return;

            foreach (Transform child in slotsContainer)
                Destroy(child.gameObject);

            int count = inventorySystem != null ? inventorySystem.MaxSlots : 4;
            _slotBackgrounds = new Image[count];

            for (int i = 0; i < count; i++)
            {
                GameObject slot = Instantiate(slotPrefab, slotsContainer);
                // Le prefab doit avoir une Image racine (fond du slot) et un enfant "ItemIcon" (Image)
                _slotBackgrounds[i] = slot.GetComponent<Image>();
            }
        }

        // ─── Callbacks des events ────────────────────────────────────────────────

        // Met à jour le slider et le texte de vie
        private void UpdateHealthBar(float current, float max)
        {
            if (healthBar != null)
                healthBar.value = max > 0f ? current / max : 0f;

            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        // Met à jour le nom et l'icône du rôle affiché
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
        }

        // Rafraîchit les icônes des slots d'inventaire
        private void UpdateInventorySlots()
        {
            if (_slotBackgrounds == null || inventorySystem == null) return;

            for (int i = 0; i < _slotBackgrounds.Length; i++)
            {
                if (_slotBackgrounds[i] == null) continue;

                // L'icône se trouve dans l'enfant "ItemIcon"
                Transform iconTransform = _slotBackgrounds[i].transform.Find("ItemIcon");
                if (iconTransform == null) continue;

                Image  iconImage = iconTransform.GetComponent<Image>();
                if (iconImage == null) continue;

                ItemData item    = inventorySystem.GetItem(i);
                iconImage.sprite = item?.Icon;
                iconImage.enabled = item != null;
            }
        }

        // Surligne le fond du slot actif chaque frame
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

        // Affiche le prompt "[E] Ramasser <nom>" ou le cache
        private void UpdateInteractionPrompt(string itemName)
        {
            if (interactionPrompt == null) return;

            if (itemName != null)
            {
                interactionPrompt.text = $"[E] Ramasser {itemName}";
                interactionPrompt.gameObject.SetActive(true);
            }
            else
            {
                interactionPrompt.gameObject.SetActive(false);
            }
        }
    }
}
