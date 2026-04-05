using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectFPS.Roles;

namespace ProjectFPS.UI
{
    /// <summary>
    /// Menu de sélection de classe en jeu.
    ///
    /// CONTRÔLES :
    ///   = (Égal) → ouvre / ferme le menu
    ///
    /// SETUP SCÈNE :
    ///   IMPORTANT : ce composant doit être sur le Canvas (ou tout objet toujours actif),
    ///   PAS sur le RoleSelectionPanel. Le panel peut commencer désactivé — c'est ce script
    ///   qui l'active/désactive.
    ///
    ///   Si les champs panelRoot / buttonContainer / roleButtonPrefab ne sont pas assignés,
    ///   le menu est créé entièrement en code.
    ///
    /// CAPACITÉS PAR CLASSE :
    ///   Villageois   → Aucune capacité spéciale
    ///   Chasseur     → RMB : Viser (ralentit) | Q : Lancer
    ///   Fils_Chasseur → 2 slots d'items | 1/2 ou Molette : Changer de slot
    ///   Loup         → R : Transformation ↔ Humain/Loup | LMB (forme loup) : Attaque
    /// </summary>
    public class RoleSelectionUI : MonoBehaviour
    {
        // ── Champs Inspector ──────────────────────────────────────────────────────
        [Header("Références UI (laissez vide : auto-créé)")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform  buttonContainer;
        [SerializeField] private GameObject roleButtonPrefab;

        [Header("Apparence (si auto-créé)")]
        [SerializeField] private Color overlayColor    = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Color panelColor      = new Color(0.1f, 0.1f, 0.12f, 0.97f);
        [SerializeField] private Color buttonNormal    = new Color(0.18f, 0.18f, 0.22f, 1f);
        [SerializeField] private Color buttonSelected  = new Color(0.25f, 0.55f, 0.25f, 1f);
        [SerializeField] private Color buttonHover     = new Color(0.28f, 0.28f, 0.35f, 1f);

        // ── État ──────────────────────────────────────────────────────────────────
        private bool _isOpen;
        private readonly List<RoleButtonEntry> _buttons = new List<RoleButtonEntry>();

        /// <summary>Vrai si le menu est ouvert (utilisé par PlayerController).</summary>
        public static bool IsOpen { get; private set; }

        private struct RoleButtonEntry
        {
            public GameObject  Root;
            public RoleData    Role;
            public Image       Background;
            public Button      Button;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            EnsurePanel();
            GenerateRoleButtons();
            SetPanelVisible(false);

            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged += OnRoleChanged;

            Debug.Log("[RoleSelectionUI] Démarré — touche '=' pour ouvrir/fermer le menu de classe.");
        }

        private void OnDestroy()
        {
            if (RoleManager.Instance != null)
                RoleManager.Instance.OnRoleChanged -= OnRoleChanged;
        }

        private void Update()
        {
            // Touche "=" pour ouvrir/fermer le menu de classe
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadEquals))
            {
                Debug.Log("[RoleSelectionUI] Touche '=' détectée → toggle menu");
                TogglePanel();
            }
        }

        // ── Toggle ────────────────────────────────────────────────────────────────

        private void TogglePanel()
        {
            SetPanelVisible(!_isOpen);
        }

        private void SetPanelVisible(bool visible)
        {
            _isOpen = visible;
            IsOpen  = visible;

            if (panelRoot != null)
                panelRoot.SetActive(visible);

            Cursor.lockState = visible ? CursorLockMode.None  : CursorLockMode.Locked;
            Cursor.visible   = visible;

            Debug.Log($"[RoleSelectionUI] Menu {(visible ? "ouvert" : "fermé")}");
        }

        // ── Génération des boutons ────────────────────────────────────────────────

