using UnityEngine;
using OutOfSync.Gameplay;

namespace OutOfSync.World
{
    public enum ResourceToolRequirement { None, Axe, Pickaxe, Sword }

    public sealed class ResourceNode : MonoBehaviour
    {
        private int type;
        private bool depleted;
        private ResourceToolRequirement requirement = ResourceToolRequirement.None;
        private float durability = 1f;
        private float maxDurability = 1f;

        public int Type => type;
        public ResourceToolRequirement RequiredTool => requirement;
        public float Progress01 { get; private set; }
        public float Remaining01 => 1f - Progress01;
        public string DisplayName => type == 10 ? "ДЕРЕВО" : type == 11 ? "КАМЕНЬ" : type == 12 ? "МЕДНАЯ РУДА" : type == 13 ? "КРИСТАЛЛ" : type == 5 ? "КАМЕНЬ" : "РЕСУРС";

        public void Configure(int resourceType)
        {
            type = resourceType;
            requirement = resourceType == 10 ? ResourceToolRequirement.Axe :
                          resourceType == 11 || resourceType == 12 || resourceType == 13 ? ResourceToolRequirement.Pickaxe :
                          resourceType == 5 ? ResourceToolRequirement.Pickaxe : ResourceToolRequirement.Pickaxe;
            maxDurability = resourceType == 10 ? 3.0f : resourceType == 11 ? 2.2f : resourceType == 12 ? 2.8f : resourceType == 13 ? 3.5f : 1.5f;
            durability = maxDurability;
        }

        public bool TryDamage(CoopPlayer player, float delta)
        {
            if (depleted || player == null || !ToolSystem.CanBreak(requirement)) return false;
            durability -= Mathf.Max(0f, delta);
            Progress01 = Mathf.Clamp01(1f - durability / maxDurability);
            if (durability > 0f) return false;
            Gather(player);
            return true;
        }

        public void Gather(CoopPlayer player)
        {
            if (depleted || player == null) return;
            depleted = true;
            var inventory = player.GetComponent<Inventory>();
            if (inventory != null)
            {
                int kind = type == 10 ? 0 : type == 11 ? 1 : type == 12 ? 3 : type == 13 ? 4 : type == 5 ? 1 : 1;
                if (player.IsStandalone) inventory.AddResourceLocal(kind, 1);
                else inventory.AddResourceAuthoritative(kind, 1);
            }
            DestroyNode();
        }

        public void DestroyNode()
        {
            depleted = true;
            Destroy(gameObject);
        }

        public static ResourceNode FindAt(Vector2 point, float radius = 1.4f)
        {
            ResourceNode best = null;
            float bestSq = radius * radius;
            foreach (var node in Object.FindObjectsByType<ResourceNode>())
            {
                float sq = ((Vector2)node.transform.position - point).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = node; }
            }
            return best;
        }
    }
}
