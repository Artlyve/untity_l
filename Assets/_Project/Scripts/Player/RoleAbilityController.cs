using UnityEngine;
using ProjectFPS.Roles;
using ProjectFPS.Inventory;

namespace ProjectFPS.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class RoleAbilityController : MonoBehaviour
    {
        // ─── Références ───────────────────────────────────────────────────────────
        [Header("Références")]
        [SerializeField] private Camera playerCamera;

        // ─── Chasseur : Visée ────────────────────────────────────────────────────
        [Header("Chasseur – Visée")]
        [SerializeField] private float aimFOV             = 40f;
        [SerializeField] private float normalFOV          = 70f;
        [SerializeField] private float aimSpeedPenalty    = 0.4f;
        [SerializeField] private float fovTransitionSpeed = 10f;

        // ─── Loup : Transformation ────────────────────────────────────────────────
        [Header("Loup – Transformation")]
        [SerializeField] private GameObject humanMesh;
        [SerializeField] private GameObject wolfMesh;
        [SerializeField] private float      wolfSpeedBonus   = 1.5f;
        [SerializeField] private float      wolfAttackDamage = 30f;
        [SerializeField] private float      wolfAttackRange  = 2f;
        [SerializeField] private LayerMask  wolfAttackLayer  = ~0;

        // ─── État interne ─────────────────────────────────────────────────────────
        private PlayerRole      _role              = PlayerRole.Villageois;
        private InventorySystem _inventory;
        private bool            _isAiming;
        private bool            _isWolfForm;
        private float           _attackCooldown;
        private float           _roleBaseSpeedMult = 1f;
        private const float     AttackCooldownTime = 0.8f;

        // Animator du mesh actuellement visible (humain ou loup)
        private Animator _activeAnimator;

        // ─── Hash paramètres Animator ─────────────────────────────────────────────
        private static readonly int IsAimingParam   = Animator.StringToHash("IsAiming");
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

            TryAutoFindMeshes();
        }

        private void Start()
        {
            // Animator actif au démarrage = HumanMesh
            _activeAnimator = humanMesh != null
                ? humanMesh.GetComponent<Animator>()
                : GetComponentInChildren<Animator>();

            if (_activeAnimator != null)
            {
                _activeAnimator.applyRootMotion = false;
                GetComponent<PlayerController>()?.SetAnimator(_activeAnimator);
            }

            // Désactive root motion sur WolfMesh aussi (évite drift à l'activation)
            var wolfAnim = wolfMesh != null ? wolfMesh.GetComponent<Animator>() : null;
            if (wolfAnim != null) wolfAnim.applyRootMotion = false;

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
                $"\n  Camera         : {(playerCamera  != null ? playerCamera.name  : "NULL")}" +
                $"\n  HumanMesh      : {(humanMesh     != null ? humanMesh.name     : "NULL")}" +
                $"\n  WolfMesh       : {(wolfMesh      != null ? wolfMesh.name      : "NULL")}" +
                $"\n  Animator actif : {(_activeAnimator != null ? _activeAnimator.name : "NULL")}");
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
                if (t != null) humanMesh = t.gameObject;
            }
            if (wolfMesh == null)
            {
                var t = FindChildByName(transform, "WolfMesh");
                if (t != null) wolfMesh = t.gameObject;
            }
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
        }

        private void ExitAim()
        {
            if (!_isAiming) return;
            _isAiming = false;
            _activeAnimator?.SetBool(IsAimingParam, false);
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

            // 1. Swap visibilité
            humanMesh?.SetActive(!_isWolfForm);
            wolfMesh?.SetActive(_isWolfForm);

            // 2. Récupère l'Animator du mesh maintenant actif
            //    Chaque mesh a DÉJÀ son controller assigné dans Unity → pas de swap nécessaire
            GameObject activeMesh = _isWolfForm ? wolfMesh : humanMesh;
            if (activeMesh == null)
            {
                Debug.LogWarning($"[RoleAbilityController] {(_isWolfForm ? "WolfMesh" : "HumanMesh")} non assigné !");
                return;
            }

            _activeAnimator = activeMesh.GetComponent<Animator>();
            if (_activeAnimator == null)
            {
                Debug.LogWarning($"[RoleAbilityController] Pas d'Animator sur '{activeMesh.name}' !");
                return;
            }

            _activeAnimator.applyRootMotion = false;
            GetComponent<PlayerController>()?.SetAnimator(_activeAnimator);

            Debug.Log($"[RoleAbilityController] → {(_isWolfForm ? "LOUP" : "HUMAIN")}" +
                $" | Animator : {_activeAnimator.name}" +
                $" | Controller : {_activeAnimator.runtimeAnimatorController?.name}");
        }

        private void WolfAttack()
        {
            _attackCooldown = AttackCooldownTime;

            // Envoie le trigger à l'Animator ACTIF (WolfMesh), pas à l'ancien HumanMesh
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

            Debug.Log($"[RoleAbilityController] ✔ Rôle : {_role}" +
                $" | vitesse ×{_roleBaseSpeedMult}" +
                $" | slots = {role.InventorySlots}");
        }
    }
}
