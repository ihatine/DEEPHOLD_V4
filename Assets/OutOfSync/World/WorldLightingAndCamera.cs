using UnityEngine;
using OutOfSync.Gameplay;

namespace OutOfSync.World
{
    /// <summary>
    /// Establishes the Core Keeper-like 2.5D presentation: an angled orthographic camera,
    /// soft sun, ambient fill and restrained fog. Everything stays lightweight and built-in-pipeline friendly.
    /// </summary>
    public sealed class WorldLightingAndCamera : MonoBehaviour
    {
        [SerializeField] private float cameraDistance = 22f;
        [SerializeField] private float cameraHeight = 10f;
        [SerializeField] private float cameraAngle = 31f;
        [SerializeField] private float orthographicSize = 9.5f;
        private void Awake()
        {
            SetupLighting();
        }

        private static void SetupLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.065f);
            RenderSettings.fogDensity = 0.0075f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.20f, 0.26f, 0.30f);
            RenderSettings.ambientEquatorColor = new Color(0.11f, 0.15f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.045f, 0.055f, 0.05f);
            RenderSettings.ambientIntensity = 0.85f;
            RenderSettings.reflectionIntensity = 0.45f;

            var existing = FindAnyObjectByType<Light>();
            if (existing != null && existing.type == LightType.Directional)
            {
                existing.intensity = 1.15f;
                existing.color = new Color(1.0f, 0.88f, 0.70f);
                existing.shadows = LightShadows.Soft;
                existing.shadowStrength = 0.58f;
                existing.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
                return;
            }

            var go = new GameObject("Sun_Directional");
            var sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1.0f, 0.88f, 0.70f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.58f;
            sun.shadowBias = 0.035f;
            sun.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
        }
    }
}
