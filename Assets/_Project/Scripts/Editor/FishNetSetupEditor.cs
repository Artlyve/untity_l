// FishNetSetupEditor.cs
// Menu : ProjectFPS → Outils FishNet
//
// Outils pour configurer FishNet correctement :
//   1. Diagnostic — explique l'état actuel de la scène
//   2. Régénérer les prefabs réseau  (AssetPathHash + duplicate key)
//   3. Ajouter NetworkManager à la scène active (ouvre Lobby d'abord)
//   4. Supprimer le PlayerSpawner FishNet intégré (NullReferenceException)

using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProjectFPS.Editor
{
    public static class FishNetSetupEditor
    {
        // ─── 1. Diagnostic ───────────────────────────────────────────────────────
        [MenuItem("ProjectFPS/Outils FishNet/1. Diagnostic (lire avant tout)")]
        static void Diagnostic()
        {
            var nm = Object.FindObjectOfType<FishNet.Managing.NetworkManager>();
            var sceneName = EditorSceneManager.GetActiveScene().name;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Diagnostic FishNet — scène active : {sceneName} ===\n");

            // NetworkManager
            if (nm != null)
            {
                sb.AppendLine($"✓ NetworkManager trouvé : {nm.gameObject.name}");

                // Chercher Tugboat
                var tugboat = nm.GetComponent(System.AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } })
                    .FirstOrDefault(t => t.Name == "Tugboat" && typeof(Component).IsAssignableFrom(t)));
                sb.AppendLine(tugboat != null ? "✓ Transport Tugboat présent" : "✗ Transport Tugboat ABSENT — ajoute-le sur NetworkManager");

                // Vérifier built-in PlayerSpawner
                const string builtinFQN = "FishNet.Component.Spawning.PlayerSpawner";
                var spawnerType = System.AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } })
                    .FirstOrDefault(t => t.FullName == builtinFQN);
                if (spawnerType != null && nm.GetComponent(spawnerType) != null)
                    sb.AppendLine("✗ FishNet PlayerSpawner intégré PRÉSENT sur NetworkManager → lance l'action 4 pour le supprimer");
                else
                    sb.AppendLine("✓ Pas de PlayerSpawner FishNet intégré (correct)");
            }
            else
            {
                sb.AppendLine("✗ NetworkManager ABSENT de cette scène");
                sb.AppendLine("  → Si c'est la scène Lobby : lance l'action 3 pour en créer un");
                sb.AppendLine("  → Si c'est la scène de JEU : c'est NORMAL, le NM vient de la scène Lobby via DontDestroyOnLoad");
            }

            // DontDestroyOnLoad explication
            sb.AppendLine("\n─── Pourquoi le NM 'disparaît' en Play Mode ───────────────────");
            sb.AppendLine("C'est NORMAL. FishNet appelle DontDestroyOnLoad sur le NetworkManager.");
            sb.AppendLine("Il ne disparaît pas — il se déplace dans la section 'DontDestroyOnLoad'");
            sb.AppendLine("tout en bas de la Hierarchy en Play Mode.");

            // Architecture correcte
            sb.AppendLine("\n─── Architecture attendue ──────────────────────────────────────");
            sb.AppendLine("Build Settings index 0 : scène LOBBY  ← NetworkManager ICI uniquement");
            sb.AppendLine("Build Settings index 1 : SampleScene  ← pas de NetworkManager");
            sb.AppendLine("Le NM persiste via DontDestroyOnLoad quand FishNet charge SampleScene.");

            // Vérifier Build Settings
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length > 0)
            {
                sb.AppendLine("\n─── Build Settings actuels ─────────────────────────────────────");
                for (int i = 0; i < scenes.Length; i++)
                    sb.AppendLine($"  [{i}] {System.IO.Path.GetFileNameWithoutExtension(scenes[i].path)}{(scenes[i].enabled ? "" : " (désactivé)")}");
            }
            else
            {
                sb.AppendLine("\n✗ Aucune scène dans Build Settings — ajoute Lobby (index 0) et SampleScene (index 1)");
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Diagnostic FishNet", sb.ToString(), "OK");
        }

        // ─── 2. Régénérer les prefabs ─────────────────────────────────────────────
        [MenuItem("ProjectFPS/Outils FishNet/2. Régénérer les prefabs réseau")]
        static void RegeneratePrefabs()
        {
            bool ok = EditorApplication.ExecuteMenuItem("Fish-Networking/Refresh Default Prefabs");
            if (!ok)
                Debug.LogWarning(
                    "[FishNetSetup] Menu 'Fish-Networking/Refresh Default Prefabs' introuvable.\n" +
                    "→ Va dans la barre de menus Unity : Fish-Networking > Refresh Default Prefabs");
            else
                Debug.Log("[FishNetSetup] Prefabs réseau régénérés — les erreurs AssetPathHash devraient disparaître.");
        }

        // ─── 3. Ajouter NetworkManager à la scène Lobby ───────────────────────────
        [MenuItem("ProjectFPS/Outils FishNet/3. Ajouter NetworkManager à la scène active (ouvre Lobby d'abord)")]
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
                "→ Sauvegarde la scène (Ctrl+S)\n" +
                "→ Lance ensuite : ProjectFPS > Outils FishNet > 2. Régénérer les prefabs réseau");
        }

        // ─── 4. Supprimer le PlayerSpawner FishNet intégré ───────────────────────
        [MenuItem("ProjectFPS/Outils FishNet/4. Supprimer le PlayerSpawner FishNet intégré")]
        static void RemoveBuiltinPlayerSpawner()
        {
            const string builtinFQN = "FishNet.Component.Spawning.PlayerSpawner";

            var spawnerType = System.AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } })
                .FirstOrDefault(t => t.FullName == builtinFQN);

            if (spawnerType == null)
            {
                EditorUtility.DisplayDialog(
                    "FishNet Setup",
                    $"Type '{builtinFQN}' introuvable.\nFishNet est peut-être une version différente.",
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
