// FishNetSetupEditor.cs
// Menu : ProjectFPS → Outils FishNet
//
// 3 actions pour régler les erreurs FishNet courantes :
//   1. Régénérer les prefabs réseau  (AssetPathHash + duplicate key)
//   2. Ajouter NetworkManager à la scène Lobby active
//   3. Supprimer le PlayerSpawner FishNet intégré (NullReferenceException)

using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProjectFPS.Editor
{
    public static class FishNetSetupEditor
    {
        // ─── 1. Régénérer les prefabs ─────────────────────────────────────────────
        [MenuItem("ProjectFPS/Outils FishNet/1. Régénérer les prefabs réseau")]
        static void RegeneratePrefabs()
        {
            // Appelle le menu FishNet natif qui déclenche GenerateFull
            bool ok = EditorApplication.ExecuteMenuItem("Fish-Networking/Refresh Default Prefabs");
            if (!ok)
                Debug.LogWarning(
                    "[FishNetSetup] Menu 'Fish-Networking/Refresh Default Prefabs' introuvable.\n" +
                    "→ Va dans la barre de menus Unity : Fish-Networking > Refresh Default Prefabs");
            else
                Debug.Log("[FishNetSetup] Prefabs réseau régénérés — les erreurs AssetPathHash devraient disparaître.");
        }

        // ─── 2. Ajouter NetworkManager à la scène Lobby ───────────────────────────
        [MenuItem("ProjectFPS/Outils FishNet/2. Ajouter NetworkManager à la scène active (ouvre Lobby d'abord)")]
        static void AddNetworkManagerToScene()
        {
            if (Object.FindObjectOfType<FishNet.Managing.NetworkManager>() != null)
            {
                EditorUtility.DisplayDialog(
                    "FishNet Setup",
                    "Un NetworkManager est déjà présent dans cette scène.",
                    "OK");
                return;
            }

            var nmGO = new GameObject("NetworkManager");
            nmGO.AddComponent<FishNet.Managing.NetworkManager>();

            // Chercher Tugboat via reflection (son namespace peut varier selon la version)
            var tugboatType = System.AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } })
                .FirstOrDefault(t => t.Name == "Tugboat" && typeof(Component).IsAssignableFrom(t));

            if (tugboatType != null)
            {
                nmGO.AddComponent(tugboatType);
                Debug.Log("[FishNetSetup] Tugboat ajouté comme transport.");
            }
            else
            {
                Debug.LogWarning("[FishNetSetup] Tugboat introuvable — ajoute le composant transport manuellement sur NetworkManager.");
            }

            Undo.RegisterCreatedObjectUndo(nmGO, "Add NetworkManager to Lobby");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = nmGO;

            Debug.Log(
                "[FishNetSetup] NetworkManager ajouté à la scène.\n" +
                "→ Configure DefaultPrefabObjects sur le NetworkManager, puis lance\n" +
                "  'ProjectFPS > Outils FishNet > 1. Régénérer les prefabs réseau'.");
        }

        // ─── 3. Supprimer le PlayerSpawner FishNet intégré ───────────────────────
        [MenuItem("ProjectFPS/Outils FishNet/3. Supprimer le PlayerSpawner FishNet intégré")]
        static void RemoveBuiltinPlayerSpawner()
        {
            // On cible uniquement FishNet.Component.Spawning.PlayerSpawner (le built-in),
            // PAS le ProjectFPS.Network.PlayerSpawner (le custom du projet).
            const string builtinFQN = "FishNet.Component.Spawning.PlayerSpawner";

            var spawnerType = System.AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } })
                .FirstOrDefault(t => t.FullName == builtinFQN);

            if (spawnerType == null)
            {
                EditorUtility.DisplayDialog(
                    "FishNet Setup",
                    $"Type '{builtinFQN}' introuvable dans l'assembly.\nFishNet est peut-être une version différente.",
                    "OK");
                return;
            }

            var found = Object.FindObjectsOfType(spawnerType);
            if (found.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "FishNet Setup",
                    "Aucun FishNet PlayerSpawner intégré trouvé dans la scène active.\n" +
                    "(Ouvre la scène qui contient le NetworkManager et relance.)",
                    "OK");
                return;
            }

            int count = found.Length;
            foreach (var comp in found)
                Undo.DestroyObjectImmediate(comp);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[FishNetSetup] {count} FishNet.Component.Spawning.PlayerSpawner supprimé(s).\n" +
                      "Ton PlayerSpawner.cs custom (ProjectFPS.Network) n'a pas été touché.");
        }
    }
}
