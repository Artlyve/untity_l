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
    /// Ajouter ce composant sur le même GameObject que PlayerController.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class RoleAbilityController : MonoBehaviour
    {
        // ── Références ────────────────────────────────────────────────────────────
        [Header("Références")]
        [SerializeField] private Camera playerCamera;

        [Header("Chasseur – Visée")]
        [SerializeField] private float aimFOV          = 40f;
        [SerializeField] private float normalFOV        = 70f;
        [SerializeField] private float aimSpeedPenalty  = 0.4f;   // multiplicateur vitesse en visée
        [SerializeField] private float fovTransitionSpeed = 10f;

        [Header("Loup – Transformation")]
        [SerializeField] private GameObject humanMesh;
        [SerializeField] private GameObject wolfMesh;
        [SerializeField] private float      wolfSpeedBonus   = 1.5f;   // multiplicateur vitesse en forme loup
        [SerializeField] private float      wolfAttackDamage = 30f;
        [SerializeField] private float      wolfAttackRange  = 2f;
        [SerializeField] private LayerMask  wolfAttackLayer  = ~0;

        // ── État interne ──────────────────────────────────────────────────────────
        private PlayerRole    _role          = PlayerRole.Villageois;
        private InventorySystem _inventory;
        private bool          _isAiming;
        private bool          _isWolfForm;
        private float         _attackCooldown;
        private const float   AttackCooldownTime = 0.8f;

        /// <summary>Multiplicateur de vitesse issu de la capacité de rôle (Chasseur visée, Loup).</summary>
        public float RoleSpeedMultiplier
        {
            get
            {
                if (_isAiming)   return aimSpeedPenalty;
                if (_isWolfForm) return wolfSpeedBonus;
                return 1f;
            }
        }

        /// <summary>Vrai si le joueur est en forme loup.</summary>
        public bool IsWolfForm => _isWolfForm;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _inventory = GetComponent<InventorySystem>();

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera != null)
                normalFOV = playerCamera.fieldOfView;
        }

        private void OnEnable()
        {
            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged += OnRoleChanged;
        }

        private void OnDisable()
        {
            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged -= OnRoleChanged;

            // Nettoyer l'état de visée quand désactivé
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

        // ── Chasseur ──────────────────────────────────────────────────────────────

        private void HandleChasseurInputs()
        {
            if (Input.GetMouseButtonDown(1))  // RMB
                EnterAim();
            else if (Input.GetMouseButtonUp(1))
                ExitAim();
        }

        private void EnterAim()
        {
            if (_isAiming) return;
            _isAiming = true;
            Debug.Log("[RoleAbilityController] Chasseur : mode visée activé.");
        }

        private void ExitAim()
        {
            if (!_isAiming) return;
            _isAiming = false;
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
            Debug.Log($"[RoleAbilityController] Loup : forme {(_isWolfForm ? "loup" : "humaine")}.");

            if (humanMesh != null) humanMesh.SetActive(!_isWolfForm);
            if (wolfMesh  != null) wolfMesh.SetActive(_isWolfForm);
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
            if (role == null) return;
            _role = role.RoleType;

            // Réinitialisation états
            if (_isAiming)   ExitAim();
            if (_isWolfForm) ToggleWolfForm();  // force retour humain

            Debug.Log($"[RoleAbilityController] Rôle changé → {_role}");
        }
    }
}
