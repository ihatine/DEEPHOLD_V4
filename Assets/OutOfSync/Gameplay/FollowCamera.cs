using UnityEngine;

namespace OutOfSync.Gameplay
{
    /// <summary>
    /// Angled orthographic 2.5D camera. The shallow Z offset gives terrain depth while
    /// preserving the readable Core Keeper-style top-down composition.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        private Transform target;
        [SerializeField] private float smooth = 8f;
        [SerializeField] private float orthographicSize = 9.5f;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, -9.5f, -20f);

        public void SetTarget(Transform t) => target = t;

        private void Awake()
        {
            var cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = orthographicSize;
                cam.allowHDR = true;
                cam.allowMSAA = true;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var cam = GetComponent<Camera>();
            if (cam == null) return;

            Vector3 desired = target.position + followOffset;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));
            Quaternion look = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        }
    }
}
