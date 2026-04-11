using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Managing.Scened;

namespace ProjectFPS.Network
{
    /// <summary>
    /// Gère le lobby multijoueur : hébergement, connexion, liste joueurs, lancement de partie.
    ///
    /// ═══ HIÉRARCHIE CANVAS RECOMMANDÉE ═══════════════════════════════════════
    ///
    ///  Canvas (Screen Space – Overlay)
    ///  └── LobbyManager (ce script sur ce GameObject)
    ///
    ///  ┌── ConnectPanel
    ///  │   ├── Title         (TextMeshPro)   "ProjectFPS"
    ///  │   ├── HostButton    (Button + TMP)  "Héberger"
    ///  │   ├── Separator     (Image)
    ///  │   ├── IpInputField  (TMP_InputField) placeholder = "Adresse IP"
    ///  │   ├── JoinButton    (Button + TMP)  "Rejoindre"
    ///  │   └── StatusText    (TextMeshPro)   feedback connexion
    ///  │
    ///  └── LobbyPanel
    ///      ├── Title         (TextMeshPro)   "Lobby"
    ///      ├── PlayerCountText (TextMeshPro) "Joueurs : 0"
    ///      ├── PlayerListText  (TextMeshPro) liste des joueurs (multi-ligne)
    ///      ├── StartGameButton (Button + TMP) "Lancer la partie" — host only
    ///      └── LeaveButton     (Button + TMP) "Quitter"
    ///
    /// ═══ CONFIGURATION ════════════════════════════════════════════════════════
    ///  1. Crée une scène "Lobby" (File → New Scene)
    ///  2. Copie le NetworkManager + Tugboat depuis ta scène de jeu
    ///  3. Crée le Canvas ci-dessus et assigne les champs dans l'Inspector
    ///  4. Champ "Game Scene Name" → nom exact de ta scène de jeu (ex. "SampleScene")
    ///  5. Dans Build Settings → ajoute Lobby (index 0) et SampleScene (index 1)
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        // ─── Panels ───────────────────────────────────────────────────────────────
        [Header("Panels")]
        [SerializeField] private GameObject connectPanel;
        [SerializeField] private GameObject lobbyPanel;

        // ─── Connect Panel ────────────────────────────────────────────────────────
        [Header("Connect Panel")]
        [SerializeField] private Button          hostButton;
        [SerializeField] private TMP_InputField  ipInputField;
        [SerializeField] private Button          joinButton;
        [SerializeField] private TextMeshProUGUI statusText;

        // ─── Lobby Panel ──────────────────────────────────────────────────────────
        [Header("Lobby Panel")]
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI playerListText;
        [SerializeField] private Button          startGameButton;
        [SerializeField] private Button          leaveButton;

        // ─── Paramètres ───────────────────────────────────────────────────────────
        [Header("Paramètres")]
        [Tooltip("Nom exact de la scène de jeu à charger (doit être dans Build Settings).")]
        [SerializeField] private string gameSceneName = "SampleScene";
        [SerializeField] private ushort port          = 7770;

        // ═════════════════════════════════════════════════════════════════════════
        // Lifecycle
        // ═════════════════════════════════════════════════════════════════════════

        private void Start()
        {
            // Valeur par défaut du champ IP
            if (ipInputField != null)
                ipInputField.text = "localhost";

            // Listeners boutons
            hostButton?.onClick.AddListener(OnHostClicked);
            joinButton?.onClick.AddListener(OnJoinClicked);
            startGameButton?.onClick.AddListener(OnStartGameClicked);
            leaveButton?.onClick.AddListener(OnLeaveClicked);

            // Événements réseau FishNet
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;

            // Démarrage sur le panel de connexion
            ShowConnectPanel();
        }

        private void OnDestroy()
        {
            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            if (InstanceFinder.ClientManager != null)
                InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Actions boutons
        // ═════════════════════════════════════════════════════════════════════════

        private void OnHostClicked()
        {
            SetStatus("Démarrage du serveur...");
            hostButton.interactable = false;
            joinButton.interactable = false;

            // Démarre le serveur puis se connecte en tant que client local
            InstanceFinder.ServerManager.StartConnection(port);
            InstanceFinder.ClientManager.StartConnection("localhost", port);
        }

        private void OnJoinClicked()
        {
            string ip = ipInputField != null ? ipInputField.text.Trim() : "localhost";
            if (string.IsNullOrEmpty(ip)) ip = "localhost";

            SetStatus($"Connexion à {ip}:{port}...");
            hostButton.interactable = false;
            joinButton.interactable = false;

            InstanceFinder.ClientManager.StartConnection(ip, port);
        }

        private void OnStartGameClicked()
        {
            // Seul l'hôte peut lancer la partie
            if (!InstanceFinder.IsServer)
            {
                SetStatus("Seul l'hôte peut lancer la partie.");
                return;
            }

            Debug.Log($"[LobbyManager] Chargement de la scène '{gameSceneName}'...");

            // FishNet SceneManager — synchronise la scène pour TOUS les clients
            SceneLoadData sld = new SceneLoadData(gameSceneName);
            InstanceFinder.NetworkManager.SceneManager.LoadGlobalScenes(sld);
        }

        private void OnLeaveClicked()
        {
            InstanceFinder.ClientManager.StopConnection();
            if (InstanceFinder.IsServer)
                InstanceFinder.ServerManager.StopConnection(true);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Événements FishNet
        // ═════════════════════════════════════════════════════════════════════════

        // Appelé sur le serveur quand un client se connecte ou se déconnecte
        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            RefreshPlayerList();
        }

        // Appelé sur le client local quand son état de connexion change
        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case LocalConnectionState.Starting:
                    SetStatus("Connexion en cours...");
                    break;

                case LocalConnectionState.Started:
                    SetStatus("Connecté !");
                    ShowLobbyPanel();
                    break;

                case LocalConnectionState.Stopping:
                    SetStatus("Déconnexion...");
                    break;

                case LocalConnectionState.Stopped:
                    SetStatus("Déconnecté.");
                    ShowConnectPanel();
                    break;
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // UI
        // ═════════════════════════════════════════════════════════════════════════

        private void ShowConnectPanel()
        {
            connectPanel?.SetActive(true);
            lobbyPanel?.SetActive(false);

            if (hostButton != null) hostButton.interactable = true;
            if (joinButton != null) joinButton.interactable = true;
        }

        private void ShowLobbyPanel()
        {
            connectPanel?.SetActive(false);
            lobbyPanel?.SetActive(true);

            // Le bouton "Lancer" n'est visible que pour l'hôte
            if (startGameButton != null)
                startGameButton.gameObject.SetActive(InstanceFinder.IsServer);

            RefreshPlayerList();
        }

        private void RefreshPlayerList()
        {
            if (!InstanceFinder.IsServer)
            {
                if (playerListText != null)
                    playerListText.text = "En attente du serveur...";
                if (playerCountText != null)
                    playerCountText.text = "";
                return;
            }

            var clients = InstanceFinder.ServerManager.Clients;
            int count   = clients.Count;

            if (playerCountText != null)
                playerCountText.text = $"Joueurs connectés : {count}";

            if (playerListText != null)
            {
                var sb = new System.Text.StringBuilder();
                int i  = 1;
                foreach (var kv in clients)
                    sb.AppendLine($"  {i++}. Joueur #{kv.Key}");
                playerListText.text = sb.ToString();
            }

            Debug.Log($"[LobbyManager] Liste joueurs mise à jour : {count} joueur(s)");
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
            Debug.Log($"[LobbyManager] {message}");
        }
    }
}
