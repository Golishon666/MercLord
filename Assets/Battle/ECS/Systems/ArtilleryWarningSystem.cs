using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class ArtilleryWarningSystem : IBattleRuntimeSystem
    {
        private readonly List<Entity> warningBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<ArtilleryWarningComponent> warnings;

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("ArtilleryWarningSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("ArtilleryWarningSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<ArtilleryWarningComponent>()
                .Build();
            warnings = world.GetStash<ArtilleryWarningComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            warningBuffer.Clear();
            foreach (var entity in filter)
            {
                warningBuffer.Add(entity);
            }

            for (var warningIndex = 0; warningIndex < warningBuffer.Count; warningIndex++)
            {
                var entity = warningBuffer[warningIndex];
                ref var warning = ref warnings.Get(entity);
                warning.RemainingTime -= deltaTime;
                if (warning.RemainingTime <= 0f)
                {
                    world.RemoveEntity(entity);
                }
            }

            warningBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            warningBuffer.Clear();
            filter = null;
            world = null;
            warnings = null;
        }
    }
}
