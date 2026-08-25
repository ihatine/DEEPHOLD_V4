using UnityEngine;

namespace OutOfSync.Gameplay
{
    /// <summary>Procedural pixel-art item held by the player. Keeps the prototype self-contained.</summary>
    public sealed class HeldItemVisual : MonoBehaviour
    {
        private SpriteRenderer sr;
        private CoopPlayer player;
        private Texture2D texture;
        private Sprite sprite;
        private ToolType lastTool = (ToolType)(-1);
        private Vector2 lastLook;

        private void Awake()
        {
            player = GetComponentInParent<CoopPlayer>();
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 65;
            transform.localPosition = new Vector3(0.48f, -0.02f, -0.08f);
            transform.localScale = Vector3.one * 0.82f;
            Refresh();
        }

        private void Update()
        {
            if (player == null) player = GetComponentInParent<CoopPlayer>();
            var tool = ToolSystem.SelectedTool;
            if (tool != lastTool) Refresh();

            Vector2 look = player != null ? new Vector2(player.LookValue.x, player.LookValue.y) : Vector2.right;
            if (look.sqrMagnitude < 0.01f) look = Vector2.right;
            look.Normalize();
            lastLook = look;

            bool left = look.x < 0f;
            float side = left ? -1f : 1f;
            transform.localPosition = new Vector3(0.42f * side, -0.03f + Mathf.Clamp(look.y, -0.35f, 0.35f), -0.12f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg + (left ? 180f : 0f));
            sr.flipX = left;
        }

        private void Refresh()
        {
            lastTool = ToolSystem.SelectedTool;
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
            texture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Clear();
            switch (lastTool)
            {
                case ToolType.WoodenAxe: DrawAxe(); break;
                case ToolType.WoodenPickaxe: DrawPick(); break;
                case ToolType.WoodenSword: DrawSword(); break;
                default: DrawHand(); break;
            }
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 24, 24), new Vector2(0.18f, 0.5f), 24f);
            sr.sprite = sprite;
        }

        private void Clear()
        {
            var px = texture.GetPixels32();
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0,0,0,0);
            texture.SetPixels32(px);
        }

        private void P(int x, int y, Color c) { if (x>=0&&x<24&&y>=0&&y<24) texture.SetPixel(x,y,c); }
        private void Rect(int x, int y, int w, int h, Color c) { for(int ix=x;ix<x+w;ix++) for(int iy=y;iy<y+h;iy++) P(ix,iy,c); }

        private void DrawAxe()
        {
            var wood = new Color(0.42f,0.20f,0.08f); var edge = new Color(0.20f,0.09f,0.035f); var metal = new Color(0.55f,0.58f,0.62f);
            Rect(5,3,3,15,edge); Rect(6,4,2,14,wood); Rect(8,15,10,3,edge); Rect(9,16,8,2,metal); Rect(15,14,3,5,metal);
        }
        private void DrawPick()
        {
            var wood = new Color(0.42f,0.20f,0.08f); var metal = new Color(0.55f,0.58f,0.62f); var edge = new Color(0.18f,0.19f,0.21f);
            Rect(5,4,3,15,edge); Rect(6,5,2,13,wood); Rect(8,16,12,3,edge); Rect(9,17,10,2,metal); Rect(17,14,3,5,metal);
        }
        private void DrawSword()
        {
            var grip = new Color(0.38f,0.18f,0.07f); var blade = new Color(0.72f,0.76f,0.80f); var edge = new Color(0.22f,0.24f,0.27f); var gold = new Color(0.72f,0.48f,0.12f);
            Rect(5,3,3,8,grip); Rect(4,10,5,2,gold); Rect(6,12,3,8,edge); Rect(8,13,7,2,blade); Rect(13,11,5,2,blade); Rect(16,9,4,2,blade); P(19,8,edge);
        }
        private void DrawHand() { var skin = new Color(0.75f,0.48f,0.32f); Rect(5,8,7,7,skin); Rect(10,10,4,5,skin); }
    }
}
