using UnityEngine;
using ProjectFPS.Roles;
using ProjectFPS.Player;
using ProjectFPS.Inventory;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Gère les capacités spéciales propres à chaque rôle.
    ///
    /// Rôles et capacités :
    ///   Chasseur    → [RMB] Mode visée : réduit FOV et vitesse pendant la visée
    ///   Loup        → [R] Transformation : toggle entre forme humaine et loup
    ///                 [LMB] Attaque : dégâts en corps-à-corps (forme loup)
    ///   Fils_Chasseur → [1][2] ou Scroll pour changer de slot actif
    ///
    /// Configuration des meshes Loup :
    ///   • Assignez HumanMesh et WolfMesh dans l'Inspecteur, OU
    ///   • Nommez les GameObjects enfants "HumanMesh" et "WolfMesh"
    ///     → ils seront trouvés automatiquement au démarrage.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class RoleAbilityController : MonoBehaviour
    {
        // ── Références ────────────────────────────────────────────────────────────
        [Header("Références")]
        [SerializeField] private Camera   playerCamera;
        [SerializeField] private Animator animator;

        [Header("Chasseur – Visée")]
        [SerializeField] private float aimFOV            = 40f;
        [SerializeField] private float normalFOV         = 70f;
        [SerializeField] private float aimSpeedPenalty   = 0.4f;
        [SerializeField] private float fovTransitionSpeed = 10f;

        [Header("Loup – Transformation")]
        [Tooltip("GameObject du mesh humain. Laissez vide → cherché automatiquement (enfant nommé 'HumanMesh').")]
        [SerializeField] private GameObject humanMesh;
        [Tooltip("GameObject du mesh loup.  Laissez vide → cherché automatiquement (enfant nommé 'WolfMesh').")]
        [SerializeField] private GameObject wolfMesh;
        [SerializeField] private float      wolfSpeedBonus   = 1.5f;
        [SerializeField] private float      wolfAttackDamage = 30f;
        [SerializeField] private float      wolfAttackRange  = 2f;
        [SerializeField] private LayerMask  wolfAttackLayer  = ~0;

        // ── État interne ──────────────────────────────────────────────────────────
        private PlayerRole      _role              = PlayerRole.Villageois;
        private InventorySystem _inventory;
        private bool            _isAiming;
        private bool            _isWolfForm;
        private float           _attackCooldown;
        private float           _roleBaseSpeedMult = 1f;
        private const float     AttackCooldownTime = 0.8f;

        // ── Paramètres Animator ───────────────────────────────────────────────────
        private static readonly int IsAimingParam    = Animator.StringToHash("IsAiming");
        private static readonly int IsWolfFormParam  = Animator.StringToHash("IsWolfForm");

        /// <summary>
        /// Multiplicateur de vitesse total :
        ///   base (RoleData.SpeedMultiplier) × situationnel (visée / forme loup).
        /// </summary>
        public float RoleSpeedMultiplier
        {
            get
            {
                if (_isAiming)   return _roleBaseSpeedMult * aimSpeedPenalty;
                if (_isWolfForm) return _roleBaseSpeedMult * wolfSpeedBonus;
                return _roleBaseSpeedMult;
            }
        }

        public bool IsWolfForm => _isWolfForm;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _inventory = GetComponent<InventorySystem>();

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (playerCamera != null)
                normalFOV = playerCamera.fieldOfView;

            // Auto-cherche les meshes par nom si non assignés dans l'Inspecteur
            TryAutoFindMeshes();
        }

        private void Start()
        {
            if (RoleManager.Instance != null)
            {
                RoleManager.Instance.OnRoleChanged += OnRoleChanged;
                OnRoleChanged(RoleManager.Instance.CurrentRole);
            }
            else
            {
                Debug.LogError("[RoleAbilityController] RoleManager.Instance est NULL ! " +
                    "Ajoutez un GameObject RoleManager dans la scène.");
            }

            Debug.Log("[RoleAbilityController] Démarré :" +
                $"\n  Rôle          : {_role}" +
                $"\n  Camera        : {(playerCamera != null ? playerCamera.name : "NULL ← vérifiez !")}" +
                $"\n  Animator      : {(animator != null ? animator.name : "non assigné")}" +
                $"\n  Human Mesh    : {(humanMesh != null ? humanMesh.name : "NULL ← assignez dans l'Inspecteur ou nommez l'enfant 'HumanMesh'")}" +
                $"\n  Wolf Mesh     : {(wolfMesh  != null ? wolfMesh.name  : "NULL ← assignez dans l'Inspecteur ou nommez l'enfant 'WolfMesh'")}");
        }

        private void OnDestroy()
        {
            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged -= OnRoleChanged;
        }

        private void OnDisable()
        {
            if (_isAiming) ExitAim();
        }

        private void Update()
        {
            if (_attackCooldown > 0f)
                _attackCooldown -= Time.deltaTime;

            switch (_role)
            {
                case PlayerRole.Chasseur:
                case PlayerRole.Fils_Chasseur:
                    HandleChasseurInputs();
                    break;

                case PlayerRole.Loup:
                    HandleLoupInputs();
                    break;
            }

            // Transition FOV (Chasseur visée)
            if (playerCamera != null)
            {
                float targetFOV = _isAiming ? aimFOV : normalFOV;
                playerCamera.fieldOfView = Mathf.Lerp(
                    playerCamera.fieldOfView, targetFOV,
                    fovTransitionSpeed * Time.deltaTime);
            }
        }

        // ── Auto-détection des meshes ─────────────────────────────────────────────

        private void TryAutoFindMeshes()
        {
            if (humanMesh == null)
            {
                var t = transform.Find("HumanMesh");
                if (t == null) t = FindChildByName(transform, "HumanMesh");
                if (t != null) humanMesh = t.gameObject;
            }

            if (wolfMesh == null)
            {
                var t = transform.Find("WolfMesh");
                if (t == null) t = FindChildByName(transform, "WolfMesh");
                if (t != null) wolfMesh = t.gameObject;
            }
        }

        private static Transform FindChildByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ── Chasseur ──────────────────────────────────────────────────────────────

        private void HandleChasseurInputs()
        {
            if (Input.GetMouseButtonDown(1))
                EnterAim();
            else if (Input.GetMouseButtonUp(1))
                ExitAim();
        }

        private void EnterAim()
        {
            if (_isAiming) return;
            _isAiming = true;
            if (animator != null) animator.SetBool(IsAimingParam, true);
            Debug.Log("[RoleAbilityController] Chasseur : mode visée activé.");
        }

        private void ExitAim()
        {
            if (!_isAiming) return;
            _isAiming = false;
            if (animator != null) animator.SetBool(IsAimingParam, false);
            Debug.Log("[RoleAbilityController] Chasseur : mode visée désactivé.");
        }

        // ── Loup ──────────────────────────────────────────────────────────────────

        private void HandleLoupInputs()
        {
            if (Input.GetKeyDown(KeyCode.R))
                ToggleWolfForm();

            if (_isWolfForm && Input.GetMouseButtonDown(0) && _attackCooldown <= 0f)
                WolfAttack();
        }

        private void ToggleWolfForm()
        {
            _isWolfForm = !_isWolfForm;

            if (humanMesh != null)
                humanMesh.SetActive(!_isWolfForm);
            else
                Debug.LogWarning("[RoleAbilityController] HumanMesh non assigné — " +
                    "créez un enfant nommé 'HumanMesh' ou assignez-le dans l'Inspecteur.");

            if (wolfMesh != null)
                wolfMesh.SetActive(_isWolfForm);
            else
                Debug.LogWarning("[RoleAbilityController] WolfMesh non assigné — " +
                    "créez un enfant nommé 'WolfMesh' ou assignez-le dans l'Inspecteur.");

            if (animator != null)
                animator.SetBool(IsWolfFormParam, _isWolfForm);

            Debug.Log($"[RoleAbilityController] Loup : forme {(_isWolfForm ? "loup ▶" : "humaine ◀")}.");
        }

        private void WolfAttack()
        {
            _attackCooldown = AttackCooldownTime;
            Debug.Log("[RoleAbilityController] Loup : attaque !");

            if (playerCamera == null) return;
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, wolfAttackRange, wolfAttackLayer,
                                QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<PlayerState>()
                          ?? hit.collider.GetComponent<PlayerState>();
                if (target != null && target.gameObject != gameObject)
                {
                    target.TakeDamage(wolfAttackDamage);
                    Debug.Log($"[RoleAbilityController] Loup attaque '{hit.collider.name}' : -{wolfAttackDamage} PV");
                }
            }
        }

        // ── Callback rôle ─────────────────────────────────────────────────────────

        private void OnRoleChanged(RoleData role)
        {
            if (role == null)
            {
                Debug.LogWarning("[RoleAbilityController] OnRoleChanged reçu avec role == null.");
                return;
            }

            _role              = role.RoleType;
            _roleBaseSpeedMult = role.SpeedMultiplier > 0f ? role.SpeedMultiplier : 1f;

            if (_isAiming)   ExitAim();
            if (_isWolfForm) ToggleWolfForm();

            Debug.Log($"[RoleAbilityController] ✔ Rôle appliqué : {_role}" +
                $" | vitesse ×{_roleBaseSpeedMult}" +
                $" | inventorySlots = {role.InventorySlots}");

            switch (_role)
            {
                case PlayerRole.Loup:
                    Debug.Log("[RoleAbilityController] Loup actif → [R] transformer | [LMB] attaque (forme loup)");
                    break;
                case PlayerRole.Chasseur:
                    Debug.Log("[RoleAbilityController] Chasseur actif → [RMB] viser (ralentit)");
                    break;
                case PlayerRole.Fils_Chasseur:
                    Debug.Log($"[RoleAbilityController] Fils_Chasseur actif → {role.InventorySlots} slots | [1][2] ou molette");
                    break;
            }
        }
    }
}
