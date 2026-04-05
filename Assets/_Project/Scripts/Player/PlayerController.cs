using System.Collections;
using UnityEngine;
using ProjectFPS.Inventory;
using ProjectFPS.UI;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Contrôleur FPS complet — déplacement, regard, saut, roulade, accroupissement.
    ///
    /// ═══ PARAMÈTRES ANIMATOR (à créer dans Unity) ═══════════════════════════════
    ///
    ///   Float   MoveX       Input horizontal mis à l'échelle (-1..1)
    ///   Float   MoveY       Input vertical mis à l'échelle (-1..1)
    ///                         walk = ×0.3  →  walk positions (0.3) du Blend Tree
    ///                         run  = ×1.0  →  run  positions (1.0) du Blend Tree
    ///   Bool    IsGrounded  Vrai si le joueur touche le sol
    ///   Bool    IsFalling   Vrai si en chute (vertical velocity < -1)
    ///   Bool    IsCrouching Vrai si accroupi
    ///   Trigger JumpStart   Déclenche l'animation de saut
    ///   Trigger Roll        Déclenche la roulade
    ///   Float   RollDirX    Direction roulade X (-1/0/1) — régler AVANT le trigger
    ///   Float   RollDirY    Direction roulade Y (-1/0/1)
    ///   Trigger Hit         Déclenche l'animation de coup reçu
    ///   Bool    IsDead      Vrai si mort (état terminal)
    ///
    /// ═══ LAYERS ANIMATOR (optionnels mais recommandés) ═══════════════════════════
    ///
    ///   Layer 0 : Base      Blend Tree locomotion (existant)
    ///   Layer 1 : Crouch    Blend Tree accroupi   (même params MoveX/MoveY)
    ///                         → weight piloté par IsCrouching (lerp 0→1 en code)
    ///   Layer 2 : UpperBody Hit reaction (masque Avatar = Spine only)
    ///   Layer 3 : FullBody  Jump SSM + Roll SSM + Death (override total)
    ///
    /// ═══ SAUT ════════════════════════════════════════════════════════════════════
    ///   [Espace] → JumpStart trigger → gravité physique dans CharacterController
    ///   Coyote time (0.15 s) = tolérance en bord de plateforme
    ///
    /// ═══ ROULADE ═════════════════════════════════════════════════════════════════
    ///   [Alt gauche] → roulade dans la direction du mouvement (avant si immobile)
    ///   Bloque les inputs de déplacement pendant la durée de la roulade.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // ─── Références ───────────────────────────────────────────────────────────
        [Header("Références")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Animator  animator;

        // ─── Vitesses ─────────────────────────────────────────────────────────────
        [Header("Vitesses")]
        [SerializeField] private float walkSpeed   = 3f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private float rollSpeed   = 7f;

        // ─── Souris ───────────────────────────────────────────────────────────────
        [Header("Souris")]
        [SerializeField] private float mouseSensitivity = 2f;

        // ─── Caméra FPS ───────────────────────────────────────────────────────────
        [Header("Caméra (FPS)")]
        [SerializeField] private float cameraEyeHeight       = 1.7f;
        [SerializeField] private float cameraEyeHeightCrouch = 0.85f;
        [SerializeField] private float cameraForwardOffset   = 0.12f;

        // ─── Accroupissement ──────────────────────────────────────────────────────
        [Header("Crouch")]
        [SerializeField] private float standHeight           = 2f;
        [SerializeField] private float crouchHeight          = 1f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        // ─── Saut ─────────────────────────────────────────────────────────────────
        [Header("Saut")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity    = -9.81f;
        [Tooltip("Durée (s) pendant laquelle le saut est encore possible après avoir quitté un bord.")]
        [SerializeField] private float coyoteTime = 0.15f;

        // ─── Roulade ──────────────────────────────────────────────────────────────
        [Header("Roulade")]
        [SerializeField] private float   rollDuration = 0.55f;
        [SerializeField] private KeyCode rollKey      = KeyCode.LeftAlt;

        // ─── Animation Smoothing ──────────────────────────────────────────────────
        [Header("Animation")]
        [Tooltip("Temps de lissage SmoothDamp pour MoveX/MoveY (s). 0.05–0.15 recommandé.")]
        [SerializeField] private float animSmoothTime = 0.08f;

        // ─── Debug ────────────────────────────────────────────────────────────────
        [Header("Debug")]
        [Tooltip("Logue les paramètres Animator dans la Console à chaque changement.")]
        [SerializeField] private bool logAnimParams = false;

        // ─── Composants ───────────────────────────────────────────────────────────
        private CharacterController   _cc;
        private EffectSystem          _effectSystem;
        private RoleAbilityController _roleAbility;
        private PlayerState           _playerState;

        // ─── État ─────────────────────────────────────────────────────────────────
        private float _verticalVelocity;
        private float _cameraPitch;
        private bool  _isCrouching;
        private bool  _isGrounded;
        private bool  _isDead;
        private bool  _isRolling;
        private float _coyoteTimer;

        // ─── SmoothDamp pour MoveX/MoveY ─────────────────────────────────────────
        private float _animMoveX;
        private float _animMoveY;
        private float _velX;
        private float _velY;

        // ─── Layer indices (trouvés en Start) ────────────────────────────────────
        private int _layerCrouch    = -1;  // "Crouch"
        private int _layerUpperBody = -1;  // "UpperBody"
        private int _layerFullBody  = -1;  // "FullBody"

        // ─── Hash des paramètres Animator ────────────────────────────────────────
        private static readonly int MoveXParam      = Animator.StringToHash("MoveX");
        private static readonly int MoveYParam      = Animator.StringToHash("MoveY");
        private static readonly int IsGroundedParam  = Animator.StringToHash("IsGrounded");
        private static readonly int IsFallingParam   = Animator.StringToHash("IsFalling");
        private static readonly int IsCrouchingParam = Animator.StringToHash("IsCrouching");
        private static readonly int JumpStartParam   = Animator.StringToHash("JumpStart");
        private static readonly int RollParam        = Animator.StringToHash("Roll");
        private static readonly int RollDirXParam    = Animator.StringToHash("RollDirX");
        private static readonly int RollDirYParam    = Animator.StringToHash("RollDirY");
        private static readonly int HitParam         = Animator.StringToHash("Hit");
        private static readonly int IsDeadParam      = Animator.StringToHash("IsDead");

        // ═════════════════════════════════════════════════════════════════════════
        // Lifecycle
        // ═════════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            _cc           = GetComponent<CharacterController>();
            _effectSystem = GetComponent<EffectSystem>();
            _roleAbility  = GetComponent<RoleAbilityController>();
            _playerState  = GetComponent<PlayerState>();

            // Root motion désactivé pour éviter le glissement du modèle
            if (animator != null)
                animator.applyRootMotion = false;

            // CameraHolder : position initiale
            if (cameraHolder != null)
            {
                Vector3 lp = cameraHolder.localPosition;
                if (Mathf.Approximately(lp.y, 0f)) lp.y = cameraEyeHeight;
                if (lp.z < cameraForwardOffset)     lp.z = cameraForwardOffset;
                cameraHolder.localPosition = lp;
            }
            else
            {
                Debug.LogError("[PlayerController] CameraHolder non assigné dans l'Inspecteur !");
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            // Abonnement aux événements PlayerState
            if (_playerState != null)
            {
                _playerState.OnDamageReceived += OnDamageReceived;
                _playerState.OnDeath          += OnDied;
            }

            // Trouver les indices de layers (optionnels)
            if (animator != null)
            {
                _layerCrouch    = animator.GetLayerIndex("Crouch");
                _layerUpperBody = animator.GetLayerIndex("UpperBody");
                _layerFullBody  = animator.GetLayerIndex("FullBody");
            }

            Debug.Log("[PlayerController] Démarré :" +
                $"\n  CameraHolder  : {(cameraHolder != null ? cameraHolder.name + " " + cameraHolder.localPosition : "NULL !")}" +
                $"\n  Animator      : {(animator != null ? animator.name : "non assigné")}" +
                $"\n  Layers        : Crouch={_layerCrouch} | UpperBody={_layerUpperBody} | FullBody={_layerFullBody}" +
                $"\n  Walk/Sprint   : {walkSpeed}/{sprintSpeed} m/s | Roll : {rollSpeed} m/s");
        }

        private void OnDestroy()
        {
            if (_playerState != null)
            {
                _playerState.OnDamageReceived -= OnDamageReceived;
                _playerState.OnDeath          -= OnDied;
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Update
        // ═════════════════════════════════════════════════════════════════════════

        private void Update()
        {
            HandleCursorLock();

            if (_isDead) return;
            if (RoleSelectionUI.IsOpen) return;

            HandleMouseLook();
            ComputeJumpVelocity();   // calcule _verticalVelocity, PAS de Move ici

            if (_isRolling)
            {
                // La roulade gère son propre Move dans la coroutine
                _isGrounded = _cc.isGrounded;
                UpdateAnimatorState();
                return;
            }

            HandleMovement();        // UN SEUL _cc.Move (horizontal + vertical fusionnés)
            HandleCrouch();
            HandleRollInput();

            // isGrounded lu APRÈS le Move pour être fiable
            _isGrounded = _cc.isGrounded;
            UpdateAnimatorState();
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Curseur
        // ═════════════════════════════════════════════════════════════════════════

        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !RoleSelectionUI.IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else if (Input.GetMouseButtonDown(0)
                     && !RoleSelectionUI.IsOpen
                     && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Regard souris
        // ═════════════════════════════════════════════════════════════════════════

        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up, mouseX);

            _cameraPitch -= mouseY;
            _cameraPitch  = Mathf.Clamp(_cameraPitch, -85f, 85f);

            if (cameraHolder != null)
                cameraHolder.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Calcul de la vélocité verticale (PAS de _cc.Move ici)
        // Le Move est fait dans HandleMovement() avec un seul appel fusionné.
        // ═════════════════════════════════════════════════════════════════════════

        private void ComputeJumpVelocity()
        {
            // Coller au sol (basé sur l'état de la frame précédente)
            if (_isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            // Coyote time
            if (_isGrounded)
                _coyoteTimer = coyoteTime;
            else
                _coyoteTimer -= Time.deltaTime;

            // Input saut
            bool canJump = _coyoteTimer > 0f && !_isCrouching && !_isRolling;
            if (Input.GetKeyDown(KeyCode.Space) && canJump)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _coyoteTimer      = 0f;

                if (animator != null)
                    animator.SetTrigger(JumpStartParam);

                if (logAnimParams)
                    Debug.Log("[Anim] JumpStart trigger");
            }

            // Accumule la gravité
            _verticalVelocity += gravity * Time.deltaTime;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Déplacement
        // ═════════════════════════════════════════════════════════════════════════

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical   = Input.GetAxis("Vertical");

            bool isSprinting = Input.GetKey(KeyCode.LeftShift)
                               && !_isCrouching
                               && new Vector2(horizontal, vertical).magnitude > 0.1f;

            float baseSpeed    = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            float effectMult   = _effectSystem != null ? _effectSystem.SpeedMultiplier : 1f;
            float roleMult     = _roleAbility  != null ? _roleAbility.RoleSpeedMultiplier : 1f;
            float currentSpeed = baseSpeed * effectMult * roleMult;

            Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
            moveDir = Vector3.ClampMagnitude(moveDir, 1f);

            // ── UN SEUL _cc.Move par frame — horizontal + vertical fusionnés ────
            // Essentiel : séparer les deux Move() rendait isGrounded non fiable
            // (Falling jouait trop longtemps après l'atterrissage).
            _cc.Move((moveDir * currentSpeed + Vector3.up * _verticalVelocity) * Time.deltaTime);

            // ── Mise à l'échelle animation ─────────────────────────────────────
            // walk × 0.3 = positions "Walking" du Blend Tree
            // run  × 1.0 = positions "Run"     du Blend Tree
            float speedMult    = isSprinting ? 1f : 0.3f;
            float targetMoveX  = horizontal * speedMult;
            float targetMoveY  = vertical   * speedMult;

            _animMoveX = Mathf.SmoothDamp(_animMoveX, targetMoveX, ref _velX, animSmoothTime);
            _animMoveY = Mathf.SmoothDamp(_animMoveY, targetMoveY, ref _velY, animSmoothTime);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Mise à jour Animator — appelé après le Move(), donc isGrounded est frais
        // ═════════════════════════════════════════════════════════════════════════

        private void UpdateAnimatorState()
        {
            if (animator == null) return;

            // Locomotion
            animator.SetFloat(MoveXParam, _animMoveX);
            animator.SetFloat(MoveYParam, _animMoveY);

            // Sol / Chute — lus après _cc.Move donc fiables
            bool isFalling = !_isGrounded && _verticalVelocity < -1f;
            animator.SetBool(IsGroundedParam, _isGrounded);
            animator.SetBool(IsFallingParam,  isFalling);

            // Poids du layer Crouch (lerp vers 1 si accroupi, 0 sinon)
            if (_layerCrouch >= 0)
            {
                float target  = _isCrouching ? 1f : 0f;
                float current = animator.GetLayerWeight(_layerCrouch);
                animator.SetLayerWeight(
                    _layerCrouch,
                    Mathf.Lerp(current, target, crouchTransitionSpeed * Time.deltaTime));
            }

            if (logAnimParams)
                Debug.Log($"[Anim] MoveX={_animMoveX:F2} MoveY={_animMoveY:F2}" +
                    $" | Grounded={_isGrounded} | Falling={isFalling} | Crouch={_isCrouching}");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Accroupissement
        // ═════════════════════════════════════════════════════════════════════════

        private void HandleCrouch()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
                _isCrouching = !_isCrouching;

            float targetHeight = _isCrouching ? crouchHeight : standHeight;
            _cc.height = Mathf.Lerp(_cc.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

            Vector3 center = _cc.center;
            center.y  = _cc.height / 2f;
            _cc.center = center;

            if (cameraHolder != null)
            {
                float targetEyeY = _isCrouching ? cameraEyeHeightCrouch : cameraEyeHeight;
                Vector3 lp = cameraHolder.localPosition;
                lp.y = Mathf.Lerp(lp.y, targetEyeY, crouchTransitionSpeed * Time.deltaTime);
                cameraHolder.localPosition = lp;
            }

            if (animator != null)
                animator.SetBool(IsCrouchingParam, _isCrouching);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Roulade
        // ═════════════════════════════════════════════════════════════════════════

        private void HandleRollInput()
        {
            if (!Input.GetKeyDown(rollKey)) return;
            if (!_isGrounded)               return;

            // Direction de la roulade = input courant (avant si aucun input)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f)) v = 1f;

            StartCoroutine(RollCoroutine(h, v));
        }

        private IEnumerator RollCoroutine(float dirX, float dirY)
        {
            _isRolling = true;

            // Remettre le crouching à false pendant la roulade
            bool wasCrouching = _isCrouching;
            _isCrouching = false;

            if (animator != null)
            {
                // Régler la direction AVANT le trigger
                animator.SetFloat(RollDirXParam, dirX);
                animator.SetFloat(RollDirYParam, dirY);
                animator.SetTrigger(RollParam);
            }

            if (logAnimParams)
                Debug.Log($"[Anim] Roll trigger — dir=({dirX:F0},{dirY:F0})");

            Vector3 rollDir = (transform.right * dirX + transform.forward * dirY).normalized;
            float elapsed   = 0f;

            while (elapsed < rollDuration)
            {
                // Gravité + déplacement horizontal → UN SEUL Move par frame
                _verticalVelocity += gravity * Time.deltaTime;
                if (_cc.isGrounded && _verticalVelocity < 0f)
                    _verticalVelocity = -2f;

                _cc.Move((rollDir * rollSpeed + Vector3.up * _verticalVelocity) * Time.deltaTime);
                _isGrounded = _cc.isGrounded;

                elapsed += Time.deltaTime;
                yield return null;
            }

            _isRolling   = false;
            _isCrouching = wasCrouching;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Hit / Mort — déclenchés par PlayerState
        // ═════════════════════════════════════════════════════════════════════════

        private void OnDamageReceived()
        {
            if (_isDead || animator == null) return;
            animator.SetTrigger(HitParam);

            if (logAnimParams)
                Debug.Log("[Anim] Hit trigger");
        }

        private void OnDied()
        {
            _isDead = true;

            if (animator != null)
                animator.SetBool(IsDeadParam, true);

            // Débloquer le curseur à la mort
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            Debug.Log("[PlayerController] Joueur mort — inputs bloqués.");
        }

        /// <summary>Réinitialise l'état mort (pour respawn).</summary>
        public void Respawn()
        {
            _isDead      = false;
            _isRolling   = false;
            _isCrouching = false;
            _verticalVelocity = 0f;

            if (animator != null)
                animator.SetBool(IsDeadParam, false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            Debug.Log("[PlayerController] Respawn — état réinitialisé.");
        }
    }
}