        private void GenerateRoleButtons()
        {
            if (RoleManager.Instance == null)
            {
                Debug.LogWarning("[RoleSelectionUI] RoleManager.Instance introuvable ! " +
                    "Ajoutez un GameObject RoleManager dans la scène.");
                return;
            }

            foreach (Transform child in buttonContainer)
                Destroy(child.gameObject);
            _buttons.Clear();

            var roles = RoleManager.Instance.AvailableRoles;

            if (roles == null || roles.Count == 0)
            {
                Debug.LogWarning("[RoleSelectionUI] RoleManager.AvailableRoles est vide ! " +
                    "Assignez des RoleData dans la liste AvailableRoles du RoleManager.");

                // Affiche un message dans le menu
                var warnGO  = new GameObject("Warning");
                warnGO.transform.SetParent(buttonContainer, false);
                var tmp     = warnGO.AddComponent<TextMeshProUGUI>();
                tmp.text    = "Aucun rôle configuré.\n\nAssignez des RoleData\ndans RoleManager.AvailableRoles.";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color   = new Color(1f, 0.7f, 0.3f);
                tmp.fontSize = 18f;
                return;
            }

            foreach (var role in roles)
            {
                if (role == null) continue;
                var entry = BuildRoleButton(role);
                _buttons.Add(entry);
            }

            // Met en surbrillance le rôle actif
            HighlightCurrentRole(RoleManager.Instance.CurrentRole);
        }

        private RoleButtonEntry BuildRoleButton(RoleData role)
        {
            GameObject btnGO;

            if (roleButtonPrefab != null)
            {
                btnGO = Instantiate(roleButtonPrefab, buttonContainer);
                SetupPrefabButton(btnGO, role);
            }
            else
            {
                btnGO = CreateDefaultButton(role);
            }

            var bg  = btnGO.GetComponent<Image>() ?? btnGO.GetComponentInChildren<Image>();
            var btn = btnGO.GetComponent<Button>();

            if (btn != null)
            {
                RoleData captured = role;
                btn.onClick.AddListener(() => SelectRole(captured));
            }

            return new RoleButtonEntry
            {
                Root       = btnGO,
                Role       = role,
                Background = bg,
                Button     = btn,
            };
        }

        // ── Bouton par défaut ─────────────────────────────────────────────────────

        private GameObject CreateDefaultButton(RoleData role)
        {
            // Conteneur principal du bouton
            var btnGO    = new GameObject($"Btn_{role.RoleName}");
            btnGO.transform.SetParent(buttonContainer, false);

            var rt       = btnGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440f, 90f);

            var bg       = btnGO.AddComponent<Image>();
            bg.color     = buttonNormal;

            var btn      = btnGO.AddComponent<Button>();
            var colors   = btn.colors;
            colors.normalColor      = buttonNormal;
            colors.highlightedColor = buttonHover;
            colors.selectedColor    = buttonSelected;
            btn.colors   = colors;
            btn.targetGraphic = bg;

            // Colonne gauche : icône rôle
            if (role.Icon != null)
            {
                var iconGO  = new GameObject("Icon");
                iconGO.transform.SetParent(btnGO.transform, false);
                var iconRT  = iconGO.AddComponent<RectTransform>();
                iconRT.anchorMin        = new Vector2(0f, 0f);
                iconRT.anchorMax        = new Vector2(0f, 1f);
                iconRT.sizeDelta        = new Vector2(70f, 0f);
                iconRT.anchoredPosition = new Vector2(35f, 0f);
                var iconImg = iconGO.AddComponent<Image>();
                iconImg.sprite         = role.Icon;
                iconImg.preserveAspect = true;
            }

            // Colonne droite : textes
            var textZone = new GameObject("TextZone");
            textZone.transform.SetParent(btnGO.transform, false);
            var tzRT     = textZone.AddComponent<RectTransform>();
            tzRT.anchorMin        = new Vector2(0f, 0f);
            tzRT.anchorMax        = new Vector2(1f, 1f);
            tzRT.offsetMin        = new Vector2(role.Icon != null ? 80f : 14f, 6f);
            tzRT.offsetMax        = new Vector2(-10f, -6f);

            var tzVLG    = textZone.AddComponent<VerticalLayoutGroup>();
            tzVLG.childAlignment         = TextAnchor.MiddleLeft;
            tzVLG.childForceExpandWidth  = true;
            tzVLG.childForceExpandHeight = false;
            tzVLG.spacing = 2f;

            // Nom du rôle
            AddTMP(textZone.transform, "Name", role.RoleName,
                fontSize: 20f, bold: true, color: Color.white);

