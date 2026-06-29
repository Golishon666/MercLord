using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Systems
{
    public sealed class MovementSystem : IBattleRuntimeSystem
    {
        private World world;
        private Filter filter;
        private Stash<PositionComponent> positions;
        private Stash<VelocityComponent> velocities;
        private Stash<MovementStatsComponent> movementStats;
        private Stash<BotStateComponent> botStates;

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("MovementSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("MovementSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<PositionComponent>()
                .With<VelocityComponent>()
                .With<MovementStatsComponent>()
                .Without<DeadComponent>()
                .Build();

            positions = world.GetStash<PositionComponent>();
            velocities = world.GetStash<VelocityComponent>();
            movementStats = world.GetStash<MovementStatsComponent>();
            botStates = world.GetStash<BotStateComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            foreach (var entity in filter)
            {
                ref var position = ref positions.Get(entity);
                ref var velocity = ref velocities.Get(entity);
                ref var stats = ref movementStats.Get(entity);

                var direction = velocity.Value;
                var moving = math.lengthsq(direction) > 0f;
                if (moving)
                {
                    if (math.lengthsq(direction) > 1f)
                    {
                        direction = math.normalizesafe(direction);
                    }

                    position.Value += direction * stats.MoveSpeed * deltaTime;
                }

                ref var botState = ref botStates.Get(entity, out var hasBotState);
                if (hasBotState &&
                    (botState.Value == BotStateType.Idle || botState.Value == BotStateType.Moving))
                {
                    botState.Value = moving ? BotStateType.Moving : BotStateType.Idle;
                }
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
            velocities = null;
            movementStats = null;
            botStates = null;
        }
    }
}
