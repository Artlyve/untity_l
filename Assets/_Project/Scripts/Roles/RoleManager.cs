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
            // Applique le rôle par défaut au démarrage
            if (defaultRole != null)
                SetRole(defaultRole);
            else if (availableRoles.Count > 0)
                SetRole(availableRoles[0]);
        }

        // Définit le rôle actif et notifie les abonnés
        public void SetRole(RoleData role)
        {
            if (role == null) return;

            _currentRole = role;
            OnRoleChanged?.Invoke(_currentRole);
        }
    }
}
