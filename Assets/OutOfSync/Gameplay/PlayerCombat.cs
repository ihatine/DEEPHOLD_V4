using UnityEngine;
using Unity.Netcode;

namespace OutOfSync.Gameplay
{
    public sealed class PlayerCombat : NetworkBehaviour
    {
        private float nextAttack;
        private CoopPlayer player;

        private void Awake()
        {
            player = GetComponent<CoopPlayer>();
        }

        private void Update()
        {
            if (player == null || !player.CanControl || Camera.main == null) return;

            if (Input.GetKeyDown(KeyCode.F) && Time.time >= nextAttack)
            {
                nextAttack = Time.time + 0.35f;
                var p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                p.z = 0f;

                if (player.IsStandalone)
                    AttackLocal(p);
                else
                    AttackServerRpc(p);
            }
        }

        private void AttackLocal(Vector3 point)
        {
            foreach (var target in FindObjectsByType<CoopPlayer>())
            {
                if (target == player) continue;
                if (Vector2.Distance(point, target.transform.position) < 1.2f)
                    target.DamageLocal(10);
            }
        }

        [ServerRpc]
        private void AttackServerRpc(Vector3 point)
        {
            foreach (var target in FindObjectsByType<CoopPlayer>())
            {
                if (target != this && Vector2.Distance(point, target.transform.position) < 1.2f)
                    target.DamageServerRpc(10);
            }
        }
    }
}
