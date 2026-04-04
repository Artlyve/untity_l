using UnityEngine;

namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Zone de dépôt des ressources.
    /// Ajouter ce composant sur un GameObject avec un Collider en mode Trigger.
    ///
    /// Comportement :
    ///   • Le joueur entre dans le trigger → le prompt "[E] Déposer" s'affiche.
    ///   • Pression de [E] → tous les points personnels sont transférés au ResourceSystem.
    ///   • Si ResourceSystem.GlobalTotal ≥ WinThreshold → fin de partie.
    /// </summary>
    public class RitualZone : MonoBehaviour
    {
        [Header("Feedback visuel (optionnel)")]
        [SerializeField] private Renderer zoneRenderer;
        [SerializeField] private Color    activeColor   = new Color(0.8f, 0.4f, 1f, 0.4f);
        [SerializeField] private Color    inactiveColor = new Color(0.4f, 0.4f, 1f, 0.2f);

        // Joueurs actuellement dans la zone (par leur InventorySystem)
        private InventorySystem _playerInZone;

        private void Start()
        {
            if (zoneRenderer != null)
                zoneRenderer.material.color = inactiveColor;
        }

        private void Update()
        {
            if (_playerInZone == null) return;

            if (Input.GetKeyDown(KeyCode.E))
                TryDeposit(_playerInZone);
        }

        // ── Trigger ───────────────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            var inv = other.GetComponentInParent<InventorySystem>()
                   ?? other.GetComponent<InventorySystem>();
            if (inv == null) return;

            _playerInZone = inv;

            if (zoneRenderer != null)
                zoneRenderer.material.color = activeColor;
        }

        private void OnTriggerExit(Collider other)
        {
            var inv = other.GetComponentInParent<InventorySystem>()
                   ?? other.GetComponent<InventorySystem>();
            if (inv == null || inv != _playerInZone) return;

            _playerInZone = null;

            if (zoneRenderer != null)
                zoneRenderer.material.color = inactiveColor;
        }

        // ── Dépôt ─────────────────────────────────────────────────────────────────

        private void TryDeposit(InventorySystem inventory)
        {
            if (ResourceSystem.Instance == null)
            {
                Debug.LogWarning("[RitualZone] ResourceSystem introuvable dans la scène.");
                return;
            }

            int deposited = inventory.DepositResources();   // transfère tout
            if (deposited > 0)
            {
                ResourceSystem.Instance.Deposit(deposited);
                Debug.Log($"[RitualZone] {deposited} pts déposés — Total global : {ResourceSystem.Instance.GlobalTotal}");
            }
            else
            {
                Debug.Log("[RitualZone] Aucun point à déposer.");
            }
        }

        // ── API publique (utilisée par PlayerInteraction pour le prompt) ───────────

        /// <summary>Renvoie le texte de prompt à afficher pour le joueur dans la zone.</summary>
        public string GetPromptText(InventorySystem inventory)
        {
            int pts = inventory != null ? inventory.PersonalResources : 0;
            return pts > 0
                ? $"[E] Déposer {pts} ressource(s) (total : {ResourceSystem.Instance?.GlobalTotal ?? 0}/{ResourceSystem.Instance?.WinThreshold ?? 100})"
                : "Zone de rituel (pas de ressources à déposer)";
        }
    }
}
