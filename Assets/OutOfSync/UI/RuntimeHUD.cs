using UnityEngine;
using OutOfSync.Gameplay;
using OutOfSync.World;
using OutOfSync.Core;

namespace OutOfSync.UI
{
    public sealed class RuntimeHUD : MonoBehaviour
    {
        private GUIStyle panel, title, label, small, hotbar, slot, objective, prompt, progressLabel, percent, toolBig;
        private Texture2D pixel;
        private CoopPlayer player;
        private bool inventoryOpen;
        private readonly string[] itemNames = { "ТОПОР", "КИРКА", "МЕЧ", "ФАКЕЛ", "ДЕРЕВО", "КАМЕНЬ", "МЕДЬ", "КРИСТАЛЛ" };

        private void Awake()
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I)) inventoryOpen = !inventoryOpen;
            if (player == null) player = FindAnyObjectByType<CoopPlayer>();
        }

        private void BuildStyles()
        {
            if (panel != null) return;
            panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 10, 10), alignment = TextAnchor.UpperLeft };
            title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            small = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            hotbar = new GUIStyle(GUI.skin.box) { padding = new RectOffset(6, 6, 6, 6) };
            slot = new GUIStyle(GUI.skin.box) { fontSize = 12, alignment = TextAnchor.LowerRight, padding = new RectOffset(4, 4, 4, 4) };
            objective = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            prompt = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            progressLabel = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            percent = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            toolBig = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        private void OnGUI()
        {
            if (!GameBootstrap.GameStarted) return;
            BuildStyles();
            if (player == null) player = FindAnyObjectByType<CoopPlayer>();
            if (player == null) return;

            DrawStatus();
            DrawObjective();
            DrawHotbar();
            DrawBreakProgress();
            if (inventoryOpen) DrawInventory();
        }

        private void DrawStatus()
        {
            float x = 18f, y = 18f, w = 330f, h = 124f;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, panel);
            GUI.Label(new Rect(x + 16, y + 10, 160, 26), "DEEPHOLD", title);
            string area = CaveSystem.Instance != null ? CaveSystem.Instance.CurrentArea : "SURFACE";
            GUI.Label(new Rect(x + 18, y + 42, 190, 20), area, label);
            GUI.Label(new Rect(x + 18, y + 70, 45, 18), "HP", small);
            DrawBar(new Rect(x + 52, y + 72, 150, 14), player.HealthValue / 100f, new Color(0.75f, 0.18f, 0.16f), new Color(0.16f, 0.07f, 0.07f));
            GUI.Label(new Rect(x + 207, y + 68, 95, 22), $"{player.HealthValue}/100", small);
            GUI.Label(new Rect(x + 18, y + 94, 290, 22), ToolSystem.DisplayName, toolBig);
        }

        private void DrawObjective()
        {
            float w = 290f, h = 130f;
            float x = Screen.width - w - 18f, y = 18f;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, panel);
            GUI.Label(new Rect(x + 14, y + 10, w - 28, 24), "ЦЕЛЬ", label);
            string text = CaveSystem.Instance != null && CaveSystem.Instance.InsideCave
                ? "Исследуйте шахту\n• Киркой добывайте камень и медь\n• Мечом защищайтесь от существ\n• Найдите редкие кристаллы"
                : "Исследуйте поверхность\n• Найдите пещеру\n• Топором добывайте дерево\n• Подготовьтесь к спуску";
            GUI.Label(new Rect(x + 14, y + 40, w - 28, 82), text, objective);
        }

        private void DrawHotbar()
        {
            var inv = player.GetComponent<Inventory>();
            float slotSize = 64f;
            float total = slotSize * 8f + 18f;
            float x = (Screen.width - total) * 0.5f;
            float y = Screen.height - 88f;
            GUI.Box(new Rect(x, y, total, 76f), GUIContent.none, hotbar);
            for (int i = 0; i < 8; i++)
            {
                Rect r = new Rect(x + 7f + i * slotSize, y + 7f, 58f, 62f);
                bool selected = inv != null && inv.SelectedSlot == i;
                GUI.Box(r, GUIContent.none, slot);
                if (selected) GUI.Box(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), GUIContent.none, hotbar);
                GUI.Label(new Rect(r.x + 4, r.y + 2, 16, 15), (i + 1).ToString(), small);
                GUI.Label(new Rect(r.x + 2, r.y + 21, r.width - 4, 18), itemNames[i], new GUIStyle(label) { fontSize = 8, alignment = TextAnchor.MiddleCenter });
                GUI.Label(new Rect(r.x + 3, r.y + 43, r.width - 6, 15), CountFor(i, inv).ToString(), small);
            }
        }

        private int CountFor(int index, Inventory inv)
        {
            if (inv == null) return 0;
            return index switch
            {
                4 => inv.WoodCount,
                5 => inv.StoneCount,
                3 => inv.TorchCount,
                6 => inv.CopperCount,
                7 => inv.CrystalCount,
                _ => 1
            };
        }

        private void DrawBreakProgress()
        {
            var interactor = player.GetComponent<PlayerInteractor>();
            if (interactor == null || string.IsNullOrEmpty(interactor.ActionText)) return;
            float w = 420f, h = 92f, x = (Screen.width - w) * 0.5f, y = Screen.height - 196f;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, panel);
            GUI.Label(new Rect(x + 12, y + 8, w - 24, 22), interactor.ActionText, progressLabel);
            float pct = Mathf.Clamp01(interactor.ActionProgress01);
            DrawBar(new Rect(x + 24, y + 38, w - 48, 18), pct, new Color(0.88f, 0.65f, 0.22f), new Color(0.10f, 0.08f, 0.05f));
            GUI.Label(new Rect(x + 24, y + 59, w - 48, 20), $"{Mathf.RoundToInt(pct * 100f)}%", percent);
        }

        private void DrawInventory()
        {
            float w = 620f, h = 420f;
            float x = (Screen.width - w) * 0.5f, y = (Screen.height - h) * 0.5f;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, panel);
            GUI.Label(new Rect(x + 20, y + 18, 300, 32), "ИНВЕНТАРЬ", title);
            GUI.Label(new Rect(x + 20, y + 54, 300, 20), "I — закрыть • 1–8 — выбрать слот", small);
            var inv = player.GetComponent<Inventory>();
            for (int i = 0; i < 8; i++)
            {
                float sx = x + 24 + (i % 4) * 145;
                float sy = y + 100 + (i / 4) * 125;
                GUI.Box(new Rect(sx, sy, 125, 100), GUIContent.none, slot);
                GUI.Label(new Rect(sx + 8, sy + 12, 109, 22), itemNames[i], label);
                GUI.Label(new Rect(sx + 8, sy + 48, 109, 30), CountFor(i, inv).ToString(), title);
            }
        }

        private void DrawBar(Rect rect, float value)
        {
            DrawBar(rect, value, new Color(0.72f, 0.52f, 0.16f), new Color(0.08f, 0.07f, 0.05f));
        }

        private void DrawBar(Rect rect, float value, Color fill, Color background)
        {
            value = Mathf.Clamp01(value);
            GUI.color = background;
            GUI.DrawTexture(rect, pixel);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, Mathf.Max(0f, (rect.width - 4) * value), rect.height - 4), pixel);
            GUI.color = Color.white;
        }
    }
}
