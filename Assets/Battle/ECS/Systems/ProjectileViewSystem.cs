using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using Scellecs.Morpeh;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class ProjectileViewSystem : IBattleRuntimeSystem
    {
        private readonly BattleSceneRoot sceneRoot;
        private readonly IBattleViewFactory viewFactory;
        private readonly Dictionary<Entity, int> activeProjectileViews = new Dictionary<Entity, int>();
        private readonly HashSet<Entity> visibleThisFrame = new HashSet<Entity>();
        private readonly List<Entity> staleBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<ViewRefComponent> viewRefs;

        public ProjectileViewSystem(
            BattleSceneRoot sceneRoot,
            IBattleViewFactory viewFactory)
        {
            this.sceneRoot = sceneRoot != null ? sceneRoot : throw new ArgumentNullException(nameof(sceneRoot));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (sceneRoot.UnitViewRoot == null)
            {
                throw new InvalidOperationException("ProjectileViewSystem requires BattleSceneRoot.UnitViewRoot.");
            }

            world = session.World ?? throw new InvalidOperationException("ProjectileViewSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("ProjectileViewSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<ProjectileComponent>()
                .With<PositionComponent>()
                .Build();
            viewRefs = world.GetStash<ViewRefComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            visibleThisFrame.Clear();
            foreach (var entity in filter)
            {
                visibleThisFrame.Add(entity);
                EnsureProjectileView(entity);
            }

            ReleaseStaleViews();
            visibleThisFrame.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            foreach (var viewId in activeProjectileViews.Values)
            {
                viewFactory.ReleaseView(viewId);
            }

            activeProjectileViews.Clear();
            visibleThisFrame.Clear();
            staleBuffer.Clear();
            world = null;
            filter = null;
            viewRefs = null;
        }

        private void EnsureProjectileView(Entity entity)
        {
            if (activeProjectileViews.ContainsKey(entity))
            {
                return;
            }

            if (viewRefs.Has(entity))
            {
                activeProjectileViews[entity] = viewRefs.Get(entity).ViewId;
                return;
            }

            var viewId = viewFactory.SpawnProjectileView(sceneRoot.UnitViewRoot);
            viewRefs.Set(entity, new ViewRefComponent { ViewId = viewId });
            activeProjectileViews.Add(entity, viewId);
        }

        private void ReleaseStaleViews()
        {
            staleBuffer.Clear();
            foreach (var entry in activeProjectileViews)
            {
                if (!visibleThisFrame.Contains(entry.Key) || !world.Has(entry.Key))
                {
                    staleBuffer.Add(entry.Key);
                }
            }

            for (var staleIndex = 0; staleIndex < staleBuffer.Count; staleIndex++)
            {
                var entity = staleBuffer[staleIndex];
                var viewId = activeProjectileViews[entity];
                activeProjectileViews.Remove(entity);
                if (world.Has(entity) && viewRefs.Has(entity))
                {
                    viewRefs.Remove(entity);
                }

                viewFactory.ReleaseView(viewId);
            }

            staleBuffer.Clear();
        }
    }
}
