using System;
using UnityEngine;
using ProjectFPS.Inventory;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Gère toutes les interactions du joueur avec le monde :
    ///
    ///   [E]  Ramasser un item utilitaire ou une ressource ;
    ///        Dans une RitualZone : déposer ses ressources.
    ///   [G]  Poser l'item sélectionné au sol (drop contrôlé, sans effet).
    ///   [Q]  Lancer l'item sélectionné (uniquement si CanThrow).
    ///   [F]  Utiliser l'item sélectionné (uniquement si CanUse).
    ///
    /// Dépendances : InventorySystem (sur ce GameObject ou sur le parent).
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private float     interactionRange  = 3f;
        [SerializeField] private LayerMask interactionLayer  = ~0;

        [Header("Lancer")]
        [Tooltip("Angle vers le haut (en degrés) ajouté à la direction de regard pour le lancer.")]
        [SerializeField] private float throwUpwardAngle = 10f;

        [Header("Références")]
        [SerializeField] private Camera    playerCamera;
        [Tooltip("Point d'apparition du prefab lors d'un drop ou d'un lancer (optionnel). " +
                 "Si non assigné, la position du joueur est utilisée.")]
        [SerializeField] private Transform itemSpawnPoint;

        // ── Références internes ───────────────────────────────────────────────────
        private InventorySystem _inventory;

        // Objet/zone actuellement visé par le raycast
        private ItemWorldObject _lookedAtItem;
        private RitualZone      _lookedAtRitual;

        // ── Événements ────────────────────────────────────────────────────────────

        /// <summary>
        /// Déclenché chaque frame : texte du prompt d'interaction à afficher,
        /// ou null pour masquer le prompt.
        /// </summary>
        public event Action<string> OnInteractionPrompt;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _inventory = GetComponent<InventorySystem>()
                      ?? GetComponentInParent<InventorySystem>();

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            HandleRaycast();
            HandleInputActions();
        }

        // ── Détection (raycast) ───────────────────────────────────────────────────
        private void HandleRaycast()
        {
            _lookedAtItem   = null;
            _lookedAtRitual = null;

            if (playerCamera == null)
            {
                OnInteractionPrompt?.Invoke(null);
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
            {
                // ── Item monde ────────────────────────────────────────────────────
                var worldItem = hit.collider.GetComponent<ItemWorldObject>()
                             ?? hit.collider.GetComponentInParent<ItemWorldObject>();

                if (worldItem != null && worldItem.CanBePickedUp && worldItem.Data != null)
                {
                    _lookedAtItem = worldItem;
                    bool isResource = worldItem.Data.IsResource;
                    string prompt = isResource
                        ? $"[E] Collecter {worldItem.Data.ItemName} (+{worldItem.Data.ResourceValue} pts)"
                        : $"[E] Ramasser {worldItem.Data.ItemName}";
                    OnInteractionPrompt?.Invoke(prompt);
                    return;
                }

                // ── Zone de rituel ────────────────────────────────────────────────
                var ritual = hit.collider.GetComponent<RitualZone>()
                          ?? hit.collider.GetComponentInParent<RitualZone>();

                if (ritual != null)
                {
                    _lookedAtRitual = ritual;
                    OnInteractionPrompt?.Invoke(ritual.GetPromptText(_inventory));
                    return;
                }
            }

            OnInteractionPrompt?.Invoke(null);
        }

        // ── Actions clavier ───────────────────────────────────────────────────────
        private void HandleInputActions()
        {
            if (Input.GetKeyDown(KeyCode.E))   TryPickupOrDeposit();
            if (Input.GetKeyDown(KeyCode.G))   TryDrop();
            if (Input.GetKeyDown(KeyCode.Q))   TryThrow();
            if (Input.GetKeyDown(KeyCode.F))   TryUse();
        }

        // ── [E] Ramasser / Déposer ────────────────────────────────────────────────
        private void TryPickupOrDeposit()
        {
            // Priorité 1 : zone de rituel
            if (_lookedAtRitual != null)
            {
                // Le dépôt est géré par RitualZone.Update() (joueur dans le trigger + E)
                // Mais si on regarde directement la zone depuis l'extérieur, on peut aussi
                // déclencher via raycast :
                if (_inventory != null)
                {
                    int deposited = _inventory.DepositResources();
                    if (deposited > 0 && ResourceSystem.Instance != null)
                        ResourceSystem.Instance.Deposit(deposited);
                }
                return;
            }

            // Priorité 2 : item monde
            if (_lookedAtItem == null || !_lookedAtItem.CanBePickedUp) return;

            var data = _lookedAtItem.Data;
            if (data == null || _inventory == null) return;

            if (data.IsResource)
            {
                // Ressource → points directs, pas de slot
                _inventory.AddResource(data.ResourceValue);
                _lookedAtItem.OnPickedUp();
                Debug.Log($"[PlayerInteraction] Ressource collectée : +{data.ResourceValue} pts");
            }
            else
            {
                // Item utilitaire
                if (_inventory.TryPickupUtility(data))
                {
                    _lookedAtItem.OnPickedUp();
                    Debug.Log($"[PlayerInteraction] Ramassé : {data.ItemName}");
                }
                else
                {
                    Debug.Log("[PlayerInteraction] Inventaire plein, impossible de ramasser.");
                }
            }
        }

        // ── [G] Poser ─────────────────────────────────────────────────────────────
        private void TryDrop()
        {
            if (_inventory == null) return;

            var item = _inventory.RemoveFromSlot(_inventory.SelectedSlot);
            if (item == null) return;

            SpawnWorldItem(item, ItemWorldObject.WorldItemState.Dropped);
            Debug.Log($"[PlayerInteraction] Posé : {item.ItemName}");
        }

        // ── [Q] Lancer ────────────────────────────────────────────────────────────
        private void TryThrow()
        {
            if (_inventory == null || playerCamera == null) return;

            var item = _inventory.GetSelectedItem();
            if (item == null)
            {
                Debug.Log("[PlayerInteraction] Aucun item sélectionné.");
                return;
            }
            if (!item.CanThrow)
            {
                Debug.Log($"[PlayerInteraction] {item.ItemName} ne peut pas être lancé.");
                return;
            }

            _inventory.RemoveFromSlot(_inventory.SelectedSlot);

            // Direction = regard caméra + angle vers le haut
            Vector3 throwDir = Quaternion.AngleAxis(-throwUpwardAngle, playerCamera.transform.right)
                             * playerCamera.transform.forward;

            var worldObj = SpawnWorldItem(item, ItemWorldObject.WorldItemState.Thrown);
            if (worldObj != null)
                worldObj.ApplyThrowVelocity(throwDir * item.ThrowForce);

            Debug.Log($"[PlayerInteraction] Lancé : {item.ItemName}");
        }

        // ── [F] Utiliser ──────────────────────────────────────────────────────────
        private void TryUse()
        {
            if (_inventory == null) return;

            var item = _inventory.UseSelectedItem();
            if (item == null)
            {
                Debug.Log("[PlayerInteraction] Aucun item utilisable sélectionné.");
                return;
            }

            ApplyUseEffect(item);
            Debug.Log($"[PlayerInteraction] Utilisé : {item.ItemName}" +
                      (item.Consumes ? " (consommé)" : ""));
        }

        // ── Effets d'utilisation directe ──────────────────────────────────────────
        private void ApplyUseEffect(ItemData item)
        {
            switch (item.Type)
            {
                case ItemType.Potion:
                    // TODO: soigner le joueur (GetComponent<PlayerState>().Heal(...))
                    Debug.Log("[PlayerInteraction] Potion utilisée → soin appliqué.");
                    break;

                case ItemType.Balle:
                    // TODO: tirer une balle (appeler un système de tir)
                    Debug.Log("[PlayerInteraction] Balle utilisée → tir effectué.");
                    break;

                case ItemType.Armure:
                    // TODO: activer bouclier (PlayerState.ActivateArmor())
                    Debug.Log("[PlayerInteraction] Armure activée → absorbe la prochaine attaque.");
                    break;

                case ItemType.MalusEnnemi:
                    // TODO: appliquer debuff à la cible visée
                    Debug.Log("[PlayerInteraction] Malus ennemi appliqué à la cible.");
                    break;
            }
        }

        // ── Utilitaire : spawn d'un objet monde ───────────────────────────────────

        /// <summary>
        /// Instancie le WorldPrefab de l'item et l'initialise dans l'état donné.
        /// Le spawn se fait au point itemSpawnPoint ou à la position du joueur + 0.5 m devant.
        /// </summary>
        private ItemWorldObject SpawnWorldItem(ItemData item, ItemWorldObject.WorldItemState state)
        {
            if (item.WorldPrefab == null)
            {
                Debug.LogWarning($"[PlayerInteraction] {item.ItemName} n'a pas de WorldPrefab assigné.");
                return null;
            }

            Vector3    spawnPos = GetSpawnPosition();
            Quaternion spawnRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            var go  = UnityEngine.Object.Instantiate(item.WorldPrefab, spawnPos, spawnRot);
            var wio = go.GetComponent<ItemWorldObject>() ?? go.AddComponent<ItemWorldObject>();
            wio.Initialize(item, state);
            return wio;
        }

        private Vector3 GetSpawnPosition()
        {
            if (itemSpawnPoint != null)
                return itemSpawnPoint.position;

            // Spawn devant le joueur, à hauteur de la caméra
            return playerCamera != null
                ? playerCamera.transform.position + playerCamera.transform.forward * 0.6f
                : transform.position + transform.forward * 0.6f;
        }
    }
}
