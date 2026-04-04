namespace ProjectFPS.Inventory
{
    /// <summary>
    /// Types d'objets du jeu.
    ///
    /// Objets utilitaires (1 slot par joueur, 2 pour Fils_Chasseur) :
    ///   Potion, Piège, Balle, Fumigène, Armure, MalusEnnemi
    ///
    /// Objets de récolte (convertis en points, jamais stockés physiquement) :
    ///   Resource
    /// </summary>
    public enum ItemType
    {
        Potion,
        Piège,
        Balle,
        Fumigène,
        Armure,
        MalusEnnemi,
        Resource       // objet de récolte → convertis en points via ResourceSystem
    }
}
