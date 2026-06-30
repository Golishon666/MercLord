using MercLord.Game.Configs;
using Unity.Cinemachine;
using UnityEngine;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalSceneRoot : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private ConfigDatabase configDatabase;
        [SerializeField] private bool generateOnStart;

        [Header("View References")]
        [SerializeField] private PlanetRenderer planetRenderer;
        [SerializeField] private ProceduralGlobalMapRenderer proceduralMapRenderer;
        [SerializeField] private GlobalMapDebugController debugController;
        [SerializeField] private GlobalMapCellTooltipView tooltipView;
        [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
        [SerializeField] private Camera inputCamera;

        [Header("Camera")]
        [SerializeField] private float rotationSpeed = 0.35f;
        [SerializeField] private float verticalSpeed = 0.25f;
        [SerializeField] private float zoomSpeed = 4f;
        [SerializeField, Min(0.01f)] private float minZoomSurfaceDistance = 0.1f;
        [SerializeField, Min(0.01f)] private float maxZoomSurfaceDistance = 7.5f;
        [SerializeField, Range(0f, 1f)] private float markerIconVisibilityZoomThreshold = 0.5f;

        public ConfigDatabase ConfigDatabase => configDatabase;
        public bool GenerateOnStart => generateOnStart;
        public PlanetRenderer PlanetRenderer => planetRenderer;
        public ProceduralGlobalMapRenderer ProceduralMapRenderer => proceduralMapRenderer;
        public GlobalMapDebugController DebugController => debugController;
        public GlobalMapCellTooltipView TooltipView => tooltipView;
        public CinemachineOrbitalFollow OrbitalFollow => orbitalFollow;
        public Camera InputCamera => inputCamera;
        public float RotationSpeed => rotationSpeed;
        public float VerticalSpeed => verticalSpeed;
        public float ZoomSpeed => zoomSpeed;
        public float MinZoomSurfaceDistance => Mathf.Max(0.01f, minZoomSurfaceDistance);
        public float MaxZoomSurfaceDistance => Mathf.Max(MinZoomSurfaceDistance, maxZoomSurfaceDistance);
        public float MarkerIconVisibilityZoomThreshold => markerIconVisibilityZoomThreshold;
    }
}
