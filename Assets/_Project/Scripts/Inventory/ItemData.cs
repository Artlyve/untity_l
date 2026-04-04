using UnityEngine;

namespace ProjectFPS.Inventory
{
    /// <summary>
    /// ScriptableObject décrivant un objet du jeu.
    ///
    /// Capacités dérivées automatiquement du type (table de spec) :
    ///   Objet       | Utiliser | Jeter | Poser | Consommer
    ///   ─────────────────────────────────────────────────
    ///   Potion      |   ✅    |  ✅   |  ❌   |   ✅
    ///   Piège       |   ❌    |  ✅   |  ✅   |   ❌
    ///   Fumigène    |   ❌    |  ✅   |  ❌   |   ❌
    ///   Balle       |   ✅    |  ❌   |  ❌   |   ✅
    ///   Armure      |   ✅    |  ❌   |  ❌   |   ❌
    ///   MalusEnnemi |   ✅    |  ❌   |  ❌   |   ❌
    ///   Resource    |   —     |  —    |  —    |   —  (converti en points)
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemData")]
    public class ItemData : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField] private string   itemName;
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private Sprite   icon;
        [SerializeField] private ItemType itemType;

        [Header("Préfab monde")]
        [Tooltip("Prefab instancié quand l'objet est posé ou lancé dans le monde.")]
        [SerializeField] private GameObject worldPrefab;

        [Header("Propriétés")]
        [Tooltip("Force appliquée lors du lancer (ItemType throwable uniquement).")]
        [SerializeField] private float throwForce    = 12f;
        [Tooltip("Valeur en points accordée lors de la collecte (ItemType.Resource uniquement).")]
        [SerializeField] private int   resourceValue = 10;

        // ── Propriétés d'identité ─────────────────────────────────────────────────
        public string     ItemName      => itemName;
        public string     Description   => description;
        public Sprite     Icon          => icon;
        public ItemType   Type          => itemType;
        public GameObject WorldPrefab   => worldPrefab;
        public float      ThrowForce    => throwForce;
        public int        ResourceValue => resourceValue;

        // ── Capacités dérivées (lecture seule, pas de flags à cocher) ─────────────

        /// <summary>Peut être utilisé directement depuis l'inventaire (touche F).</summary>
        public bool CanUse =>
            itemType == ItemType.Potion     ||
            itemType == ItemType.Balle      ||
            itemType == ItemType.Armure     ||
            itemType == ItemType.MalusEnnemi;

        /// <summary>Peut être lancé avec une physique et déclenche un effet à l'impact (touche Q).</summary>
        public bool CanThrow =>
            itemType == ItemType.Potion  ||
            itemType == ItemType.Piège   ||
            itemType == ItemType.Fumigène;

        /// <summary>Peut être posé au sol de façon intentionnelle (actif, ex : piège armé) (touche G).</summary>
        public bool CanPlace =>
            itemType == ItemType.Piège;

        /// <summary>Détruit l'objet après utilisation.</summary>
        public bool Consumes =>
            itemType == ItemType.Potion ||
            itemType == ItemType.Balle;

        /// <summary>Objet de récolte : converti directement en points, jamais stocké.</summary>
        public bool IsResource => itemType == ItemType.Resource;
    }
}
