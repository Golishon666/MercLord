using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class BattleLodSystem : IBattleRuntimeSystem
    {
        private const float TickInterval = 0.25f;
        private const float FullLodDistance = 18f;
        private const float SimplifiedLodDistance = 48f;

        private readonly List<Entity> entityBuffer = new List<Entity>();

        private World world;
        private BattleModel model;
        private Filter entityFilter;
        private Filter focusFilter;
        private Stash<PositionComponent> positions;
        private Stash<PlayerControlledComponent> playerControlled;
        private Stash<DeadComponent> dead;
        private Stash<BattleLodComponent> lods;
        private BattleCadenceTimer tickTimer;

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("BattleLodSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleLodSystem cannot initialize on a disposed Morpeh world.");
            }

            model = session.Model ?? throw new InvalidOperationException("BattleLodSystem requires a BattleModel.");
            entityFilter = world.Filter
                .With<PositionComponent>()
                .Build();
            focusFilter = world.Filter
                .With<PositionComponent>()
                .With<PlayerControlledComponent>()
                .Without<DeadComponent>()
                .Build();

            positions = world.GetStash<PositionComponent>();
            playerControlled = world.GetStash<PlayerControlledComponent>();
            dead = world.GetStash<DeadComponent>();
            lods = world.GetStash<BattleLodComponent>();
            tickTimer = new BattleCadenceTimer(TickInterval);
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || entityFilter == null)
            {
                return;
            }

            if (!tickTimer.Consume(deltaTime))
            {
                return;
            }

            var focus = ResolveFocusPosition();
            entityBuffer.Clear();
            foreach (var entity in entityFilter)
            {
                entityBuffer.Add(entity);
            }

            for (var entityIndex = 0; entityIndex < entityBuffer.Count; entityIndex++)
            {
                RefreshLod(entityBuffer[entityIndex], focus);
            }

            entityBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed)
            {
                entityFilter?.Dispose();
                focusFilter?.Dispose();
            }

            entityBuffer.Clear();
            world = null;
            model = null;
            entityFilter = null;
            focusFilter = null;
            positions = null;
            playerControlled = null;
            dead = null;
            lods = null;
            tickTimer = default;
        }

        private void RefreshLod(Entity entity, float2 focus)
        {
            var position = positions.Get(entity).Value;
            var distance = math.distance(position, focus);
            var level = ResolveLevel(entity, distance);
            lods.Set(entity, new BattleLodComponent
            {
                Level = level,
                DistanceToFocus = distance
            });
        }

        private BattleLodLevel ResolveLevel(Entity entity, float distance)
        {
            if (dead.Has(entity))
            {
                return BattleLodLevel.Dead;
            }

            if (playerControlled.Has(entity) || distance <= FullLodDistance)
            {
                return BattleLodLevel.Full;
            }

            return distance <= SimplifiedLodDistance
                ? BattleLodLevel.Simplified
                : BattleLodLevel.Strategic;
        }

        private float2 ResolveFocusPosition()
        {
            if (focusFilter != null)
            {
                foreach (var entity in focusFilter)
                {
                    return positions.Get(entity).Value;
                }
            }

            return new float2(model.Width * 0.5f, model.Height * 0.5f);
        }
    }
}
