using UnityEngine;
using Unity.Netcode;
using OutOfSync.World;

namespace OutOfSync.Gameplay
{
    public sealed class PlayerInteractor : NetworkBehaviour
    {
        private CoopPlayer player;
        private ResourceNode activeNode;
        private float breakTimer;
        private const float Range = 5.0f;

        public string ActionText { get; private set; } = "";
        public float ActionProgress01 { get; private set; }
        public bool IsBreaking => activeNode != null && ActionProgress01 > 0f;

        private void Awake() => player = GetComponent<CoopPlayer>();
        private bool CanControl => player != null && player.CanControl;

        private void Update()
        {
            if (!CanControl || Camera.main == null) return;

            for (int i = 0; i < 3; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) ToolSystem.SelectSlot(i);

            if (Input.GetKeyDown(KeyCode.F)) TryUseNearbyCave();
            if (Input.GetMouseButtonDown(1)) TryCombat();
            if (Input.GetMouseButton(0)) TryGatherOrBreak();
            else ResetBreak();
        }

        private Vector2 MouseWorld()
        {
            var p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return new Vector2(p.x, p.y);
        }

        private void TryGatherOrBreak()
        {
            var point = MouseWorld();
            if (Vector2.Distance(point, transform.position) > Range) { ResetBreak(); return; }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 100f)) { ResetBreak(); return; }

            var cave = hit.collider.GetComponentInParent<CaveEntrance>();
            if (cave != null && Input.GetMouseButtonDown(0)) { cave.Use(player); return; }

            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node == null) { ResetBreak(); return; }
            if (!ToolSystem.CanBreak(node.RequiredTool))
            {
                activeNode = node;
                ActionProgress01 = 0f;
                ActionText = node.RequiredTool == ResourceToolRequirement.Axe ? "НУЖЕН ТОПОР" : "НУЖНА КИРКА";
                return;
            }

            if (activeNode != node) { activeNode = node; breakTimer = 0f; ActionProgress01 = 0f; }
            breakTimer += Time.deltaTime * ToolSystem.DamagePerSecond(node.RequiredTool);
            ActionProgress01 = player.IsStandalone ? node.Progress01 : Mathf.Clamp01(breakTimer / BreakDuration(node));
            ActionText = node.DisplayName;

            if (player.IsStandalone)
            {
                if (node.TryDamage(player, Time.deltaTime * ToolSystem.DamagePerSecond(node.RequiredTool)))
                    ResetBreak();
            }
            else if (ActionProgress01 >= 0.999f)
            {
                BreakResourceServerRpc(point, node.Type);
                ResetBreak();
            }
        }

        private float BreakDuration(ResourceNode node) => node == null ? 1f : (node.Type == 10 ? 3f : node.Type == 11 ? 2.2f : node.Type == 12 ? 2.8f : node.Type == 13 ? 3.5f : 1.5f);

        [ServerRpc]
        private void BreakResourceServerRpc(Vector2 point, int expectedType)
        {
            var node = ResourceNode.FindAt(point);
            if (node == null || node.Type != expectedType) return;
            node.Gather(player);
            BreakResourceClientRpc(point, expectedType);
        }

        [ClientRpc]
        private void BreakResourceClientRpc(Vector2 point, int expectedType)
        {
            var node = ResourceNode.FindAt(point);
            if (node != null && node.Type == expectedType) node.DestroyNode();
        }

        private void ResetBreak()
        {
            activeNode = null;
            breakTimer = 0f;
            ActionProgress01 = 0f;
            ActionText = "";
        }

        private void TryUseNearbyCave()
        {
            foreach (var cave in FindObjectsByType<CaveEntrance>())
            {
                if (Vector2.Distance(cave.transform.position, transform.position) <= 2.6f)
                {
                    cave.Use(player);
                    return;
                }
            }
        }

        private void TryCombat()
        {
            if (ToolSystem.SelectedTool != ToolType.WoodenSword) return;
            var point = MouseWorld();
            if (Vector2.Distance(point, transform.position) > 2.4f) return;
            foreach (var enemy in FindObjectsByType<CaveCrawler>())
            {
                if (Vector2.Distance(point, enemy.transform.position) <= 1.5f)
                {
                    if (player.IsStandalone) enemy.TakeDamage(20);
                    else AttackEnemyServerRpc(point);
                    break;
                }
            }
        }

        [ServerRpc]
        private void AttackEnemyServerRpc(Vector3 point)
        {
            foreach (var enemy in FindObjectsByType<CaveCrawler>())
            {
                if (Vector2.Distance(point, enemy.transform.position) <= 1.5f)
                {
                    enemy.TakeDamage(20);
                    break;
                }
            }
        }
    }
}
