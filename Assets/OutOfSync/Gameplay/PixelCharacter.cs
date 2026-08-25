using UnityEngine;

namespace OutOfSync.Gameplay
{
    /// <summary>
    /// Lightweight sprite-sheet character. No Animator Controller is required:
    /// the component selects a 4x4 pixel-art sheet at runtime.
    /// Rows: down, left, right, up. Columns: walk/idle frames.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelCharacter : MonoBehaviour
    {
        [SerializeField] private float pixelsPerUnit = 32f;
        [SerializeField] private float walkFps = 8f;

        private SpriteRenderer spriteRenderer;
        private Sprite[] sprites;
        private CoopPlayer player;
        private Vector2Int facing = Vector2Int.down;
        private float animClock;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            player = GetComponent<CoopPlayer>();
            LoadSheet();
        }

        private void LoadSheet()
        {
            var tex = Resources.Load<Texture2D>("Characters/DeepMiner");
            if (tex == null)
            {
                Debug.LogError("[PixelCharacter] Missing Resources/Characters/DeepMiner.png");
                return;
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            sprites = new Sprite[16];
            int index = 0;
            for (int row = 0; row < 4; row++)
            {
                // Unity sprites use a bottom-left origin, so invert the visual row.
                int sourceRow = 3 - row;
                for (int col = 0; col < 4; col++)
                {
                    var rect = new Rect(col * 32, sourceRow * 48, 32, 48);
                    sprites[index++] = Sprite.Create(
                        tex, rect, new Vector2(0.5f, 0.08f), pixelsPerUnit, 0,
                        SpriteMeshType.FullRect);
                }
            }

            spriteRenderer.sprite = sprites[0];
            spriteRenderer.sortingOrder = 50;
            spriteRenderer.color = Color.white;
        }

        private void Update()
        {
            if (sprites == null || sprites.Length == 0) return;

            Vector2 velocity = Vector2.zero;
            if (player != null)
            {
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) velocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
            }

            if (velocity.sqrMagnitude > 0.05f)
            {
                if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                    facing = velocity.x < 0f ? Vector2Int.left : Vector2Int.right;
                else
                    facing = velocity.y < 0f ? Vector2Int.down : Vector2Int.up;

                animClock += Time.deltaTime * walkFps;
            }
            else
            {
                animClock = 0f;
            }

            int row = facing == Vector2Int.down ? 0 :
                      facing == Vector2Int.left ? 1 :
                      facing == Vector2Int.right ? 2 : 3;

            int frame = velocity.sqrMagnitude > 0.05f
                ? 1 + (Mathf.FloorToInt(animClock) % 3)
                : 0;

            spriteRenderer.sprite = sprites[row * 4 + frame];
        }
    }
}
