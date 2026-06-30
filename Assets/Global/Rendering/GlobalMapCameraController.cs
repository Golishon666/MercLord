using Unity.Cinemachine;
using UnityEngine;
using VContainer.Unity;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalMapCameraController : IStartable, ITickable
    {
        private const float MinimumVerticalAngle = -89f;
        private const float MaximumVerticalAngle = 89f;

        private readonly GlobalSceneRoot sceneRoot;
        private readonly CinemachineOrbitalFollow orbitalFollow;
        private readonly GlobalMapDebugController debugController;
        private readonly ProceduralGlobalMapRenderer mapRenderer;

        public CinemachineOrbitalFollow OrbitalFollow => orbitalFollow;
        public GlobalMapDebugController DebugController => debugController;
        public ProceduralGlobalMapRenderer MapRenderer => mapRenderer;

        public GlobalMapCameraController(GlobalSceneRoot sceneRoot)
        {
            this.sceneRoot = sceneRoot;
            orbitalFollow = sceneRoot.OrbitalFollow;
            debugController = sceneRoot.DebugController;
            mapRenderer = sceneRoot.ProceduralMapRenderer;
        }

        public void Start()
        {
            ApplyOrbitLimits();
            ClampZoom();
            UpdateMarkerIconVisibility();
        }

        public void Tick()
        {
            if (orbitalFollow == null)
            {
                return;
            }

            UpdateRotation();
            UpdateZoom();
            ClampZoom();
            UpdateMarkerIconVisibility();
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
            horizontal.Value = horizontal.ClampValue(horizontal.Value + Input.GetAxisRaw("Mouse X") * sceneRoot.RotationSpeed * 100f);
            vertical.Value = vertical.ClampValue(vertical.Value - Input.GetAxisRaw("Mouse Y") * sceneRoot.VerticalSpeed * 100f);
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

            orbitalFollow.Radius = Mathf.Clamp(
                orbitalFollow.Radius - scroll * sceneRoot.ZoomSpeed * UnityEngine.Time.unscaledDeltaTime * 10f,
                GetMinimumZoomRadius(),
                GetMaximumZoomRadius());
        }

        private void ClampZoom()
        {
            if (orbitalFollow == null)
            {
                return;
            }

            orbitalFollow.Radius = Mathf.Clamp(orbitalFollow.Radius, GetMinimumZoomRadius(), GetMaximumZoomRadius());
        }

        private void UpdateMarkerIconVisibility()
        {
            if (orbitalFollow == null || mapRenderer == null)
            {
                return;
            }

            var zoomRatio = Mathf.InverseLerp(GetMinimumZoomRadius(), GetMaximumZoomRadius(), orbitalFollow.Radius);
            mapRenderer.SetMarkerIconsVisible(zoomRatio >= sceneRoot.MarkerIconVisibilityZoomThreshold);
        }

        private float GetMinimumZoomRadius()
        {
            return GetMapSurfaceRadius() + sceneRoot.MinZoomSurfaceDistance;
        }

        private float GetMaximumZoomRadius()
        {
            return GetMapSurfaceRadius() + sceneRoot.MaxZoomSurfaceDistance;
        }

        private float GetMapSurfaceRadius()
        {
            return mapRenderer != null ? mapRenderer.PlanetRadius : 0f;
        }
    }
}
