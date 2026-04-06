using UnityEngine;
using ProjectFPS.Roles;
using ProjectFPS.Inventory;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Gère les capacités spéciales par rôle + la transformation Loup.
    ///
    /// ═══ HIÉRARCHIE RECOMMANDÉE ══════════════════════════════════════════════
    ///
    ///   Player (root)
    ///     ├── PlayerController
    ///     ├── RoleAbilityController
    ///     ├── CharacterController
    ///     ├── HumanFBX  (actif au démarrage)
    ///     │     └── Animator  → assignez dans "Human Animator"
    ///     └── WolfFBX   (INACTIF au démarrage)
    ///           └── Animator  → assignez dans "Wolf Animator"
    ///
    ///   Les deux Animator ont CHACUN leur propre controller configuré.
    ///   Le code switche juste quelle référence PlayerController utilise.
    ///   Pas besoin de swapper runtimeAnimatorController.
    ///
    /// ═══ RÔLES ════════════════════════════════════════════════════════════════
    ///   Chasseur / Fils_Chasseur  → [Clic droit] Mode visée
    ///   Loup                      → [R] Transformation | [Clic gauche] Attaque
    ///
    /// ═══ PARAMÈTRES ANIMATOR (identiques dans les deux controllers) ══════════
    ///   MoveX, MoveY, IsGrounded, IsFalling, IsNearGround, IsCrouching
    ///   JumpStart (trigger), Hit (trigger), IsDead
    ///   WolfAttack (trigger) — dans le Wolf Controller uniquement
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class RoleAbilityController : MonoBehaviour
    {
        // ─── Références caméra ────────────────────────────────────────────────────
        [Header("Références")]
        [SerializeField] private Camera playerCamera;

        // ─── Animators (un par forme) ─────────────────────────────────────────────
        [Header("Animators — un par forme")]
        [Tooltip("Animator du modèle HUMAIN. Glissez le composant Animator du HumanFBX.")]
        [SerializeField] private Animator humanAnimator;
        [Tooltip("Animator du modèle LOUP.  Glissez le composant Animator du WolfFBX.")]
        [SerializeField] private Animator wolfAnimator;

        // ─── Chasseur : Visée ────────────────────────────────────────────────────
        [Header("Chasseur – Visée")]
        [SerializeField] private float aimFOV             = 40f;
        [SerializeField] private float normalFOV          = 70f;
        [SerializeField] private float aimSpeedPenalty    = 0.4f;
        [SerializeField] private float fovTransitionSpeed = 10f;

        // ─── Loup : Transformation ────────────────────────────────────────────────
        [Header("Loup – Transformation")]
        [Tooltip("Glissez le GameObject HumanFBX depuis la Hierarchy.")]
        [SerializeField] private GameObject humanMesh;
        [Tooltip("Glissez le GameObject WolfFBX depuis la Hierarchy.")]
        [SerializeField] private GameObject wolfMesh;
        [SerializeField] private float      wolfSpeedBonus   = 1.5f;
        [SerializeField] private float      wolfAttackDamage = 30f;
        [SerializeField] private float      wolfAttackRange  = 2f;
        [SerializeField] private LayerMask  wolfAttackLayer  = ~0;

        // ─── État interne ─────────────────────────────────────────────────────────
        private PlayerRole _role              = PlayerRole.Villageois;
        private InventorySystem _inventory;
        private bool       _isAiming;
        private bool       _isWolfForm;
        private float      _attackCooldown;
        private float      _roleBaseSpeedMult = 1f;
        private const float AttackCooldownTime = 0.8f;

        // Animator actif (bascule entre humanAnimator et wolfAnimator)
        private Animator _activeAnimator;

        // ─── Hash paramètres Animator ─────────────────────────────────────────────
        private static readonly int IsAimingParam   = Animator.StringToHash("IsAiming");
        private static readonly int IsWolfFormParam = Animator.StringToHash("IsWolfForm");
        private static readonly int WolfAttackParam = Animator.StringToHash("WolfAttack");

        // ─── Propriétés publiques ─────────────────────────────────────────────────
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

        // ═════════════════════════════════════════════════════════════════════════
        // Lifecycle
        // ═════════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            _inventory = GetComponent<InventorySystem>();

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera != null)
                normalFOV = playerCamera.fieldOfView;

            // Auto-détection des meshes par nom si non assignés
            TryAutoFindMeshes();

            // Auto-détection des Animators si non assignés
            TryAutoFindAnimators();
        }

        private void Start()
        {
            // L'Animator actif au démarrage est le humain
            _activeAnimator = humanAnimator;

            // S'assure que PlayerController utilise aussi le bon Animator dès le départ
            GetComponent<PlayerController>()?.SetAnimator(_activeAnimator);

            if (RoleManager.Instance != null)
            {
                RoleManager.Instance.OnRoleChanged += OnRoleChanged;
                OnRoleChanged(RoleManager.Instance.CurrentRole);
            }
            else
            {
                Debug.LogError("[RoleAbilityController] RoleManager.Instance est NULL !");
            }

            Debug.Log("[RoleAbilityController] Démarré :" +
                $"\n  Rôle           : {_role}" +
                $"\n  Camera         : {(playerCamera != null ? playerCamera.name : "NULL !")}" +
                $"\n  Human Animator : {(humanAnimator != null ? humanAnimator.name : "NULL ← assignez dans l'Inspecteur")}" +
                $"\n  Wolf Animator  : {(wolfAnimator  != null ? wolfAnimator.name  : "NULL ← assignez dans l'Inspecteur")}" +
                $"\n  Human Mesh     : {(humanMesh != null ? humanMesh.name : "NULL")}" +
                $"\n  Wolf Mesh      : {(wolfMesh  != null ? wolfMesh.name  : "NULL")}");
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

            // Transition FOV Chasseur
            if (playerCamera != null)
            {
                float targetFOV = _isAiming ? aimFOV : normalFOV;
                playerCamera.fieldOfView = Mathf.Lerp(
                    playerCamera.fieldOfView, targetFOV,
                    fovTransitionSpeed * Time.deltaTime);
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Auto-détection
        // ═════════════════════════════════════════════════════════════════════════

        private void TryAutoFindMeshes()
        {
            if (humanMesh == null)
            {
                var t = FindChildByName(transform, "HumanMesh");
                if (t == null) t = FindChildByName(transform, "HumanFBX");
                if (t != null) humanMesh = t.gameObject;
            }

            if (wolfMesh == null)
            {
                var t = FindChildByName(transform, "WolfMesh");
                if (t == null) t = FindChildByName(transform, "WolfFBX");
                if (t != null) wolfMesh = t.gameObject;
            }
        }

        private void TryAutoFindAnimators()
        {
            // Cherche les Animators sur les meshes s'ils ne sont pas assignés
            if (humanAnimator == null && humanMesh != null)
                humanAnimator = humanMesh.GetComponentInChildren<Animator>();

            if (wolfAnimator == null && wolfMesh != null)
                wolfAnimator = wolfMesh.GetComponentInChildren<Animator>();

            // S'assure que la root motion est désactivée sur les deux
            if (humanAnimator != null) humanAnimator.applyRootMotion = false;
            if (wolfAnimator  != null) wolfAnimator.applyRootMotion  = false;
        }

        private static Transform FindChildByName(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                var found = FindChildByName(child, childName);
                if (found != null) return found;
            }
            return null;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Chasseur – Visée
        // ═════════════════════════════════════════════════════════════════════════

        private void HandleChasseurInputs()
        {
            if (Input.GetMouseButtonDown(1))    EnterAim();
            else if (Input.GetMouseButtonUp(1)) ExitAim();
        }

        private void EnterAim()
        {
            if (_isAiming) return;
            _isAiming = true;
            _activeAnimator?.SetBool(IsAimingParam, true);
            Debug.Log("[RoleAbilityController] Chasseur : visée ON.");
        }

        private void ExitAim()
        {
            if (!_isAiming) return;
            _isAiming = false;
            _activeAnimator?.SetBool(IsAimingParam, false);
            Debug.Log("[RoleAbilityController] Chasseur : visée OFF.");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Loup – Transformation + Attaque
        // ═════════════════════════════════════════════════════════════════════════

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

            // ── 1. Swap meshes ────────────────────────────────────────────────────
            if (humanMesh != null)
            {
                humanMesh.SetActive(!_isWolfForm);
                Debug.Log($"[RoleAbilityController] HumanMesh '{humanMesh.name}' → {!_isWolfForm}");
            }
            else Debug.LogWarning("[RoleAbilityController] HumanMesh non assigné !");

            if (wolfMesh != null)
            {
                wolfMesh.SetActive(_isWolfForm);
                Debug.Log($"[RoleAbilityController] WolfMesh '{wolfMesh.name}' → {_isWolfForm}");
            }
            else Debug.LogWarning("[RoleAbilityController] WolfMesh non assigné !");

            // ── 2. Switcher l'Animator actif ─────────────────────────────────────
            // Chaque FBX a son propre Animator déjà configuré avec son controller.
            // On indique juste à PlayerController lequel utiliser désormais.
            _activeAnimator = _isWolfForm ? wolfAnimator : humanAnimator;

            if (_activeAnimator == null)
            {
                Debug.LogWarning($"[RoleAbilityController] {(_isWolfForm ? "Wolf" : "Human")}Animator non assigné ! " +
                    "Assignez-le dans l'Inspecteur de RoleAbilityController.");
                return;
            }

            _activeAnimator.applyRootMotion = false;

            var playerCtrl = GetComponent<PlayerController>();
            playerCtrl?.SetAnimator(_activeAnimator);

            Debug.Log($"[RoleAbilityController] Transformation → {(_isWolfForm ? "LOUP" : "HUMAIN")}" +
                $" | Animator actif : {_activeAnimator.name}" +
                $" | Controller : {_activeAnimator.runtimeAnimatorController?.name}");
        }

        private void WolfAttack()
        {
            _attackCooldown = AttackCooldownTime;
            _activeAnimator?.SetTrigger(WolfAttackParam);
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
                    Debug.Log($"[RoleAbilityController] Touché '{hit.collider.name}' : -{wolfAttackDamage} PV");
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Callback rôle
        // ═════════════════════════════════════════════════════════════════════════

        private void OnRoleChanged(RoleData role)
        {
            if (role == null) return;

            _role              = role.RoleType;
            _roleBaseSpeedMult = role.SpeedMultiplier > 0f ? role.SpeedMultiplier : 1f;

            if (_isAiming)   ExitAim();
            if (_isWolfForm) ToggleWolfForm();

            Debug.Log($"[RoleAbilityController] ✔ Rôle : {_role} | vitesse ×{_roleBaseSpeedMult}");
        }
    }
}
