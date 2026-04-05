namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Sous-type d'une Potion.
    /// Utilisé uniquement quand ItemData.Type == ItemType.Potion.
    ///
    /// Self = peut être consommée par le joueur lui-même (F)
    /// Thrown = peut être jetée sur autrui (Q), applique l'effet à l'impact
    ///
    /// Vitesse    | self ✅ | thrown ✅ | durée 60s
    /// Poison     | self ❌ | thrown ✅ | durée 10s (−20% vit) + −50% PV immédiats
    /// Géant      | self ✅ | thrown ❌ | durée 45s
    /// Invisible  | self ✅ | thrown ❌ | durée 30s
    /// Ouïe       | self ✅ | thrown ❌ | durée 60s
    /// Vie        | self ✅ | thrown ❌ | durée jusqu'à usage (résurrection passive)
    /// Aveuglant  | self ❌ | thrown ✅ | durée 45s (cible aveuglée)
    /// </summary>
    public enum PotionType
    {
        Vitesse,
        Poison,
        Géant,
        Invisible,
        Ouïe,
        Vie,
        Aveuglant,
    }
}
