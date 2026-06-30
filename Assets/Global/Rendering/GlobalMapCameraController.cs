using Unity.Cinemachine;
using UnityEngine;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalMapCameraController : MonoBehaviour
    {
        private const float MinimumVerticalAngle = -89f;
        private const float MaximumVerticalAngle = 89f;

        [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
        [SerializeField] private GlobalMapDebugController debugController;
        [SerializeField] private float rotationSpeed = 0.35f;
        [SerializeField] private float verticalSpeed = 0.25f;
        [SerializeField] private float zoomSpeed = 4f;
        [SerializeField] private float minRadius = 4.2f;
        [SerializeField] private float maxRadius = 10.5f;

        public void Configure(CinemachineOrbitalFollow follow, GlobalMapDebugController debug)
        {
            orbitalFollow = follow;
            debugController = debug;
            ApplyOrbitLimits();
        }

        private void Update()
        {
            if (orbitalFollow == null)
            {
                return;
            }

            UpdateRotation();
            UpdateZoom();
        }

        private void UpdateRotation()
        {
            if (!Input.GetMouseButton(1) && !Input.GetMouseButton(2))
            {
                return;
            }

            if (debugController != null && debugController.IsPointerOverDebugButton(Input.mousePosition))
            {
                return;
            }

            var horizontal = orbitalFollow.HorizontalAxis;
            var vertical = orbitalFollow.VerticalAxis;
            horizontal.Value = horizontal.ClampValue(horizontal.Value + Input.GetAxisRaw("Mouse X") * rotationSpeed * 100f);
            vertical.Value = vertical.ClampValue(vertical.Value - Input.GetAxisRaw("Mouse Y") * verticalSpeed * 100f);
            orbitalFollow.HorizontalAxis = horizontal;
            orbitalFollow.VerticalAxis = vertical;
        }

        private void ApplyOrbitLimits()
        {
            if (orbitalFollow == null)
            {
                return;
            }

            var horizontal = orbitalFollow.HorizontalAxis;
            horizontal.Range = new Vector2(-180f, 180f);
            horizontal.Wrap = true;
            orbitalFollow.HorizontalAxis = horizontal;

            var vertical = orbitalFollow.VerticalAxis;
            vertical.Range = new Vector2(MinimumVerticalAngle, MaximumVerticalAngle);
            vertical.Wrap = false;
            vertical.Value = vertical.ClampValue(vertical.Value);
            orbitalFollow.VerticalAxis = vertical;
        }

        private void UpdateZoom()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) <= 0.0001f)
            {
                return;
            }

            orbitalFollow.Radius = Mathf.Clamp(orbitalFollow.Radius - scroll * zoomSpeed * UnityEngine.Time.unscaledDeltaTime * 10f, minRadius, maxRadius);
        }
    }
}
