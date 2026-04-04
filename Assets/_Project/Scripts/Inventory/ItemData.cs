using UnityEngine;

namespace ProjectFPS.Inventory
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemData")]
    public class ItemData : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField] private string itemName;
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Préfab scène")]
        [SerializeField] private GameObject prefab;

        [Header("Propriétés")]
        [SerializeField] private float weight      = 1f;
        [SerializeField] private bool  isStackable = false;
        [SerializeField] private int   maxStack    = 1;

        // Propriétés publiques en lecture seule
        public string     ItemName    => itemName;
        public string     Description => description;
        public Sprite     Icon        => icon;
        public GameObject Prefab      => prefab;
        public float      Weight      => weight;
        public bool       IsStackable => isStackable;
        public int        MaxStack    => maxStack;
    }
}
