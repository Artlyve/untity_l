using UnityEngine;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Cache le mesh du joueur à sa propre caméra FPS tout en le laissant
    /// visible pour les autres joueurs et pour les ombres.
    ///
    /// SETUP (une seule fois) :
    ///   1. Dans Edit → Project Settings → Tags and Layers, créez le layer
    ///      "PlayerBody" (n'importe quel slot libre, ex. layer 6).
    ///   2. Assignez ce layer dans le champ playerBodyLayer de ce composant.
    ///   3. Assignez la caméra FPS du joueur (playerCamera).
    ///   4. Ce script trouvera automatiquement tous les Renderer enfants du joueur
    ///      et les basculera sur le layer PlayerBody.
    ///   5. La caméra FPS exclura automatiquement ce layer de son culling mask.
    ///
    /// Résultat :
    ///   • Le joueur ne voit plus son propre corps.
    ///   • Les autres caméras (celles des autres joueurs, caméras de shadow)
    ///     voient toujours le corps car elles n'excluent pas ce layer.
    /// </summary>
    public class FPSBodyHider : MonoBehaviour
    {
        [Header("Layer du corps joueur")]
        [Tooltip("Layer dédié au mesh du corps (ex. 'PlayerBody'). " +
                 "Créez-le dans Project Settings → Tags and Layers.")]
        [SerializeField] private int playerBodyLayer = 6;

        [Header("Références")]
        [Tooltip("Caméra FPS du joueur (celle dans CameraHolder).")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Racine du mesh du joueur (le GameObject qui porte les Renderers). " +
                 "Laissez vide pour chercher automatiquement dans les enfants.")]
        [SerializeField] private Transform bodyRoot;

        private void Awake()
        {
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            ApplyBodyLayer();
            ExcludeLayerFromCamera();
        }

        /// <summary>
        /// Bascule tous les Renderers du corps sur playerBodyLayer.
        /// Les ombres (shadow caster) ne dépendent pas du culling mask → restent visibles.
        /// </summary>
        private void ApplyBodyLayer()
        {
            Transform root = bodyRoot != null ? bodyRoot : transform;

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                // Ne pas toucher les Renderers qui seraient sur d'autres GameObjects
                // (ex. items tenus en main) — on cible uniquement les enfants du body root
                r.gameObject.layer = playerBodyLayer;
            }
        }

        /// <summary>
        /// Retire le layer PlayerBody du culling mask de la caméra FPS.
        /// Tous les autres layers restent rendus normalement.
        /// </summary>
        private void ExcludeLayerFromCamera()
        {
            if (playerCamera == null)
            {
                Debug.LogWarning("[FPSBodyHider] playerCamera non assignée, le corps restera visible.");
                return;
            }

            // Retire le bit correspondant au layer du culling mask
            playerCamera.cullingMask &= ~(1 << playerBodyLayer);
        }

        // ── Editor helper ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Vérifie que le layer existe bien dans le projet
            string layerName = LayerMask.LayerToName(playerBodyLayer);
            if (string.IsNullOrEmpty(layerName))
            {
                Debug.LogWarning($"[FPSBodyHider] Le layer {playerBodyLayer} n'existe pas. " +
                                 "Créez 'PlayerBody' dans Project Settings → Tags and Layers.");
            }
        }
#endif
    }
}
