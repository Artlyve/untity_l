// UISetupEditor.cs
// Menu : ProjectFPS → Setup UI Canvas
// Crée (ou complète) toute la hiérarchie Canvas/HUD en une seule commande.
// Les références des scripts sont câblées automatiquement ; seules les refs
// PlayerState / InventorySystem / PlayerInteraction / CharacterController
// devront être assignées manuellement depuis l'Inspecteur.

using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using ProjectFPS.UI;

namespace ProjectFPS.Editor
{
    public static class UISetupEditor
    {
        private const string PrefabsFolder = "Assets/_Project/Prefabs/UI";

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("ProjectFPS/Setup UI Canvas")]
        public static void SetupUICanvas()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder(PrefabsFolder);

            // ── Canvas ────────────────────────────────────────────────────────────
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            GameObject canvasGO;
            if (canvas == null)
            {
                canvasGO = new GameObject("Canvas");
                canvas   = canvasGO.AddComponent<Canvas>();
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight  = 0.5f;
                canvasGO.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            }
            else
            {
                canvasGO = canvas.gameObject;
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // ── EventSystem ───────────────────────────────────────────────────────
            // Le projet utilise le nouveau Input System → on ne crée pas de
            // StandaloneInputModule si un EventSystem existe déjà.
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            }

            // ── HUDPanel ──────────────────────────────────────────────────────────
            GameObject hudPanel = FindOrCreateChild("HUDPanel", canvasGO.transform);
            StretchRT(hudPanel);
            AddIfMissing<HUD>(hudPanel);

            // Reparent HealthBar / RoleNameText s'ils sont encore enfants directs du Canvas
            ReparentChild(canvasGO.transform, hudPanel.transform, "HealthBar");
            ReparentChild(canvasGO.transform, hudPanel.transform, "RoleNameText");

            // ── HealthBar (Slider) ────────────────────────────────────────────────
            GameObject healthBarGO = BuildSlider("HealthBar", hudPanel.transform);
            SetRT(healthBarGO,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                pos:       new Vector2(20f, 20f),
                size:      new Vector2(300f, 40f));

            // ── RoleNameText (TextMeshProUGUI) ────────────────────────────────────
            GameObject roleNameGO = FindOrCreateChild("RoleNameText", hudPanel.transform);
            TextMeshProUGUI roleTMP = ConfigureTMP(roleNameGO, "Villageois", 24f, Color.white);
            SetRT(roleNameGO,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                pos:       new Vector2(20f, -20f),
                size:      new Vector2(250f, 40f));

            // ── InventorySlots (Horizontal Layout Group) ──────────────────────────
            GameObject slotsGO = FindOrCreateChild("InventorySlots", hudPanel.transform);
            var hlg = GetOrAdd<HorizontalLayoutGroup>(slotsGO);
            hlg.spacing              = 5f;
            hlg.childAlignment       = TextAnchor.LowerCenter;
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            SetRT(slotsGO,
                anchorMin: new Vector2(0.5f, 0f),
                anchorMax: new Vector2(0.5f, 0f),
                pos:       new Vector2(0f, 35f),
                size:      new Vector2(260f, 60f));

            // ── InteractionText (TMP, désactivé par défaut) ───────────────────────
            GameObject interTextGO = FindOrCreateChild("InteractionText", hudPanel.transform);
            TextMeshProUGUI interTMP = ConfigureTMP(interTextGO, "[E] Ramasser", 20f, Color.white);
            interTMP.alignment = TextAlignmentOptions.Center;
            SetRT(interTextGO,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                pos:       new Vector2(0f, -80f),
                size:      new Vector2(400f, 50f));
            interTextGO.SetActive(false);

            // ── RoleSelectionPanel (Panel, désactivé par défaut) ──────────────────
            // IMPORTANT : RoleSelectionUI est sur le Canvas (toujours actif), PAS sur le panel.
            // Un script sur un panel désactivé ne s'exécute jamais (Start/Update bloqués).
            AddIfMissing<RoleSelectionUI>(canvasGO);

            GameObject rolePanel = FindOrCreateChild("RoleSelectionPanel", canvasGO.transform);
            StretchRT(rolePanel);
            GetOrAdd<Image>(rolePanel).color = new Color(0f, 0f, 0f, 0.85f);

            //    ButtonContainer (Vertical Layout Group)
            GameObject btnContainer = FindOrCreateChild("ButtonContainer", rolePanel.transform);
            var vlg = GetOrAdd<VerticalLayoutGroup>(btnContainer);
            vlg.spacing              = 10f;
            vlg.childAlignment       = TextAnchor.UpperCenter;
            vlg.childControlWidth    = true;
            vlg.childControlHeight   = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            var csf = GetOrAdd<ContentSizeFitter>(btnContainer);
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SetRT(btnContainer,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                pos:       Vector2.zero,
                size:      new Vector2(450f, 600f));

