using UnityEngine;

namespace ProjectFPS.Roles
{
    [CreateAssetMenu(fileName = "NewRole", menuName = "Roles/RoleData")]
    public class RoleData : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField] private string roleName;
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Type")]
        [SerializeField] private PlayerRole roleType;

        [Header("Statistiques")]
        [Tooltip("Modificateur de vitesse de base pour ce rôle (1 = normal, 1.2 = +20%, etc.)")]
        [SerializeField] private float speedMultiplier  = 1f;
        [SerializeField] private float healthMultiplier = 1f;
        [Tooltip("Nombre de slots d'inventaire utilitaire. Villageois/Chasseur/Loup = 1, Fils_Chasseur = 2.")]
        [SerializeField] private int   inventorySlots   = 1;

        // Propriétés publiques en lecture seule
        public string     RoleName         => roleName;
        public string     Description      => description;
        public Sprite     Icon             => icon;
        public PlayerRole RoleType         => roleType;
        public float      SpeedMultiplier  => speedMultiplier;
        public float      HealthMultiplier => healthMultiplier;
        public int        InventorySlots   => inventorySlots;
    }
}
