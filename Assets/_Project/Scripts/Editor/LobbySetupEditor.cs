// LobbySetupEditor.cs
// Menu : ProjectFPS → Setup Lobby Canvas
// Crée la hiérarchie Canvas du Lobby en une seule commande,
// câble automatiquement tous les champs du LobbyManager.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using ProjectFPS.Network;

namespace ProjectFPS.Editor
{
    public static class LobbySetupEditor
    {
        // Couleurs identiques au reste du projet
        private static readonly Color ColBg        = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        private static readonly Color ColPanel      = new Color(0.12f, 0.12f, 0.12f, 0.90f);
        private static readonly Color ColButton     = new Color(0.20f, 0.20f, 0.20f, 0.95f);
        private static readonly Color ColButtonHost = new Color(0.18f, 0.38f, 0.18f, 1.00f); // vert
        private static readonly Color ColButtonJoin = new Color(0.18f, 0.28f, 0.45f, 1.00f); // bleu
        private static readonly Color ColButtonStart= new Color(0.18f, 0.45f, 0.18f, 1.00f); // vert vif
        private static readonly Color ColButtonLeave= new Color(0.45f, 0.12f, 0.12f, 1.00f); // rouge
        private static readonly Color ColSeparator  = new Color(1.00f, 1.00f, 1.00f, 0.10f);
        private static readonly Color ColSubtext    = new Color(0.65f, 0.65f, 0.65f, 1.00f);
        private static readonly Color ColInput      = new Color(0.16f, 0.16f, 0.16f, 1.00f);

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("ProjectFPS/Setup Lobby Canvas")]
        public static void SetupLobbyCanvas()
        {
            // ── Canvas ────────────────────────────────────────────────────────────
            // Always use a canvas named "LobbyCanvas" so we never contaminate
            // the game HUD canvas (which may already exist in the scene).
            const string canvasName = "LobbyCanvas";
            GameObject canvasGO = GameObject.Find(canvasName);
            Canvas canvas;
            if (canvasGO == null)
            {
                canvasGO = new GameObject(canvasName);
                canvas   = canvasGO.AddComponent<Canvas>();
                var scaler             = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight  = 0.5f;
                canvasGO.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Lobby Canvas");
            }
            else
            {
                canvas = canvasGO.GetComponent<Canvas>();
                if (canvas == null) canvas = canvasGO.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // ── EventSystem ───────────────────────────────────────────────────────
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            }

            // ── LobbyManager sur le Canvas ────────────────────────────────────────
            var lobbyMgr = canvasGO.GetComponent<LobbyManager>();
            if (lobbyMgr == null)
                lobbyMgr = canvasGO.AddComponent<LobbyManager>();

            // ── Fond global ───────────────────────────────────────────────────────
            GameObject bgGO = FindOrCreateChild("Background", canvasGO.transform);
            StretchRT(bgGO);
            bgGO.GetComponent<Image>()?.Destroy();
            GetOrAdd<Image>(bgGO).color = ColBg;

            // ═════════════════════════════════════════════════════════════════════
            // CONNECT PANEL
            // ═════════════════════════════════════════════════════════════════════
            GameObject connectPanel = FindOrCreateChild("ConnectPanel", canvasGO.transform);
            StretchRT(connectPanel);

            // Carte centrale
            GameObject card = FindOrCreateChild("Card", connectPanel.transform);
            SetRT(card,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pos: Vector2.zero, size: new Vector2(480f, 520f));
            GetOrAdd<Image>(card).color = ColPanel;

            // Titre
            GameObject titleGO = FindOrCreateChild("Title", card.transform);
            SetRT(titleGO,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -60f), size: new Vector2(-40f, 70f));
            var titleTMP = ConfigureTMP(titleGO, "ProjectFPS", 42f, Color.white);
            titleTMP.fontStyle  = FontStyles.Bold;
            titleTMP.alignment  = TextAlignmentOptions.Center;

