using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectFPS.Roles;

namespace ProjectFPS.UI
{
    public class RoleSelectionUI : MonoBehaviour
    {
        [Header("Références UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform  buttonContainer;
        [SerializeField] private GameObject roleButtonPrefab;

        private bool _isOpen;

        private void Start()
        {
            panelRoot.SetActive(false);
            GenerateRoleButtons();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                TogglePanel();
        }

        // Affiche ou cache le panneau et ajuste l'état du curseur
        private void TogglePanel()
        {
            _isOpen = !_isOpen;
            panelRoot.SetActive(_isOpen);
            Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = _isOpen;
        }

        // Instancie dynamiquement un bouton par rôle disponible
        private void GenerateRoleButtons()
        {
            if (RoleManager.Instance == null) return;

            foreach (Transform child in buttonContainer)
                Destroy(child.gameObject);

            foreach (RoleData role in RoleManager.Instance.AvailableRoles)
            {
                GameObject btnObj = Instantiate(roleButtonPrefab, buttonContainer);
                SetupRoleButton(btnObj, role);
            }
        }

        // Remplit les éléments visuels d'un bouton de rôle
        // Conventions de nommage dans le prefab : "Icon" (Image), "RoleName" (TMP), "Description" (TMP)
        private void SetupRoleButton(GameObject btnObj, RoleData role)
        {
            Image icon = btnObj.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite  = role.Icon;
                icon.enabled = role.Icon != null;
            }

            TextMeshProUGUI nameText = btnObj.transform.Find("RoleName")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = role.RoleName;

            TextMeshProUGUI descText = btnObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            if (descText != null)
                descText.text = role.Description;

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                // Capture locale obligatoire pour éviter le piège de la closure dans la boucle
                RoleData captured = role;
                btn.onClick.AddListener(() => SelectRole(captured));
            }
        }

        // Applique le rôle sélectionné et ferme le panneau
        private void SelectRole(RoleData role)
        {
            RoleManager.Instance?.SetRole(role);
            _isOpen = false;
            panelRoot.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}
