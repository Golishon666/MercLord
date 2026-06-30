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
        private Rect highQualityButtonRect;
        private Rect renderHighQualityButtonRect;
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
            Generate(seed, false);
        }

        [ContextMenu("Generate Current Seed High Quality")]
        public void GenerateCurrentSeedHighQuality()
        {
            Generate(seed, true);
        }

        [ContextMenu("Generate New Seed")]
        public void GenerateNewSeed()
        {
            GenerateNewSeed(false);
        }

        [ContextMenu("Generate New Seed High Quality")]
        public void GenerateNewSeedHighQuality()
        {
            GenerateNewSeed(true);
        }

        [ContextMenu("Render Current Map High Quality")]
        public void RenderCurrentMapHighQuality()
        {
            RenderCurrentMap(true);
        }

        public void GenerateNewSeed(bool finalQuality)
        {
            seed = unchecked(seed * 1103515245 + 12345);
            if (seed == int.MinValue)
            {
                seed = GlobalGenerationConfig.DefaultSeed;
            }

            Generate(seed, finalQuality);
        }

        public void Generate(int generationSeed)
        {
            Generate(generationSeed, false);
        }

        public void Generate(int generationSeed, bool finalQuality)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GenerateImmediate(generationSeed, finalQuality);
                return;
            }
#endif
            GenerateAsync(generationSeed, finalQuality).Forget();
        }

        public async UniTask GenerateAsync(
            int generationSeed,
            bool finalQuality = false,
            CancellationToken cancellationToken = default)
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
                await mapRenderer.RenderAsync(CurrentWorld, finalQuality, localCancellation.Token);
                PersistEditorGeneratedMap();
                Debug.Log($"Generated global map. Seed={generationSeed}, Cells={CurrentWorld.Cells.Length}, Roads={CurrentWorld.RoadEdges.Length}, Rivers={CurrentWorld.RiverEdges.Length}, TerrainQuality={(finalQuality ? "HQ" : "Preview")}");
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

        public void RenderCurrentMap(bool finalQuality)
        {
            if (CurrentWorld == null)
            {
                Generate(seed, finalQuality);
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (EnsureRenderer())
                {
                    mapRenderer.Configure(configDatabase);
                    mapRenderer.Render(CurrentWorld, finalQuality);
                    PersistEditorGeneratedMap();
                }

                return;
            }
#endif
            RenderCurrentMapAsync(finalQuality).Forget();
        }

        private async UniTask RenderCurrentMapAsync(bool finalQuality)
        {
            CancelGeneration();
            generationCancellation = new CancellationTokenSource();
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
                await mapRenderer.RenderAsync(CurrentWorld, finalQuality, localCancellation.Token);
                PersistEditorGeneratedMap();
                Debug.Log($"Rendered global map. Seed={CurrentWorld.Seed}, Cells={CurrentWorld.Cells.Length}, TerrainQuality={(finalQuality ? "HQ" : "Preview")}");
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

        private void GenerateImmediate(int generationSeed, bool finalQuality)
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
            mapRenderer.Render(CurrentWorld, finalQuality);
            PersistEditorGeneratedMap();
            Debug.Log($"Generated global map. Seed={generationSeed}, Cells={CurrentWorld.Cells.Length}, Roads={CurrentWorld.RoadEdges.Length}, Rivers={CurrentWorld.RiverEdges.Length}, TerrainQuality={(finalQuality ? "HQ" : "Preview")}");
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
            const float spacing = 8f;
            buttonRect = new Rect(Screen.width - width - 16f, 16f, width, height);
            highQualityButtonRect = new Rect(buttonRect.x, buttonRect.yMax + spacing, width, height);
            renderHighQualityButtonRect = new Rect(buttonRect.x, highQualityButtonRect.yMax + spacing, width, height);

            var wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && !isGenerating;
            if (GUI.Button(buttonRect, isGenerating ? "Generating..." : "Generate"))
            {
                GenerateNewSeed(false);
            }

            if (GUI.Button(highQualityButtonRect, isGenerating ? "Generating..." : "Generate HQ"))
            {
                GenerateNewSeed(true);
            }

            if (GUI.Button(renderHighQualityButtonRect, isGenerating ? "Rendering..." : "Render HQ"))
            {
                RenderCurrentMap(true);
            }

            GUI.enabled = wasEnabled;
        }

        public bool IsPointerOverDebugButton(Vector2 screenPosition)
        {
            var guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return buttonRect.Contains(guiPosition) ||
                   highQualityButtonRect.Contains(guiPosition) ||
                   renderHighQualityButtonRect.Contains(guiPosition);
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