            // Sous-titre
            GameObject subtitleGO = FindOrCreateChild("Subtitle", card.transform);
            SetRT(subtitleGO,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -105f), size: new Vector2(-40f, 30f));
            var subTMP = ConfigureTMP(subtitleGO, "Multijoueur", 18f, ColSubtext);
            subTMP.alignment = TextAlignmentOptions.Center;

            // Séparateur
            GameObject sep1 = FindOrCreateChild("Separator1", card.transform);
            SetRT(sep1,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -130f), size: new Vector2(-40f, 2f));
            GetOrAdd<Image>(sep1).color = ColSeparator;

            // Bouton Héberger
            GameObject hostBtnGO = FindOrCreateChild("HostButton", card.transform);
            SetRT(hostBtnGO,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -190f), size: new Vector2(-40f, 60f));
            var hostBtn = BuildButton(hostBtnGO, "Héberger une partie", 20f, ColButtonHost);

            // Séparateur
            GameObject sep2 = FindOrCreateChild("Separator2", card.transform);
            SetRT(sep2,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -250f), size: new Vector2(-40f, 2f));
            GetOrAdd<Image>(sep2).color = ColSeparator;

            // Champ IP
            GameObject ipFieldGO = FindOrCreateChild("IpInputField", card.transform);
            SetRT(ipFieldGO,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -315f), size: new Vector2(-40f, 55f));
            var ipField = BuildInputField(ipFieldGO, "Adresse IP du serveur...");

            // Bouton Rejoindre
            GameObject joinBtnGO = FindOrCreateChild("JoinButton", card.transform);
            SetRT(joinBtnGO,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -390f), size: new Vector2(-40f, 60f));
            var joinBtn = BuildButton(joinBtnGO, "Rejoindre", 20f, ColButtonJoin);

            // Status text
            GameObject statusGO = FindOrCreateChild("StatusText", card.transform);
            SetRT(statusGO,
                anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0f),
                pos: new Vector2(0f, 25f), size: new Vector2(-40f, 30f));
            var statusTMP = ConfigureTMP(statusGO, "", 16f, ColSubtext);
            statusTMP.alignment = TextAlignmentOptions.Center;

            // ═════════════════════════════════════════════════════════════════════
            // LOBBY PANEL (désactivé par défaut)
            // ═════════════════════════════════════════════════════════════════════
            GameObject lobbyPanel = FindOrCreateChild("LobbyPanel", canvasGO.transform);
            StretchRT(lobbyPanel);

            // Carte centrale lobby
            GameObject lobbyCard = FindOrCreateChild("LobbyCard", lobbyPanel.transform);
            SetRT(lobbyCard,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pos: Vector2.zero, size: new Vector2(500f, 560f));
            GetOrAdd<Image>(lobbyCard).color = ColPanel;

            // Titre Lobby
            GameObject lobbyTitleGO = FindOrCreateChild("LobbyTitle", lobbyCard.transform);
            SetRT(lobbyTitleGO,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -55f), size: new Vector2(-40f, 60f));
            var lobbyTitleTMP = ConfigureTMP(lobbyTitleGO, "Salon d'attente", 32f, Color.white);
            lobbyTitleTMP.fontStyle = FontStyles.Bold;
            lobbyTitleTMP.alignment = TextAlignmentOptions.Center;

            // Compteur joueurs
            GameObject countGO = FindOrCreateChild("PlayerCountText", lobbyCard.transform);
            SetRT(countGO,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -100f), size: new Vector2(-40f, 30f));
            var countTMP = ConfigureTMP(countGO, "Joueurs connectés : 0", 18f, ColSubtext);
            countTMP.alignment = TextAlignmentOptions.Center;

            // Séparateur
            GameObject sep3 = FindOrCreateChild("Separator3", lobbyCard.transform);
            SetRT(sep3,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -125f), size: new Vector2(-40f, 2f));
            GetOrAdd<Image>(sep3).color = ColSeparator;

            // Liste joueurs
            GameObject listGO = FindOrCreateChild("PlayerListText", lobbyCard.transform);
            SetRT(listGO,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pos: new Vector2(0f, -285f), size: new Vector2(-40f, 300f));
            var listTMP = ConfigureTMP(listGO, "", 18f, Color.white);
            listTMP.alignment = TextAlignmentOptions.Left;

            // Bouton Lancer
            GameObject startBtnGO = FindOrCreateChild("StartGameButton", lobbyCard.transform);
            SetRT(startBtnGO,
                anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0f),
                pos: new Vector2(0f, 130f), size: new Vector2(-40f, 55f));
            var startBtn = BuildButton(startBtnGO, "Lancer la partie", 20f, ColButtonStart);

            // Bouton Quitter
            GameObject leaveBtnGO = FindOrCreateChild("LeaveButton", lobbyCard.transform);
            SetRT(leaveBtnGO,
                anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0f),
                pos: new Vector2(0f, 65f), size: new Vector2(-40f, 50f));
            var leaveBtn = BuildButton(leaveBtnGO, "Quitter", 18f, ColButtonLeave);

            lobbyPanel.SetActive(false);

            // ── Câblage automatique du LobbyManager ──────────────────────────────
            var so = new SerializedObject(lobbyMgr);
            so.FindProperty("connectPanel").objectReferenceValue   = connectPanel;
            so.FindProperty("lobbyPanel").objectReferenceValue     = lobbyPanel;
            so.FindProperty("hostButton").objectReferenceValue     = hostBtn;
            so.FindProperty("ipInputField").objectReferenceValue   = ipField;
            so.FindProperty("joinButton").objectReferenceValue     = joinBtn;
            so.FindProperty("statusText").objectReferenceValue     = statusTMP;
            so.FindProperty("playerCountText").objectReferenceValue = countTMP;
            so.FindProperty("playerListText").objectReferenceValue  = listTMP;
            so.FindProperty("startGameButton").objectReferenceValue = startBtn;
            so.FindProperty("leaveButton").objectReferenceValue     = leaveBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = canvasGO;

            Debug.Log(
                "[LobbySetup] Canvas Lobby créé avec succès !\n" +
                "Il reste à configurer dans l'Inspecteur de LobbyManager :\n" +
                "  • Game Scene Name → nom exact de ta scène de jeu (ex. 'SampleScene')\n" +
                "  • Port → 7770 (doit correspondre au Tugboat du NetworkManager)\n\n" +
                "N'oublie pas d'ajouter les deux scènes dans File → Build Settings :\n" +
                "  • Index 0 : Lobby\n" +
                "  • Index 1 : SampleScene (ou ton nom de scène de jeu)");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Utilitaires
        // ═════════════════════════════════════════════════════════════════════════

        static Button BuildButton(GameObject go, string label, float fontSize, Color bgColor)
        {
            GetOrAdd<Image>(go).color = bgColor;
            var btn = GetOrAdd<Button>(go);

            // Effet hover : légèrement plus clair
            var colors         = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            // Label
            GameObject labelGO = FindOrCreateChild("Label", go.transform);
            var rt = GetOrAdd<RectTransform>(labelGO);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = ConfigureTMP(labelGO, label, fontSize, Color.white);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        static TMP_InputField BuildInputField(GameObject go, string placeholder)
        {
            GetOrAdd<Image>(go).color = ColInput;
            var field = GetOrAdd<TMP_InputField>(go);

            // Text area
            GameObject textAreaGO = FindOrCreateChild("Text Area", go.transform);
            GetOrAdd<RectTransform>(textAreaGO).anchorMin = Vector2.zero;
            GetOrAdd<RectTransform>(textAreaGO).anchorMax = Vector2.one;

            // Placeholder
            GameObject phGO = FindOrCreateChild("Placeholder", textAreaGO.transform);
            var phRT  = GetOrAdd<RectTransform>(phGO);
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(10, 0);
            phRT.offsetMax = new Vector2(-10, 0);
            var phTMP = ConfigureTMP(phGO, placeholder, 18f, new Color(0.5f, 0.5f, 0.5f));
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            field.placeholder = phTMP;

            // Input text
            GameObject inputTextGO = FindOrCreateChild("Text", textAreaGO.transform);
            var itRT  = GetOrAdd<RectTransform>(inputTextGO);
            itRT.anchorMin = Vector2.zero;
            itRT.anchorMax = Vector2.one;
            itRT.offsetMin = new Vector2(10, 0);
            itRT.offsetMax = new Vector2(-10, 0);
            var itTMP = ConfigureTMP(inputTextGO, "", 18f, Color.white);
            itTMP.alignment = TextAlignmentOptions.MidlineLeft;
            field.textComponent = itTMP;
            field.text = "localhost";

            return field;
        }

        static GameObject FindOrCreateChild(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static void StretchRT(GameObject go)
        {
            var rt       = GetOrAdd<RectTransform>(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void SetRT(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pos, Vector2 size)
        {
            var rt              = GetOrAdd<RectTransform>(go);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = (anchorMin + anchorMax) * 0.5f;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
        }

        static TextMeshProUGUI ConfigureTMP(GameObject go, string text, float fontSize, Color color)
        {
            var tmp      = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text     = text;
            tmp.fontSize = fontSize;
            tmp.color    = color;
            return tmp;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
            => go.TryGetComponent(out T c) ? c : go.AddComponent<T>();
    }

    // Extension helper (évite conflict avec Unity built-in)
    internal static class ComponentExtensions
    {
        internal static void Destroy(this Component c)
        {
            if (c != null) Object.DestroyImmediate(c);
        }
    }
}
