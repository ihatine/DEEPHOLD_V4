using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using OutOfSync.Gameplay;

namespace OutOfSync.Networking
{
    public sealed class NetworkBootstrap : MonoBehaviour
    {
        public static NetworkBootstrap Instance { get; private set; }
        public NetworkManager Manager { get; private set; }
        public bool IsRunning => Manager != null && Manager.IsListening;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildNetworkManager();
        }

        private void BuildNetworkManager()
        {
            if (NetworkManager.Singleton != null)
            {
                Manager = NetworkManager.Singleton;
                return;
            }

            // NetworkManager is deliberately a root object. This avoids the nested
            // NetworkManager error that broke the previous prototype.
            var go = new GameObject("NetworkManager");
            DontDestroyOnLoad(go);
            Manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");

            Manager.NetworkConfig.NetworkTransport = transport;
            Manager.NetworkConfig.EnableSceneManagement = false;
            Manager.NetworkConfig.ConnectionApproval = false;
            Manager.NetworkConfig.TickRate = 30;

            var prefab = PlayerFactory.CreatePlayerPrefab();
            Manager.NetworkConfig.PlayerPrefab = prefab;
            Manager.AddNetworkPrefab(prefab);
        }

        public bool StartHost()
        {
            if (Manager == null) BuildNetworkManager();
            if (IsRunning) return true;
            return Manager.StartHost();
        }

        public bool StartClient(string address)
        {
            if (Manager == null) BuildNetworkManager();
            if (IsRunning) return true;
            var transport = Manager.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null) return false;
            transport.SetConnectionData(string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim(), 7777, "0.0.0.0");
            return Manager.StartClient();
        }

        public void Stop()
        {
            if (Manager != null && IsRunning) Manager.Shutdown();
        }
    }
}
