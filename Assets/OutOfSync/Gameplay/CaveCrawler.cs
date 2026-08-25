using UnityEngine;

namespace OutOfSync.Gameplay
{
    public sealed class CaveCrawler : MonoBehaviour
    {
        private int damage = 5;
        private int health = 40;
        private float cooldown;
        private CoopPlayer target;

        public void SetDamage(int value) => damage = Mathf.Max(1, value);
        public void TakeDamage(int amount)
        {
            health -= Mathf.Max(0, amount);
            if (health <= 0) Destroy(gameObject);
        }

        private void Update()
        {
            if (OutOfSync.World.CaveSystem.Instance == null || !OutOfSync.World.CaveSystem.Instance.InsideCave) return;
            if (target == null || !target.CanControl)
            {
                target = FindAnyObjectByType<CoopPlayer>();
                if (target == null) return;
            }

            var delta = target.transform.position - transform.position;
            if (delta.sqrMagnitude < 36f && delta.sqrMagnitude > 0.6f)
                transform.position += delta.normalized * (1.35f * Time.deltaTime);

            cooldown -= Time.deltaTime;
            if (delta.sqrMagnitude < 1.4f && cooldown <= 0f)
            {
                cooldown = 1.2f;
                if (target.IsStandalone) target.DamageLocal(damage);
                else if (target.IsSpawned) target.DamageServerRpc(damage);
            }
        }
    }
}
