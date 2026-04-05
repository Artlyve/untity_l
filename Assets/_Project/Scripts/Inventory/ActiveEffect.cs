using UnityEngine;

namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Représente un effet de potion actif sur un joueur.
    /// Utilisé par EffectSystem pour le suivi et l'affichage HUD.
    /// </summary>
    public class ActiveEffect
    {
        public PotionType Type          { get; }
        public string     DisplayName   { get; }
        public Sprite     Icon          { get; }
        public float      TotalDuration { get; }
        public float      TimeRemaining { get; set; }

        /// <summary>Progression 0→1 (1 = plein, 0 = expiré).</summary>
        public float Progress => TotalDuration > 0f ? TimeRemaining / TotalDuration : 0f;

        public ActiveEffect(PotionType type, float duration, string displayName, Sprite icon)
        {
            Type          = type;
            TotalDuration = duration;
            TimeRemaining = duration;
            DisplayName   = displayName;
            Icon          = icon;
        }
    }
}
