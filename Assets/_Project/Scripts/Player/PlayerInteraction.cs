using UnityEngine;
using System;
using ProjectFPS.Inventory;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Gère toutes les interactions du joueur avec le monde.
    /// Activer debugMode dans l'Inspector pour afficher les logs détaillés et le rayon.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private float     interactionRange  = 3f;
        [SerializeField] private LayerMask interactionLayer  = ~0;

        [Header("Lancer")]
        [SerializeField] private float throwUpwardAngle = 10f;

        [Header("Références")]
        [SerializeField] private Camera    playerCamera;
        [SerializeField] private Transform itemSpawnPoint;

        [Header("━━ DEBUG ━━")]
        [Tooltip("Active les logs détaillés et le rayon visible dans la Scene view.")]
        [SerializeField] private bool debugMode = true;
        [Tooltip("Couleur du rayon quand il ne touche rien.")]
        [SerializeField] private Color debugRayMiss = Color.red;
        [Tooltip("Couleur du rayon quand il touche un item ramassable.")]
        [SerializeField] private Color debugRayHit  = Color.green;
        [Tooltip("Couleur du rayon quand il touche autre chose.")]
        [SerializeField] private Color debugRayOther = Color.yellow;

        // ── Références internes ───────────────────────────────────────────────────
        private InventorySystem _inventory;
        private EffectSystem    _effectSystem;
        private ItemWorldObject _lookedAtItem;
        private RitualZone      _lookedAtRitual;

        // Throttle des logs "rien détecté" pour ne pas spammer
        private float _lastMissLogTime = -999f;
        private const float MissLogInterval = 2f;

        // ── Événements ────────────────────────────────────────────────────────────
        public event Action<string> OnInteractionPrompt;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _inventory    = GetComponent<InventorySystem>()
                         ?? GetComponentInParent<InventorySystem>();
            _effectSystem = GetComponent<EffectSystem>()
                         ?? GetComponentInParent<EffectSystem>();

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (debugMode)
                DebugStartup();
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
                if (debugMode) Debug.LogError("[PlayerInteraction] playerCamera est NULL ! " +
                    "Assurez-vous qu'une Camera est enfant du Player ou assignée dans l'Inspector.");
                OnInteractionPrompt?.Invoke(null);
                return;
            }

            var origin = playerCamera.transform.position;
            var dir    = playerCamera.transform.forward;
            var ray    = new Ray(origin, dir);

            // Portée doublée si l'effet Ouïe est actif
            float effectiveRange = interactionRange
                * (_effectSystem != null ? _effectSystem.InteractionRangeMultiplier : 1f);

            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, effectiveRange,
                                                interactionLayer,
                                                QueryTriggerInteraction.Collide);

            if (hitSomething)
            {
                // ── Tente de trouver ItemWorldObject ──────────────────────────────
                var worldItem = hit.collider.GetComponent<ItemWorldObject>()
                             ?? hit.collider.GetComponentInParent<ItemWorldObject>();

                if (worldItem != null)
                {
                    if (debugMode)
                        Debug.DrawRay(origin, dir * hit.distance, debugRayHit);

                    // Diagnostic si l'item est invalide
                    if (!worldItem.CanBePickedUp)
                    {
                        if (debugMode)
                            Debug.Log($"[PlayerInteraction] Touché : '{hit.collider.name}' " +
                                      $"mais CanBePickedUp=false (état={worldItem.State})");
                        OnInteractionPrompt?.Invoke(null);
                        return;
                    }

                    if (worldItem.Data == null)
                    {
                        if (debugMode)
                            Debug.LogWarning($"[PlayerInteraction] '{hit.collider.name}' a " +
                                             "ItemWorldObject mais Data (ItemData) est NULL ! " +
                                             "Assignez-le dans l'Inspector du prefab.");
                        OnInteractionPrompt?.Invoke("[!] Item sans données (voir Console)");
                        return;
                    }

                    _lookedAtItem = worldItem;
                    bool isResource = worldItem.Data.IsResource;
                    string prompt = isResource
                        ? $"[E] Collecter {worldItem.Data.ItemName} (+{worldItem.Data.ResourceValue} pts)"
                        : $"[E] Ramasser {worldItem.Data.ItemName}";
                    OnInteractionPrompt?.Invoke(prompt);
                    return;
                }

                // ── Tente de trouver RitualZone ───────────────────────────────────
                var ritual = hit.collider.GetComponent<RitualZone>()
                          ?? hit.collider.GetComponentInParent<RitualZone>();

                if (ritual != null)
                {
                    if (debugMode)
                        Debug.DrawRay(origin, dir * hit.distance, debugRayHit);

                    _lookedAtRitual = ritual;
                    OnInteractionPrompt?.Invoke(ritual.GetPromptText(_inventory));
                    return;
                }

                // ── Touche quelque chose mais pas un item connu ───────────────────
                if (debugMode)
                {
                    Debug.DrawRay(origin, dir * hit.distance, debugRayOther);

                    // Log throttlé pour ne pas spammer la console
                    if (Time.time - _lastMissLogTime > MissLogInterval)
                    {
                        _lastMissLogTime = Time.time;
                        Debug.Log($"[PlayerInteraction] Raycast touche '{hit.collider.name}' " +
                                  $"(layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}, " +
                                  $"dist={hit.distance:F2}m) mais pas de ItemWorldObject ni RitualZone. " +
                                  $"Ajoutez ItemWorldObject sur ce prefab.");
                    }
                }
            }
            else
            {
                // ── Rien détecté ──────────────────────────────────────────────────
                if (debugMode)
                {
                    Debug.DrawRay(origin, dir * effectiveRange, debugRayMiss);

                    if (Time.time - _lastMissLogTime > MissLogInterval)
                    {
                        _lastMissLogTime = Time.time;
                        Debug.Log($"[PlayerInteraction] Raycast: rien détecté sur {effectiveRange:F1}m" +
                                  $"{(effectiveRange > interactionRange ? " (Ouïe ×2)" : "")}. " +
                                  $"LayerMask={interactionLayer.value} " +
                                  $"(~0 = tout détecter, vérifiez la valeur si un layer est exclu).");
                    }
                }
            }

            OnInteractionPrompt?.Invoke(null);
        }

        // ── Actions clavier ───────────────────────────────────────────────────────
        private void HandleInputActions()
        {
            if (Input.GetKeyDown(KeyCode.E)) TryPickupOrDeposit();
            if (Input.GetKeyDown(KeyCode.G)) TryDrop();
            if (Input.GetKeyDown(KeyCode.Q)) TryThrow();
            if (Input.GetKeyDown(KeyCode.F)) TryUse();
        }

        // ── [E] Ramasser / Déposer ────────────────────────────────────────────────
        private void TryPickupOrDeposit()
        {
            if (debugMode)
                Debug.Log($"[PlayerInteraction] [E] pressé — " +
                          $"lookedAtItem={(_lookedAtItem != null ? _lookedAtItem.name : "null")}, " +
                          $"lookedAtRitual={(_lookedAtRitual != null ? _lookedAtRitual.name : "null")}, " +
                          $"inventory={(_inventory != null ? "OK" : "NULL")}");

            // Priorité 1 : zone de rituel
            if (_lookedAtRitual != null)
            {
                if (_inventory == null) { Debug.LogError("[PlayerInteraction] InventorySystem manquant !"); return; }
                int deposited = _inventory.DepositResources();
                if (deposited > 0 && ResourceSystem.Instance != null)
                {
                    ResourceSystem.Instance.Deposit(deposited);
                    if (debugMode) Debug.Log($"[PlayerInteraction] Dépôt : {deposited} pts transférés.");
                }
                else if (debugMode)
                    Debug.Log("[PlayerInteraction] Pas de ressources à déposer.");
                return;
            }

            // Priorité 2 : item monde
            if (_lookedAtItem == null)
            {
                if (debugMode) Debug.Log("[PlayerInteraction] [E] : aucun item visé, action ignorée.");
                return;
            }

            if (!_lookedAtItem.CanBePickedUp)
            {
                if (debugMode) Debug.Log($"[PlayerInteraction] '{_lookedAtItem.name}' ne peut pas être ramassé (état={_lookedAtItem.State}).");
                return;
            }

            var data = _lookedAtItem.Data;

            if (data == null)
            {
                Debug.LogWarning($"[PlayerInteraction] '{_lookedAtItem.name}' : ItemData est NULL ! Assignez-le dans l'Inspector.");
                return;
            }

            if (_inventory == null)
            {
                Debug.LogError("[PlayerInteraction] InventorySystem introuvable sur le Player !");
                return;
            }

            if (data.IsResource)
            {
                _inventory.AddResource(data.ResourceValue);
                _lookedAtItem.OnPickedUp();
                Debug.Log($"[PlayerInteraction] ✔ Ressource '{data.ItemName}' collectée : +{data.ResourceValue} pts");
            }
            else
            {
                if (_inventory.TryPickupUtility(data))
                {
                    _lookedAtItem.OnPickedUp();
                    Debug.Log($"[PlayerInteraction] ✔ Ramassé : '{data.ItemName}'");
                }
                else
                {
                    Debug.Log($"[PlayerInteraction] ✘ Inventaire plein — impossible de ramasser '{data.ItemName}'. " +
                              $"Slots: {_inventory.OccupiedSlotCount}/{_inventory.MaxSlots}");
                }
            }
        }

        // ── [G] Poser ─────────────────────────────────────────────────────────────
        private void TryDrop()
        {
            if (_inventory == null) return;
            var item = _inventory.RemoveFromSlot(_inventory.SelectedSlot);
            if (item == null) { if (debugMode) Debug.Log("[PlayerInteraction] [G] : slot sélectionné vide."); return; }
            SpawnWorldItem(item, ItemWorldObject.WorldItemState.Dropped);
            Debug.Log($"[PlayerInteraction] Posé : {item.ItemName}");
        }

        // ── [Q] Lancer ────────────────────────────────────────────────────────────
        private void TryThrow()
        {
            if (_inventory == null || playerCamera == null) return;
            var item = _inventory.GetSelectedItem();
            if (item == null) { if (debugMode) Debug.Log("[PlayerInteraction] [Q] : slot vide."); return; }
            if (!item.CanThrow) { if (debugMode) Debug.Log($"[PlayerInteraction] [Q] : '{item.ItemName}' non lançable."); return; }

            _inventory.RemoveFromSlot(_inventory.SelectedSlot);
            Vector3 throwDir = Quaternion.AngleAxis(-throwUpwardAngle, playerCamera.transform.right)
                             * playerCamera.transform.forward;
            var worldObj = SpawnWorldItem(item, ItemWorldObject.WorldItemState.Thrown);
            if (worldObj != null) worldObj.ApplyThrowVelocity(throwDir * item.ThrowForce);
            Debug.Log($"[PlayerInteraction] Lancé : {item.ItemName}");
        }

        // ── [F] Utiliser ──────────────────────────────────────────────────────────
        private void TryUse()
        {
            if (_inventory == null) return;
            var item = _inventory.UseSelectedItem();
            if (item == null) { if (debugMode) Debug.Log("[PlayerInteraction] [F] : aucun item utilisable sélectionné."); return; }
            ApplyUseEffect(item);
            Debug.Log($"[PlayerInteraction] Utilisé : {item.ItemName}{(item.Consumes ? " (consommé)" : "")}");
        }

        private void ApplyUseEffect(ItemData item)
        {
            switch (item.Type)
            {
                case ItemType.Potion:
                    if (_effectSystem != null)
                    {
                        _effectSystem.ApplyEffect(item);
                        Debug.Log($"[PlayerInteraction] Potion '{item.ItemName}' appliquée via EffectSystem.");
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerInteraction] EffectSystem introuvable sur le Player ! " +
                                         "Ajoutez le composant EffectSystem.");
                    }
                    break;
                case ItemType.Balle:       Debug.Log("[PlayerInteraction] Balle → tir. (TODO)"); break;
                case ItemType.Armure:      Debug.Log("[PlayerInteraction] Armure → bouclier. (TODO)"); break;
                case ItemType.MalusEnnemi: Debug.Log("[PlayerInteraction] Malus → debuff. (TODO)"); break;
            }
        }

        // ── Spawn ─────────────────────────────────────────────────────────────────
        private ItemWorldObject SpawnWorldItem(ItemData item, ItemWorldObject.WorldItemState state)
        {
            if (item.WorldPrefab == null)
            {
                Debug.LogWarning($"[PlayerInteraction] '{item.ItemName}' n'a pas de WorldPrefab assigné.");
                return null;
            }
            var go  = UnityEngine.Object.Instantiate(item.WorldPrefab, GetSpawnPosition(),
                                                     Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            var wio = go.GetComponent<ItemWorldObject>() ?? go.AddComponent<ItemWorldObject>();
            wio.Initialize(item, state);
            return wio;
        }

        private Vector3 GetSpawnPosition()
        {
            if (itemSpawnPoint != null) return itemSpawnPoint.position;
            return playerCamera != null
                ? playerCamera.transform.position + playerCamera.transform.forward * 0.6f
                : transform.position + transform.forward * 0.6f;
        }

        // ── Debug startup ─────────────────────────────────────────────────────────
        private void DebugStartup()
        {
            Debug.Log("━━━━━━━━━━ [PlayerInteraction] DIAGNOSTIC DÉMARRAGE ━━━━━━━━━━");

            if (playerCamera == null)
                Debug.LogError("  ✘ playerCamera : NULL (cherche dans les enfants...)");
            else
                Debug.Log($"  ✔ playerCamera : {playerCamera.name}");

            if (_inventory == null)
                Debug.LogError("  ✘ InventorySystem : introuvable sur ce GameObject ou sur le parent !");
            else
                Debug.Log($"  ✔ InventorySystem : {_inventory.name} ({_inventory.MaxSlots} slot(s))");

            Debug.Log($"  • interactionRange : {interactionRange}m (×2 avec Ouïe)");
            Debug.Log($"  • interactionLayer : {interactionLayer.value} " +
                      $"({(interactionLayer.value == -1 ? "ALL layers" : "layers filtrés")})");
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }
    }
}
