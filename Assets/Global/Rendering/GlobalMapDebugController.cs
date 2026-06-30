using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Game.Configs;
using MercLord.Global.Cells;
using MercLord.Global.Generation;
using UnityEngine;

namespace MercLord.Global.Rendering
{
    [ExecuteAlways]
    public sealed class GlobalMapDebugController : MonoBehaviour
    {
        [SerializeField] private ProceduralGlobalMapRenderer mapRenderer;
        [SerializeField] private ConfigDatabase configDatabase;
        [SerializeField] private int targetCellCount = GlobalGenerationConfig.DefaultTargetCellCount;
        [SerializeField] private int seed = GlobalGenerationConfig.DefaultSeed;
        [SerializeField] private bool generateOnStart;

        private Rect buttonRect;
        private CancellationTokenSource generationCancellation;
        private bool isGenerating;
        private int generationVersion;

        public WorldModel CurrentWorld { get; private set; }

        public void Configure(
            ProceduralGlobalMapRenderer renderer,
            ConfigDatabase database,
            int cellCount,
            bool shouldGenerateOnStart)
        {
            mapRenderer = renderer;
            configDatabase = database;
            targetCellCount = Mathf.Max(GlobalGenerationConfig.MinimumTargetCellCount, cellCount);
            generateOnStart = shouldGenerateOnStart;
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (generateOnStart && CurrentWorld == null)
            {
                Generate(seed);
            }
        }

        private void OnDestroy()
        {
            CancelGeneration();
        }

        [ContextMenu("Generate Current Seed")]
        public void GenerateCurrentSeed()
        {
            Generate(seed);
        }

        [ContextMenu("Generate New Seed")]
        public void GenerateNewSeed()
        {
            seed = unchecked(seed * 1103515245 + 12345);
            if (seed == int.MinValue)
            {
                seed = GlobalGenerationConfig.DefaultSeed;
            }

            Generate(seed);
        }

        public void Generate(int generationSeed)
        {
            if (!Application.isPlaying)
            {
                GenerateImmediate(generationSeed);
                return;
            }

            GenerateAsync(generationSeed).Forget();
        }

        public async UniTask GenerateAsync(int generationSeed, CancellationToken cancellationToken = default)
        {
            CancelGeneration();
            generationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var localCancellation = generationCancellation;
            var localVersion = ++generationVersion;
            isGenerating = true;

            try
            {
                if (!EnsureRenderer())
                {
                    return;
                }

                mapRenderer.Configure(configDatabase);
                var generator = new SphericalWorldGenerator(configDatabase, new InfluenceService());
                var world = await generator.GenerateAsync(
                    new WorldGenerationRequest(
                        generationSeed,
                        Mathf.Max(GlobalGenerationConfig.MinimumTargetCellCount, targetCellCount)),
                    localCancellation.Token);

                if (localCancellation.IsCancellationRequested || localVersion != generationVersion)
                {
                    return;
                }

                CurrentWorld = world;
                mapRenderer.Render(CurrentWorld);
                PersistEditorGeneratedMap();
                Debug.Log($"Generated global map. Seed={generationSeed}, Cells={CurrentWorld.Cells.Length}, Roads={CurrentWorld.RoadEdges.Length}, Rivers={CurrentWorld.RiverEdges.Length}");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (localVersion == generationVersion)
                {
                    isGenerating = false;
                    generationCancellation?.Dispose();
                    generationCancellation = null;
                }
            }
        }

        private void GenerateImmediate(int generationSeed)
        {
            if (!EnsureRenderer())
            {
                return;
            }

            mapRenderer.Configure(configDatabase);
            var generator = new SphericalWorldGenerator(configDatabase, new InfluenceService());
            CurrentWorld = generator.Generate(new WorldGenerationRequest(
                generationSeed,
                Mathf.Max(GlobalGenerationConfig.MinimumTargetCellCount, targetCellCount)));
            mapRenderer.Render(CurrentWorld);
            PersistEditorGeneratedMap();
            Debug.Log($"Generated global map. Seed={generationSeed}, Cells={CurrentWorld.Cells.Length}, Roads={CurrentWorld.RoadEdges.Length}, Rivers={CurrentWorld.RiverEdges.Length}");
        }

        public void ClearGenerated()
        {
            if (!EnsureRenderer())
            {
                return;
            }

            CurrentWorld = null;
            mapRenderer.ClearGenerated();
            PersistEditorGeneratedMap();
        }

        private bool EnsureRenderer()
        {
            if (mapRenderer == null)
            {
                mapRenderer = GetComponent<ProceduralGlobalMapRenderer>();
            }

            if (mapRenderer != null)
            {
                return true;
            }

            Debug.LogError("GlobalMapDebugController requires a ProceduralGlobalMapRenderer.");
            return false;
        }

        private void CancelGeneration()
        {
            if (generationCancellation == null)
            {
                return;
            }

            generationCancellation.Cancel();
            generationCancellation.Dispose();
            generationCancellation = null;
        }

        private void OnGUI()
        {
            const float width = 150f;
            const float height = 40f;
            buttonRect = new Rect(Screen.width - width - 16f, 16f, width, height);

            var wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && !isGenerating;
            if (GUI.Button(buttonRect, isGenerating ? "Generating..." : "Generate"))
            {
                GenerateNewSeed();
            }

            GUI.enabled = wasEnabled;
        }

        public bool IsPointerOverDebugButton(Vector2 screenPosition)
        {
            return buttonRect.Contains(new Vector2(screenPosition.x, Screen.height - screenPosition.y));
        }

        private void PersistEditorGeneratedMap()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }

            UnityEditor.EditorUtility.SetDirty(this);
            if (mapRenderer != null)
            {
                UnityEditor.EditorUtility.SetDirty(mapRenderer);
            }

            var scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrEmpty(scene.path))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
#endif
        }
    }
}
