using System;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using VContainer;
using VContainer.Unity;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalSceneInitializer : IStartable
    {
        private readonly GlobalSceneRoot sceneRoot;
        private readonly ConfigDatabase configDatabase;
        private readonly ISaveService saveService;
        private readonly IGlobalMapPresenter globalMapPresenter;

        public GlobalSceneInitializer(
            GlobalSceneRoot sceneRoot,
            ConfigDatabase configDatabase,
            IObjectResolver resolver)
        {
            this.sceneRoot = sceneRoot ?? throw new ArgumentNullException(nameof(sceneRoot));
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            saveService = resolver.TryResolve<ISaveService>(out var resolvedSaveService) ? resolvedSaveService : null;
            globalMapPresenter = resolver.TryResolve<IGlobalMapPresenter>(out var resolvedPresenter) ? resolvedPresenter : null;
        }

        public void Start()
        {
            ValidateRequiredReferences();
            ConfigureDebugController();

            var world = saveService?.Current?.World;
            if (world == null)
            {
                return;
            }

            if (sceneRoot.ProceduralMapRenderer != null)
            {
                sceneRoot.ProceduralMapRenderer.Configure(configDatabase);
                sceneRoot.ProceduralMapRenderer.Render(world);
                return;
            }

            if (globalMapPresenter == null)
            {
                throw new InvalidOperationException("GlobalSceneInitializer requires IGlobalMapPresenter injection when no ProceduralGlobalMapRenderer is assigned.");
            }

            globalMapPresenter.Render(sceneRoot.PlanetRenderer, world);
        }

        private void ConfigureDebugController()
        {
            var debugController = sceneRoot.DebugController;
            if (debugController == null)
            {
                return;
            }

            debugController.Configure(
                sceneRoot.ProceduralMapRenderer,
                configDatabase,
                configDatabase.GlobalGeneration?.TargetCellCount ?? GlobalGenerationConfig.DefaultTargetCellCount,
                sceneRoot.GenerateOnStart);
        }

        private void ValidateRequiredReferences()
        {
            if (sceneRoot.ProceduralMapRenderer == null && sceneRoot.PlanetRenderer == null)
            {
                throw new InvalidOperationException("GlobalSceneRoot requires a ProceduralGlobalMapRenderer or PlanetRenderer reference.");
            }
        }
    }
}
