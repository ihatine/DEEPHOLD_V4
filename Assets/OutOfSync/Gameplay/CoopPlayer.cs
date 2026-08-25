using UnityEngine;
using Unity.Netcode;

namespace OutOfSync.Gameplay
{
    /// <summary>
    /// 2.5D top-down player. Movement is on the XY plane; Z is only used for
    /// render depth. The class still derives from NetworkBehaviour so the same
    /// prefab can later be replicated by Netcode, but the current build is
    /// intentionally offline-first.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CoopPlayer : NetworkBehaviour
    {
        public NetworkVariable<int> Health = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<Vector3> Look = new(Vector3.right, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [SerializeField] private float speed = 5.5f;
        [SerializeField] private float acceleration = 22f;

        private Rigidbody rb;
        private Vector2 input;
        private Vector3 localLook = Vector3.right;
        private int localHealth = 100;
        private bool standalone;

        public bool IsStandalone => standalone;
        public bool CanControl => IsOwner || standalone;
        public int HealthValue => standalone ? localHealth : Health.Value;
        public Vector3 LookValue => standalone ? localLook : Look.Value;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            standalone = NetworkManager.Singleton == null;
        }

        public override void OnNetworkSpawn()
        {
            standalone = false;
            if (IsServer && OutOfSync.World.WorldGenerator.Instance != null)
            {
                var baseSpawn = OutOfSync.World.WorldGenerator.Instance.GetSpawnPosition();
                float offset = (OwnerClientId % 4) * 1.2f;
                transform.position = baseSpawn + new Vector3(offset, 0f, 0f);
            }
            if (IsOwner)
                FindAnyObjectByType<FollowCamera>()?.SetTarget(transform);
        }

        private void Update()
        {
            if (!CanControl) return;

            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f) input.Normalize();

            var cam = Camera.main;
            if (cam != null)
            {
                var mouse = cam.ScreenToWorldPoint(Input.mousePosition);
                mouse.z = transform.position.z;
                var direction = mouse - transform.position;
                if (direction.sqrMagnitude > 0.001f)
                {
                    localLook = direction.normalized;
                    if (!standalone)
                        Look.Value = localLook;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!CanControl) return;

            var desired = input * speed;
            var velocity = rb.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, desired.x, acceleration * Time.fixedDeltaTime);
            velocity.y = Mathf.MoveTowards(velocity.y, desired.y, acceleration * Time.fixedDeltaTime);
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }

        [ServerRpc]
        public void DamageServerRpc(int amount)
        {
            Health.Value = Mathf.Max(0, Health.Value - Mathf.Max(0, amount));
            if (Health.Value == 0) Health.Value = 100;
        }

        public void DamageLocal(int amount)
        {
            localHealth = Mathf.Max(0, localHealth - Mathf.Max(0, amount));
            if (localHealth == 0) localHealth = 100;
        }
    }
}
