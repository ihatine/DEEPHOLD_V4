using UnityEngine;
using OutOfSync.Gameplay;
using OutOfSync.Networking;
using OutOfSync.World;

namespace OutOfSync.Core
{
    /// <summary>
    /// Creates one local player without starting Netcode.
    /// This is the default development/test mode.
    /// </summary>
    public sealed class SinglePlayerBootstrap : MonoBehaviour
    {
        public static SinglePlayerBootstrap Instance { get; private set; }
        public CoopPlayer Player { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SpawnLocalPlayer();
        }

        private void SpawnLocalPlayer()
        {
            if (Player != null) return;

            var existing = FindAnyObjectByType<CoopPlayer>();
            if (existing != null)
            {
                Player = existing;
                return;
            }

            var playerObject = PlayerFactory.CreatePlayerPrefab();
            playerObject.name = "PLAYER_SINGLEPLAYER";
            playerObject.transform.position = WorldGenerator.Instance != null
                ? WorldGenerator.Instance.GetSpawnPosition() + new Vector3(0f, 1.25f, 0f)
                : new Vector3(0f, 2f, -0.25f);
            playerObject.SetActive(true);

            Player = playerObject.GetComponent<CoopPlayer>();

            var camera = Camera.main;
            if (camera != null)
            {
                var follow = camera.GetComponent<FollowCamera>();
                follow?.SetTarget(Player.transform);
            }
        }
    }
}
