// UISetupEditor.cs — Refonte complète HUD Lycans
// Menu : ProjectFPS → Setup UI Canvas

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

        // ── Couleurs ──────────────────────────────────────────────────────────────
        private static readonly Color ColDark    = new Color(0.06f, 0.06f, 0.06f, 0.88f);
        private static readonly Color ColDarker  = new Color(0.10f, 0.10f, 0.10f, 0.92f);
        private static readonly Color ColBar     = new Color(0.15f, 0.15f, 0.15f, 0.90f);
        private static readonly Color ColSubtext = new Color(0.65f, 0.65f, 0.65f, 1.00f);
        private static readonly Color ColGreen   = new Color(0.15f, 0.75f, 0.25f, 1.00f);
        private static readonly Color ColPurple  = new Color(0.55f, 0.15f, 0.85f, 1.00f);
        private static readonly Color ColHarvest = new Color(0.55f, 0.75f, 0.25f, 1.00f);
        private static readonly Color ColAccent  = new Color(1.00f, 0.85f, 0.30f, 1.00f);

        [MenuItem("ProjectFPS/Setup UI Canvas")]
        public static void SetupUICanvas()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder(PrefabsFolder);

            // ── Canvas "GameHUD" ──────────────────────────────────────────────────
            const string canvasName = "GameHUD";
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
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create GameHUD");
            }
            else
            {
                canvas = canvasGO.GetComponent<Canvas>() ?? canvasGO.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            var hudMgr = canvasGO.GetComponent<HUDManager>() ?? canvasGO.AddComponent<HUDManager>();

            // ═══════════════════════════════════════════════════════════════════════
            // VIGNETTE + DAMAGE FLASH (plein écran, tout en bas de la hiérarchie)
            // ═══════════════════════════════════════════════════════════════════════
            GameObject vignetteGO = Recreate("Vignette", canvasGO.transform);
            StretchRT(vignetteGO);
            var vigImg = GetOrAdd<Image>(vignetteGO);
            vigImg.color         = new Color(0, 0, 0, 0.30f);
            vigImg.raycastTarget = false;

            GameObject flashGO = Recreate("DamageFlash", canvasGO.transform);
            StretchRT(flashGO);
            var flashImg = GetOrAdd<Image>(flashGO);
            flashImg.color         = new Color(1, 0, 0, 0);
            flashImg.raycastTarget = false;
            flashGO.SetActive(false);

            // ═══════════════════════════════════════════════════════════════════════
            // CROSSHAIR (centre exact)
            // ═══════════════════════════════════════════════════════════════════════
            GameObject crosshairGO = Recreate("Crosshair", canvasGO.transform);
            SetRT(crosshairGO, V(.5f,.5f), V(.5f,.5f), Vector2.zero, new Vector2(24, 24));

            GameObject chH = Child("H", crosshairGO.transform);
            var chHRT = GetOrAdd<RectTransform>(chH);
            chHRT.anchorMin = V(.1f,.5f); chHRT.anchorMax = V(.9f,.5f);
            chHRT.pivot = V(.5f,.5f); chHRT.anchoredPosition = Vector2.zero; chHRT.sizeDelta = new Vector2(0,2);
            GetOrAdd<Image>(chH).color = new Color(1,1,1,0.80f);

            GameObject chV = Child("V", crosshairGO.transform);
            var chVRT = GetOrAdd<RectTransform>(chV);
            chVRT.anchorMin = V(.5f,.1f); chVRT.anchorMax = V(.5f,.9f);
            chVRT.pivot = V(.5f,.5f); chVRT.anchoredPosition = Vector2.zero; chVRT.sizeDelta = new Vector2(2,0);
            GetOrAdd<Image>(chV).color = new Color(1,1,1,0.80f);

            // ═══════════════════════════════════════════════════════════════════════
            // INDICATEUR JOUR / NUIT (haut centre)
            // ═══════════════════════════════════════════════════════════════════════
            GameObject dayNightGO = Recreate("DayNightIndicator", canvasGO.transform);
            GetOrAdd<CanvasGroup>(dayNightGO);
            SetRT(dayNightGO, V(.5f,1), V(.5f,1), new Vector2(0,-18), new Vector2(150, 38));
            GetOrAdd<Image>(dayNightGO).color = ColDark;

            GameObject dnIconGO = Child("Icon", dayNightGO.transform);
            SetRT(dnIconGO, V(0,0), V(0,1), new Vector2(20,0), new Vector2(30,0));
            var dnIcon = TMP(dnIconGO, "✦", 20f, ColAccent);
            dnIcon.alignment = TextAlignmentOptions.Center;

            GameObject dnLabelGO = Child("Label", dayNightGO.transform);
            StretchRT(dnLabelGO);
            var dnLabelRT = GetOrAdd<RectTransform>(dnLabelGO);
            dnLabelRT.offsetMin = new Vector2(52, 3);
            dnLabelRT.offsetMax = new Vector2(-8, -3);
            var dnLabel = TMP(dnLabelGO, "JOUR", 14f, Color.white);
            dnLabel.fontStyle = FontStyles.Bold;
            dnLabel.alignment = TextAlignmentOptions.MidlineLeft;

            // ═══════════════════════════════════════════════════════════════════════
            // PROMPT D'INTERACTION (centre, légèrement au-dessus du slot)
            // ═══════════════════════════════════════════════════════════════════════
            GameObject interPromptGO = Recreate("InteractionPrompt", canvasGO.transform);
            SetRT(interPromptGO, V(.5f,.5f), V(.5f,.5f), new Vector2(0, 72), new Vector2(340, 48));
            GetOrAdd<Image>(interPromptGO).color = ColDark;
            interPromptGO.SetActive(false);

            GameObject interKeyGO = Child("Key", interPromptGO.transform);
            SetRT(interKeyGO, V(0,0), V(0,1), new Vector2(18,0), new Vector2(38,0));
            var interKey = TMP(interKeyGO, "[E]", 15f, ColAccent);
            interKey.fontStyle = FontStyles.Bold;
            interKey.alignment = TextAlignmentOptions.Center;

            GameObject interActionGO = Child("Action", interPromptGO.transform);
            StretchRT(interActionGO);
            var interActionRT = GetOrAdd<RectTransform>(interActionGO);
            interActionRT.offsetMin = new Vector2(62, 4);
            interActionRT.offsetMax = new Vector2(-10, -4);
            var interAction = TMP(interActionGO, "Ramasser — Lanterne", 14f, Color.white);
            interAction.alignment = TextAlignmentOptions.MidlineLeft;

            // ═══════════════════════════════════════════════════════════════════════
            // BAS GAUCHE — Badge de rôle + Barre de vie
            // ═══════════════════════════════════════════════════════════════════════
            GameObject bottomLeft = Recreate("BottomLeft", canvasGO.transform);
            GetOrAdd<CanvasGroup>(bottomLeft);
            SetRT(bottomLeft, V(0,0), V(0,0), new Vector2(20, 20), new Vector2(280, 130));

            // Role Badge
            GameObject roleBadgeGO = Child("RoleBadge", bottomLeft.transform);
            SetRT(roleBadgeGO, V(0,1), V(1,1), new Vector2(0,-44), new Vector2(0, 44));
            var roleBadgeImg = GetOrAdd<Image>(roleBadgeGO);
            roleBadgeImg.color = new Color(0.6f, 0.1f, 0.2f, 0.80f);

            GameObject roleIconGO = Child("RoleIcon", roleBadgeGO.transform);
            SetRT(roleIconGO, V(0,0), V(0,1), new Vector2(0,0), new Vector2(44,0));
            var roleIconTMP = TMP(roleIconGO, "◈", 20f, Color.white);
            roleIconTMP.alignment = TextAlignmentOptions.Center;

            GameObject roleNameGO = Child("RoleName", roleBadgeGO.transform);
            StretchRT(roleNameGO);
            var rnRT = GetOrAdd<RectTransform>(roleNameGO);
            rnRT.offsetMin = new Vector2(48, 2);
            rnRT.offsetMax = new Vector2(-8, -2);
            var roleNameTMP = TMP(roleNameGO, "VILLAGEOIS", 14f, Color.white);
            roleNameTMP.fontStyle = FontStyles.Bold;
            roleNameTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // HP Section
            GameObject hpSection = Child("HPSection", bottomLeft.transform);
            SetRT(hpSection, V(0,0), V(1,0), new Vector2(0,4), new Vector2(0, 72));

            GameObject hpLabelGO = Child("HPLabel", hpSection.transform);
            SetRT(hpLabelGO, V(0,1), V(1,1), new Vector2(0,-22), new Vector2(0, 22));
            var hpLabelTMP = TMP(hpLabelGO, "❤  SANTÉ", 12f, ColSubtext);
            hpLabelTMP.alignment = TextAlignmentOptions.Left;

            GameObject hpBarBgGO = Child("HPBarBg", hpSection.transform);
            SetRT(hpBarBgGO, V(0,1), V(1,1), new Vector2(0,-38), new Vector2(0, 14));
            GetOrAdd<Image>(hpBarBgGO).color = ColBar;

            GameObject hpBarFillGO = Child("HPBarFill", hpBarBgGO.transform);
            StretchRT(hpBarFillGO);
            var hpFillImg = GetOrAdd<Image>(hpBarFillGO);
            hpFillImg.color      = ColGreen;
            hpFillImg.type       = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.fillAmount = 1f;

            GameObject hpTextGO = Child("HPText", hpSection.transform);
            SetRT(hpTextGO, V(0,0), V(1,0), new Vector2(0,2), new Vector2(0, 18));
            var hpTMP = TMP(hpTextGO, "100 / 100", 12f, ColSubtext);
            hpTMP.alignment = TextAlignmentOptions.Right;

            // ═══════════════════════════════════════════════════════════════════════
            // HAUT GAUCHE — Effets actifs (potions)
            // ═══════════════════════════════════════════════════════════════════════
            GameObject topLeft = Recreate("TopLeft", canvasGO.transform);
            GetOrAdd<CanvasGroup>(topLeft);
            SetRT(topLeft, V(0,1), V(0,1), new Vector2(20,-20), new Vector2(210, 300));

            GameObject effectsContainerGO = Child("EffectsContainer", topLeft.transform);
            StretchRT(effectsContainerGO);
            var vlgEff = GetOrAdd<VerticalLayoutGroup>(effectsContainerGO);
            vlgEff.spacing              = 6f;
            vlgEff.childAlignment       = TextAnchor.UpperLeft;
            vlgEff.childControlWidth    = true;
            vlgEff.childControlHeight   = false;
            vlgEff.childForceExpandWidth  = true;
            vlgEff.childForceExpandHeight = false;
            var csfEff = GetOrAdd<ContentSizeFitter>(effectsContainerGO);
            csfEff.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ═══════════════════════════════════════════════════════════════════════
            // HAUT DROIT — Récolte + Rituel
            // ═══════════════════════════════════════════════════════════════════════
            GameObject topRight = Recreate("TopRight", canvasGO.transform);
            GetOrAdd<CanvasGroup>(topRight);
            SetRT(topRight, V(1,1), V(1,1), new Vector2(-20,-20), new Vector2(260, 200));

            // Récolte
            GameObject harvestGroup = Child("HarvestGroup", topRight.transform);
            SetRT(harvestGroup, V(0,1), V(1,1), new Vector2(0,-50), new Vector2(0, 50));

            GameObject harvestLabelGO = Child("HarvestLabel", harvestGroup.transform);
            SetRT(harvestLabelGO, V(0,1), V(1,1), new Vector2(0,-22), new Vector2(0, 22));
            var harvestLabelTMP = TMP(harvestLabelGO, "RÉCOLTE", 12f, ColSubtext);
            harvestLabelTMP.fontStyle = FontStyles.Bold;
            harvestLabelTMP.alignment = TextAlignmentOptions.Right;

            GameObject harvestBarBgGO = Child("HarvestBarBg", harvestGroup.transform);
            SetRT(harvestBarBgGO, V(0,1), V(1,1), new Vector2(0,-38), new Vector2(0, 12));
            GetOrAdd<Image>(harvestBarBgGO).color = ColBar;

            GameObject harvestFillGO = Child("HarvestBarFill", harvestBarBgGO.transform);
            StretchRT(harvestFillGO);
            var harvestFillImg = GetOrAdd<Image>(harvestFillGO);
            harvestFillImg.color      = ColHarvest;
            harvestFillImg.type       = Image.Type.Filled;
            harvestFillImg.fillMethod = Image.FillMethod.Horizontal;
            harvestFillImg.fillAmount = 0f;

            GameObject harvestTextGO = Child("HarvestText", harvestGroup.transform);
            SetRT(harvestTextGO, V(0,0), V(1,0), new Vector2(0,2), new Vector2(0, 15));
            var harvestTMP = TMP(harvestTextGO, "0 / 150", 11f, ColSubtext);
            harvestTMP.alignment = TextAlignmentOptions.Right;

            // Séparateur
            GameObject sepGO = Child("Separator", topRight.transform);
            SetRT(sepGO, V(0,1), V(1,1), new Vector2(0,-62), new Vector2(0, 1));
            GetOrAdd<Image>(sepGO).color = new Color(1,1,1,0.10f);

            // Rituel
            GameObject ritualGroup = Child("RitualGroup", topRight.transform);
            SetRT(ritualGroup, V(0,1), V(1,1), new Vector2(0,-124), new Vector2(0, 55));

            GameObject ritualLabelGO = Child("RitualLabel", ritualGroup.transform);
            SetRT(ritualLabelGO, V(0,1), V(1,1), new Vector2(0,-22), new Vector2(0, 22));
            var ritualLabelTMP = TMP(ritualLabelGO, "RITUEL", 12f, ColSubtext);
            ritualLabelTMP.fontStyle = FontStyles.Bold;
            ritualLabelTMP.alignment = TextAlignmentOptions.Right;

            GameObject ritualBarBgGO = Child("RitualBarBg", ritualGroup.transform);
            SetRT(ritualBarBgGO, V(0,1), V(1,1), new Vector2(0,-38), new Vector2(0, 12));
            GetOrAdd<Image>(ritualBarBgGO).color = ColBar;

            GameObject ritualFillGO = Child("RitualBarFill", ritualBarBgGO.transform);
            StretchRT(ritualFillGO);
            var ritualFillImg = GetOrAdd<Image>(ritualFillGO);
            ritualFillImg.color      = ColPurple;
            ritualFillImg.type       = Image.Type.Filled;
            ritualFillImg.fillMethod = Image.FillMethod.Horizontal;
            ritualFillImg.fillAmount = 0f;

            GameObject ritualTextGO = Child("RitualText", ritualGroup.transform);
            SetRT(ritualTextGO, V(0,0), V(1,0), new Vector2(0,2), new Vector2(0, 15));
            var ritualTMP = TMP(ritualTextGO, "0 / 2000", 11f, ColSubtext);
            ritualTMP.alignment = TextAlignmentOptions.Right;

            // Prompt rituel disponible (désactivé)
            GameObject ritualPromptGO = Child("RitualPrompt", topRight.transform);
            SetRT(ritualPromptGO, V(0,1), V(1,1), new Vector2(0,-172), new Vector2(0, 32));
            GetOrAdd<Image>(ritualPromptGO).color = new Color(0.35f, 0.08f, 0.55f, 0.90f);
            GameObject rpTextGO = Child("PromptText", ritualPromptGO.transform);
            StretchRT(rpTextGO);
            var rpTMP = TMP(rpTextGO, "RITUEL DISPONIBLE  [E]", 13f, Color.white);
            rpTMP.fontStyle = FontStyles.Bold;
            rpTMP.alignment = TextAlignmentOptions.Center;
            ritualPromptGO.SetActive(false);

            // ═══════════════════════════════════════════════════════════════════════
            // BAS CENTRE — Slot d'inventaire
            // ═══════════════════════════════════════════════════════════════════════
            GameObject bottomCenter = Recreate("BottomCenter", canvasGO.transform);
            GetOrAdd<CanvasGroup>(bottomCenter);
            SetRT(bottomCenter, V(.5f,0), V(.5f,0), new Vector2(0, 20), new Vector2(90, 90));
            GetOrAdd<Image>(bottomCenter).color = ColDarker;

            // Slot vide
            GameObject slotEmptyGO = Child("SlotEmpty", bottomCenter.transform);
            StretchRT(slotEmptyGO);
            GetOrAdd<Image>(slotEmptyGO).color = new Color(1,1,1,0.08f);
            GameObject slotEmptyLabelGO = Child("EmptyLabel", slotEmptyGO.transform);
            StretchRT(slotEmptyLabelGO);
            var emptyLabelTMP = TMP(slotEmptyLabelGO, "+", 26f, new Color(1,1,1,0.18f));
            emptyLabelTMP.alignment = TextAlignmentOptions.Center;

            // Slot plein (désactivé)
            GameObject slotFullGO = Child("SlotFull", bottomCenter.transform);
            StretchRT(slotFullGO);
            GetOrAdd<Image>(slotFullGO).color = ColDarker;
            slotFullGO.SetActive(false);

            GameObject slotIconGO = Child("SlotIcon", slotFullGO.transform);
            SetRT(slotIconGO, V(.1f,.25f), V(.9f,.95f), Vector2.zero, Vector2.zero);
            var slotIconImg = GetOrAdd<Image>(slotIconGO);
            slotIconImg.color = Color.white;

            GameObject slotNameGO = Child("SlotName", slotFullGO.transform);
            SetRT(slotNameGO, V(0,0), V(1,0), new Vector2(0,2), new Vector2(0, 17));
            var slotNameTMP = TMP(slotNameGO, "Potion", 11f, ColSubtext);
            slotNameTMP.alignment = TextAlignmentOptions.Center;

            // ═══════════════════════════════════════════════════════════════════════
            // DEBUG PANEL (haut centre, F1 pour afficher, désactivé par défaut)
            // ═══════════════════════════════════════════════════════════════════════
            GameObject debugPanelGO = Recreate("DebugPanel", canvasGO.transform);
            SetRT(debugPanelGO, V(.5f,1), V(.5f,1), new Vector2(0,-62), new Vector2(760, 50));
            GetOrAdd<Image>(debugPanelGO).color = new Color(0.05f, 0.05f, 0.05f, 0.92f);
            GetOrAdd<DebugPanelHUD>(debugPanelGO);
            debugPanelGO.SetActive(false);

            GameObject dbTitleGO = Child("Title", debugPanelGO.transform);
            SetRT(dbTitleGO, V(0,0), V(0,1), new Vector2(0,0), new Vector2(130,0));
            var dbTitleTMP = TMP(dbTitleGO, "◈ DEBUG\n[F1]", 11f, new Color(0.4f, 0.9f, 0.5f));
            dbTitleTMP.fontStyle = FontStyles.Bold;
            dbTitleTMP.alignment = TextAlignmentOptions.Center;

            GameObject dbButtonsGO = Child("Buttons", debugPanelGO.transform);
            StretchRT(dbButtonsGO);
            var dbBtnRT = GetOrAdd<RectTransform>(dbButtonsGO);
            dbBtnRT.offsetMin = new Vector2(134, 4);
            dbBtnRT.offsetMax = new Vector2(-4, -4);
            var hlg = GetOrAdd<HorizontalLayoutGroup>(dbButtonsGO);
            hlg.spacing              = 5f;
            hlg.childAlignment       = TextAnchor.MiddleLeft;
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            DebugBtn("DamageBtn",    "-15 HP",     new Color(0.60f,0.12f,0.12f), dbButtonsGO.transform);
            DebugBtn("HealBtn",      "+20 HP",     new Color(0.12f,0.50f,0.18f), dbButtonsGO.transform);
            DebugBtn("HarvestBtn",   "+5 Récolte", new Color(0.20f,0.35f,0.15f), dbButtonsGO.transform);
            DebugBtn("RitualBtn",    "+80 Rituel", new Color(0.30f,0.10f,0.50f), dbButtonsGO.transform);
            DebugBtn("ToggleSlotBtn","Slot ↕",     new Color(0.20f,0.25f,0.40f), dbButtonsGO.transform);
            DebugBtn("DayNightBtn",  "Jour/Nuit",  new Color(0.35f,0.30f,0.10f), dbButtonsGO.transform);
            DebugBtn("RoleBtn",      "Rôle →",    new Color(0.15f,0.30f,0.45f), dbButtonsGO.transform);

            // ═══════════════════════════════════════════════════════════════════════
            // ROLE SELECTION PANEL (conservé pour compatibilité)
            // ═══════════════════════════════════════════════════════════════════════
            AddIfMissing<RoleSelectionUI>(canvasGO);
            GameObject rolePanel = FindOrCreate("RoleSelectionPanel", canvasGO.transform);
            StretchRT(rolePanel);
            GetOrAdd<Image>(rolePanel).color = new Color(0, 0, 0, 0.85f);

            GameObject btnContainer = FindOrCreate("ButtonContainer", rolePanel.transform);
            var vlgRole = GetOrAdd<VerticalLayoutGroup>(btnContainer);
            vlgRole.spacing = 10f;
            vlgRole.childAlignment = TextAnchor.UpperCenter;
            vlgRole.childControlWidth = true;
            vlgRole.childControlHeight = false;
            vlgRole.childForceExpandWidth = true;
            vlgRole.childForceExpandHeight = false;
            vlgRole.padding = new RectOffset(20, 20, 20, 20);
            GetOrAdd<ContentSizeFitter>(btnContainer).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SetRT(btnContainer, V(.5f,.5f), V(.5f,.5f), Vector2.zero, new Vector2(450f, 600f));
            rolePanel.SetActive(false);

            var roleButtonPrefab = BuildOrLoadRoleButtonPrefab();

            // ── Câblage automatique HUDManager ────────────────────────────────────
            var so = new SerializedObject(hudMgr);
            so.FindProperty("damageFlashImage").objectReferenceValue    = flashImg;
            so.FindProperty("vignetteImage").objectReferenceValue       = vigImg;
            so.FindProperty("dayNightIcon").objectReferenceValue        = dnIcon;
            so.FindProperty("dayNightLabel").objectReferenceValue       = dnLabel;
            so.FindProperty("interactionPromptGO").objectReferenceValue = interPromptGO;
            so.FindProperty("interactionKey").objectReferenceValue      = interKey;
            so.FindProperty("interactionAction").objectReferenceValue   = interAction;
            so.FindProperty("roleBadgeBg").objectReferenceValue         = roleBadgeImg;
            so.FindProperty("roleIconText").objectReferenceValue        = roleIconTMP;
            so.FindProperty("roleNameText").objectReferenceValue        = roleNameTMP;
            so.FindProperty("hpBarFill").objectReferenceValue           = hpFillImg;
            so.FindProperty("hpText").objectReferenceValue              = hpTMP;
            so.FindProperty("effectsContainer").objectReferenceValue    = effectsContainerGO.transform;
            so.FindProperty("harvestBarFill").objectReferenceValue      = harvestFillImg;
            so.FindProperty("harvestText").objectReferenceValue         = harvestTMP;
            so.FindProperty("ritualBarFill").objectReferenceValue       = ritualFillImg;
            so.FindProperty("ritualText").objectReferenceValue          = ritualTMP;
            so.FindProperty("ritualPromptGO").objectReferenceValue      = ritualPromptGO;
            so.FindProperty("slotFullGO").objectReferenceValue          = slotFullGO;
            so.FindProperty("slotEmptyGO").objectReferenceValue         = slotEmptyGO;
            so.FindProperty("slotIconImage").objectReferenceValue       = slotIconImg;
            so.FindProperty("slotNameText").objectReferenceValue        = slotNameTMP;
            so.FindProperty("debugPanelGO").objectReferenceValue        = debugPanelGO;
            so.ApplyModifiedProperties();

            // Câblage RoleSelectionUI
            var soRole = new SerializedObject(canvasGO.GetComponent<RoleSelectionUI>());
            soRole.FindProperty("panelRoot").objectReferenceValue        = rolePanel;
            soRole.FindProperty("buttonContainer").objectReferenceValue  = btnContainer.transform;
            soRole.FindProperty("roleButtonPrefab").objectReferenceValue = roleButtonPrefab;
            soRole.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = canvasGO;

            Debug.Log(
                "[UISetup] HUD Lycans créé ✓\n\n" +
                "Structure :\n" +
                "  BottomLeft   — Badge de rôle + Barre de vie\n" +
                "  TopLeft      — Effets actifs (potions)\n" +
                "  TopRight     — Récolte + Rituel\n" +
                "  BottomCenter — Slot d'inventaire\n" +
                "  Centre       — Réticule + Prompt interaction\n" +
                "  TopCentre    — Indicateur Jour/Nuit\n" +
                "  DebugPanel   — [F1] pour afficher/masquer\n\n" +
                "Debug : touche F1 en Play mode pour ouvrir le panneau.");
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════════════════════

        static void DebugBtn(string name, string label, Color bgColor, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(88, 0);
            GetOrAdd<Image>(go).color = bgColor;
            var btn    = GetOrAdd<Button>(go);
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
            btn.colors = colors;

            var lGO = new GameObject("Label");
            lGO.transform.SetParent(go.transform, false);
            var lRT = lGO.AddComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = lRT.offsetMax = Vector2.zero;
            var tmp = lGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 11f; tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
        }

        static GameObject BuildOrLoadRoleButtonPrefab()
        {
            const string path = PrefabsFolder + "/RoleButtonPrefab.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject("RoleButtonPrefab");
            root.AddComponent<RectTransform>().sizeDelta = new Vector2(400f, 80f);
            root.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            root.AddComponent<Button>();

            var nameGO = new GameObject("RoleName");
            nameGO.transform.SetParent(root.transform, false);
            var nRT = nameGO.AddComponent<RectTransform>();
            nRT.anchorMin = V(.0f,.5f); nRT.anchorMax = Vector2.one;
            nRT.offsetMin = new Vector2(16,0); nRT.offsetMax = new Vector2(-10,0);
            var nTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nTMP.text = "Rôle"; nTMP.fontSize = 20f; nTMP.fontStyle = FontStyles.Bold; nTMP.color = Color.white;

            var descGO = new GameObject("Description");
            descGO.transform.SetParent(root.transform, false);
            var dRT = descGO.AddComponent<RectTransform>();
            dRT.anchorMin = Vector2.zero; dRT.anchorMax = V(1f,.5f);
            dRT.offsetMin = new Vector2(16,0); dRT.offsetMax = new Vector2(-10,0);
            var dTMP = descGO.AddComponent<TextMeshProUGUI>();
            dTMP.text = "Description"; dTMP.fontSize = 14f; dTMP.color = new Color(0.8f,0.8f,0.8f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // Recréation propre (supprime l'ancien pour éviter les états corrompus)
        static GameObject Recreate(string name, Transform parent)
        {
            Transform ex = parent.Find(name);
            if (ex != null) Object.DestroyImmediate(ex.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        // Trouve ou crée un enfant (pour les éléments qui peuvent exister)
        static GameObject FindOrCreate(string name, Transform parent)
        {
            Transform ex = parent.Find(name);
            if (ex != null) return ex.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        // Crée ou retrouve un sous-enfant
        static GameObject Child(string name, Transform parent)
        {
            Transform ex = parent.Find(name);
            if (ex != null) return ex.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static void StretchRT(GameObject go)
        {
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void SetRT(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = (anchorMin + anchorMax) * 0.5f;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        static TextMeshProUGUI TMP(GameObject go, string text, float fontSize, Color color)
        {
            var tmp = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
            => go.TryGetComponent(out T c) ? c : go.AddComponent<T>();

        static void AddIfMissing<T>(GameObject go) where T : Component
        { if (!go.TryGetComponent<T>(out _)) go.AddComponent<T>(); }

        static Vector2 V(float x, float y) => new Vector2(x, y);

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
