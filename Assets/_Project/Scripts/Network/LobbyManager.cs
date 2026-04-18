using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Managing.Scened;

namespace ProjectFPS.Network
{
    /// <summary>
    /// Gère le lobby multijoueur : hébergement, connexion (via code de partie), liste joueurs, lancement.
    ///
    /// ═══ CODE DE PARTIE ══════════════════════════════════════════════════════════
    ///  L'hôte voit son IP locale encodée sous forme de code 8 caractères (ex: C0A8-0164).
    ///  Il partage ce code verbalement ou par chat.
    ///  Le client l'entre dans le champ et rejoindre décode automatiquement en IP.
    ///  Fonctionne sur réseau local (LAN). Pour Internet, un relay est nécessaire.
    ///
    /// ═══ CONFIGURATION ════════════════════════════════════════════════════════════
    ///  1. Scène "Lobby" avec NetworkManager + Tugboat
    ///  2. Menu ProjectFPS → Setup Lobby Canvas
    ///  3. Inspector LobbyManager : Game Scene Name + Port
    ///  4. Build Settings : Lobby (index 0) + scène de jeu (index 1)
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
        [SerializeField] private TMP_InputField  codeInputField;
        [SerializeField] private Button          joinButton;
        [SerializeField] private TextMeshProUGUI statusText;

        // ─── Lobby Panel ──────────────────────────────────────────────────────────
        [Header("Lobby Panel")]
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI playerListText;
        [SerializeField] private TextMeshProUGUI partyCodeText;
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
            hostButton?.onClick.AddListener(OnHostClicked);
            joinButton?.onClick.AddListener(OnJoinClicked);
            startGameButton?.onClick.AddListener(OnStartGameClicked);
            leaveButton?.onClick.AddListener(OnLeaveClicked);

            // Guard : le NetworkManager doit être dans la scène Lobby
            if (InstanceFinder.NetworkManager == null)
            {
                SetStatus("NetworkManager absent ! Voir Console.");
                Debug.LogError("[LobbyManager] NetworkManager introuvable dans la scène.\n" +
                               "→ Copie le GameObject NetworkManager (+ Tugboat) depuis ta scène de jeu dans la scène Lobby.");
                if (hostButton != null) hostButton.interactable = false;
                if (joinButton != null) joinButton.interactable = false;
                return;
            }

            InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;

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
            if (InstanceFinder.NetworkManager == null) { SetStatus("NetworkManager absent !"); return; }

            SetStatus("Démarrage du serveur...");
            hostButton.interactable = false;
            joinButton.interactable = false;

            InstanceFinder.ServerManager.StartConnection(port);
            InstanceFinder.ClientManager.StartConnection("localhost", port);
        }

        private void OnJoinClicked()
        {
            if (InstanceFinder.NetworkManager == null) { SetStatus("NetworkManager absent !"); return; }

            string input = codeInputField != null ? codeInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(input)) input = "localhost";

            // Décode le code de partie (ex. "C0A8-0164") → IP, ou utilise directement si c'est déjà une IP
            string ip = CodeToIp(input);

            SetStatus("Connexion...");
            hostButton.interactable = false;
            joinButton.interactable = false;

            InstanceFinder.ClientManager.StartConnection(ip, port);
        }

        private void OnStartGameClicked()
        {
            if (InstanceFinder.NetworkManager == null) { SetStatus("NetworkManager absent !"); return; }

            if (!InstanceFinder.IsServer)
            {
                SetStatus("Seul l'hôte peut lancer la partie.");
                return;
            }

            Debug.Log($"[LobbyManager] Chargement de la scène '{gameSceneName}'...");
            SceneLoadData sld = new SceneLoadData(gameSceneName);
            InstanceFinder.NetworkManager.SceneManager.LoadGlobalScenes(sld);
        }

        private void OnLeaveClicked()
        {
            if (InstanceFinder.NetworkManager == null) { SetStatus("NetworkManager absent !"); return; }

            InstanceFinder.ClientManager.StopConnection();
            if (InstanceFinder.IsServer)
                InstanceFinder.ServerManager.StopConnection(true);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Événements FishNet
        // ═════════════════════════════════════════════════════════════════════════

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            RefreshPlayerList();
        }

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
            if (codeInputField != null) codeInputField.text = "";
        }

        private void ShowLobbyPanel()
        {
            connectPanel?.SetActive(false);
            lobbyPanel?.SetActive(true);

            if (startGameButton != null)
                startGameButton.gameObject.SetActive(InstanceFinder.IsServer);

            // L'hôte voit son code de partie pour le partager avec les autres joueurs
            if (partyCodeText != null)
            {
                if (InstanceFinder.IsServer)
                {
                    string code = IpToCode(GetLocalIP());
                    partyCodeText.text = $"Code de la partie\n<b><size=130%>{code}</size></b>";
                }
                else
                {
                    partyCodeText.text = "";
                }
            }

            RefreshPlayerList();
        }

        private void RefreshPlayerList()
        {
            if (!InstanceFinder.IsServer)
            {
                if (playerListText != null)  playerListText.text  = "En attente du serveur...";
                if (playerCountText != null) playerCountText.text = "";
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
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
            Debug.Log($"[LobbyManager] {message}");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // Code de partie — encode/décode IPv4 ↔ code hexadécimal 8 caractères
        // Exemple : 192.168.1.100  ↔  C0A8-0164
        // Fonctionne sur LAN : l'hôte partage le code, le client le saisit.
        // ═════════════════════════════════════════════════════════════════════════

        public static string IpToCode(string ip)
        {
            try
            {
                byte[] b = IPAddress.Parse(ip).GetAddressBytes();
                string h = BitConverter.ToString(b).Replace("-", "");
                return h.Substring(0, 4) + "-" + h.Substring(4);
            }
            catch { return ip; }
        }

        public static string CodeToIp(string code)
        {
            try
            {
                string hex = code.Replace("-", "").Replace(" ", "").ToUpper();
                if (hex.Length == 8 && IsHexString(hex))
                {
                    byte[] bytes = new byte[4];
                    for (int i = 0; i < 4; i++)
                        bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                    return new IPAddress(bytes).ToString();
                }
            }
            catch { }
            return code; // fallback : traité comme IP directe
        }

        private static bool IsHexString(string s)
        {
            foreach (char c in s)
                if (!Uri.IsHexDigit(c)) return false;
            return true;
        }

        private static string GetLocalIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var addr in host.AddressList)
                    if (addr.AddressFamily == AddressFamily.InterNetwork
                        && !addr.ToString().StartsWith("127."))
                        return addr.ToString();
            }
            catch { }
            return "127.0.0.1";
        }
    }
}
