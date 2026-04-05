using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectFPS.Roles
{
    public class RoleManager : MonoBehaviour
    {
        // Singleton (sans DontDestroyOnLoad)
        public static RoleManager Instance { get; private set; }

        [Header("Rôles disponibles")]
        [SerializeField] private List<RoleData> availableRoles = new List<RoleData>();

        [Header("Rôle par défaut")]
        [SerializeField] private RoleData defaultRole;

        private RoleData _currentRole;

        // Accesseurs publics
        public RoleData         CurrentRole    => _currentRole;
        public List<RoleData>   AvailableRoles => availableRoles;

        // Déclenché après chaque changement de rôle
        public event Action<RoleData> OnRoleChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Diagnostics de démarrage
            Debug.Log($"[RoleManager] Démarré — {availableRoles.Count} rôle(s) disponible(s)" +
                      $", rôle par défaut : '{defaultRole?.RoleName ?? "aucun"}'");

            if (availableRoles.Count == 0)
                Debug.LogError("[RoleManager] AvailableRoles est vide ! " +
                    "Assignez des RoleData ScriptableObjects dans la liste AvailableRoles.");

            // Applique le rôle par défaut au démarrage
            if (defaultRole != null)
                SetRole(defaultRole);
            else if (availableRoles.Count > 0)
                SetRole(availableRoles[0]);
            else
                Debug.LogError("[RoleManager] Impossible d'appliquer un rôle : aucun rôle configuré !");
        }

        /// <summary>
        /// Définit le rôle actif et notifie tous les abonnés à OnRoleChanged.
        /// </summary>
        public void SetRole(RoleData role)
        {
            if (role == null) { Debug.LogWarning("[RoleManager] SetRole(null) ignoré."); return; }

            _currentRole = role;

            Debug.Log($"[RoleManager] ▶ SetRole → '{role.RoleName}'" +
                $" | type={role.RoleType}" +
                $" | slots={role.InventorySlots}" +
                $" | vitesse={role.SpeedMultiplier}" +
                $" | abonnés={OnRoleChanged?.GetInvocationList().Length ?? 0}");

            OnRoleChanged?.Invoke(_currentRole);
        }

        /// <summary>
        /// Ré-envoie l'événement OnRoleChanged avec le rôle courant.
        /// Utile pour forcer la re-synchronisation des systèmes (debug, spawn tardif).
        /// </summary>
        public void ForceReapplyCurrentRole()
        {
            if (_currentRole == null)
            {
                Debug.LogWarning("[RoleManager] ForceReapplyCurrentRole : aucun rôle courant.");
                return;
            }
            Debug.Log($"[RoleManager] Force-reapply → {_currentRole.RoleName}");
            OnRoleChanged?.Invoke(_currentRole);
        }
    }
}
