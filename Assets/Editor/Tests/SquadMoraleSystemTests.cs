using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Editor.Tests
{
    public sealed class SquadMoraleSystemTests
    {
        [Test]
        public void MoraleTracksAliveMemberRatio()
        {
            var world = World.Create();
            var system = new SquadMoraleSystem();

            try
            {
                var squad = CreateSquad(
                    world,
                    squadId: 7,
                    team: BattleTeamType.Attacker,
                    memberCount: 4,
                    order: SquadOrderType.AttackNearest,
                    anchor: new float2(6f, 4f));
                CreateMember(world, squadId: 7, currentHealth: 10);
                CreateMember(world, squadId: 7, currentHealth: 10);
                CreateMember(world, squadId: 7, currentHealth: 10);
                CreateMember(world, squadId: 7, currentHealth: 0);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                var morale = world.GetStash<SquadMoraleComponent>().Get(squad);
                var order = world.GetStash<SquadOrderComponent>().Get(squad);
                Assert.AreEqual(75f, morale.Current, 0.001f);
                Assert.IsFalse(morale.IsRouted);
                Assert.AreEqual(SquadOrderType.AttackNearest, order.Value);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void RoutedSquadSwitchesToRetreatTowardOwnMapEdge()
        {
            var world = World.Create();
            var system = new SquadMoraleSystem();

            try
            {
                var squad = CreateSquad(
                    world,
                    squadId: 9,
                    team: BattleTeamType.Defender,
                    memberCount: 4,
                    order: SquadOrderType.AttackNearest,
                    anchor: new float2(6f, 3f));
                CreateMember(world, squadId: 9, currentHealth: 10);
                CreateMember(world, squadId: 9, currentHealth: 0);
                CreateMember(world, squadId: 9, currentHealth: 0);
                CreateMember(world, squadId: 9, currentHealth: 0);
                world.Commit();

                system.Initialize(CreateSession(world, width: 12, height: 8));
                system.Tick(0f);

                var morale = world.GetStash<SquadMoraleComponent>().Get(squad);
                var order = world.GetStash<SquadOrderComponent>().Get(squad);
                Assert.AreEqual(25f, morale.Current, 0.001f);
                Assert.IsTrue(morale.IsRouted);
                Assert.AreEqual(SquadOrderType.Retreat, order.Value);
                Assert.AreEqual(11.5f, order.TargetPosition.x, 0.001f);
                Assert.AreEqual(3f, order.TargetPosition.y, 0.001f);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void DestroyedSquadDropsMoraleWithoutRewritingOrder()
        {
            var world = World.Create();
            var system = new SquadMoraleSystem();

            try
            {
                var squad = CreateSquad(
                    world,
                    squadId: 3,
                    team: BattleTeamType.Attacker,
                    memberCount: 2,
                    order: SquadOrderType.HoldPosition,
                    anchor: new float2(5f, 5f));
                CreateMember(world, squadId: 3, currentHealth: 0, dead: true);
                CreateMember(world, squadId: 3, currentHealth: 0, dead: true);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);

                var morale = world.GetStash<SquadMoraleComponent>().Get(squad);
                var order = world.GetStash<SquadOrderComponent>().Get(squad);
                Assert.AreEqual(0f, morale.Current, 0.001f);
                Assert.IsFalse(morale.IsRouted);
                Assert.AreEqual(SquadOrderType.HoldPosition, order.Value);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void MoraleRunsAtOneSecondCadence()
        {
            var world = World.Create();
            var system = new SquadMoraleSystem();

            try
            {
                var squad = CreateSquad(
                    world,
                    squadId: 11,
                    team: BattleTeamType.Attacker,
                    memberCount: 2,
                    order: SquadOrderType.AttackNearest,
                    anchor: new float2(5f, 5f));
                CreateMember(world, squadId: 11, currentHealth: 10);
                var wounded = CreateMember(world, squadId: 11, currentHealth: 10);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);
                Assert.AreEqual(100f, world.GetStash<SquadMoraleComponent>().Get(squad).Current, 0.001f);

                ref var woundedHealth = ref world.GetStash<HealthComponent>().Get(wounded);
                woundedHealth.Current = 0;
                system.Tick(0.5f);
                Assert.AreEqual(100f, world.GetStash<SquadMoraleComponent>().Get(squad).Current, 0.001f);

                system.Tick(0.5f);
                Assert.AreEqual(50f, world.GetStash<SquadMoraleComponent>().Get(squad).Current, 0.001f);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world, int width = 10, int height = 10)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = width,
                    Height = height
                },
                world);
        }

        private static Entity CreateSquad(
            World world,
            int squadId,
            BattleTeamType team,
            int memberCount,
            SquadOrderType order,
            float2 anchor)
        {
            var entity = world.CreateEntity();
            world.GetStash<SquadComponent>().Set(entity, new SquadComponent
            {
                SquadId = squadId,
                UnitConfigId = 501,
                FactionId = team == BattleTeamType.Attacker ? 1 : 2,
                Team = team,
                MemberCount = memberCount
            });
            world.GetStash<SquadAnchorComponent>().Set(entity, new SquadAnchorComponent
            {
                Position = anchor,
                ForwardDirection = new float2(1f, 0f)
            });
            world.GetStash<SquadOrderComponent>().Set(entity, new SquadOrderComponent
            {
                Value = order,
                TargetPosition = anchor
            });
            world.GetStash<SquadMoraleComponent>().Set(entity, new SquadMoraleComponent
            {
                Current = 100f,
                Max = 100f,
                RoutThreshold = 35f,
                IsRouted = false
            });
            return entity;
        }

        private static Entity CreateMember(World world, int squadId, int currentHealth, bool dead = false)
        {
            var entity = world.CreateEntity();
            world.GetStash<SquadMemberComponent>().Set(entity, new SquadMemberComponent
            {
                SquadId = squadId,
                SlotIndex = 0,
                SquadSize = 1
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = currentHealth,
                Max = 10
            });

            if (dead)
            {
                world.GetStash<DeadComponent>().Set(entity, new DeadComponent());
            }

            return entity;
        }

        private static void DisposeWorld(World world)
        {
            if (world != null && !world.IsDisposed)
            {
                world.Dispose();
            }
        }
    }
}
