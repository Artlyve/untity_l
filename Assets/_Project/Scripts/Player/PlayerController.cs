using UnityEngine;
using ProjectFPS.Inventory;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Contrôle FPS : déplacement, regard souris, accroupissement.
    ///
    /// BUGS CORRIGÉS :
    ///  1. applyRootMotion désactivé en Awake → le modèle ne dérive plus devant la caméra.
    ///  2. CameraHolder auto-positionné à hauteur des yeux si son Z local = 0
    ///     → évite de voir l'intérieur de la tête au démarrage.
    ///     Ajustez cameraEyeHeight et cameraForwardOffset dans l'Inspecteur si besoin.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Animator  animator;

        [Header("Vitesses")]
        [SerializeField] private float walkSpeed   = 3f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float crouchSpeed = 1.5f;

        [Header("Souris")]
        [SerializeField] private float mouseSensitivity = 2f;

        [Header("Crouch")]
        [SerializeField] private float standHeight         = 2f;
        [SerializeField] private float crouchHeight        = 1f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Gravité")]
        [SerializeField] private float gravity = -9.81f;

        [Header("Positionnement caméra (FPS)")]
        [Tooltip("Hauteur des yeux en position debout (relative à la base du CharacterController).")]
        [SerializeField] private float cameraEyeHeight    = 1.7f;
        [Tooltip("Hauteur des yeux en position accroupie.")]
        [SerializeField] private float cameraEyeHeightCrouch = 0.85f;
        [Tooltip("Décalage avant de la caméra pour éviter de clipper dans le mesh de la tête.")]
        [SerializeField] private float cameraForwardOffset = 0.12f;

        private CharacterController   _cc;
        private EffectSystem          _effectSystem;
        private RoleAbilityController _roleAbility;
        private float _verticalVelocity;
        private float _cameraPitch;
        private bool  _isCrouching;

        private static readonly int SpeedParam       = Animator.StringToHash("Speed");
        private static readonly int IsCrouchingParam = Animator.StringToHash("IsCrouching");

        private void Awake()
        {
            _cc           = GetComponent<CharacterController>();
            _effectSystem = GetComponent<EffectSystem>();
            _roleAbility  = GetComponent<RoleAbilityController>();

            // ── FIX 1 : désactiver la Root Motion pour éviter que le modèle
            //            glisse indépendamment de la caméra.
            if (animator != null)
                animator.applyRootMotion = false;

            // ── FIX 2 : positionner le CameraHolder à hauteur des yeux.
            //    On ne touche à la position que si le CameraHolder est encore à (0,0,0)
            //    ou si son Z local est nul (caméra dans la tête).
            if (cameraHolder != null)
            {
                Vector3 lp = cameraHolder.localPosition;

                bool needsEyeHeight = Mathf.Approximately(lp.y, 0f);
                bool needsForward   = lp.z < cameraForwardOffset;

                if (needsEyeHeight) lp.y = cameraEyeHeight;
                if (needsForward)   lp.z = cameraForwardOffset;

                cameraHolder.localPosition = lp;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void Update()
        {
            HandleCursorLock();
            HandleMouseLook();
            HandleMovement();
            HandleCrouch();
        }

        // ── Curseur ───────────────────────────────────────────────────────────────
        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        // ── Regard ────────────────────────────────────────────────────────────────
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

        // ── Déplacement ───────────────────────────────────────────────────────────
        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical   = Input.GetAxis("Vertical");

            bool  isSprinting  = Input.GetKey(KeyCode.LeftShift) && !_isCrouching;
            float baseSpeed    = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            float effectMult   = _effectSystem != null ? _effectSystem.SpeedMultiplier : 1f;
            float roleMult     = _roleAbility  != null ? _roleAbility.RoleSpeedMultiplier : 1f;
            float currentSpeed = baseSpeed * effectMult * roleMult;

            Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
            moveDir = Vector3.ClampMagnitude(moveDir, 1f);

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            _verticalVelocity += gravity * Time.deltaTime;

            _cc.Move((moveDir * currentSpeed + Vector3.up * _verticalVelocity) * Time.deltaTime);

            if (animator != null)
            {
                float inputMag        = new Vector2(horizontal, vertical).magnitude;
                float normalizedSpeed = inputMag * (isSprinting ? 1f : 0.5f);
                animator.SetFloat(SpeedParam, normalizedSpeed);
            }
        }

        // ── Accroupissement ───────────────────────────────────────────────────────
        private void HandleCrouch()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
                _isCrouching = !_isCrouching;

            // Hauteur du CharacterController
            float targetHeight = _isCrouching ? crouchHeight : standHeight;
            _cc.height = Mathf.Lerp(_cc.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

            Vector3 center = _cc.center;
            center.y  = _cc.height / 2f;
            _cc.center = center;

            // Caméra suit la hauteur des yeux en temps réel (fluide)
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
    }
}
