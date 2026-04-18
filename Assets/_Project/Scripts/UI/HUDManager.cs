using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectFPS.UI
{
    public enum RoleType
    {
        Villageois,
        Chasseur,
        Loup,
        Maire,
        Sorcier,
        Ancien,
        FilsDuChasseur
    }

    public class HUDManager : MonoBehaviour
    {
        // ── Damage Flash ─────────────────────────────────────────────────────────
        [Header("Damage Flash")]
        [SerializeField] private Image damageFlashImage;

        // ── Vignette ─────────────────────────────────────────────────────────────
        [Header("Vignette")]
        [SerializeField] private Image vignetteImage;

        // ── Jour / Nuit ───────────────────────────────────────────────────────────
        [Header("Jour / Nuit")]
        [SerializeField] private TextMeshProUGUI dayNightIcon;
        [SerializeField] private TextMeshProUGUI dayNightLabel;

        // ── Interaction Prompt ────────────────────────────────────────────────────
        [Header("Interaction Prompt")]
        [SerializeField] private GameObject interactionPromptGO;
        [SerializeField] private TextMeshProUGUI interactionKey;
        [SerializeField] private TextMeshProUGUI interactionAction;

        // ── Role Badge ────────────────────────────────────────────────────────────
        [Header("Role Badge")]
        [SerializeField] private Image roleBadgeBg;
        [SerializeField] private TextMeshProUGUI roleIconText;
        [SerializeField] private TextMeshProUGUI roleNameText;

        // ── HP ────────────────────────────────────────────────────────────────────
        [Header("HP")]
        [SerializeField] private Image hpBarFill;
        [SerializeField] private TextMeshProUGUI hpText;

        // ── Effects ───────────────────────────────────────────────────────────────
        [Header("Effects")]
        [SerializeField] private Transform effectsContainer;

        // ── Harvest ───────────────────────────────────────────────────────────────
        [Header("Harvest")]
        [SerializeField] private Image harvestBarFill;
        [SerializeField] private TextMeshProUGUI harvestText;

        // ── Ritual ────────────────────────────────────────────────────────────────
        [Header("Ritual")]
        [SerializeField] private Image ritualBarFill;
        [SerializeField] private TextMeshProUGUI ritualText;
        [SerializeField] private GameObject ritualPromptGO;

        // ── Inventory Slot ────────────────────────────────────────────────────────
        [Header("Inventory Slot")]
        [SerializeField] private GameObject slotFullGO;
        [SerializeField] private GameObject slotEmptyGO;
        [SerializeField] private Image slotIconImage;
        [SerializeField] private TextMeshProUGUI slotNameText;

        // ── Debug ─────────────────────────────────────────────────────────────────
        [Header("Debug")]
        [SerializeField] private GameObject debugPanelGO;

        // ── Private State ─────────────────────────────────────────────────────────
        private int _hp        = 100;
        private int _maxHp     = 100;
        private int _harvest   = 0;
        private int _maxHarvest = 150;
        private int _ritual    = 0;
        private int _maxRitual = 2000;
        private bool _isNight  = false;
        private int _roleIndex = 0;
        private bool _slotFull = false;

        // ── Static Colors ─────────────────────────────────────────────────────────
        private static readonly Color HpGreen   = new Color(0.15f, 0.75f, 0.25f);
        private static readonly Color HpYellow  = new Color(0.85f, 0.75f, 0.10f);
        private static readonly Color HpRed     = new Color(0.85f, 0.15f, 0.10f);
        private static readonly Color RitualColor = new Color(0.55f, 0.15f, 0.85f);

        // ── Role Data ─────────────────────────────────────────────────────────────
        private static readonly (string icon, string name, Color color)[] RoleData =
        {
            ("◈", "VILLAGEOIS",       new Color(0.70f, 0.65f, 0.50f)),
            ("⚔", "CHASSEUR",         new Color(0.60f, 0.35f, 0.15f)),
            ("☽", "LOUP",             new Color(0.60f, 0.10f, 0.20f)),
            ("♔", "MAIRE",            new Color(0.20f, 0.40f, 0.70f)),
            ("✦", "SORCIER",          new Color(0.50f, 0.15f, 0.70f)),
            ("♾", "ANCIEN",           new Color(0.65f, 0.65f, 0.60f)),
            ("⚔", "FILS DU CHASSEUR", new Color(0.70f, 0.45f, 0.15f)),
        };

        // ─────────────────────────────────────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (interactionPromptGO != null) interactionPromptGO.SetActive(false);
            if (ritualPromptGO     != null) ritualPromptGO.SetActive(false);
            if (debugPanelGO       != null) debugPanelGO.SetActive(false);
            damageFlashImage?.gameObject.SetActive(false);

            RefreshHP();
            RefreshHarvest();
            RefreshRitual();
            RefreshRole();
            RefreshDayNight();
            RefreshSlot();

            StartCoroutine(IntroAnimation());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && debugPanelGO != null)
                debugPanelGO.SetActive(!debugPanelGO.activeSelf);

            if (_hp < 30 && _hp > 0 && hpBarFill != null)
            {
                float t = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                hpBarFill.color = Color.Lerp(HpRed, new Color(1f, 0.4f, 0.3f), t);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────────

        public void TakeDamage(int amount)
        {
            _hp = Mathf.Clamp(_hp - amount, 0, _maxHp);
            RefreshHP();
            StartCoroutine(DamageFlash());
        }

        public void Heal(int amount)
        {
            _hp = Mathf.Clamp(_hp + amount, 0, _maxHp);
            RefreshHP();
        }

        public void SetHP(int current, int max)
        {
            _maxHp = max;
            _hp    = Mathf.Clamp(current, 0, _maxHp);
            RefreshHP();
        }

        public void AddHarvest(int amount)
        {
            _harvest = Mathf.Clamp(_harvest + amount, 0, _maxHarvest);
            RefreshHarvest();
        }

        public void SetHarvest(int current, int max)
        {
            _maxHarvest = max;
            _harvest    = Mathf.Clamp(current, 0, _maxHarvest);
            RefreshHarvest();
        }

        public void AddRitual(int amount)
        {
            _ritual = Mathf.Clamp(_ritual + amount, 0, _maxRitual);
            RefreshRitual();
        }

        public void SetRitual(int current, int max)
        {
            _maxRitual = max;
            _ritual    = Mathf.Clamp(current, 0, _maxRitual);
            RefreshRitual();
        }

        public void ToggleDayNight()
        {
            _isNight = !_isNight;
            RefreshDayNight();
        }

        public void SetDayNight(bool isNight)
        {
            _isNight = isNight;
            RefreshDayNight();
        }

        public void CycleRole()
        {
            _roleIndex = (_roleIndex + 1) % 7;
            RefreshRole();
        }

        public void SetRole(RoleType role)
        {
            _roleIndex = (int)role;
            RefreshRole();
        }

        public void ToggleInventorySlot()
        {
            if (_slotFull)
                ClearInventorySlot();
            else
                SetInventorySlot("Potion", null);
        }

        public void SetInventorySlot(string itemName, Sprite icon)
        {
            _slotFull = true;
            if (slotNameText  != null) slotNameText.text  = itemName;
            if (slotIconImage != null) slotIconImage.sprite = icon;
            RefreshSlot();
        }

        public void ClearInventorySlot()
        {
            _slotFull = false;
            RefreshSlot();
        }

        public void ShowInteractionPrompt(string keyLabel, string action)
        {
            if (interactionKey    != null) interactionKey.text    = keyLabel;
            if (interactionAction != null) interactionAction.text = action;
            if (interactionPromptGO != null) interactionPromptGO.SetActive(true);
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptGO != null) interactionPromptGO.SetActive(false);
        }

        public void AddEffect(string effectName, float duration)
        {
            StartCoroutine(ShowEffect(effectName, duration));
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Private Refresh Methods
        // ─────────────────────────────────────────────────────────────────────────

        private void RefreshHP()
        {
            if (hpBarFill != null)
            {
                float ratio = _maxHp > 0 ? (float)_hp / _maxHp : 0f;
                hpBarFill.fillAmount = ratio;

                float pct = ratio * 100f;
                if (pct > 60f)
                    hpBarFill.color = HpGreen;
                else if (pct > 30f)
                    hpBarFill.color = HpYellow;
                else
                    hpBarFill.color = HpRed;
            }

            if (hpText != null)
                hpText.text = $"{_hp} / {_maxHp}";
        }

        private void RefreshHarvest()
        {
            if (harvestBarFill != null)
                harvestBarFill.fillAmount = _maxHarvest > 0 ? (float)_harvest / _maxHarvest : 0f;

            if (harvestText != null)
                harvestText.text = $"{_harvest} / {_maxHarvest}";
        }

        private void RefreshRitual()
        {
            if (ritualBarFill != null)
                ritualBarFill.fillAmount = _maxRitual > 0 ? (float)_ritual / _maxRitual : 0f;

            if (ritualText != null)
                ritualText.text = $"{_ritual} / {_maxRitual}";

            if (ritualPromptGO != null)
            {
                if (_ritual >= _maxRitual && !ritualPromptGO.activeSelf)
                {
                    ritualPromptGO.SetActive(true);
                    StartCoroutine(RitualPromptBlink());
                }
                else if (_ritual < _maxRitual)
                {
                    ritualPromptGO.SetActive(false);
                }
            }
        }

        private void RefreshRole()
        {
            var (icon, roleName, color) = RoleData[_roleIndex];

            if (roleBadgeBg != null)
            {
                Color c = color;
                c.a = 0.80f;
                roleBadgeBg.color = c;
            }

            if (roleIconText != null) roleIconText.text = icon;
            if (roleNameText != null) roleNameText.text = roleName;
        }

        private void RefreshDayNight()
        {
            if (dayNightIcon  != null) dayNightIcon.text  = _isNight ? "☽" : "✦";
            if (dayNightLabel != null) dayNightLabel.text = _isNight ? "NUIT" : "JOUR";

            if (vignetteImage != null)
            {
                Color c = vignetteImage.color;
                c.a = _isNight ? 0.55f : 0.30f;
                vignetteImage.color = c;
            }
        }

        private void RefreshSlot()
        {
            slotFullGO?.SetActive(_slotFull);
            slotEmptyGO?.SetActive(!_slotFull);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Coroutines
        // ─────────────────────────────────────────────────────────────────────────

        private IEnumerator DamageFlash()
        {
            if (damageFlashImage == null) yield break;

            damageFlashImage.gameObject.SetActive(true);
            Color startColor = new Color(1f, 0f, 0f, 0.35f);
            damageFlashImage.color = startColor;

            float elapsed  = 0f;
            float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.35f, 0f, elapsed / duration);
                damageFlashImage.color = new Color(1f, 0f, 0f, alpha);
                yield return null;
            }

            damageFlashImage.color = new Color(1f, 0f, 0f, 0f);
            damageFlashImage.gameObject.SetActive(false);
        }

        private IEnumerator RitualPromptBlink()
        {
            while (ritualPromptGO != null && ritualPromptGO.activeSelf)
            {
                yield return new WaitForSeconds(0.45f);

                if (ritualPromptGO == null) yield break;

                if (_ritual >= _maxRitual)
                    ritualPromptGO.SetActive(false);
                else
                    yield break;

                yield return new WaitForSeconds(0.35f);

                if (ritualPromptGO != null)
                    ritualPromptGO.SetActive(true);
            }
        }

        private IEnumerator ShowEffect(string effectName, float duration)
        {
            if (effectsContainer == null) yield break;

            // ── Root entry ──────────────────────────────────────────────────────
            GameObject entryGO = new GameObject(effectName + "_Effect");
            entryGO.transform.SetParent(effectsContainer, false);

            RectTransform entryRect = entryGO.AddComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(200f, 56f);

            Image entryBg = entryGO.AddComponent<Image>();
            entryBg.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);

            CanvasGroup cg = entryGO.AddComponent<CanvasGroup>();

            // ── Name label ──────────────────────────────────────────────────────
            GameObject nameGO = new GameObject("Name");
            nameGO.transform.SetParent(entryGO.transform, false);

            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin  = new Vector2(0f, 0.5f);
            nameRect.anchorMax  = new Vector2(1f, 1f);
            nameRect.offsetMin  = new Vector2(4f, 0f);
            nameRect.offsetMax  = new Vector2(-4f, 0f);

            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text      = effectName.ToUpper();
            nameTMP.fontSize  = 13f;
            nameTMP.color     = Color.white;
            nameTMP.fontStyle = FontStyles.Bold;

            // ── Bar background ──────────────────────────────────────────────────
            GameObject barBgGO = new GameObject("BarBg");
            barBgGO.transform.SetParent(entryGO.transform, false);

            RectTransform barBgRect = barBgGO.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0f, 0f);
            barBgRect.anchorMax = new Vector2(1f, 0.5f);
            barBgRect.offsetMin = new Vector2(4f, 4f);
            barBgRect.offsetMax = new Vector2(-4f, 0f);

            Image barBgImg = barBgGO.AddComponent<Image>();
            barBgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // ── Bar fill ────────────────────────────────────────────────────────
            GameObject barFillGO = new GameObject("BarFill");
            barFillGO.transform.SetParent(barBgGO.transform, false);

            RectTransform barFillRect = barFillGO.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;

            Image barFillImg = barFillGO.AddComponent<Image>();
            barFillImg.color      = new Color(0.3f, 0.75f, 0.4f);
            barFillImg.type       = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;

            // ── Timer label ─────────────────────────────────────────────────────
            GameObject timerGO = new GameObject("Timer");
            timerGO.transform.SetParent(entryGO.transform, false);

            RectTransform timerRect = timerGO.AddComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 0.5f);
            timerRect.anchorMax = new Vector2(1f,   1f);
            timerRect.offsetMin = new Vector2(0f,   0f);
            timerRect.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI timerTMP = timerGO.AddComponent<TextMeshProUGUI>();
            timerTMP.fontSize          = 12f;
            timerTMP.color             = Color.grey;
            timerTMP.alignment         = TextAlignmentOptions.Right;

            // ── Animate ─────────────────────────────────────────────────────────
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float remaining = Mathf.Max(0f, duration - elapsed);

                if (barFillImg  != null) barFillImg.fillAmount = 1f - (elapsed / duration);
                if (timerTMP    != null) timerTMP.text         = $"{remaining:F0}s";

                yield return null;
            }

            Destroy(entryGO);
        }

        private IEnumerator IntroAnimation()
        {
            string[] panelNames = { "BottomLeft", "TopLeft", "TopRight", "BottomCenter", "DayNightIndicator" };

            GameObject[] panels = new GameObject[panelNames.Length];

            for (int i = 0; i < panelNames.Length; i++)
            {
                Transform t = transform.Find(panelNames[i]);
                if (t == null) continue;

                panels[i] = t.gameObject;

                CanvasGroup cg = panels[i].GetComponent<CanvasGroup>();
                if (cg == null) cg = panels[i].AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }

            yield return new WaitForSeconds(0.15f);

            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null) continue;
                StartCoroutine(FadeIn(panels[i], 0.35f));
                yield return new WaitForSeconds(0.07f);
            }
        }

        private IEnumerator FadeIn(GameObject go, float dur)
        {
            if (go == null) yield break;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            cg.alpha = 0f;

            while (elapsed < dur)
            {
                elapsed  += Time.deltaTime;
                cg.alpha  = Mathf.Clamp01(elapsed / dur);
                yield return null;
            }

            cg.alpha = 1f;
        }
    }
}
