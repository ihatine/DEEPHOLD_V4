using UnityEngine;
using Unity.Netcode;
using OutOfSync.Gameplay;

namespace OutOfSync.Networking
{
    public static class PlayerFactory
    {
        public static GameObject CreatePlayerPrefab()
        {
            var root = new GameObject("PlayerPrefab");
            root.layer = LayerMask.NameToLayer("Default");

            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Shadow";
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, -0.28f, 0.18f);
            shadow.transform.localScale = new Vector3(0.72f, 0.025f, 0.42f);
            ApplyMaterial(shadow.GetComponent<Renderer>(), new Color(0.015f, 0.012f, 0.02f));
            Object.Destroy(shadow.GetComponent<Collider>());

            var visual = root.AddComponent<SpriteRenderer>();
            visual.name = "CharacterSprite";

            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.height = 1.55f;
            collider.radius = 0.30f;
            collider.center = new Vector3(0f, 0.03f, 0f);

            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.mass = 70f;

            root.AddComponent<NetworkObject>();
            root.AddComponent<OwnerNetworkTransform>();
            root.AddComponent<CoopPlayer>();
            root.AddComponent<Inventory>();
            root.AddComponent<PlayerInteractor>();
            root.AddComponent<PixelCharacter>();
            var held = new GameObject("HeldItem");
            held.transform.SetParent(root.transform, false);
            held.AddComponent<HeldItemVisual>();

            root.SetActive(false);
            Object.DontDestroyOnLoad(root);
            return root;
        }

        private static void ApplyMaterial(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            var material = renderer.material;
            if (material != null) material.color = color;
        }
    }
}
