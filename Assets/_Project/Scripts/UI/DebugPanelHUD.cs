using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace ProjectFPS.UI
{
    public class DebugPanelHUD : MonoBehaviour
    {
        private HUDManager _hud;

        private void Awake()
        {
            _hud = GetComponentInParent<HUDManager>();

            if (_hud == null)
            {
                Debug.LogWarning("[DebugPanelHUD] Could not find HUDManager in parent hierarchy.");
                return;
            }

            WireBtn("Buttons/DamageBtn",      () => _hud.TakeDamage(15));
            WireBtn("Buttons/HealBtn",         () => _hud.Heal(20));
            WireBtn("Buttons/HarvestBtn",      () => _hud.AddHarvest(5));
            WireBtn("Buttons/RitualBtn",       () => _hud.AddRitual(80));
            WireBtn("Buttons/ToggleSlotBtn",   () => _hud.ToggleInventorySlot());
            WireBtn("Buttons/DayNightBtn",     () => _hud.ToggleDayNight());
            WireBtn("Buttons/RoleBtn",         () => _hud.CycleRole());
        }

        private void WireBtn(string path, UnityAction action)
        {
            Transform t = transform.Find(path);
            if (t == null)
            {
                Debug.LogWarning($"[DebugPanelHUD] Could not find button at path: {path}");
                return;
            }

            Button btn = t.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogWarning($"[DebugPanelHUD] No Button component found at path: {path}");
                return;
            }

            btn.onClick.AddListener(action);
        }
    }
}
