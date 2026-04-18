// FishNetSetupEditor.cs
// Menu : ProjectFPS → Outils FishNet
//
// Outils pour configurer FishNet correctement :
//   0. Créer la scène Lobby + l'ajouter aux Build Settings
//   1. Diagnostic — explique l'état actuel de la scène
//   2. Régénérer les prefabs réseau  (AssetPathHash + duplicate key)
//   3. Ajouter NetworkManager à la scène active (ouvre Lobby d'abord)
//   4. Supprimer le PlayerSpawner FishNet intégré (NullReferenceException)
//   5. Créer le PlayerSpawner + RoleManager dans SampleScene

using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ProjectFPS.Editor
{
    public static class FishNetSetupEditor
    {
        // ─── 0. Créer la scène Lobby ─────────────────────────────────────────────
        [MenuItem("ProjectFPS/Outils FishNet/0. Créer la scène Lobby (si elle n'existe pas)")]
        static void CreateLobbyScene()
        {
            const string lobbyScenePath = "Assets/Scenes/Lobby.unity";

            // Vérifie si la scène existe déjà
            if (System.IO.File.Exists(lobbyScenePath))
            {
                bool addToBuild = EditorUtility.DisplayDialog(
                    "Scène Lobby",
                    "Lobby.unity existe déjà.\nVoulez-vous l'ajouter aux Build Settings si elle n'y est pas ?",
                    "Oui", "Non");
                if (addToBuild) EnsureInBuildSettings(lobbyScenePath, 0);
                return;
            }

            // S'assure que le dossier Scenes existe
            if (!System.IO.Directory.Exists("Assets/Scenes"))
                System.IO.Directory.CreateDirectory("Assets/Scenes");

            // Crée la scène, sauvegarde
            var lobbyScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            lobbyScene.name = "Lobby";
            EditorSceneManager.SaveScene(lobbyScene, lobbyScenePath);
            AssetDatabase.Refresh();

            // Ajoute aux Build Settings : Lobby = index 0, SampleScene = index 1
            EnsureInBuildSettings(lobbyScenePath, 0);

            // Ouvre la scène Lobby seule pour configuration
            EditorSceneManager.OpenScene(lobbyScenePath, OpenSceneMode.Single);

            EditorUtility.DisplayDialog(
                "Scène Lobby créée",
                "Assets/Scenes/Lobby.unity créée et ajoutée aux Build Settings (index 0).\n\n" +
                "Étapes suivantes :\n" +
                "1. ProjectFPS > Outils FishNet > 3. Ajouter NetworkManager\n" +
                "2. ProjectFPS > Setup Lobby Canvas\n" +
                "3. ProjectFPS > Outils FishNet > 4. Supprimer PlayerSpawner FishNet intégré\n" +
                "4. ProjectFPS > Outils FishNet > 2. Régénérer les prefabs réseau\n" +
                "5. File > Build Settings : vérifie SampleScene en index 1",
                "OK");
        }

        static void EnsureInBuildSettings(string scenePath, int preferredIndex)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            bool alreadyIn = scenes.Any(s => s.path == scenePath);

            if (!alreadyIn)
            {
                var entry = new EditorBuildSettingsScene(scenePath, true);
                if (preferredIndex >= 0 && preferredIndex <= scenes.Count)
                    scenes.Insert(preferredIndex, entry);
                else
                    scenes.Add(entry);
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"[FishNetSetup] '{System.IO.Path.GetFileNameWithoutExtension(scenePath)}' ajoutée aux Build Settings (index {preferredIndex}).");
            }
            else
            {
                Debug.Log($"[FishNetSetup] '{System.IO.Path.GetFileNameWithoutExtension(scenePath)}' déjà dans Build Settings.");
            }
        }

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
            sb.AppendLine("Build Settings index 0 : scène LOBBY      ← NetworkManager + LobbyManager");
            sb.AppendLine("Build Settings index 1 : SampleScene      ← PlayerSpawner + RoleManager");
            sb.AppendLine("Le NM persiste via DontDestroyOnLoad quand FishNet charge SampleScene.");
            sb.AppendLine("");
            sb.AppendLine("⚠ PlayerSpawner doit être dans SampleScene (PAS dans Lobby) !");
            sb.AppendLine("  → Dans Lobby : spawn avant chargement → joueurs en mauvaise scène");
            sb.AppendLine("  → Dans SampleScene : Start() spawn TOUS les clients déjà connectés");
            sb.AppendLine("");
            sb.AppendLine("⚠ RoleManager doit être dans SampleScene !");
            sb.AppendLine("  → Sans lui : RoleManager.Instance == null → erreurs InventorySystem/RoleAbilityController");
            sb.AppendLine("");
            sb.AppendLine("⚠ Multiple AudioListeners = 1 par joueur spawné + caméra Lobby");
            sb.AppendLine("  → Cause : joueurs spawnés dans Lobby à cause d'un PlayerSpawner mal placé");
            sb.AppendLine("  → Fix : PlayerSpawner dans SampleScene (action 5)");

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

        // ─── 5. Créer PlayerSpawner + RoleManager dans SampleScene ──────────────
        [MenuItem("ProjectFPS/Outils FishNet/5. Setup scène de jeu (PlayerSpawner + RoleManager)")]
        static void SetupGameScene()
        {
            const string gameScenePath = "Assets/Scenes/SampleScene.unity";
            const string gameSceneName = "SampleScene";

            // Vérifie que SampleScene est dans Build Settings
            var buildScenes = EditorBuildSettings.scenes;
            bool inBuild = buildScenes.Any(s =>
                System.IO.Path.GetFileNameWithoutExtension(s.path) == gameSceneName);

            if (!inBuild)
            {
                if (System.IO.File.Exists(gameScenePath))
                    EnsureInBuildSettings(gameScenePath, 1);
                else
                {
                    EditorUtility.DisplayDialog("FishNet Setup",
                        $"Scène '{gameSceneName}' introuvable à '{gameScenePath}'.\n" +
                        "Vérifie que ta scène de jeu existe et ajuste le chemin.", "OK");
                    return;
                }
            }

            // Ouvre SampleScene en mode Single
            if (EditorSceneManager.GetActiveScene().name != gameSceneName)
            {
                bool open = EditorUtility.DisplayDialog("FishNet Setup",
                    $"Cette action va ouvrir la scène '{gameSceneName}'.\nContinuer ?",
                    "Oui", "Non");
                if (!open) return;
                EditorSceneManager.OpenScene(gameScenePath, OpenSceneMode.Single);
            }

            var activeScene = EditorSceneManager.GetActiveScene();

            // ── PlayerSpawner ────────────────────────────────────────────────────
            var existingSpawner = Object.FindObjectOfType<ProjectFPS.Network.PlayerSpawner>();
            if (existingSpawner == null)
            {
                var spawnerGO = new GameObject("PlayerSpawner");
                spawnerGO.AddComponent<ProjectFPS.Network.PlayerSpawner>();
                Undo.RegisterCreatedObjectUndo(spawnerGO, "Add PlayerSpawner");
                Debug.Log("[FishNetSetup] PlayerSpawner créé dans SampleScene.\n" +
                          "→ Assigne le Player Prefab + les SpawnPoints dans l'Inspector.");
            }
            else
            {
                Debug.Log("[FishNetSetup] PlayerSpawner déjà présent dans SampleScene.");
            }

            // ── RoleManager ──────────────────────────────────────────────────────
            var roleManagerType = System.AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } })
                .FirstOrDefault(t => t.FullName == "ProjectFPS.Roles.RoleManager");

            if (roleManagerType != null)
            {
                var existingRM = Object.FindObjectOfType(roleManagerType);
                if (existingRM == null)
                {
                    var rmGO = new GameObject("RoleManager");
                    rmGO.AddComponent(roleManagerType);
                    Undo.RegisterCreatedObjectUndo(rmGO, "Add RoleManager");
                    Debug.Log("[FishNetSetup] RoleManager créé dans SampleScene.\n" +
                              "→ Assigne les RoleData ScriptableObjects dans Available Roles.\n" +
                              "→ Assigne un rôle par défaut dans Default Role.");
                }
                else
                {
                    Debug.Log("[FishNetSetup] RoleManager déjà présent dans SampleScene.");
                }
            }
            else
            {
                Debug.LogWarning("[FishNetSetup] Type 'ProjectFPS.Roles.RoleManager' introuvable. " +
                                 "Crée le GameObject RoleManager manuellement.");
            }

            EditorSceneManager.MarkSceneDirty(activeScene);

            EditorUtility.DisplayDialog(
                "Setup scène de jeu",
                "Étapes restantes dans l'Inspector :\n\n" +
                "① PlayerSpawner\n" +
                "  • Player Prefab → ton prefab NetworkPlayer\n" +
                "  • Spawn Points  → GameObjects vides positionnés dans SampleScene\n\n" +
                "② RoleManager\n" +
                "  • Available Roles → glisse tes RoleData ScriptableObjects\n" +
                "  • Default Role    → glisse le rôle appliqué au démarrage\n\n" +
                "③ Sauvegarde la scène (Ctrl+S)\n\n" +
                "④ Build Settings : Lobby = index 0, SampleScene = index 1\n\n" +
                "⑤ Dans Lobby : retire tout PlayerSpawner si présent\n" +
                "   (Menu : ProjectFPS → Outils FishNet → 4)",
                "OK");
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
