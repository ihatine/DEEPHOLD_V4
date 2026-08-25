using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using OutOfSync.Gameplay;

namespace OutOfSync.World
{
    /// <summary>
    /// Optimized deterministic 2.5D world.
    /// Floor tiles are rendered in 16x16 chunks instead of one GameObject per tile.
    /// Only interactive props keep individual GameObjects/colliders.
    /// </summary>
    public sealed class WorldGenerator : MonoBehaviour
    {
        public static WorldGenerator Instance { get; private set; }

        private readonly Dictionary<Vector2Int, int> tiles = new();
        private readonly Dictionary<Vector2Int, GameObject> chunks = new();
        private Transform root;
        private Material floorMat, dirtMat, stoneMat, grassMat, waterMat;
        private Material woodMat, leafMat, crystalMat, rockMat, berryMat, torchMat;
        private Vector2Int spawnCell;
        private const int WorldSeed = 274918;

        private const int Width = 128;
        private const int Height = 80;
        private const int ChunkSize = 16;

        /// <summary>Material used by generated water surfaces for visual polish.</summary>
        public Material WaterMaterial => waterMat;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateMaterials();
            Generate();
        }

        private void CreateMaterials()
        {
            var shader = Shader.Find("DEEPHOLD/Surface") ?? Shader.Find("Standard");
            var waterShader = Shader.Find("DEEPHOLD/Water") ?? shader;
            if (shader == null)
            {
                Debug.LogError("[WorldGenerator] No compatible shader found.");
                return;
            }

            floorMat = Make(shader, new Color(0.10f, 0.15f, 0.12f));
            dirtMat = Make(shader, new Color(0.28f, 0.19f, 0.14f));
            stoneMat = Make(shader, new Color(0.24f, 0.26f, 0.29f));
            grassMat = Make(shader, new Color(0.30f, 0.49f, 0.23f));
            waterMat = Make(waterShader, new Color(0.035f, 0.24f, 0.48f));
            ConfigureWaterMaterial(waterMat);
            woodMat = Make(shader, new Color(0.30f, 0.16f, 0.08f));
            leafMat = Make(shader, new Color(0.12f, 0.31f, 0.15f));
            crystalMat = Make(shader, new Color(0.42f, 0.30f, 0.78f));
            rockMat = Make(shader, new Color(0.32f, 0.34f, 0.37f));
            berryMat = Make(shader, new Color(0.65f, 0.10f, 0.20f));
            torchMat = Make(shader, new Color(0.92f, 0.48f, 0.10f));
        }

