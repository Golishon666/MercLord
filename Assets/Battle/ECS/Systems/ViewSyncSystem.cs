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
                .Without<DeadComponent>()
                .Build();

            positions = world.GetStash<PositionComponent>();
            viewRefs = world.GetStash<ViewRefComponent>();
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
                view.transform.position = viewCatalog.GridToWorld(position.Value);
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
        }
    }
}
