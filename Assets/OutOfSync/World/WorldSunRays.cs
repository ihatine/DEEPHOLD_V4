using UnityEngine;

namespace OutOfSync.World
{
    /// <summary>Subtle fake god-rays used to sell the warm volumetric 2.5D look without a post-processing package.</summary>
    public sealed class WorldSunRays : MonoBehaviour
    {
        private Material material;
        private readonly Transform[] rays = new Transform[4];
        private float time;

        private void Awake()
        {
            var shader = Shader.Find("DEEPHOLD/LightRay");
            if (shader == null) return;
            material = new Material(shader);
            material.SetColor("_Color", new Color(1f, 0.76f, 0.42f, 0.10f));

            for (int i = 0; i < rays.Length; i++)
            {
                var go = new GameObject("SunRay_" + i);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(-16f + i * 10f, 10f + (i % 2) * 8f, -0.10f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, -12f + i * 8f);
                go.transform.localScale = new Vector3(9f + i * 2f, 22f + i * 3f, 1f);
                var filter = go.AddComponent<MeshFilter>();
                var renderer = go.AddComponent<MeshRenderer>();
                filter.sharedMesh = MakeQuad();
                renderer.sharedMaterial = material;
                rays[i] = go.transform;
            }
        }

        private void Update()
        {
            time += Time.deltaTime;
            for (int i = 0; i < rays.Length; i++)
            {
                if (rays[i] == null) continue;
                var p = rays[i].localPosition;
                p.x += Mathf.Sin(time * 0.08f + i) * Time.deltaTime * 0.3f;
                rays[i].localPosition = p;
            }
        }

        private static Mesh MakeQuad()
        {
            var m = new Mesh { name = "SunRayQuad" };
            m.vertices = new[] { new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0), new Vector3(0.5f,0.5f,0), new Vector3(-0.5f,0.5f,0) };
            m.uv = new[] { new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1) };
            m.triangles = new[] {0,1,2,0,2,3};
            m.RecalculateBounds();
            return m;
        }
    }
}