        private static Material Make(Shader shader, Color color)
        {
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.18f);
            return mat;
        }

        private static void ConfigureWaterMaterial(Material mat)
        {
            if (mat == null) return;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.82f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.82f);
            if (mat.HasProperty("_SpecColor")) mat.SetColor("_SpecColor", new Color(0.65f,0.82f,1f));
        }

        private void Generate()
        {
            root = new GameObject("World").transform;
            spawnCell = Vector2Int.zero;
            tiles.Clear();

            // Deterministic, seed-based surface generation. The same seed produces
            // the same world on host and clients, while protected spawn corridors
            // prevent common procedural dead-ends.
            for (int x = -Width / 2; x <= Width / 2; x++)
            {
                for (int y = -Height / 2; y <= Height / 2; y++)
                {
                    float large = Mathf.PerlinNoise((x + WorldSeed * 0.001f) * 0.028f, (y - WorldSeed * 0.0017f) * 0.028f);
                    float detail = Mathf.PerlinNoise((x - WorldSeed * 0.0009f) * 0.075f, (y + WorldSeed * 0.0013f) * 0.075f);
                    int type = large < 0.22f ? 5 : large < 0.47f ? 0 : large < 0.82f ? 2 : 1;
                    if (type == 2 && detail > 0.86f) type = 0;
                    tiles[new Vector2Int(x, y)] = type;
                }
            }

            // Guaranteed safe starting clearing and a dry route to the cave.
            CreateSpawnPath();
            for (int x = 6; x <= 21; x++)
                for (int y = 6; y <= 11; y++)
                    tiles[new Vector2Int(x, y)] = 2;

            PaintWater(new Vector2Int(-34, 18), 7, 5);
            PaintWater(new Vector2Int(34, -17), 8, 5);
            PaintWater(new Vector2Int(29, 22), 5, 4);

            // Re-open the safe corridor if a water patch overlaps it.
            for (int x = 6; x <= 21; x++)
                for (int y = 6; y <= 11; y++)
                    tiles[new Vector2Int(x, y)] = 2;

            RebuildAllChunks();
            CreateCore();
            CreateTrees();
            CreateCaveEntrance();
            CreateResourcePatches();
            CreateDecorations();
            new GameObject("WorldVisualPolish").AddComponent<WorldVisualPolish>();
            new GameObject("WorldLightingAndCamera").AddComponent<WorldLightingAndCamera>();
            new GameObject("WorldSunRays").AddComponent<WorldSunRays>();
        }

        private void CreateSpawnPath()
        {
            for (int x = -10; x <= 10; x++)
                for (int y = -1; y <= 1; y++)
                    tiles[new Vector2Int(x, y)] = 2;
        }

        private void PaintWater(Vector2Int center, int radiusX, int radiusY)
        {
            for (int x = -radiusX; x <= radiusX; x++)
                for (int y = -radiusY; y <= radiusY; y++)
                    if ((x * x) / (float)(radiusX * radiusX) + (y * y) / (float)(radiusY * radiusY) <= 1f)
                        tiles[center + new Vector2Int(x, y)] = 5;
        }

        private void RebuildAllChunks()
        {
            foreach (var go in chunks.Values) Destroy(go);
            chunks.Clear();

            int minX = -Width / 2, maxX = Width / 2;
            int minY = -Height / 2, maxY = Height / 2;

            for (int cx = Mathf.FloorToInt(minX / (float)ChunkSize);
                 cx <= Mathf.FloorToInt(maxX / (float)ChunkSize); cx++)
            {
                for (int cy = Mathf.FloorToInt(minY / (float)ChunkSize);
                     cy <= Mathf.FloorToInt(maxY / (float)ChunkSize); cy++)
                {
                    RebuildChunk(new Vector2Int(cx, cy));
                }
            }
        }

        private void RebuildChunk(Vector2Int chunk)
        {
            if (chunks.TryGetValue(chunk, out var old))
            {
                Destroy(old);
                chunks.Remove(chunk);
            }

            var go = new GameObject($"Chunk_{chunk.x}_{chunk.y}");
            go.transform.SetParent(root, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { floorMat, dirtMat, stoneMat, grassMat, waterMat };

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var subTris = new List<int>[5];
            for (int i = 0; i < subTris.Length; i++) subTris[i] = new List<int>();

            int startX = chunk.x * ChunkSize;
            int startY = chunk.y * ChunkSize;
            float topZ = 0.24f;
            float bottomZ = 0.82f;
            float waterZ = 0.12f;

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, int matIndex)
            {
                int vi = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                normals.Add(n); normals.Add(n); normals.Add(n); normals.Add(n);
                var t = subTris[matIndex];
                t.Add(vi); t.Add(vi + 1); t.Add(vi + 2);
                t.Add(vi); t.Add(vi + 2); t.Add(vi + 3);
            }

            bool SameOrSolid(Vector2Int c, int type)
            {
                return tiles.TryGetValue(c, out int neighbour) && neighbour == type;
            }

            for (int lx = 0; lx < ChunkSize; lx++)
            {
                for (int ly = 0; ly < ChunkSize; ly++)
                {
                    var cell = new Vector2Int(startX + lx, startY + ly);
                    if (!tiles.TryGetValue(cell, out int type)) continue;
                    int matIndex = type switch { 0 => 0, 1 => 2, 2 => 3, 5 => 4, _ => 0 };
                    float z = type == 5 ? waterZ : topZ;
                    float x0 = lx, x1 = lx + 1f, y0 = ly, y1 = ly + 1f;

                    // Camera sits on the negative-Z side, therefore this winding faces the camera.
                    AddQuad(new Vector3(x0,y0,z), new Vector3(x0,y1,z), new Vector3(x1,y1,z), new Vector3(x1,y0,z), Vector3.back, matIndex);

                    // Solid blocks have depth. Only exposed edges receive side faces, which keeps the mesh small.
                    if (type != 5)
                    {
                        if (!SameOrSolid(cell + Vector2Int.down, type))
                            AddQuad(new Vector3(x0,y0,z), new Vector3(x1,y0,z), new Vector3(x1,y0,bottomZ), new Vector3(x0,y0,bottomZ), Vector3.down, matIndex);
                        if (!SameOrSolid(cell + Vector2Int.up, type))
                            AddQuad(new Vector3(x1,y1,z), new Vector3(x0,y1,z), new Vector3(x0,y1,bottomZ), new Vector3(x1,y1,bottomZ), Vector3.up, matIndex);
                        if (!SameOrSolid(cell + Vector2Int.left, type))
                            AddQuad(new Vector3(x0,y1,z), new Vector3(x0,y0,z), new Vector3(x0,y0,bottomZ), new Vector3(x0,y1,bottomZ), Vector3.left, matIndex);
                        if (!SameOrSolid(cell + Vector2Int.right, type))
                            AddQuad(new Vector3(x1,y0,z), new Vector3(x1,y1,z), new Vector3(x1,y1,bottomZ), new Vector3(x1,y0,bottomZ), Vector3.right, matIndex);
                    }
                }
            }

            var mesh = new Mesh { name = $"WorldChunkVolume_{chunk.x}_{chunk.y}" };
            mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.subMeshCount = 5;
            for (int i = 0; i < 5; i++) mesh.SetTriangles(subTris[i], i, true);
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;

            go.transform.position = new Vector3(startX, startY, 0f);
            chunks[chunk] = go;
        }

        private void CreateCore()
        {
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "AncientCore";
            core.transform.SetParent(root, false);
            core.transform.position = new Vector3(0f, 0f, 0.10f);
            core.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            core.transform.localScale = new Vector3(3.2f, 0.32f, 3.2f);
            core.GetComponent<Renderer>().sharedMaterial = crystalMat;
            Destroy(core.GetComponent<Collider>());

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "AncientCoreRing";
            ring.transform.SetParent(root, false);
            ring.transform.position = new Vector3(0f, 0f, 0.06f);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(4.2f, 0.16f, 4.2f);
            ring.GetComponent<Renderer>().sharedMaterial = stoneMat;
            Destroy(ring.GetComponent<Collider>());

            for (int i = 0; i < 4; i++)
                CreateTorch(new Vector2(Mathf.Cos(i * Mathf.PI * 0.5f) * 4.5f,
                                        Mathf.Sin(i * Mathf.PI * 0.5f) * 4.5f));
        }

        private void CreateTrees()
        {
            // Deterministic Poisson-like placement: trees only appear on land,
            // never inside the spawn/core area or the cave approach.
            var used = new List<Vector2Int>();
            var rng = new System.Random(WorldSeed);
            int attempts = 0;
            while (used.Count < 26 && attempts++ < 800)
            {
                int x = rng.Next(-Width / 2 + 5, Width / 2 - 4);
                int y = rng.Next(-Height / 2 + 5, Height / 2 - 4);
                var cell = new Vector2Int(x, y);
                if (!tiles.TryGetValue(cell, out var type) || (type != 2 && type != 0)) continue;
                if (Mathf.Abs(x) < 13 && Mathf.Abs(y) < 11) continue;
                if (x > 2 && x < 25 && y > 3 && y < 14) continue;
                bool tooClose = false;
                foreach (var other in used)
                    if ((other - cell).sqrMagnitude < 64) { tooClose = true; break; }
                if (tooClose) continue;
                used.Add(cell);
                CreateTree(cell);
            }
        }

        private void CreateTree(Vector2Int cell)
        {
            var go = new GameObject("Tree_Large_Volumetric");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0.18f);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk_Volume";
            trunk.transform.SetParent(go.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 2.15f, 0.10f);
            trunk.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            trunk.transform.localScale = new Vector3(0.58f, 1.65f, 0.58f);
            trunk.GetComponent<Renderer>().sharedMaterial = woodMat;
            Destroy(trunk.GetComponent<Collider>());

            Vector3[] canopyPos = {
                new Vector3(-0.75f,4.1f,0.08f), new Vector3(0.75f,4.25f,0.12f),
                new Vector3(0f,5.1f,-0.02f), new Vector3(0f,3.8f,-0.10f)
            };
            Vector3[] canopyScale = {
                new Vector3(2.35f,1.65f,0.82f), new Vector3(2.15f,1.55f,0.78f),
                new Vector3(2.45f,1.7f,0.86f), new Vector3(2.6f,1.35f,0.78f)
            };
            for (int i = 0; i < canopyPos.Length; i++)
            {
                var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy.name = "Canopy_Volume_" + i;
                canopy.transform.SetParent(go.transform, false);
                canopy.transform.localPosition = canopyPos[i];
                canopy.transform.localScale = canopyScale[i];
                canopy.GetComponent<Renderer>().sharedMaterial = leafMat;
                Destroy(canopy.GetComponent<Collider>());
            }

            var shadow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shadow.name = "Tree_Shadow";
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0f, 0.30f);
            shadow.transform.localScale = new Vector3(2.6f, 1.35f, 0.08f);
            shadow.GetComponent<Renderer>().sharedMaterial = Make(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"), new Color(0.02f,0.015f,0.012f));
            Destroy(shadow.GetComponent<Collider>());

            var node = go.AddComponent<ResourceNode>();
            node.Configure(10);
        }

        private void CreateCaveEntrance()
        {
            if (CaveSystem.Instance == null)
                new GameObject("CaveSystem").AddComponent<CaveSystem>();
            CaveSystem.Instance?.CreateSurfaceEntrance(new Vector3(20.5f, 9.5f, 0.15f));
        }

        private void CreateResourcePatches()
        {
            var rng = new System.Random(WorldSeed + 77);
            int created = 0, attempts = 0;
            while (created < 22 && attempts++ < 500)
            {
                int x = rng.Next(-Width / 2 + 4, Width / 2 - 4);
                int y = rng.Next(-Height / 2 + 4, Height / 2 - 4);
                var cell = new Vector2Int(x, y);
                if (!tiles.TryGetValue(cell, out var ground) || ground == 5) continue;
                if (cell.sqrMagnitude < 100) continue;
                int type = created % 3;
                CreateResourceNode(new Vector2(x + 0.5f, y + 0.5f), type);
                created++;
            }
        }

        private void CreateResourceNode(Vector2 p, int type)
        {
            var go = new GameObject(type == 0 ? "CopperVein" : type == 1 ? "CrystalCluster" : "MushroomPatch");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(p.x, p.y, 0.15f);

            PrimitiveType primitive = type == 1 ? PrimitiveType.Cylinder :
                                      type == 2 ? PrimitiveType.Sphere : PrimitiveType.Cube;
            var visual = GameObject.CreatePrimitive(primitive);
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = type == 1 ? new Vector3(0.7f, 1.8f, 0.7f)
                                    : type == 2 ? new Vector3(1.1f, 0.65f, 1.1f)
                                    : new Vector3(1.1f, 0.75f, 0.9f);
            visual.GetComponent<Renderer>().sharedMaterial = type == 0 ? rockMat :
                                                              type == 1 ? crystalMat : berryMat;

            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.1f, 1.1f, 1f);
            collider.isTrigger = false;

            var node = go.AddComponent<ResourceNode>();
            node.Configure(type);
        }

        private void CreateDecorations()
        {
            for (int i = 0; i < 42; i++)
            {
                float a = i * 1.731f;
                float r = 7f + (i % 4) * 7f;
                var p = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                if (i % 3 == 0) CreateRock(p, 0.45f + (i % 4) * 0.12f);
                else if (i % 3 == 1) CreateMushroom(p, 0.5f + (i % 3) * 0.15f);
                else CreateFlowerPatch(p, 0.35f + (i % 3) * 0.08f);
            }
        }

        private void CreateMushroom(Vector2 p, float scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Mushroom";
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(p.x, p.y, 0.20f);
            go.transform.localScale = new Vector3(scale, scale * 0.7f, scale);
            go.GetComponent<Renderer>().sharedMaterial = berryMat;
            Destroy(go.GetComponent<Collider>());
        }

        private void CreateRock(Vector2 p, float scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "RockDecor";
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(p.x, p.y, 0.18f);
            go.transform.localScale = new Vector3(scale * 1.5f, scale, scale * 1.15f);
            go.GetComponent<Renderer>().sharedMaterial = rockMat;
            Destroy(go.GetComponent<Collider>());
        }

        private void CreateFlowerPatch(Vector2 p, float scale)
        {
            var go = new GameObject("FlowerPatch");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(p.x, p.y, 0.17f);
            for (int i = 0; i < 3; i++)
            {
                var flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.transform.SetParent(go.transform, false);
                flower.transform.localPosition = new Vector3((i - 1) * 0.18f, 0f, 0f);
                flower.transform.localScale = Vector3.one * scale * (0.55f + i * 0.12f);
                flower.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? berryMat : crystalMat;
                Destroy(flower.GetComponent<Collider>());
            }
        }

        private void CreateTorch(Vector2 p)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Torch";
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(p.x, p.y, 0.22f);
            go.transform.localScale = new Vector3(0.15f, 0.55f, 0.15f);
            go.GetComponent<Renderer>().sharedMaterial = torchMat;
            Destroy(go.GetComponent<Collider>());

            var light = new GameObject("TorchLight").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 0.6f, -0.1f);
            light.type = LightType.Point;
            light.range = 3.5f;
            light.intensity = 1.0f;
            light.color = new Color(1f, 0.55f, 0.22f);
        }

        public Vector3 GetSpawnPosition() => new Vector3(spawnCell.x + 0.5f, spawnCell.y + 0.5f, -0.25f);

        public void Mine(Vector2 point, ulong owner)
        {
            // Surface terrain is deliberately immutable. Underground resources are
            // represented by ResourceNode objects and can be gathered there.
            if (CaveSystem.Instance != null && !CaveSystem.Instance.InsideCave) return;
            var cell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            if (cell == spawnCell || (Mathf.Abs(cell.x) <= 4 && Mathf.Abs(cell.y) <= 4)) return;
            if (!tiles.ContainsKey(cell)) return;

            tiles.Remove(cell);
            RebuildChunk(WorldToChunk(cell));

            foreach (var p in FindObjectsByType<CoopPlayer>())
            {
                if (NetworkManager.Singleton == null)
                    p.GetComponent<Inventory>()?.AddResourceLocal(1, 1);
                else if (p.IsSpawned && p.OwnerClientId == owner)
                    p.GetComponent<Inventory>()?.AddResourceServerRpc(1, 1);
            }
        }

        public void Place(Vector2 point, ulong owner)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            if (tiles.ContainsKey(cell)) return;
            if (Mathf.Abs(cell.x) > Width / 2 || Mathf.Abs(cell.y) > Height / 2) return;

            tiles[cell] = 0;
            RebuildChunk(WorldToChunk(cell));
        }

        public void ApplyMine(Vector2 point)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            if (!tiles.Remove(cell)) return;
            RebuildChunk(WorldToChunk(cell));
        }

        public void ApplyPlace(Vector2 point)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            if (tiles.ContainsKey(cell)) return;
            tiles[cell] = 0;
            RebuildChunk(WorldToChunk(cell));
        }

        private static Vector2Int WorldToChunk(Vector2Int cell)
        {
            return new Vector2Int(
                Mathf.FloorToInt(cell.x / (float)ChunkSize),
                Mathf.FloorToInt(cell.y / (float)ChunkSize));
        }
    }
}
