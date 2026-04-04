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
        [SerializeField] private Transform            playerTransform;
        [SerializeField] private CharacterController  characterController;
        [SerializeField] private InventorySystem      inventorySystem;

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

        private void TogglePanel()
        {
            _isVisible = !_isVisible;
            panel.SetActive(_isVisible);
        }

        private void RefreshFps()
        {
            _fpsTimer += Time.deltaTime;
            if (_fpsTimer >= FpsUpdateInterval)
            {
                _fps      = 1f / Time.unscaledDeltaTime;
                _fpsTimer = 0f;
            }
        }

        private void RefreshText()
        {
            if (debugText == null) return;

            Vector3 pos   = playerTransform     != null ? playerTransform.position           : Vector3.zero;
            float   speed = characterController != null ? characterController.velocity.magnitude : 0f;
            string  role  = RoleManager.Instance?.CurrentRole?.RoleName ?? "Aucun";

            // Utilise OccupiedSlotCount (nouvelle API, évite Items.Count inexistant)
            int     items = inventorySystem != null ? inventorySystem.OccupiedSlotCount : 0;
            int     resources = inventorySystem != null ? inventorySystem.PersonalResources : 0;
            int     globalRes = ResourceSystem.Instance?.GlobalTotal ?? 0;
            int     winThreshold = ResourceSystem.Instance?.WinThreshold ?? 0;

            debugText.text =
                $"<b>DEBUG</b>\n" +
                $"Position  : ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})\n" +
                $"Vitesse   : {speed:F2} m/s\n" +
                $"Rôle      : {role}\n" +
                $"Slots     : {items}/{(inventorySystem != null ? inventorySystem.MaxSlots : 0)}\n" +
                $"Ressources: {resources} perso | {globalRes}/{winThreshold} global\n" +
                $"FPS       : {Mathf.RoundToInt(_fps)}";
        }
    }
}
