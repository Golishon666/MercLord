using System;
using MercLord.Game.Configs;
using Unity.Cinemachine;
using UnityEngine;

namespace MercLord.Global.Rendering
{
    [ExecuteAlways]
    public sealed class GlobalMapSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private ConfigDatabase configDatabase;
        [SerializeField] private int targetCellCount = GlobalGenerationConfig.DefaultTargetCellCount;
        [SerializeField] private bool generateOnStart;
        [SerializeField] private bool showEditorPreview;
        [SerializeField] private int editorPreviewSeed = GlobalGenerationConfig.DefaultSeed;

        [Header("Scene References")]
        [SerializeField] private ProceduralGlobalMapRenderer mapRenderer;
        [SerializeField] private GlobalMapDebugController debugController;
        [SerializeField] private GlobalMapCellInfoController cellInfoController;
        [SerializeField] private GlobalMapCameraController cameraController;
        [SerializeField] private GlobalMapCellTooltipView tooltipView;
        [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
        [SerializeField] private Camera inputCamera;

        public ProceduralGlobalMapRenderer MapRenderer => mapRenderer;
        public GlobalMapDebugController DebugController => debugController;
        public GlobalMapCellInfoController CellInfoController => cellInfoController;
        public GlobalMapCameraController CameraController => cameraController;
        public GlobalMapCellTooltipView TooltipView => tooltipView;
        public CinemachineOrbitalFollow OrbitalFollow => orbitalFollow;
        public Camera InputCamera => inputCamera;

        private bool editorPreviewGenerated;

        private void Awake()
        {
            SetupScene();
        }

        private void OnEnable()
        {
            SetupScene();

            if (!Application.isPlaying && showEditorPreview && !editorPreviewGenerated)
            {
                GenerateEditorPreview();
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            TryAutoWireSceneReferences();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                TryAutoWireSceneReferences();
            }
        }
#endif

        private void SetupScene()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                TryAutoWireSceneReferences();
            }
#endif

            gameObject.name = "GlobalMap";
            var configuredCellCount = configDatabase?.GlobalGeneration != null
                ? configDatabase.GlobalGeneration.TargetCellCount
                : targetCellCount;
            targetCellCount = Mathf.Max(GlobalGenerationConfig.MinimumTargetCellCount, configuredCellCount);

            ValidateRequiredReferences();
            mapRenderer.Configure(configDatabase);
            debugController.Configure(mapRenderer, configDatabase, targetCellCount, generateOnStart);
            cellInfoController.Configure(mapRenderer, debugController, configDatabase, tooltipView, inputCamera);
            cameraController.Configure(orbitalFollow, debugController, mapRenderer);
        }

        private void GenerateEditorPreview()
        {
            if (mapRenderer == null || debugController == null)
            {
                return;
            }

            debugController.Configure(mapRenderer, configDatabase, targetCellCount, generateOnStart);
            debugController.Generate(editorPreviewSeed);
            editorPreviewGenerated = true;
        }

        private void ValidateRequiredReferences()
        {
            if (mapRenderer == null)
            {
                throw new InvalidOperationException("GlobalMapSceneBootstrap requires a ProceduralGlobalMapRenderer reference.");
            }

            if (debugController == null)
            {
                throw new InvalidOperationException("GlobalMapSceneBootstrap requires a GlobalMapDebugController reference.");
            }

            if (cellInfoController == null)
            {
                throw new InvalidOperationException("GlobalMapSceneBootstrap requires a GlobalMapCellInfoController reference.");
            }

            if (cameraController == null)
            {
                throw new InvalidOperationException("GlobalMapSceneBootstrap requires a GlobalMapCameraController reference.");
            }

            if (tooltipView == null)
            {
                throw new InvalidOperationException("GlobalMapSceneBootstrap requires a GlobalMapCellTooltipView reference.");
            }

            if (orbitalFollow == null)
            {
                throw new InvalidOperationException("GlobalMapSceneBootstrap requires a CinemachineOrbitalFollow reference.");
            }

            if (inputCamera == null)
            {
                throw new InvalidOperationException("GlobalMapSceneBootstrap requires an input Camera reference.");
            }
        }

#if UNITY_EDITOR
        private void TryAutoWireSceneReferences()
        {
            var changed = false;
            changed |= AssignIfMissing(ref mapRenderer, GetComponent<ProceduralGlobalMapRenderer>());
            changed |= AssignIfMissing(ref debugController, GetComponent<GlobalMapDebugController>());
            changed |= AssignIfMissing(ref cellInfoController, GetComponent<GlobalMapCellInfoController>());
            changed |= AssignIfMissing(ref cameraController, GetComponent<GlobalMapCameraController>());
            changed |= AssignIfMissing(ref tooltipView, GetComponentInChildren<GlobalMapCellTooltipView>(true));

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private static bool AssignIfMissing<T>(ref T target, T value)
            where T : UnityEngine.Object
        {
            if (target != null || value == null)
            {
                return false;
            }

            target = value;
            return true;
        }
#endif
    }
}
