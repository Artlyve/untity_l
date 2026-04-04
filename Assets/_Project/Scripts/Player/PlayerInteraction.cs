using System;
using UnityEngine;
using ProjectFPS.Inventory;

namespace ProjectFPS.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionLayer = ~0;

        [Header("Références")]
        [SerializeField] private Camera playerCamera;

        private InventorySystem _inventorySystem;

        // Déclenché chaque frame : nom de l'item visé, ou null si rien
        public event Action<string> OnLookAtPickup;

        private void Awake()
        {
            // Cherche InventorySystem sur ce GameObject ou sur le parent (Player)
            _inventorySystem = GetComponent<InventorySystem>()
                            ?? GetComponentInParent<InventorySystem>();

            // Cherche la caméra sur ce GameObject ou dans les enfants si non assignée
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            HandleInteractionRaycast();
        }

        // Lance un raycast depuis la caméra et gère l'interaction avec les ItemPickup
        private void HandleInteractionRaycast()
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
            {
                ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    // Notifie l'UI du nom de l'item visé
                    OnLookAtPickup?.Invoke(pickup.ItemName);

                    // Ramassage sur pression de E
                    if (Input.GetKeyDown(KeyCode.E) && _inventorySystem != null)
                    {
                        if (_inventorySystem.TryAddItem(pickup.Data))
                            pickup.Pickup();
                    }
                    return;
                }
            }

            // Aucun item visé : notifie l'UI pour cacher le prompt
            OnLookAtPickup?.Invoke(null);
        }
    }
}
