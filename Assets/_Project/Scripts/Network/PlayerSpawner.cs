using UnityEngine;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using FishNet.Managing.Server;

namespace ProjectFPS.Network
{
    /// <summary>
    /// Spawn automatiquement le prefab joueur quand la scène de jeu est chargée.
    ///
    /// ═══ ARCHITECTURE ════════════════════════════════════════════════════════════
    ///  Ce script DOIT être placé dans la scène de JEU (SampleScene) — PAS dans Lobby.
    ///
    ///  Flux correct :
    ///    1. Lobby : les joueurs se connectent (Host + Clients rejoignent le serveur)
    ///    2. Host lance la partie → FishNet charge SampleScene sur tous les clients
    ///    3. SampleScene démarre → PlayerSpawner.Start() → spawn de TOUS les clients déjà connectés
    ///    4. Si un joueur rejoint en cours de partie → OnRemoteConnectionState → spawn immédiat
    ///
    /// ═══ CONFIGURATION UNITY ══════════════════════════════════════════════════════
    ///  1. Dans SampleScene : crée un GameObject vide → nomme-le "PlayerSpawner"
    ///  2. Ajoute ce script (Add Component → PlayerSpawner)
    ///  3. Champ "Player Prefab" → glisse ton prefab NetworkPlayer
    ///  4. Champ "Spawn Points" → taille = nombre de joueurs max
    ///     → glisse chaque SpawnPoint (GameObject vide positionné dans SampleScene)
    ///
    ///  IMPORTANT : retire tout PlayerSpawner de la scène Lobby (sinon double-spawn).
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Prefab joueur")]
        [Tooltip("Glisse ici le prefab NetworkPlayer (doit avoir un composant NetworkObject).")]
        [SerializeField] private GameObject playerPrefab;

        [Header("Points de spawn")]
        [Tooltip("Liste des positions de spawn. Utilisés en boucle si plusieurs joueurs.")]
        [SerializeField] private Transform[] spawnPoints;

        private int _nextSpawnIndex;

        private void Start()
        {
            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;

            // Spawn tous les clients déjà connectés via le Lobby
            // (leur connexion a eu lieu avant le chargement de cette scène)
            SpawnAllConnectedClients();
        }

        private void OnDestroy()
        {
            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        // Utilisé uniquement pour les joueurs qui rejoignent après le chargement de la scène
        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Started) return;
            SpawnPlayer(conn);
        }

        private void SpawnAllConnectedClients()
        {
            if (!InstanceFinder.IsServer) return;

            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] Player Prefab non assigné dans l'Inspector !");
                return;
            }

            var clients = InstanceFinder.ServerManager.Clients;
            Debug.Log($"[PlayerSpawner] Scène de jeu chargée — spawn de {clients.Count} client(s) connecté(s).");

            foreach (var kv in clients)
                SpawnPlayer(kv.Value);
        }

        private void SpawnPlayer(NetworkConnection conn)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] Player Prefab non assigné dans l'Inspector !");
                return;
            }

            Transform sp = GetNextSpawnPoint();
            GameObject player = Instantiate(playerPrefab, sp.position, sp.rotation);

            // Donne l'ownership du NetworkObject au client correspondant
            InstanceFinder.ServerManager.Spawn(player, conn);

            Debug.Log($"[PlayerSpawner] Joueur spawné pour client #{conn.ClientId} à {sp.position}");
        }

        private Transform GetNextSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return transform; // fallback : position du PlayerSpawner lui-même

            Transform sp = spawnPoints[_nextSpawnIndex % spawnPoints.Length];
            _nextSpawnIndex++;
            return sp;
        }
    }
}
