using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using Scellecs.Morpeh;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class ViewSyncSystem : IBattleRuntimeSystem
    {
        private readonly BattleViewCatalog viewCatalog;
        private readonly IBattleViewFactory viewFactory;

        private World world;
        private Filter filter;
        private Stash<PositionComponent> positions;
        private Stash<ViewRefComponent> viewRefs;
        private Stash<BattleLodComponent> lods;
        private Stash<DeadComponent> dead;
        private Stash<DriverComponent> drivers;

        public ViewSyncSystem(
            BattleViewCatalog viewCatalog,
            IBattleViewFactory viewFactory)
        {
            this.viewCatalog = viewCatalog ?? throw new ArgumentNullException(nameof(viewCatalog));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("ViewSyncSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("ViewSyncSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PositionComponent>()
                .With<ViewRefComponent>()
                .Build();

            positions = world.GetStash<PositionComponent>();
            viewRefs = world.GetStash<ViewRefComponent>();
            lods = world.GetStash<BattleLodComponent>();
            dead = world.GetStash<DeadComponent>();
            drivers = world.GetStash<DriverComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            foreach (var entity in filter)
            {
                var viewRef = viewRefs.Get(entity);
                if (!viewFactory.TryGetView(viewRef.ViewId, out var view))
                {
                    continue;
                }

                var position = positions.Get(entity);
                var isDeadVisual = ShouldUseDeadVisual(entity);
                view.transform.position = viewCatalog.GridToWorld(position.Value);
                view.SetActive(ShouldViewBeActive(entity, isDeadVisual));
                ApplyVisualState(view, isDeadVisual);
            }
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            filter = null;
            world = null;
            positions = null;
            viewRefs = null;
            lods = null;
            dead = null;
            drivers = null;
        }

        private static void ApplyVisualState(UnityEngine.GameObject view, bool isDeadVisual)
        {
            var infantryView = view.GetComponent<InfantryView>();
            if (infantryView != null)
            {
                infantryView.SetDeadVisual(isDeadVisual);
            }
        }

        private bool ShouldUseDeadVisual(Entity entity)
        {
            if (dead.Has(entity))
            {
                return true;
            }

            return lods.Has(entity) && lods.Get(entity).Level == BattleLodLevel.Dead;
        }

        private bool ShouldViewBeActive(Entity entity, bool isDeadVisual)
        {
            if (drivers.Has(entity))
            {
                return false;
            }

            if (isDeadVisual)
            {
                return true;
            }

            if (!lods.Has(entity))
            {
                return true;
            }

            var lod = lods.Get(entity);
            return lod.Level != BattleLodLevel.Strategic;
        }
    }
}
