using UnityEngine;

namespace ProjectFPS.Inventory
{
    public class ItemPickup : MonoBehaviour
    {
        [Header("Données de l'item")]
        [SerializeField] private ItemData itemData;

        [Header("Animation de flottement")]
        [SerializeField] private bool  enableFloating  = true;
        [SerializeField] private float rotationSpeed   = 45f;
        [SerializeField] private float bobAmplitude    = 0.15f;
        [SerializeField] private float bobFrequency    = 1f;

        private Vector3 _initialPosition;
        private float   _bobTime;

        // Accesseurs pour PlayerInteraction
        public string   ItemName => itemData != null ? itemData.ItemName : string.Empty;
        public ItemData Data     => itemData;

        private void Start()
        {
            _initialPosition = transform.position;
        }

        private void Update()
        {
            if (!enableFloating) return;
            AnimateFloat();
        }

        // Rotation lente sur Y + oscillation verticale pour la visibilité
        private void AnimateFloat()
        {
            // Rotation continue autour de l'axe vertical mondial
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Bobbing sinusoïdal sur la position Y d'origine
            _bobTime += Time.deltaTime * bobFrequency;
            Vector3 pos = _initialPosition;
            pos.y += Mathf.Sin(_bobTime * Mathf.PI * 2f) * bobAmplitude;
            transform.position = pos;
        }

        // Désactive l'objet sans le détruire (respawn possible côté serveur/timer)
        public void Pickup()
        {
            gameObject.SetActive(false);
        }
    }
}
