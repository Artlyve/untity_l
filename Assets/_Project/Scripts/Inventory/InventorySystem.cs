using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectFPS.Roles;

namespace ProjectFPS.Inventory
{
    public class InventorySystem : MonoBehaviour
    {
        [Header("Inventaire")]
        [SerializeField] private int defaultMaxSlots = 4;

        private readonly List<ItemData> _items = new List<ItemData>();
        private int _maxSlots;
        private int _selectedSlot;

        // Accesseurs publics
        public int                    SelectedSlot => _selectedSlot;
        public int                    MaxSlots     => _maxSlots;
        public IReadOnlyList<ItemData> Items        => _items;

        // Événements
        public event Action            OnInventoryChanged;
        public event Action<ItemData>  OnItemAdded;
        public event Action<ItemData>  OnItemRemoved;

        private void Awake()
        {
            _maxSlots = defaultMaxSlots;
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
            HandleSlotSelection();
        }

        // Adapte le nombre de slots au nouveau rôle, retire les items en trop
        private void HandleRoleChanged(RoleData role)
        {
            _maxSlots = role != null ? role.InventorySlots : defaultMaxSlots;

            while (_items.Count > _maxSlots)
                _items.RemoveAt(_items.Count - 1);

            _selectedSlot = Mathf.Clamp(_selectedSlot, 0, Mathf.Max(0, _maxSlots - 1));
            OnInventoryChanged?.Invoke();
        }

        // Navigation des slots : molette souris ou touches 1-4
        private void HandleSlotSelection()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
                _selectedSlot = (_selectedSlot - 1 + _maxSlots) % _maxSlots;
            else if (scroll < 0f)
                _selectedSlot = (_selectedSlot + 1) % _maxSlots;

            for (int i = 0; i < Mathf.Min(4, _maxSlots); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    _selectedSlot = i;
                    break;
                }
            }
        }

        // Tente d'ajouter un item. Retourne true si l'ajout a réussi.
        public bool TryAddItem(ItemData item)
        {
            if (item == null) return false;

            // Empilement : cherche un slot existant du même item s'il est stackable
            if (item.IsStackable)
            {
                // Logique de stack à étendre selon les besoins du projet
                // Pour l'instant on ajoute toujours une entrée distincte
            }

            if (_items.Count >= _maxSlots) return false;

            _items.Add(item);
            OnItemAdded?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }

        // Retire un item par son index de slot
        public void RemoveItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _items.Count) return;

            ItemData removed = _items[slotIndex];
            _items.RemoveAt(slotIndex);
            OnItemRemoved?.Invoke(removed);
            OnInventoryChanged?.Invoke();
        }

        // Retourne l'item à l'index donné, ou null si vide
        public ItemData GetItem(int index)
        {
            if (index < 0 || index >= _items.Count) return null;
            return _items[index];
        }
    }
}
