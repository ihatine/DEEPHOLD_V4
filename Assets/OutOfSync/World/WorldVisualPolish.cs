using UnityEngine;

namespace OutOfSync.World
{
    /// <summary>Runtime visual layer: animated water tint, subtle day pulse and light bloom-like sprites.</summary>
    public sealed class WorldVisualPolish : MonoBehaviour
    {
        private Material water;
        private Color waterBase;
        private float clock;

        private void Start()
        {
            var generator = WorldGenerator.Instance;
            if (generator != null)
            {
                water = generator.WaterMaterial;
                if (water != null) waterBase = water.HasProperty("_Color") ? water.GetColor("_Color") : water.color;
            }
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.065f);
            RenderSettings.fogDensity = 0.0075f;
        }

        private void Update()
        {
            clock += Time.deltaTime;
            if (water != null)
            {
                float pulse = (Mathf.Sin(clock * 1.35f) + Mathf.Sin(clock * 0.51f) * 0.45f) * 0.5f;
                var c = Color.Lerp(waterBase, waterBase * 1.16f, 0.18f + pulse * 0.08f);
                if (water.HasProperty("_Color")) water.SetColor("_Color", c);
            }
        }
    }
}