            rolePanel.SetActive(false);

            // ── DebugPanel (Panel, désactivé par défaut) ──────────────────────────
            GameObject debugPanel = FindOrCreateChild("DebugPanel", canvasGO.transform);
            StretchRT(debugPanel);
            AddIfMissing<DebugPanel>(debugPanel);

            //    DebugText
            GameObject debugTextGO = FindOrCreateChild("DebugText", debugPanel.transform);
            ConfigureTMP(debugTextGO, "<b>DEBUG</b>\n...", 16f, Color.green);
            SetRT(debugTextGO,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                pos:       new Vector2(10f, -10f),
                size:      new Vector2(350f, 200f));

            debugPanel.SetActive(false);

            // ── Prefabs ───────────────────────────────────────────────────────────
            GameObject slotPrefab       = BuildOrLoadSlotPrefab();
            GameObject roleButtonPrefab = BuildOrLoadRoleButtonPrefab();

            // ── Câblage automatique des champs sérialisés ─────────────────────────
            WireHUD(hudPanel, healthBarGO, roleTMP, slotsGO, slotPrefab, interTMP);
            WireRoleSelectionUI(canvasGO, rolePanel, btnContainer, roleButtonPrefab);
            WireDebugPanel(debugPanel, debugTextGO);

            // ── Marquer la scène comme modifiée ───────────────────────────────────
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Selection.activeGameObject = hudPanel;
            Debug.Log(
                "[UISetup] Hiérarchie Canvas créée avec succès !\n" +
                "Il reste à assigner manuellement depuis l'Inspecteur :\n" +
                "  • HUD          → PlayerState, InventorySystem, PlayerInteraction\n" +
                "  • DebugPanel   → PlayerTransform, CharacterController, InventorySystem\n" +
                "  • RoleManager  → AvailableRoles (assets RoleData) + DefaultRole");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Câblage des scripts
        // ═════════════════════════════════════════════════════════════════════════

        static void WireHUD(GameObject hudGO, GameObject healthBarGO, TextMeshProUGUI roleTMP,
            GameObject slotsGO, GameObject slotPrefab, TextMeshProUGUI interTMP)
        {
            var so = new SerializedObject(hudGO.GetComponent<HUD>());
            so.FindProperty("healthBar").objectReferenceValue         = healthBarGO.GetComponent<Slider>();
            so.FindProperty("roleNameText").objectReferenceValue      = roleTMP;
            so.FindProperty("slotsContainer").objectReferenceValue    = slotsGO.transform;
            so.FindProperty("slotPrefab").objectReferenceValue        = slotPrefab;
            so.FindProperty("interactionPrompt").objectReferenceValue = interTMP;
            so.ApplyModifiedProperties();
        }

