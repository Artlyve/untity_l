using UnityEngine;

namespace ProjectFPS.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Animator animator;

        [Header("Vitesses")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float crouchSpeed = 1.5f;

        [Header("Souris")]
        [SerializeField] private float mouseSensitivity = 2f;

        [Header("Crouch")]
        [SerializeField] private float standHeight = 2f;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Gravité")]
        [SerializeField] private float gravity = -9.81f;

        private CharacterController _characterController;
        private float _verticalVelocity;
        private float _cameraPitch;
        private bool _isCrouching;

        private static readonly int SpeedParam       = Animator.StringToHash("Speed");
        private static readonly int IsCrouchingParam = Animator.StringToHash("IsCrouching");

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
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

        // Verrouillage / libération du curseur avec Escape
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

        // Rotation horizontale du joueur + rotation verticale de la caméra
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

        // Déplacement WASD + sprint + gravité custom (pas de saut)
        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical   = Input.GetAxis("Vertical");

            bool  isSprinting  = Input.GetKey(KeyCode.LeftShift) && !_isCrouching;
            float currentSpeed = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

            Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
            moveDir = Vector3.ClampMagnitude(moveDir, 1f);

            // Gravité : petite valeur négative quand au sol pour garder le contact
            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveDir * currentSpeed + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);

            // Mise à jour de l'Animator (Speed normalisé 0-1)
            if (animator != null)
            {
                float inputMag        = new Vector2(horizontal, vertical).magnitude;
                float normalizedSpeed = inputMag * (isSprinting ? 1f : 0.5f);
                animator.SetFloat(SpeedParam, normalizedSpeed);
            }
        }

        // Accroupissement avec interpolation de hauteur du CharacterController
        private void HandleCrouch()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
                _isCrouching = !_isCrouching;

            float targetHeight = _isCrouching ? crouchHeight : standHeight;
            _characterController.height = Mathf.Lerp(
                _characterController.height,
                targetHeight,
                crouchTransitionSpeed * Time.deltaTime
            );

            // Maintient le centre du CharacterController aligné avec la hauteur
            Vector3 center = _characterController.center;
            center.y = _characterController.height / 2f;
            _characterController.center = center;

            if (animator != null)
                animator.SetBool(IsCrouchingParam, _isCrouching);
        }
    }
}
