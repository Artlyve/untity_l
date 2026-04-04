using UnityEngine;
using TMPro;
using ProjectFPS.Roles;
using ProjectFPS.Inventory;

namespace ProjectFPS.UI
{
    public class DebugPanel : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject      panel;
        [SerializeField] private TextMeshProUGUI debugText;

        [Header("Références joueur")]
        [SerializeField] private Transform         playerTransform;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private InventorySystem   inventorySystem;

        private bool  _isVisible;
        private float _fps;
        private float _fpsTimer;

        private const float FpsUpdateInterval = 0.5f;

        private void Start()
        {
            panel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                TogglePanel();

            if (_isVisible)
            {
                RefreshFps();
                RefreshText();
            }
        }

        // Affiche ou cache le panneau de débogage
        private void TogglePanel()
        {
            _isVisible = !_isVisible;
            panel.SetActive(_isVisible);
        }

        // Calcule le FPS moyen toutes les FpsUpdateInterval secondes
        private void RefreshFps()
        {
            _fpsTimer += Time.deltaTime;
            if (_fpsTimer >= FpsUpdateInterval)
            {
                _fps      = 1f / Time.unscaledDeltaTime;
                _fpsTimer = 0f;
            }
        }

        // Reconstruit le texte de débogage à chaque frame (tant que le panel est visible)
        private void RefreshText()
        {
            if (debugText == null) return;

            Vector3 pos   = playerTransform     != null ? playerTransform.position           : Vector3.zero;
            float   speed = characterController != null ? characterController.velocity.magnitude : 0f;
            string  role  = RoleManager.Instance?.CurrentRole?.RoleName ?? "Aucun";
            int     items = inventorySystem     != null ? inventorySystem.Items.Count         : 0;

            debugText.text =
                $"<b>DEBUG</b>\n" +
                $"Position : ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})\n" +
                $"Vitesse  : {speed:F2} m/s\n" +
                $"Rôle     : {role}\n" +
                $"Items    : {items}\n" +
                $"FPS      : {Mathf.RoundToInt(_fps)}";
        }
    }
}