        // canvasGO = objet qui porte le composant RoleSelectionUI (toujours actif)
        // panelGO  = RoleSelectionPanel (peut être désactivé, contrôlé par le script)
        static void WireRoleSelectionUI(GameObject canvasGO, GameObject panelGO,
            GameObject containerGO, GameObject prefab)
        {
            var so = new SerializedObject(canvasGO.GetComponent<RoleSelectionUI>());
            so.FindProperty("panelRoot").objectReferenceValue        = panelGO;
            so.FindProperty("buttonContainer").objectReferenceValue  = containerGO.transform;
            so.FindProperty("roleButtonPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
        }

        static void WireDebugPanel(GameObject panelGO, GameObject textGO)
        {
            var so = new SerializedObject(panelGO.GetComponent<DebugPanel>());
            so.FindProperty("panel").objectReferenceValue     = panelGO;
            so.FindProperty("debugText").objectReferenceValue = textGO.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Création des prefabs
        // ═════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// SlotPrefab : Image (fond) + enfant "ItemIcon" (Image)
        /// Utilisé par HUD.cs pour instancier les slots d'inventaire.
        /// </summary>
        static GameObject BuildOrLoadSlotPrefab()
        {
            const string path = PrefabsFolder + "/SlotPrefab.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root   = new GameObject("SlotPrefab");
            var rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(55f, 55f);
            root.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var icon   = new GameObject("ItemIcon");
            icon.transform.SetParent(root.transform, false);
            var iconRT = icon.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            icon.AddComponent<Image>().enabled = false;   // invisible tant que vide

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// RoleButtonPrefab : Button + Image de fond
        ///   ├── "Icon"        (Image)
        ///   ├── "RoleName"    (TextMeshProUGUI, bold)
        ///   └── "Description" (TextMeshProUGUI, gris)
        /// Utilisé par RoleSelectionUI.cs.
        /// </summary>
        static GameObject BuildOrLoadRoleButtonPrefab()
        {
            const string path = PrefabsFolder + "/RoleButtonPrefab.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root   = new GameObject("RoleButtonPrefab");
            var rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(400f, 80f);
            root.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            root.AddComponent<Button>();

            // Icon
            var icon   = new GameObject("Icon");
            icon.transform.SetParent(root.transform, false);
            var iconRT = icon.AddComponent<RectTransform>();
            iconRT.anchorMin        = new Vector2(0f, 0f);
            iconRT.anchorMax        = new Vector2(0f, 1f);
            iconRT.pivot            = new Vector2(0f, 0.5f);
            iconRT.anchoredPosition = new Vector2(10f, 0f);
            iconRT.sizeDelta        = new Vector2(60f, -10f);
            icon.AddComponent<Image>();

            // RoleName
            var nameGO = new GameObject("RoleName");
            nameGO.transform.SetParent(root.transform, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0.5f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.offsetMin = new Vector2(80f, 0f);
            nameRT.offsetMax = new Vector2(-10f, 0f);
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text      = "Nom du rôle";
            nameTMP.fontSize  = 20f;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color     = Color.white;

            // Description
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(root.transform, false);
            var descRT = descGO.AddComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0f, 0f);
            descRT.anchorMax = new Vector2(1f, 0.5f);
            descRT.offsetMin = new Vector2(80f, 0f);
            descRT.offsetMax = new Vector2(-10f, 0f);
            var descTMP = descGO.AddComponent<TextMeshProUGUI>();
            descTMP.text     = "Description du rôle";
            descTMP.fontSize = 14f;
            descTMP.color    = new Color(0.8f, 0.8f, 0.8f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Utilitaires
        // ═════════════════════════════════════════════════════════════════════════

        /// <summary>Trouve l'enfant direct nommé <paramref name="name"/> ou en crée un.</summary>
        static GameObject FindOrCreateChild(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        /// <summary>Déplace un enfant direct de <paramref name="from"/> vers <paramref name="to"/>.</summary>
        static void ReparentChild(Transform from, Transform to, string childName)
        {
            Transform child = from.Find(childName);
            if (child != null && child.parent != to)
                child.SetParent(to, false);
        }

        /// <summary>Étire le RectTransform pour remplir son parent.</summary>
        static void StretchRT(GameObject go)
        {
            var rt    = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Positionne le RectTransform avec ancre + position + taille.</summary>
        static void SetRT(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pos, Vector2 size)
        {
            var rt    = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin       = anchorMin;
            rt.anchorMax       = anchorMax;
            rt.pivot           = (anchorMin + anchorMax) * 0.5f;
            rt.anchoredPosition = pos;
            rt.sizeDelta       = size;
        }

        /// <summary>
        /// Construit la hiérarchie complète d'un Slider (Background / Fill Area / Fill).
        /// Si le Slider existe déjà, retourne l'objet sans le recréer.
        /// </summary>
        static GameObject BuildSlider(string name, Transform parent)
        {
            GameObject root = FindOrCreateChild(name, parent);
            if (root.GetComponent<Slider>() != null) return root;  // déjà configuré

            var slider       = root.AddComponent<Slider>();
            slider.minValue  = 0f;
            slider.maxValue  = 1f;
            slider.value     = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            // Background
            var bg   = new GameObject("Background");
            bg.transform.SetParent(root.transform, false);
            bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            // Fill Area
            var fillArea   = new GameObject("Fill Area");
            fillArea.transform.SetParent(root.transform, false);
            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5f, 0f);
            fillAreaRT.offsetMax = new Vector2(-5f, 0f);

            // Fill
            var fill   = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);  // rouge santé

            slider.fillRect = fillRT;
            return root;
        }

        /// <summary>Ajoute ou configure un TextMeshProUGUI sur <paramref name="go"/>.</summary>
        static TextMeshProUGUI ConfigureTMP(GameObject go, string text, float fontSize, Color color)
        {
            var tmp      = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            tmp.text     = text;
            tmp.fontSize = fontSize;
            tmp.color    = color;
            return tmp;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
            => go.TryGetComponent(out T c) ? c : go.AddComponent<T>();

        static void AddIfMissing<T>(GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out _))
                go.AddComponent<T>();
        }

        /// <summary>Crée récursivement les dossiers Asset manquants.</summary>
        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            string parent = (Path.GetDirectoryName(path) ?? "Assets").Replace('\\', '/');
            string folder = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
