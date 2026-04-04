using System;
using UnityEngine;

namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Singleton gérant le pool de ressources global de la partie.
    ///
    /// Cycle de victoire :
    ///   Joueurs récoltent → dépôt en zone de rituel → total global augmente
    ///   → si total ≥ winThreshold → OnRitualActivated déclenché (fin de partie).
    /// </summary>
    public class ResourceSystem : MonoBehaviour
    {
        public static ResourceSystem Instance { get; private set; }

        [Header("Victoire")]
        [Tooltip("Points globaux nécessaires pour activer le rituel et gagner la partie.")]
        [SerializeField] private int winThreshold = 100;

        private int  _globalTotal;
        private bool _ritualActivated;

        // ── Accesseurs ────────────────────────────────────────────────────────────
        public int  GlobalTotal      => _globalTotal;
        public int  WinThreshold     => winThreshold;
        public bool RitualActivated  => _ritualActivated;

        /// <summary>Progression 0–1 vers le seuil de victoire.</summary>
        public float GlobalProgress  => winThreshold > 0 ? (float)_globalTotal / winThreshold : 0f;

        // ── Événements ────────────────────────────────────────────────────────────
        /// <summary>Déclenché à chaque dépôt. Paramètre : nouveau total global.</summary>
        public event Action<int> OnGlobalTotalChanged;

        /// <summary>Déclenché une seule fois quand le seuil de victoire est atteint.</summary>
        public event Action      OnRitualActivated;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── API publique ──────────────────────────────────────────────────────────

        /// <summary>
        /// Ajoute des points au pool global (appelé par RitualZone lors d'un dépôt).
        /// </summary>
        /// <param name="points">Nombre de points à ajouter.</param>
        public void Deposit(int points)
        {
            if (_ritualActivated || points <= 0) return;

            _globalTotal += points;
            OnGlobalTotalChanged?.Invoke(_globalTotal);

            Debug.Log($"[ResourceSystem] Dépôt de {points} pts — Total : {_globalTotal}/{winThreshold}");

            if (_globalTotal >= winThreshold)
                ActivateRitual();
        }

        private void ActivateRitual()
        {
            _ritualActivated = true;
            Debug.Log("[ResourceSystem] ★ Rituel activé ! Les joueurs ont gagné !");
            OnRitualActivated?.Invoke();
        }
    }
}
