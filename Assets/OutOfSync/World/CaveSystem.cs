using System.Collections.Generic;
using UnityEngine;
using OutOfSync.Gameplay;

namespace OutOfSync.World
{
    /// <summary>
    /// Surface is a protected exploration layer. The only destructible space is
    /// the generated underground mine. Entering a cave moves the player to a
    /// separate underground room containing resources and hostile creatures.
    /// </summary>
    public sealed class CaveSystem : MonoBehaviour
    {
        public static CaveSystem Instance { get; private set; }
        public bool InsideCave { get; private set; }

        private Transform caveRoot;
        private readonly List<GameObject> spawned = new();
        private Vector3 surfaceReturn;
        private Material stoneMat, deepStoneMat, crystalMat, copperMat, mossMat;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildMaterials();
            BuildCave();
        }

        private void BuildMaterials()
        {
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null) return;
            stoneMat = new Material(shader) { color = new Color(0.18f, 0.18f, 0.22f) };
            deepStoneMat = new Material(shader) { color = new Color(0.10f, 0.11f, 0.15f) };
            crystalMat = new Material(shader) { color = new Color(0.34f, 0.28f, 0.75f) };
            copperMat = new Material(shader) { color = new Color(0.62f, 0.29f, 0.12f) };
            mossMat = new Material(shader) { color = new Color(0.18f, 0.34f, 0.22f) };
        }

        private void BuildCave()
        {
            caveRoot = new GameObject("UndergroundMine").transform;
            caveRoot.SetParent(transform, false);
            caveRoot.position = new Vector3(150f, 0f, 0f);

            CreateFloor();
            CreateWalls();
            CreateResources();
            CreateCreatures();
            CreateEntrance(150f, -9f, true);
            caveRoot.gameObject.SetActive(false);
        }

        private void CreateFloor()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MineFloor";
            go.transform.SetParent(caveRoot, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            go.transform.localScale = new Vector3(42f, 24f, 0.5f);
            go.GetComponent<Renderer>().sharedMaterial = stoneMat;
            Destroy(go.GetComponent<Collider>());
        }

        private void CreateWalls()
        {
            for (int i = 0; i < 18; i++)
            {
                float x = -20f + i * 2.35f;
                CreateRock(new Vector3(x, 11.5f, 0.45f), new Vector3(2.3f, 2.2f, 0.7f));
                CreateRock(new Vector3(x, -11.5f, 0.45f), new Vector3(2.3f, 2.2f, 0.7f));
            }
            for (int i = 0; i < 8; i++)
            {
                float y = -9f + i * 2.5f;
                CreateRock(new Vector3(-21f, y, 0.45f), new Vector3(2.0f, 2.4f, 0.7f));
                CreateRock(new Vector3(21f, y, 0.45f), new Vector3(2.0f, 2.4f, 0.7f));
            }
        }

        private void CreateRock(Vector3 localPos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MineWall";
            go.transform.SetParent(caveRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = deepStoneMat;
            Destroy(go.GetComponent<Collider>());
        }

        private void CreateResources()
        {
            var positions = new[]
            {
                new Vector2(-14, 5), new Vector2(-9, -4), new Vector2(-4, 7),
                new Vector2(3, -6), new Vector2(8, 4), new Vector2(14, -2),
                new Vector2(16, 7), new Vector2(-16, -7), new Vector2(1, 2),
                new Vector2(10, -8)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                int type = i % 3;
                var go = new GameObject(type == 0 ? "MineStone" : type == 1 ? "CopperOre" : "DeepCrystal");
                go.transform.SetParent(caveRoot, false);
                go.transform.localPosition = new Vector3(positions[i].x, positions[i].y, 0.15f);

                var visual = GameObject.CreatePrimitive(type == 2 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                visual.transform.SetParent(go.transform, false);
                visual.transform.localScale = type == 2 ? new Vector3(0.8f, 1.8f, 0.8f) : new Vector3(1.15f, 1.0f, 0.9f);
                visual.GetComponent<Renderer>().sharedMaterial = type == 0 ? stoneMat : type == 1 ? copperMat : crystalMat;
                Destroy(visual.GetComponent<Collider>());

                var collider = go.AddComponent<BoxCollider>();
                collider.size = new Vector3(1.2f, 1.2f, 1f);
                var node = go.AddComponent<ResourceNode>();
                node.Configure(type == 0 ? 11 : type == 1 ? 12 : 13);
            }

            for (int i = 0; i < 14; i++)
            {
                float a = i * 2.1f;
                var p = new Vector2(Mathf.Cos(a) * (5f + (i % 3) * 4f), Mathf.Sin(a) * (6f + (i % 2) * 2f));
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "CaveMoss";
                go.transform.SetParent(caveRoot, false);
                go.transform.localPosition = new Vector3(p.x, p.y, 0.18f);
                go.transform.localScale = new Vector3(0.45f, 0.22f, 0.45f);
                go.GetComponent<Renderer>().sharedMaterial = mossMat;
                Destroy(go.GetComponent<Collider>());
            }
        }

        private void CreateCreatures()
        {
            var positions = new[] { new Vector2(-10, 1), new Vector2(5, 6), new Vector2(13, 3) };
            foreach (var p in positions)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "CaveCrawler";
                go.transform.SetParent(caveRoot, false);
                go.transform.localPosition = new Vector3(p.x, p.y, 0.22f);
                go.transform.localScale = new Vector3(1.0f, 0.75f, 0.55f);
                go.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard")) { color = new Color(0.40f, 0.16f, 0.36f) };
                var c = go.GetComponent<SphereCollider>();
                c.isTrigger = true;
                var enemy = go.AddComponent<CaveCrawler>();
                enemy.SetDamage(7);
                spawned.Add(go);
            }
        }

        private void CreateEntrance(float x, float y, bool exit)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = exit ? "CaveExit" : "CaveEntrance";
            go.transform.SetParent(caveRoot, false);
            go.transform.localPosition = new Vector3(x - caveRoot.position.x, y, 0.12f);
            go.transform.localScale = new Vector3(2.1f, 0.16f, 2.1f);
            go.GetComponent<Renderer>().sharedMaterial = deepStoneMat;
            var collider = go.GetComponent<Collider>();
            collider.isTrigger = true;
            var entrance = go.AddComponent<CaveEntrance>();
            entrance.Configure(exit);
        }

        public void CreateSurfaceEntrance(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "CaveEntrance";
            go.transform.position = position + new Vector3(0f, 0f, 0.12f);
            go.transform.localScale = new Vector3(2.2f, 0.18f, 2.2f);
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            go.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = new Color(0.035f, 0.025f, 0.045f) };
            var collider = go.GetComponent<Collider>();
            collider.isTrigger = true;
            go.AddComponent<CaveEntrance>().Configure(false);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "CaveEntranceRing";
            ring.transform.position = position + new Vector3(0f, 0f, 0.10f);
            ring.transform.localScale = new Vector3(2.8f, 0.10f, 2.8f);
            ring.GetComponent<Renderer>().sharedMaterial = stoneMat;
            Destroy(ring.GetComponent<Collider>());
        }

        public void ToggleCave(CoopPlayer player)
        {
            if (player == null) return;
            if (!InsideCave)
            {
                surfaceReturn = player.transform.position;
                InsideCave = true;
                caveRoot.gameObject.SetActive(true);
                player.transform.position = caveRoot.position + new Vector3(0f, -7f, -0.25f);
                player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            }
            else
            {
                InsideCave = false;
                player.transform.position = surfaceReturn;
                player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                caveRoot.gameObject.SetActive(false);
            }
        }

        public string CurrentArea => InsideCave ? "DEEP MINE" : "SURFACE";
    }
}
