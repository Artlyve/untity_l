// LobbySetupEditor.cs
// Menu : ProjectFPS → Setup Lobby Canvas

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
        private static readonly Color ColBg         = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        private static readonly Color ColPanel       = new Color(0.12f, 0.12f, 0.12f, 0.90f);
        private static readonly Color ColButtonHost  = new Color(0.18f, 0.38f, 0.18f, 1.00f);
        private static readonly Color ColButtonJoin  = new Color(0.18f, 0.28f, 0.45f, 1.00f);
        private static readonly Color ColButtonStart = new Color(0.18f, 0.45f, 0.18f, 1.00f);
        private static readonly Color ColButtonLeave = new Color(0.45f, 0.12f, 0.12f, 1.00f);
        private static readonly Color ColSeparator   = new Color(1.00f, 1.00f, 1.00f, 0.12f);
        private static readonly Color ColSubtext     = new Color(0.65f, 0.65f, 0.65f, 1.00f);
        private static readonly Color ColInput       = new Color(0.16f, 0.16f, 0.16f, 1.00f);
        private static readonly Color ColCode        = new Color(0.25f, 0.70f, 0.40f, 1.00f);

        [MenuItem("ProjectFPS/Setup Lobby Canvas")]
        public static void SetupLobbyCanvas()
        {
            // ── Canvas (toujours nommé "LobbyCanvas" pour éviter de polluer le HUD) ──
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

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            }

            var lobbyMgr = canvasGO.GetComponent<LobbyManager>();
            if (lobbyMgr == null) lobbyMgr = canvasGO.AddComponent<LobbyManager>();

            // ── Fond global ───────────────────────────────────────────────────────
            GameObject bgGO = Recreate("Background", canvasGO.transform);
            StretchRT(bgGO);
            GetOrAdd<Image>(bgGO).color = ColBg;

            // ═════════════════════════════════════════════════════════════════════
            // CONNECT PANEL
            // ═════════════════════════════════════════════════════════════════════
            GameObject connectPanel = Recreate("ConnectPanel", canvasGO.transform);
            StretchRT(connectPanel);

            // Carte centrale — 460×500, centrée
            GameObject card = Recreate("Card", connectPanel.transform);
            SetRT(card,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(460f, 500f));
            GetOrAdd<Image>(card).color = ColPanel;
            GetOrAdd<RectMask2D>(card); // clip les enfants qui dépassent

            // Positions mesurées depuis le haut de la carte (anchorMin/Max = 0,1 / 1,1)
            // Layout :  top-padding=20  →  éléments  →  bottom-padding=20

            // Titre "ProjectFPS" — y=-20, h=44  → [20..64]
            GameObject titleGO = MakeChild("Title", card.transform);
            SetRT(titleGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-42f), new Vector2(-40f,44f));
            var titleTMP = ConfigureTMP(titleGO, "ProjectFPS", 34f, Color.white);
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;

            // Sous-titre — y=-72, h=22  → [72..94]
            GameObject subtitleGO = MakeChild("Subtitle", card.transform);
            SetRT(subtitleGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-74f), new Vector2(-40f,22f));
            var subTMP = ConfigureTMP(subtitleGO, "Multijoueur", 16f, ColSubtext);
            subTMP.alignment = TextAlignmentOptions.Center;

            // Séparateur — y=-105, h=1  → [105]
            GameObject sep1 = MakeChild("Separator1", card.transform);
            SetRT(sep1, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-106f), new Vector2(-40f,1f));
            GetOrAdd<Image>(sep1).color = ColSeparator;

            // Bouton Héberger — y=-130, h=52  → [130..182]
            GameObject hostBtnGO = MakeChild("HostButton", card.transform);
            SetRT(hostBtnGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-156f), new Vector2(-40f,52f));
            var hostBtn = BuildButton(hostBtnGO, "Héberger une partie", 19f, ColButtonHost);

            // Séparateur — y=-200  → [200]
            GameObject sep2 = MakeChild("Separator2", card.transform);
            SetRT(sep2, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-210f), new Vector2(-40f,1f));
            GetOrAdd<Image>(sep2).color = ColSeparator;

            // Label "Code de la partie :"  — y=-218, h=20  → [218..238]
            GameObject codeLabelGO = MakeChild("CodeLabel", card.transform);
            SetRT(codeLabelGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-228f), new Vector2(-40f,20f));
            var codeLabelTMP = ConfigureTMP(codeLabelGO, "Code de la partie :", 14f, ColSubtext);
            codeLabelTMP.alignment = TextAlignmentOptions.Left;

            // Champ code — y=-246, h=44  → [246..290]
            GameObject codeFieldGO = MakeChild("CodeInputField", card.transform);
            SetRT(codeFieldGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-268f), new Vector2(-40f,44f));
            var codeField = BuildInputField(codeFieldGO, "Ex : C0A8-0164  ou  192.168.x.x");

            // Bouton Rejoindre — y=-310, h=52  → [310..362]
            GameObject joinBtnGO = MakeChild("JoinButton", card.transform);
            SetRT(joinBtnGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-336f), new Vector2(-40f,52f));
            var joinBtn = BuildButton(joinBtnGO, "Rejoindre", 19f, ColButtonJoin);

            // Status text — ancré en bas, h=22, 20px du bord bas  → [20..42] depuis le bas
            GameObject statusGO = MakeChild("StatusText", card.transform);
            SetRT(statusGO, new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0,31f), new Vector2(-40f,22f));
            var statusTMP = ConfigureTMP(statusGO, "", 14f, ColSubtext);
            statusTMP.alignment = TextAlignmentOptions.Center;

            // ═════════════════════════════════════════════════════════════════════
            // LOBBY PANEL (désactivé par défaut)
            // ═════════════════════════════════════════════════════════════════════
            GameObject lobbyPanel = Recreate("LobbyPanel", canvasGO.transform);
            StretchRT(lobbyPanel);

            GameObject lobbyCard = Recreate("LobbyCard", lobbyPanel.transform);
            SetRT(lobbyCard,
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                Vector2.zero, new Vector2(500f,580f));
            GetOrAdd<Image>(lobbyCard).color = ColPanel;
            GetOrAdd<RectMask2D>(lobbyCard);

            // Titre
            GameObject lobbyTitleGO = MakeChild("LobbyTitle", lobbyCard.transform);
            SetRT(lobbyTitleGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-45f), new Vector2(-40f,50f));
            var lobbyTitleTMP = ConfigureTMP(lobbyTitleGO, "Salon d'attente", 28f, Color.white);
            lobbyTitleTMP.fontStyle = FontStyles.Bold;
            lobbyTitleTMP.alignment = TextAlignmentOptions.Center;

            // Code de partie (affiché pour l'hôte)
            GameObject partyCodeGO = MakeChild("PartyCodeText", lobbyCard.transform);
            SetRT(partyCodeGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-105f), new Vector2(-40f,52f));
            var partyCodeTMP = ConfigureTMP(partyCodeGO, "", 16f, ColCode);
            partyCodeTMP.alignment = TextAlignmentOptions.Center;

            // Séparateur
            GameObject sep3 = MakeChild("Separator3", lobbyCard.transform);
            SetRT(sep3, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-132f), new Vector2(-40f,1f));
            GetOrAdd<Image>(sep3).color = ColSeparator;

            // Compteur joueurs
            GameObject countGO = MakeChild("PlayerCountText", lobbyCard.transform);
            SetRT(countGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-158f), new Vector2(-40f,26f));
            var countTMP = ConfigureTMP(countGO, "Joueurs connectés : 0", 16f, ColSubtext);
            countTMP.alignment = TextAlignmentOptions.Center;

            // Liste joueurs
            GameObject listGO = MakeChild("PlayerListText", lobbyCard.transform);
            SetRT(listGO, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0,-330f), new Vector2(-40f,320f));
            var listTMP = ConfigureTMP(listGO, "", 16f, Color.white);
            listTMP.alignment = TextAlignmentOptions.Left;

            // Bouton Lancer (ancré en bas)
            GameObject startBtnGO = MakeChild("StartGameButton", lobbyCard.transform);
            SetRT(startBtnGO, new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0,130f), new Vector2(-40f,52f));
            var startBtn = BuildButton(startBtnGO, "Lancer la partie", 19f, ColButtonStart);

            // Bouton Quitter (ancré en bas)
            GameObject leaveBtnGO = MakeChild("LeaveButton", lobbyCard.transform);
            SetRT(leaveBtnGO, new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0,68f), new Vector2(-40f,48f));
            var leaveBtn = BuildButton(leaveBtnGO, "Quitter", 17f, ColButtonLeave);

            lobbyPanel.SetActive(false);

            // ── Câblage automatique du LobbyManager ──────────────────────────────
            var so = new SerializedObject(lobbyMgr);
            so.FindProperty("connectPanel").objectReferenceValue    = connectPanel;
            so.FindProperty("lobbyPanel").objectReferenceValue      = lobbyPanel;
            so.FindProperty("hostButton").objectReferenceValue      = hostBtn;
            so.FindProperty("codeInputField").objectReferenceValue  = codeField;
            so.FindProperty("joinButton").objectReferenceValue      = joinBtn;
            so.FindProperty("statusText").objectReferenceValue      = statusTMP;
            so.FindProperty("playerCountText").objectReferenceValue = countTMP;
            so.FindProperty("playerListText").objectReferenceValue  = listTMP;
            so.FindProperty("partyCodeText").objectReferenceValue   = partyCodeTMP;
            so.FindProperty("startGameButton").objectReferenceValue = startBtn;
            so.FindProperty("leaveButton").objectReferenceValue     = leaveBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = canvasGO;

            Debug.Log(
                "[LobbySetup] Canvas Lobby créé avec succès !\n\n" +
                "À CONFIGURER dans l'Inspector de LobbyManager (sur LobbyCanvas) :\n" +
                "  • Game Scene Name → nom exact de ta scène (ex. 'SampleScene')\n" +
                "  • Port → 7770\n\n" +
                "ERREURS À CORRIGER :\n" +
                "  • 'AssetPathHash is not set' → NetworkManager Inspector → DefaultPrefabObjects → bouton Regenerate\n" +
                "  • FishNet.PlayerSpawner NullRef → dans ta scène, supprime ou configure le composant PlayerSpawner FishNet built-in\n" +
                "  • NetworkManager not found → copie le NetworkManager + Tugboat dans la scène Lobby\n\n" +
                "Build Settings :\n" +
                "  • Index 0 : Lobby\n" +
                "  • Index 1 : ta scène de jeu");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Utilitaires
        // ═════════════════════════════════════════════════════════════════════════

        // Supprime et recrée l'enfant (évite les états corrompus des runs précédents)
        static GameObject Recreate(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        // Crée ou retrouve un enfant (utilisé pour les sous-éléments comme Label, Text Area…)
        static GameObject MakeChild(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static Button BuildButton(GameObject go, string label, float fontSize, Color bgColor)
        {
            GetOrAdd<Image>(go).color = bgColor;
            var btn    = GetOrAdd<Button>(go);
            var colors = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            GameObject labelGO = MakeChild("Label", go.transform);
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

            GameObject textAreaGO = MakeChild("Text Area", go.transform);
            var taRT = GetOrAdd<RectTransform>(textAreaGO);
            taRT.anchorMin = Vector2.zero;
            taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(8, 4);
            taRT.offsetMax = new Vector2(-8, -4);

            GameObject phGO  = MakeChild("Placeholder", textAreaGO.transform);
            var phRT = GetOrAdd<RectTransform>(phGO);
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = phRT.offsetMax = Vector2.zero;
            var phTMP = ConfigureTMP(phGO, placeholder, 15f, new Color(0.45f, 0.45f, 0.45f));
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            field.placeholder = phTMP;

            GameObject inputTextGO = MakeChild("Text", textAreaGO.transform);
            var itRT = GetOrAdd<RectTransform>(inputTextGO);
            itRT.anchorMin = Vector2.zero;
            itRT.anchorMax = Vector2.one;
            itRT.offsetMin = itRT.offsetMax = Vector2.zero;
            var itTMP = ConfigureTMP(inputTextGO, "", 16f, Color.white);
            itTMP.alignment = TextAlignmentOptions.MidlineLeft;
            field.textComponent = itTMP;
            field.text = "";

            return field;
        }

        static void StretchRT(GameObject go)
        {
            var rt       = GetOrAdd<RectTransform>(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void SetRT(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
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
            var tmp       = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
            => go.TryGetComponent(out T c) ? c : go.AddComponent<T>();
    }
}
