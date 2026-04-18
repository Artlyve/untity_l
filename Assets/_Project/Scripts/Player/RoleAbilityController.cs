using UnityEngine;
using FishNet.Object;
using ProjectFPS.Roles;
using ProjectFPS.Inventory;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Gère les capacités spéciales par rôle + la transformation Loup.
    ///
    /// ═══ RÔLES ════════════════════════════════════════════════════════════════
    ///   Chasseur / Fils_Chasseur  → [Clic droit] Mode visée (réduit FOV + vitesse)
    ///   Loup                      → [R] Transformation humain ↔ loup
    ///                               [Clic gauche] Attaque en corps-à-corps (forme loup)
    ///
    /// ═══ MESHES LOUP ══════════════════════════════════════════════════════════
    ///   Option A (recommandée) : nommez les enfants "HumanMesh" et "WolfMesh"
    ///   Option B              : assignez-les dans l'Inspecteur
    ///
    /// ═══ ANIMATOR CONTROLLERS LOUP ════════════════════════════════════════════
    ///   Créez deux Animator Controllers dans Assets/_Project/Models/Anim/ :
    ///     • HumanAnimController.controller  → même que le controller existant
    ///     • WolfAnimController.controller   → animations loup (locomotion + attaque)
    ///   Assignez-les dans l'Inspecteur (champs "Human Anim Controller" / "Wolf Anim Controller").
    ///   Si non assignés → seuls les meshes sont swappés, pas l'animator.
    ///
    /// ═══ PARAMÈTRE ANIMATOR (WolfAnimController) ══════════════════════════════
    ///   Trigger  WolfAttack  → déclenche l'animation d'attaque loup
    ///   (Tous les autres params : MoveX, MoveY, IsGrounded, etc. → identiques)
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class RoleAbilityController : MonoBehaviour
    {
        // ─── Références ───────────────────────────────────────────────────────────
        [Header("Références")]
        [SerializeField] private Camera   playerCamera;
        [SerializeField] private Animator animator;

        // ─── Animator Controllers (Human vs Loup) ────────────────────────────────
        [Header("Animator Controllers Loup")]
        [Tooltip("Controller humain — laissez vide pour conserver celui déjà assigné sur l'Animator.")]
        [SerializeField] private RuntimeAnimatorController humanAnimController;
        [Tooltip("Controller loup — assignez ici votre WolfAnimController.controller.")]
        [SerializeField] private RuntimeAnimatorController wolfAnimController;

        // ─── Chasseur : Visée ────────────────────────────────────────────────────
        [Header("Chasseur – Visée")]
        [SerializeField] private float aimFOV             = 40f;
        [SerializeField] private float normalFOV          = 70f;
        [SerializeField] private float aimSpeedPenalty    = 0.4f;
        [SerializeField] private float fovTransitionSpeed = 10f;

        // ─── Loup : Transformation ────────────────────────────────────────────────
        [Header("Loup – Transformation")]
        [Tooltip("Enfant nommé 'HumanMesh' trouvé automatiquement si non assigné.")]
        [SerializeField] private GameObject humanMesh;
        [Tooltip("Enfant nommé 'WolfMesh' trouvé automatiquement si non assigné.")]
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

        // ─── Hash paramètres Animator ─────────────────────────────────────────────
        private static readonly int IsAimingParam     = Animator.StringToHash("IsAiming");
        private static readonly int IsWolfFormParam   = Animator.StringToHash("IsWolfForm");
        private static readonly int WolfAttackParam   = Animator.StringToHash("WolfAttack");

        // Vérifie le propriétaire via NetworkObject (true si pas en réseau = tests locaux)
        private bool IsNetworkOwner
        {
            get { var no = GetComponent<NetworkObject>(); return no == null || no.IsOwner; }
        }

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

            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (playerCamera != null)
                normalFOV = playerCamera.fieldOfView;

            // Mémoriser le controller humain actuel si non assigné
            if (humanAnimController == null && animator != null)
                humanAnimController = animator.runtimeAnimatorController;

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
                Debug.LogError("[RoleAbilityController] RoleManager.Instance est NULL !");
            }

            Debug.Log("[RoleAbilityController] Démarré :" +
                $"\n  Rôle             : {_role}" +
                $"\n  Camera           : {(playerCamera != null ? playerCamera.name : "NULL !")}" +
                $"\n  Animator         : {(animator != null ? animator.name : "non assigné")}" +
                $"\n  Human Controller : {(humanAnimController != null ? humanAnimController.name : "NULL ← auto-capturé au démarrage")}" +
                $"\n  Wolf Controller  : {(wolfAnimController  != null ? wolfAnimController.name  : "NULL ← assignez dans l'Inspecteur")}" +
                $"\n  Human Mesh       : {(humanMesh != null ? humanMesh.name : "NULL ← enfant 'HumanMesh' non trouvé")}" +
                $"\n  Wolf Mesh        : {(wolfMesh  != null ? wolfMesh.name  : "NULL ← enfant 'WolfMesh' non trouvé")}");
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
            // Seul le propriétaire traite les inputs
            if (!IsNetworkOwner) return;

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

            // Transition FOV (Chasseur / Fils_Chasseur)
            if (playerCamera != null)
            {
                float targetFOV = _isAiming ? aimFOV : normalFOV;
                playerCamera.fieldOfView = Mathf.Lerp(
                    playerCamera.fieldOfView, targetFOV,
                    fovTransitionSpeed * Time.deltaTime);
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Auto-détection des meshes
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
            if (Input.GetMouseButtonDown(1))      EnterAim();
            else if (Input.GetMouseButtonUp(1))   ExitAim();
        }

        private void EnterAim()
        {
            if (_isAiming) return;
            _isAiming = true;
            if (animator != null) animator.SetBool(IsAimingParam, true);
            Debug.Log("[RoleAbilityController] Chasseur : visée ON.");
        }

        private void ExitAim()
        {
            if (!_isAiming) return;
            _isAiming = false;
            if (animator != null) animator.SetBool(IsAimingParam, false);
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

            // ── Swap meshes ───────────────────────────────────────────────────────
            if (humanMesh != null)
            {
                humanMesh.SetActive(!_isWolfForm);
                Debug.Log($"[RoleAbilityController] HumanMesh '{humanMesh.name}' → activé={!_isWolfForm}");
            }
            else
                Debug.LogWarning("[RoleAbilityController] HumanMesh NULL — " +
                    "nommez l'enfant 'HumanMesh' ou assignez dans l'Inspecteur.");

            if (wolfMesh != null)
            {
                wolfMesh.SetActive(_isWolfForm);
                Debug.Log($"[RoleAbilityController] WolfMesh '{wolfMesh.name}' → activé={_isWolfForm}");
            }
            else
                Debug.LogWarning("[RoleAbilityController] WolfMesh NULL — " +
                    "nommez l'enfant 'WolfMesh' ou assignez dans l'Inspecteur.");

            // ── Swap Animator Controller ──────────────────────────────────────────
            if (animator != null)
            {
                RuntimeAnimatorController target =
                    _isWolfForm ? wolfAnimController : humanAnimController;

                if (target != null)
                {
                    animator.runtimeAnimatorController = target;
                    animator.applyRootMotion = false;
                    Debug.Log($"[RoleAbilityController] Controller swappé → {target.name}");

                    // Synchronise la référence Animator de PlayerController avec celle-ci.
                    // ESSENTIEL : si PlayerController.animator et RoleAbilityController.animator
                    // pointaient vers deux objets différents, PlayerController envoyait ses
                    // paramètres (MoveX/Y etc.) à l'ancien Animator humain → loup sans animation.
                    var playerCtrl = GetComponent<PlayerController>();
                    if (playerCtrl != null)
                        playerCtrl.SetAnimator(animator); // SetAnimator appelle RefreshAnimatorSetup en interne

                    // Réappliquer IsWolfForm (controller reset = params remis à 0)
                    RebuildValidParams();
                    if (_validParams.Contains(IsWolfFormParam))
                        animator.SetBool(IsWolfFormParam, _isWolfForm);
                }
                else
                {
                    if (_isWolfForm)
                        Debug.LogWarning("[RoleAbilityController] WolfAnimController non assigné — " +
                            "assignez-le dans l'Inspecteur de RoleAbilityController.");
                }
            }

            Debug.Log($"[RoleAbilityController] Transformation → {(_isWolfForm ? "LOUP" : "HUMAIN")}");
        }

        // HashSet des params valides du controller actuel (sync avec PlayerController)
        private readonly System.Collections.Generic.HashSet<int> _validParams
            = new System.Collections.Generic.HashSet<int>();

        private void RebuildValidParams()
        {
            _validParams.Clear();
            if (animator == null) return;
            foreach (var p in animator.parameters)
                _validParams.Add(p.nameHash);
        }

        private void WolfAttack()
        {
            _attackCooldown = AttackCooldownTime;

            if (animator != null)
                animator.SetTrigger(WolfAttackParam);

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
            if (role == null)
            {
                Debug.LogWarning("[RoleAbilityController] OnRoleChanged — role est null.");
                return;
            }

            _role              = role.RoleType;
            _roleBaseSpeedMult = role.SpeedMultiplier > 0f ? role.SpeedMultiplier : 1f;

            if (_isAiming)   ExitAim();
            if (_isWolfForm) ToggleWolfForm(); // repasse en forme humaine au changement de rôle

            Debug.Log($"[RoleAbilityController] ✔ Rôle : {_role}" +
                $" | vitesse ×{_roleBaseSpeedMult}" +
                $" | slots = {role.InventorySlots}");

            switch (_role)
            {
                case PlayerRole.Loup:
                    Debug.Log("[RoleAbilityController] → [R] transformer | [LMB] attaque loup");
                    break;
                case PlayerRole.Chasseur:
                    Debug.Log("[RoleAbilityController] → [RMB] viser");
                    break;
                case PlayerRole.Fils_Chasseur:
                    Debug.Log($"[RoleAbilityController] → {role.InventorySlots} slots");
                    break;
            }
        }
    }
}
