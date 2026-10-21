using System.Collections.Generic;
using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// Shows the tool in hand as a 3D model at the cursor: tip on the surface under the pointer,
    /// tilted towards the camera, constant size on screen whatever the zoom. While a repair action
    /// runs it locks onto the part (the screwdriver sits in the screw head and spins with it).
    /// </summary>
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public sealed class ToolPresenter : MonoBehaviour
    {
        [SerializeField] private ToolBelt toolBelt;
        [SerializeField] private RepairInteractor repair;
        [SerializeField] private InteractionRaycaster raycaster;
        [SerializeField] private GameInput input;
        [SerializeField] private Camera viewCamera;

        [Tooltip("Tool length as a fraction of the screen height.")]
        [SerializeField, Range(0.05f, 0.6f)] private float screenHeightFraction = 0.2f;
        [Tooltip("0 = tool stands on the surface normal, 1 = points straight at the camera.")]
        [SerializeField, Range(0f, 1f)] private float tiltTowardCamera = 0.4f;
        [SerializeField, Min(0.1f)] private float followSharpness = 30f;

        private readonly Dictionary<ToolDefinition, Instance> instances = new();
        private Instance shown;
        private bool hasPose;

        private sealed class Instance
        {
            public GameObject GameObject;
            public float Length;
        }

        private void LateUpdate()
        {
            ToolDefinition tool = toolBelt != null ? toolBelt.Current : null;
            if (tool == null || tool.Model == null || viewCamera == null)
            {
                Show(null);
                return;
            }

            Instance instance = GetInstance(tool);
            Vector3 tip = default;
            Vector3 axis = Vector3.up;
            float spin = 0f;
            bool engaged = repair != null && repair.TryGetToolPose(out tip, out axis, out spin);
            if (!engaged)
            {
                spin = 0f;
                if (input == null || raycaster == null || !raycaster.TryRaycast(input.PointerPosition, out RaycastHit hit))
                {
                    Show(null);
                    return;
                }

                Vector3 toCamera = (viewCamera.transform.position - hit.point).normalized;
                axis = Vector3.Slerp(hit.normal, toCamera, tiltTowardCamera).normalized;
                tip = hit.point + hit.normal * 0.0005f;
            }

            float distance = Vector3.Distance(viewCamera.transform.position, tip);
            float worldHeight = 2f * distance * Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float scale = Mathf.Clamp(worldHeight * screenHeightFraction / Mathf.Max(0.001f, instance.Length), 0.02f, 4f);

            Vector3 forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, axis);
            if (forward.sqrMagnitude < 1e-6f)
            {
                forward = Vector3.ProjectOnPlane(viewCamera.transform.up, axis);
            }

            Quaternion rotation = Quaternion.AngleAxis(spin, axis) * Quaternion.LookRotation(forward.normalized, axis);
            Transform t = instance.GameObject.transform;
            if (engaged || !hasPose || shown != instance)
            {
                // locked onto the part: no lag, or the screwdriver would slip out of the screw
                t.SetPositionAndRotation(tip, rotation);
            }
            else
            {
                float k = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
                t.SetPositionAndRotation(Vector3.Lerp(t.position, tip, k), Quaternion.Slerp(t.rotation, rotation, k));
            }

            t.localScale = Vector3.one * scale;
            hasPose = true;
            Show(instance);
        }

        private Instance GetInstance(ToolDefinition tool)
        {
            if (instances.TryGetValue(tool, out Instance instance) && instance.GameObject != null)
            {
                return instance;
            }

            GameObject go = Instantiate(tool.Model, transform);
            go.name = tool.Model.name;
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            float length = 0.001f;
            foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = ignoreRaycast; // the tool must never block picking
            }

            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
            }

            go.transform.localScale = Vector3.one;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Bounds local = r.localBounds;
                length = Mathf.Max(length, r.transform.localPosition.y + local.max.y);
            }

            go.SetActive(false);
            instance = new Instance { GameObject = go, Length = length };
            instances[tool] = instance;
            return instance;
        }

        private void Show(Instance instance)
        {
            if (shown == instance)
            {
                return;
            }

            if (shown != null && shown.GameObject != null)
            {
                shown.GameObject.SetActive(false);
            }

            shown = instance;
            hasPose = false;
            if (instance != null)
            {
                instance.GameObject.SetActive(true);
            }
        }
    }
}
