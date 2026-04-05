using UnityEngine;
using ProjectFPS.Player;

namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Composant à placer sur tous les prefabs d'objets dans le monde.
    /// Remplace ItemPickup et gère trois états :
    ///
    ///   Floating → objet en attente de ramassage, animation flottante.
    ///   Dropped  → posé au sol par le joueur, récupérable.
    ///   Thrown   → lancé avec physique, déclenche un effet à l'impact.
    ///
    /// Le prefab doit avoir :
    ///   - Un Collider (trigger ou non selon l'état)
    ///   - Optionnellement un Rigidbody (ajouté automatiquement si absent au lancer)
    /// </summary>
    public class ItemWorldObject : MonoBehaviour
    {
        public enum WorldItemState { Floating, Dropped, Thrown }

        [Header("Données")]
        [SerializeField] private ItemData _data;

        [Header("Impact Potion (état Thrown)")]
        [Tooltip("Rayon dans lequel les joueurs sont affectés à l'impact (potions lancées).")]
        [SerializeField] private float impactRadius = 3f;
        [SerializeField] private LayerMask playerLayer = ~0;

        [Header("Animation flottante (état Floating)")]
        [Tooltip("Désactivé par défaut : les objets restent statiques au sol.")]
        [SerializeField] private bool  enableFloat    = false;
        [SerializeField] private float rotationSpeed  = 45f;
        [SerializeField] private float bobAmplitude   = 0.15f;
        [SerializeField] private float bobFrequency   = 1f;

        // ── État interne ──────────────────────────────────────────────────────────
        private WorldItemState _state = WorldItemState.Floating;
        private Rigidbody      _rb;
        private Collider       _col;
        private Vector3        _basePosition;
        private float          _bobTime;
        private bool           _effectTriggered;

        // ── Accesseurs ────────────────────────────────────────────────────────────
        public ItemData       Data          => _data;
        public WorldItemState State         => _state;
        public bool           CanBePickedUp => _state != WorldItemState.Thrown;

        // ── Initialisation ────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb  = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            _basePosition = transform.position;
        }

        /// <summary>
        /// Appelé par PlayerInteraction après Instantiate pour configurer l'objet.
        /// </summary>
        public void Initialize(ItemData data, WorldItemState state)
        {
            _data  = data;
            _state = state;
            _effectTriggered = false;
            _basePosition = transform.position;

            ApplyState();
        }

        private void ApplyState()
        {
            switch (_state)
            {
                case WorldItemState.Floating:
                    // enableFloat reste à sa valeur sérialisée (false par défaut)
                    if (_rb != null) _rb.isKinematic = true;
                    if (_col != null) _col.isTrigger = true;
                    break;

                case WorldItemState.Dropped:
                    enableFloat = false;
                    if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
                    _rb.isKinematic = false;
                    if (_col != null) _col.isTrigger = false;
                    break;

                case WorldItemState.Thrown:
                    enableFloat = false;
                    if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
                    _rb.isKinematic = false;
                    if (_col != null) _col.isTrigger = false;
                    break;
            }
        }

        /// <summary>
        /// Applique une vélocité initiale (appelé après Initialize avec state = Thrown).
        /// </summary>
        public void ApplyThrowVelocity(Vector3 velocity)
        {
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = false;
            _rb.linearVelocity = velocity;
        }

        // ── Update ────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_state == WorldItemState.Floating && enableFloat)
                AnimateFloat();
        }

        private void AnimateFloat()
        {
            if (rotationSpeed > 0f)
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            if (bobAmplitude > 0f)
            {
                _bobTime += Time.deltaTime * bobFrequency;
                Vector3 pos = _basePosition;
                pos.y += Mathf.Sin(_bobTime * Mathf.PI * 2f) * bobAmplitude;
                transform.position = pos;
            }
        }

        // ── Collision / Impact ────────────────────────────────────────────────────

        private void OnCollisionEnter(Collision col)
        {
            if (_state != WorldItemState.Thrown || _effectTriggered) return;
            _effectTriggered = true;
            TriggerImpactEffect(col);
        }

        private void TriggerImpactEffect(Collision col)
        {
            if (_data == null) return;

            switch (_data.Type)
            {
                case ItemType.Potion:
                    Debug.Log($"[ItemWorldObject] Potion '{_data.ItemName}' explose à {transform.position} (rayon {impactRadius}m)");
                    ApplyPotionAreaEffect();
                    Destroy(gameObject);
                    break;

                case ItemType.Fumigène:
                    // ✅ Activation zone de fumée
                    Debug.Log($"[ItemWorldObject] Fumigène activé à {transform.position}");
                    // TODO: Instantiate smoke zone VFX
                    Destroy(gameObject);
                    break;

                case ItemType.Piège:
                    // ✅ Piège armé à l'impact (ou posé s'il était en mode Dropped)
                    Debug.Log($"[ItemWorldObject] Piège armé à {transform.position}");
                    _state = WorldItemState.Dropped;   // devient statique, ne peut plus être ramassé
                    if (_rb != null) _rb.isKinematic = true;
                    // TODO: Arm trap logic (OnTriggerEnter pour déclencher le piège)
                    break;

                default:
                    // Les autres types ne font rien à l'impact → deviennent objets au sol
                    _state = WorldItemState.Dropped;
                    if (_rb != null) _rb.isKinematic = true;
                    break;
            }
        }

        // ── Effet de potion en zone ───────────────────────────────────────────────

        private void ApplyPotionAreaEffect()
        {
            if (_data == null) return;

            // Détecte tous les colliders dans le rayon
            var hits = Physics.OverlapSphere(transform.position, impactRadius, playerLayer);
            int affected = 0;

            foreach (var hit in hits)
            {
                var effects = hit.GetComponent<EffectSystem>()
                           ?? hit.GetComponentInParent<EffectSystem>();
                if (effects == null) continue;

                effects.ApplyEffect(_data);
                affected++;
                Debug.Log($"[ItemWorldObject] Potion '{_data.ItemName}' appliquée à '{hit.name}'");
            }

            if (affected == 0)
                Debug.Log($"[ItemWorldObject] Potion '{_data.ItemName}' : aucun joueur dans le rayon {impactRadius}m.");
        }

        // ── Ramassage ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Appelé par PlayerInteraction quand le joueur ramasse cet objet.
        /// </summary>
        public void OnPickedUp()
        {
            gameObject.SetActive(false);
        }
    }
}
