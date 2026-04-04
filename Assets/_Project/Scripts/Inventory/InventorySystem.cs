using System;
using UnityEngine;
using ProjectFPS.Roles;

namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Gère l'inventaire utilitaire du joueur et ses ressources personnelles.
    ///
    /// Slots utilitaires :
    ///   • 1 slot par défaut (tous les rôles sauf Fils_Chasseur).
    ///   • 2 slots si RoleData.InventorySlots ≥ 2 (Fils_Chasseur).
    ///   • 1 objet unique par slot, pas de stack.
    ///
    /// Ressources :
    ///   • Compteur de points personnels (PersonalResources).
    ///   • Transféré au ResourceSystem via DepositResources() en zone de rituel.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Slots utilitaires par défaut (1 pour tous les rôles non spéciaux).")]
        [SerializeField] private int defaultUtilitySlots = 1;

        // ── État interne ──────────────────────────────────────────────────────────
        private ItemData[] _slots;          // tableau des slots utilitaires (null = vide)
        private int        _selectedSlot;
        private int        _maxSlots;
        private int        _personalResources;

        // ── Accesseurs ────────────────────────────────────────────────────────────
        public int   SelectedSlot      => _selectedSlot;
        public int   MaxSlots          => _maxSlots;
        public int   PersonalResources => _personalResources;

        // ── Événements ────────────────────────────────────────────────────────────
        public event Action           OnInventoryChanged;
        public event Action<int>      OnResourceChanged;  // paramètre : points personnels actuels
        public event Action<ItemData> OnItemUsed;         // déclenché juste avant consommation

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _maxSlots = Mathf.Max(1, defaultUtilitySlots);
            _slots    = new ItemData[_maxSlots];
        }

        private void OnEnable()
        {
            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged += HandleRoleChanged;
        }

        private void OnDisable()
        {
            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged -= HandleRoleChanged;
        }

        private void Update()
        {
            if (_maxSlots > 1)
                HandleSlotSelection();
        }

        // ── Gestion du rôle ───────────────────────────────────────────────────────
        private void HandleRoleChanged(RoleData role)
        {
            int newMax = role != null ? Mathf.Max(1, role.InventorySlots) : defaultUtilitySlots;

            if (newMax == _maxSlots) return;

            var old   = _slots;
            _maxSlots = newMax;
            _slots    = new ItemData[_maxSlots];

            for (int i = 0; i < Mathf.Min(old.Length, _maxSlots); i++)
                _slots[i] = old[i];

            _selectedSlot = Mathf.Clamp(_selectedSlot, 0, _maxSlots - 1);
            OnInventoryChanged?.Invoke();
        }

        // ── Sélection de slot ─────────────────────────────────────────────────────
        private void HandleSlotSelection()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)      _selectedSlot = (_selectedSlot - 1 + _maxSlots) % _maxSlots;
            else if (scroll < 0f) _selectedSlot = (_selectedSlot + 1) % _maxSlots;

            for (int i = 0; i < _maxSlots; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    _selectedSlot = i;
                    break;
                }
            }
        }

        // ── Actions sur les slots utilitaires ─────────────────────────────────────

        /// <summary>
        /// Tente d'ajouter un item utilitaire dans le premier slot libre.
        /// Retourne true si l'ajout a réussi, false si inventaire plein.
        /// </summary>
        public bool TryPickupUtility(ItemData item)
        {
            if (item == null || item.IsResource) return false;

            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] != null) continue;
                _slots[i]     = item;
                _selectedSlot = i;
                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;  // inventaire plein
        }

        /// <summary>
        /// Retire l'item d'un slot et le retourne (pour drop ou lancer).
        /// Retourne null si le slot est vide.
        /// </summary>
        public ItemData RemoveFromSlot(int slot)
        {
            if (slot < 0 || slot >= _maxSlots || _slots[slot] == null) return null;

            var item    = _slots[slot];
            _slots[slot] = null;
            OnInventoryChanged?.Invoke();
            return item;
        }

        /// <summary>
        /// Utilise l'item du slot sélectionné (si CanUse).
        /// Si l'item se consomme (Consumes), il est retiré du slot.
        /// Retourne l'item utilisé, ou null si aucune action.
        /// </summary>
        public ItemData UseSelectedItem()
        {
            var item = _slots[_selectedSlot];
            if (item == null || !item.CanUse) return null;

            OnItemUsed?.Invoke(item);

            if (item.Consumes)
            {
                _slots[_selectedSlot] = null;
                OnInventoryChanged?.Invoke();
            }
            return item;
        }

        // ── Ressources personnelles ───────────────────────────────────────────────

        /// <summary>Ajoute des points de ressource à l'inventaire personnel.</summary>
        public void AddResource(int points)
        {
            if (points <= 0) return;
            _personalResources += points;
            OnResourceChanged?.Invoke(_personalResources);
        }

        /// <summary>
        /// Transfère des points vers le pool global.
        /// amount = -1 → transfère tout.
        /// Retourne le nombre de points effectivement transférés.
        /// </summary>
        public int DepositResources(int amount = -1)
        {
            if (_personalResources <= 0) return 0;

            int toDeposit      = amount < 0
                ? _personalResources
                : Mathf.Min(amount, _personalResources);

            _personalResources -= toDeposit;
            OnResourceChanged?.Invoke(_personalResources);
            return toDeposit;
        }

        // ── Accesseurs de compatibilité (utilisés par HUD et DebugPanel) ──────────

        /// <summary>Retourne l'item dans le slot donné, ou null si vide.</summary>
        public ItemData GetItem(int index)
            => (index >= 0 && index < _maxSlots) ? _slots[index] : null;

        /// <summary>Retourne l'item dans le slot sélectionné, ou null si vide.</summary>
        public ItemData GetSelectedItem()
            => _slots[_selectedSlot];

        /// <summary>Nombre de slots occupés (pour le DebugPanel).</summary>
        public int OccupiedSlotCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _maxSlots; i++)
                    if (_slots[i] != null) count++;
                return count;
            }
        }
    }
}
