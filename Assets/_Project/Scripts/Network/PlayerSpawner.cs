using UnityEngine;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Server;

namespace ProjectFPS.Network
{
    /// <summary>
    /// Spawn automatiquement le prefab joueur quand un client se connecte.
    ///
    /// ═══ CONFIGURATION UNITY ══════════════════════════════════════════════════
    ///
    ///  1. Crée un GameObject vide dans la scène → nomme-le "PlayerSpawner"
    ///  2. Ajoute ce script dessus (Add Component → PlayerSpawner)
    ///  3. Champ "Player Prefab" → glisse ton prefab NetworkPlayer
    ///  4. Champ "Spawn Points" → taille = nombre de points de spawn
    ///     → glisse chaque SpawnPoint (GameObject vide positionné dans la scène)
    ///
    ///  Si aucun SpawnPoint n'est assigné → spawn à la position (0,0,0).
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
            // Abonnement à l'événement de connexion côté serveur
            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        private void OnDestroy()
        {
            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            // Ne spawner que quand un client vient de se connecter
            if (args.ConnectionState != RemoteConnectionState.Started)
                return;

            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] Player Prefab non assigné dans l'Inspector !");
                return;
            }

            Transform spawnPoint = GetNextSpawnPoint();
            GameObject player    = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // Donne l'ownership du NetworkObject au client qui vient de se connecter
            InstanceFinder.ServerManager.Spawn(player, conn);

            Debug.Log($"[PlayerSpawner] Joueur spawné pour client {conn.ClientId} " +
                      $"à {spawnPoint.position}");
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
