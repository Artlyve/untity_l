using UnityEngine;

namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Diagnostic pour les objets ramassables.
    /// Ajoutez ce composant temporairement sur un item problématique :
    /// il affichera dans la Console tous les problèmes de configuration détectés.
    ///
    /// Retirez-le en production (ou laissez-le, il ne consomme rien en dehors de Start).
    /// </summary>
    public class ItemDebugger : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"━━━ [ItemDebugger] Diagnostic de '{name}' ━━━");

            CheckItemWorldObject();
            CheckCollider();
            CheckRigidbody();
            CheckLayer();

            Debug.Log($"━━━ Fin diagnostic '{name}' ━━━");
        }

        private void CheckItemWorldObject()
        {
            var iwo = GetComponent<ItemWorldObject>();
            if (iwo == null)
            {
                // Cherche l'ancien composant
                var oldPickup = GetComponent("ItemPickup");
                if (oldPickup != null)
                    Debug.LogError($"  ✘ '{name}' a encore ItemPickup (ancien système). " +
                                   "Remplacez-le par ItemWorldObject !");
                else
                    Debug.LogError($"  ✘ '{name}' n'a PAS de composant ItemWorldObject ! " +
                                   "Ajoutez-le et assignez ItemData.");
                return;
            }

            Debug.Log($"  ✔ ItemWorldObject présent (état={iwo.State}, CanBePickedUp={iwo.CanBePickedUp})");

            if (iwo.Data == null)
                Debug.LogError($"  ✘ ItemWorldObject.Data (ItemData) est NULL ! " +
                               "Assignez un ItemData dans l'Inspector du prefab.");
            else
                Debug.Log($"  ✔ ItemData = '{iwo.Data.ItemName}' (type={iwo.Data.Type}, " +
                          $"isResource={iwo.Data.IsResource})");
        }

        private void CheckCollider()
        {
            var col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogError($"  ✘ '{name}' n'a PAS de Collider ! " +
                               "Le raycast ne peut rien détecter sans collider.");
                return;
            }

            Debug.Log($"  ✔ Collider : {col.GetType().Name}, " +
                      $"isTrigger={col.isTrigger}, enabled={col.enabled}");

            if (!col.enabled)
                Debug.LogWarning($"  ⚠ Collider désactivé sur '{name}' → le raycast ne le verra pas.");
        }

        private void CheckRigidbody()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.Log($"  • Pas de Rigidbody (OK pour un item Floating statique).");
                return;
            }
            Debug.Log($"  ✔ Rigidbody : isKinematic={rb.isKinematic}, " +
                      $"useGravity={rb.useGravity}");
        }

        private void CheckLayer()
        {
            string layerName = LayerMask.LayerToName(gameObject.layer);
            Debug.Log($"  • Layer : {gameObject.layer} ('{layerName}')");

            // Vérifie si ce layer serait exclu par le LayerMask par défaut (~0 = tout)
            // On ne peut pas accéder à PlayerInteraction ici, mais on avertit si le layer est 0
            if (gameObject.layer == 0)
                Debug.Log($"    (layer 'Default' — sera détecté si interactionLayer = ~0)");
        }

#if UNITY_EDITOR
        // Dessine la zone d'interaction dans la Scene view
        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
#endif
    }
}
