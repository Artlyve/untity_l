using UnityEngine;
using ProjectFPS.Inventory;
using ProjectFPS.UI;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Contrôle FPS : déplacement, regard souris, accroupissement.
    ///
    /// Paramètres Animator attendus :
    ///   Speed       (float) — magnitude d'input normalisée 0..1
    ///   IsRunning   (bool)  — vrai si sprint actif
    ///   IsCrouching (bool)  — vrai si accroupi
    ///   MoveX       (float) — input horizontal -1..1 (pour blend tree directionnel)
    ///   MoveY       (float) — input vertical   -1..1
    ///
    /// BUGS CORRIGÉS :
    ///  1. applyRootMotion désactivé en Awake → le modèle ne dérive plus.
    ///  2. CameraHolder auto-positionné à hauteur des yeux si position = 0.
    ///  3. normalizedSpeed utilisait ×0.5 en marche → déclenchait la course.
    ///     Maintenant : marche = inputMag (0..1), sprint = bool IsRunning séparé.
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
        [SerializeField] private float standHeight           = 2f;
        [SerializeField] private float crouchHeight          = 1f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Gravité")]
        [SerializeField] private float gravity = -9.81f;

        [Header("Positionnement caméra (FPS)")]
        [Tooltip("Hauteur des yeux en position debout (relative à la base du CharacterController).")]
        [SerializeField] private float cameraEyeHeight       = 1.7f;
        [Tooltip("Hauteur des yeux en position accroupie.")]
        [SerializeField] private float cameraEyeHeightCrouch = 0.85f;
        [Tooltip("Décalage avant de la caméra pour éviter de clipper dans le mesh de la tête.")]
        [SerializeField] private float cameraForwardOffset   = 0.12f;

        // ── Composants ────────────────────────────────────────────────────────────
        private CharacterController   _cc;
        private EffectSystem          _effectSystem;
        private RoleAbilityController _roleAbility;

        // ── État ──────────────────────────────────────────────────────────────────
        private float _verticalVelocity;
        private float _cameraPitch;
        private bool  _isCrouching;

        // ── Paramètres Animator ───────────────────────────────────────────────────
        private static readonly int SpeedParam       = Animator.StringToHash("Speed");
        private static readonly int IsRunningParam   = Animator.StringToHash("IsRunning");
        private static readonly int IsCrouchingParam = Animator.StringToHash("IsCrouching");
        private static readonly int MoveXParam       = Animator.StringToHash("MoveX");
        private static readonly int MoveYParam       = Animator.StringToHash("MoveY");

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _cc           = GetComponent<CharacterController>();
            _effectSystem = GetComponent<EffectSystem>();
            _roleAbility  = GetComponent<RoleAbilityController>();

            // Désactiver la Root Motion pour éviter que le modèle glisse seul.
            if (animator != null)
                animator.applyRootMotion = false;

            // Positionner le CameraHolder à hauteur des yeux si pas encore configuré.
            if (cameraHolder != null)
            {
                Vector3 lp = cameraHolder.localPosition;

                if (Mathf.Approximately(lp.y, 0f)) lp.y = cameraEyeHeight;
                if (lp.z < cameraForwardOffset)     lp.z = cameraForwardOffset;

                cameraHolder.localPosition = lp;
            }
            else
            {
                Debug.LogError("[PlayerController] CameraHolder est NULL ! " +
                    "Assignez le Transform CameraHolder dans l'Inspecteur.");
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            Debug.Log("[PlayerController] Démarré :" +
                $"\n  CameraHolder   : {(cameraHolder != null ? cameraHolder.name + " pos=" + cameraHolder.localPosition : "NULL !")}" +
                $"\n  Animator       : {(animator != null ? animator.name : "non assigné")}" +
                $"\n  walkSpeed      : {walkSpeed} | sprintSpeed : {sprintSpeed}");
        }

        private void Update()
        {
            HandleCursorLock();

            // Bloque mouvement + visée quand le menu de classes est ouvert
            if (RoleSelectionUI.IsOpen) return;

            HandleMouseLook();
            HandleMovement();
            HandleCrouch();
        }

        // ── Curseur ───────────────────────────────────────────────────────────────
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

        // ── Regard ────────────────────────────────────────────────────────────────
        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Rotation horizontale → tourne le joueur entier (FPS standard)
            transform.Rotate(Vector3.up, mouseX);

            // Rotation verticale → incline seulement la caméra
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

            // Sprint uniquement avec Shift maintenu, pas en accroupi
            bool  isSprinting  = Input.GetKey(KeyCode.LeftShift) && !_isCrouching
                                  && new Vector2(horizontal, vertical).magnitude > 0.1f;

            float baseSpeed    = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            float effectMult   = _effectSystem != null ? _effectSystem.SpeedMultiplier : 1f;
            float roleMult     = _roleAbility  != null ? _roleAbility.RoleSpeedMultiplier : 1f;
            float currentSpeed = baseSpeed * effectMult * roleMult;

            // FPS classique : strafe sans rotation du corps
            Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
            moveDir = Vector3.ClampMagnitude(moveDir, 1f);

            // Gravité
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            _cc.Move((moveDir * currentSpeed + Vector3.up * _verticalVelocity) * Time.deltaTime);

            // ── Animator ──────────────────────────────────────────────────────────
            if (animator != null)
            {
                float inputMag = new Vector2(horizontal, vertical).magnitude;

                // Speed : 0 = immobile, 1 = plein mouvement (walk ou run selon IsRunning)
                animator.SetFloat(SpeedParam,     inputMag,     0.1f, Time.deltaTime);
                animator.SetBool (IsRunningParam, isSprinting);

                // MoveX / MoveY pour blend tree directionnel (strafe / recul)
                animator.SetFloat(MoveXParam, horizontal, 0.1f, Time.deltaTime);
                animator.SetFloat(MoveYParam, vertical,   0.1f, Time.deltaTime);
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

            // Caméra suit la hauteur des yeux en temps réel
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