            // Description (si disponible)
            if (!string.IsNullOrEmpty(role.Description))
                AddTMP(textZone.transform, "Desc", role.Description,
                    fontSize: 14f, bold: false, color: new Color(0.8f, 0.8f, 0.8f));

            // Contrôles spécifiques au rôle
            string controls = GetControlsHint(role.RoleType);
            if (!string.IsNullOrEmpty(controls))
                AddTMP(textZone.transform, "Controls", controls,
                    fontSize: 13f, bold: false, color: new Color(0.5f, 0.85f, 0.5f));

            return btnGO;
        }

        private void SetupPrefabButton(GameObject go, RoleData role)
        {
            var nameT = go.transform.Find("RoleName")?.GetComponent<TextMeshProUGUI>();
            if (nameT != null) nameT.text = role.RoleName;

            var descT = go.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            if (descT != null) descT.text = role.Description;

            var iconI = go.transform.Find("Icon")?.GetComponent<Image>();
            if (iconI != null) { iconI.sprite = role.Icon; iconI.enabled = role.Icon != null; }

            var ctrlT = go.transform.Find("Controls")?.GetComponent<TextMeshProUGUI>();
            if (ctrlT != null) ctrlT.text = GetControlsHint(role.RoleType);
        }

        private static TextMeshProUGUI AddTMP(Transform parent, string n, string text,
            float fontSize, bool bold, Color color)
        {
            var go  = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text        = text;
            tmp.fontSize    = fontSize;
            tmp.fontStyle   = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color       = color;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        // ── Description des contrôles par rôle ───────────────────────────────────

        private static string GetControlsHint(PlayerRole type)
        {
            switch (type)
            {
                case PlayerRole.Chasseur:
                    return "🎯 RMB : Viser (ralentit)   Q : Lancer un objet";

                case PlayerRole.Fils_Chasseur:
                    return "🎒 2 slots d'items   1/2 ou Molette : changer de slot";

                case PlayerRole.Loup:
                    return "🐺 R : Transformation Humain ↔ Loup   LMB (loup) : Attaque";

                case PlayerRole.Villageois:
                    return "👤 Aucune capacité spéciale";

                default:
                    return "⚙ Capacités à venir…";
            }
        }

        // ── Sélection ─────────────────────────────────────────────────────────────

        private void SelectRole(RoleData role)
        {
            if (RoleManager.Instance == null) return;
            RoleManager.Instance.SetRole(role);
            Debug.Log($"[RoleSelectionUI] Classe sélectionnée → {role.RoleName}");
            SetPanelVisible(false);
        }

        private void OnRoleChanged(RoleData role)
        {
            HighlightCurrentRole(role);
        }

        private void HighlightCurrentRole(RoleData current)
        {
            foreach (var entry in _buttons)
            {
                if (entry.Background == null) continue;
                bool isCurrent = entry.Role == current;
                entry.Background.color = isCurrent ? buttonSelected : buttonNormal;
            }
        }

        // ── Auto-création du panel ────────────────────────────────────────────────

        private void EnsurePanel()
        {
            if (panelRoot != null)
            {
                // Panel existant : trouve le buttonContainer dedans si non assigné
                if (buttonContainer == null)
                    buttonContainer = panelRoot.transform.Find("ButtonContainer")
                                   ?? panelRoot.transform;
                return;
            }

            // Trouve ou crée le Canvas parent
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("MenuCanvas");
                canvasGO.transform.SetParent(transform, false);
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            Transform canvasT = canvas.transform;

            // Overlay sombre plein écran
            var overlayGO = new GameObject("MenuOverlay");
            overlayGO.transform.SetParent(canvasT, false);
            var oRT       = overlayGO.AddComponent<RectTransform>();
            oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one; oRT.sizeDelta = Vector2.zero;
            var oImg      = overlayGO.AddComponent<Image>();
            oImg.color    = overlayColor;

            // Panneau central (60% largeur, 80% hauteur)
            var panel     = new GameObject("RolePanel");
            panel.transform.SetParent(overlayGO.transform, false);
            var pRT       = panel.AddComponent<RectTransform>();
            pRT.anchorMin        = new Vector2(0.2f, 0.1f);
            pRT.anchorMax        = new Vector2(0.8f, 0.9f);
            pRT.sizeDelta        = Vector2.zero;
            var pImg      = panel.AddComponent<Image>();
            pImg.color    = panelColor;

            // VLG pour le layout interne du panel
            var vlg       = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding   = new RectOffset(16, 16, 16, 16);
            vlg.spacing   = 10f;
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            // Titre
            var titleGO   = new GameObject("Title");
            titleGO.transform.SetParent(panel.transform, false);
            titleGO.AddComponent<RectTransform>();
            var titleTMP  = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text      = "CHOISIR UNE CLASSE";
            titleTMP.fontSize  = 28f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color     = Color.white;
            titleTMP.alignment = TextAlignmentOptions.Center;
            var titleLE   = titleGO.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 40f;

            // Séparateur
            var sep       = new GameObject("Separator");
            sep.transform.SetParent(panel.transform, false);
            sep.AddComponent<RectTransform>();
            var sepImg    = sep.AddComponent<Image>();
            sepImg.color  = new Color(1f, 1f, 1f, 0.15f);
            var sepLE     = sep.AddComponent<LayoutElement>();
            sepLE.preferredHeight = 2f;

            // ScrollView pour la liste des boutons
            var scrollGO  = new GameObject("ScrollView");
            scrollGO.transform.SetParent(panel.transform, false);
            var scrollLE  = scrollGO.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1f;
            var scrollRT  = scrollGO.GetComponent<RectTransform>() ?? scrollGO.AddComponent<RectTransform>();

            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            // Viewport
            var viewGO    = new GameObject("Viewport");
            viewGO.transform.SetParent(scrollGO.transform, false);
            var viewRT    = viewGO.AddComponent<RectTransform>();
            viewRT.anchorMin = Vector2.zero; viewRT.anchorMax = Vector2.one; viewRT.sizeDelta = Vector2.zero;
            viewGO.AddComponent<Mask>().showMaskGraphic = false;
            viewGO.AddComponent<Image>().color          = Color.clear;
            scrollRect.viewport = viewRT;

            // Content (liste des boutons)
            var contentGO  = new GameObject("ButtonContainer");
            contentGO.transform.SetParent(viewGO.transform, false);
            var contentRT  = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = Vector2.zero;
            var contentVLG = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVLG.spacing                = 8f;
            contentVLG.childAlignment         = TextAnchor.UpperCenter;
            contentVLG.childForceExpandWidth  = true;
            contentVLG.childForceExpandHeight = false;
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRT;

            // Bouton Fermer
            var closeLE   = new GameObject("CloseBtnSpacer");
            closeLE.transform.SetParent(panel.transform, false);
            closeLE.AddComponent<RectTransform>();
            var clLE      = closeLE.AddComponent<LayoutElement>();
            clLE.preferredHeight = 4f;

            var closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(panel.transform, false);
            closeBtnGO.AddComponent<RectTransform>();
            var closeBg   = closeBtnGO.AddComponent<Image>();
            closeBg.color = new Color(0.5f, 0.15f, 0.15f);
            var closeBtn  = closeBtnGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeBtn.onClick.AddListener(() => SetPanelVisible(false));
            var closeLe   = closeBtnGO.AddComponent<LayoutElement>();
            closeLe.preferredHeight = 36f;

            var closeTGO  = new GameObject("CloseText");
            closeTGO.transform.SetParent(closeBtnGO.transform, false);
            var closeTRT  = closeTGO.AddComponent<RectTransform>();
            closeTRT.anchorMin = Vector2.zero; closeTRT.anchorMax = Vector2.one; closeTRT.sizeDelta = Vector2.zero;
            var closeTMP  = closeTGO.AddComponent<TextMeshProUGUI>();
            closeTMP.text      = "Fermer  [=]";
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.fontSize  = 16f;
            closeTMP.color     = Color.white;

            panelRoot       = overlayGO;
            buttonContainer = contentGO.transform;

            Debug.Log("[RoleSelectionUI] Menu créé automatiquement. " +
                "Appuyez sur '=' en jeu pour l'ouvrir.");
        }
    }
}
