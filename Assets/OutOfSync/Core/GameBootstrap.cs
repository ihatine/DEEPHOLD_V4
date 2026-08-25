using System.Collections;
using UnityEngine;
using OutOfSync.World;
using OutOfSync.UI;
using OutOfSync.Gameplay;
using OutOfSync.Networking;

namespace OutOfSync.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }
        public static bool GameStarted { get; private set; }
        public static bool MultiplayerMode { get; private set; }

        private bool loading;
        private float loadProgress;
        private string loadStatus = "Подготовка...";
        private bool multiplayerMenu;
        private string address = "127.0.0.1";
        private GUIStyle title, subtitle, button, label, small, field;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindAnyObjectByType<GameBootstrap>() != null) return;
            var go = new GameObject("[DEEPHOLD] GameBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 120;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
        }

        public void StartSinglePlayer() => StartGame(false, true, "127.0.0.1");
        public void StartHost() => StartGame(true, true, "127.0.0.1");
        public void StartClient(string ip) => StartGame(true, false, ip);

        private void StartGame(bool multiplayer, bool host, string ip)
        {
            if (loading || GameStarted) return;
            MultiplayerMode = multiplayer;
            address = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
            StartCoroutine(LoadGameRoutine(multiplayer, host));
        }

        private IEnumerator LoadGameRoutine(bool multiplayer, bool host)
        {
            loading = true;
            GameStarted = false;
            loadProgress = 0f;
            loadStatus = "Проверка игровых систем...";
            yield return null;
            EnsureCamera();
            loadProgress = 0.16f;
            loadStatus = "Подготовка света...";
            EnsureLighting();
            yield return null;
            loadProgress = 0.32f;
            loadStatus = "Генерация мира по seed...";
            if (FindAnyObjectByType<WorldGenerator>() == null)
                new GameObject("WorldRuntime").AddComponent<WorldGenerator>();
            if (FindAnyObjectByType<CaveSystem>() == null)
                new GameObject("CaveRuntime").AddComponent<CaveSystem>();
            yield return null;
            loadProgress = 0.62f;
            loadStatus = multiplayer ? "Запуск кооперативной сети..." : "Создание персонажа...";

            if (multiplayer)
            {
                var network = FindAnyObjectByType<NetworkBootstrap>();
                if (network == null) network = new GameObject("NetworkRuntime").AddComponent<NetworkBootstrap>();
                yield return null;
                bool started = host ? network.StartHost() : network.StartClient(address);
                if (!started)
                {
                    loadStatus = "Не удалось запустить сеть";
                    loading = false;
                    MultiplayerMode = false;
                    yield break;
                }
            }
            else
            {
                if (FindAnyObjectByType<SinglePlayerBootstrap>() == null)
                    new GameObject("SinglePlayerRuntime").AddComponent<SinglePlayerBootstrap>();
            }

            loadProgress = 0.88f;
            loadStatus = "Загрузка HUD...";
            if (FindAnyObjectByType<RuntimeHUD>() == null)
                new GameObject("UIRuntime").AddComponent<RuntimeHUD>();
            yield return null;
            loadProgress = 1f;
            loadStatus = multiplayer ? (host ? "Сервер готов" : "Подключение завершено") : "Мир готов";
            yield return new WaitForSecondsRealtime(0.45f);
            loading = false;
            GameStarted = true;
        }

        private static void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 9.5f;
            cam.transform.position = new Vector3(0f, 0f, -20f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.02f, 0.025f);
            if (cam.GetComponent<FollowCamera>() == null) cam.gameObject.AddComponent<FollowCamera>();
        }

        private static void EnsureLighting()
        {
            var light = FindAnyObjectByType<Light>();
            if (light == null)
            {
                var go = new GameObject("World Light");
                light = go.AddComponent<Light>();
            }
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(0.72f, 0.82f, 1f);
            light.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;
            light.shadowBias = 0.035f;
            light.shadowNormalBias = 0.25f;
            RenderSettings.ambientLight = new Color(0.15f, 0.19f, 0.23f);
            RenderSettings.ambientIntensity = 1f;
        }

        private void BuildStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 56, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            subtitle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            label = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            field = new GUIStyle(GUI.skin.textField) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        }

        private void OnGUI()
        {
            BuildStyles();
            if (loading) { DrawLoading(); return; }
            if (!GameStarted) DrawMainMenu();
        }

        private void DrawMainMenu()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            float width = Mathf.Min(700f, Screen.width - 60f);
            float height = multiplayerMenu ? 520f : 470f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            GUI.Label(new Rect(x + 20, y + 30, width - 40, 72), "DEEPHOLD", title);
            GUI.Label(new Rect(x + 20, y + 100, width - 40, 28), "2.5D CO-OP SURVIVAL / EXPLORATION", subtitle);
            GUI.Label(new Rect(x + 65, y + 145, width - 130, 62),
                "Поверхность — безопасная зона исследования.\nПещеры — добыча, опасности и редкие ресурсы.\nСобери деревянные инструменты и спускайся глубже.", label);

            if (!multiplayerMenu)
            {
                if (GUI.Button(new Rect(x + 130, y + 225, width - 260, 52), "ОДИНОЧНАЯ ИГРА", button)) StartSinglePlayer();
                if (GUI.Button(new Rect(x + 130, y + 290, width - 260, 52), "МУЛЬТИПЛЕЕР", button)) multiplayerMenu = true;
                GUI.Label(new Rect(x + 50, y + 365, width - 100, 40), "Деревянный топор • деревянная кирка • деревянный меч\n1–3 — инструменты • ЛКМ — добыча • ПКМ — атака", small);
                GUI.Label(new Rect(x + 20, y + height - 28, width - 40, 20), "DEEPHOLD / DEVELOPMENT BUILD", small);
            }
            else
            {
                GUI.Label(new Rect(x + 50, y + 220, width - 100, 28), "КО-ОП", label);
                if (GUI.Button(new Rect(x + 110, y + 260, width - 220, 50), "СОЗДАТЬ СЕРВЕР", button)) StartHost();
                GUI.Label(new Rect(x + 110, y + 322, 160, 24), "IP ХОСТА", small);
                address = GUI.TextField(new Rect(x + 280, y + 318, 250, 36), address, field);
                if (GUI.Button(new Rect(x + 110, y + 370, width - 220, 50), "ПОДКЛЮЧИТЬСЯ", button)) StartClient(address);
                if (GUI.Button(new Rect(x + 110, y + 430, width - 220, 34), "НАЗАД", button)) multiplayerMenu = false;
            }
        }

        private void DrawLoading()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            float width = Mathf.Min(680f, Screen.width - 100f);
            float x = (Screen.width - width) * 0.5f;
            float centerY = Screen.height * 0.5f;
            GUI.Label(new Rect(x, centerY - 100, width, 60), "DEEPHOLD", title);
            GUI.Label(new Rect(x, centerY - 42, width, 30), MultiplayerMode ? "CO-OP / ПОДГОТОВКА" : "ОДИНОЧНАЯ ИГРА", subtitle);
            GUI.Label(new Rect(x, centerY + 10, width, 30), loadStatus, label);
            GUI.Box(new Rect(x + 20, centerY + 52, width - 40, 24), GUIContent.none);
            GUI.Box(new Rect(x + 22, centerY + 54, (width - 44) * loadProgress, 20), GUIContent.none);
            GUI.Label(new Rect(x, centerY + 84, width, 24), $"{Mathf.RoundToInt(loadProgress * 100f)}%", small);
        }
    }
}
