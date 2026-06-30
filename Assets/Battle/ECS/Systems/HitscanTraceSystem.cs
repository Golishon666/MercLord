using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class HitscanTraceSystem : IBattleRuntimeSystem
    {
        private readonly List<Entity> traceBuffer = new List<Entity>();

        private World world;
        private Filter filter;
        private Stash<HitscanTraceComponent> traces;

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("HitscanTraceSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("HitscanTraceSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<HitscanTraceComponent>()
                .Build();
            traces = world.GetStash<HitscanTraceComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            traceBuffer.Clear();
            foreach (var entity in filter)
            {
                traceBuffer.Add(entity);
            }

            for (var traceIndex = 0; traceIndex < traceBuffer.Count; traceIndex++)
            {
                var entity = traceBuffer[traceIndex];
                ref var trace = ref traces.Get(entity);
                trace.RemainingTime -= Math.Max(0f, deltaTime);
                if (trace.RemainingTime <= 0f)
                {
                    world.RemoveEntity(entity);
                }
            }

            traceBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            traceBuffer.Clear();
            filter = null;
            world = null;
            traces = null;
        }
    }
}
