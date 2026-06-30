using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.UI;
using MercLord.Game.Configs;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Editor.Tests
{
    public sealed class BattlePlayerHudPresenterTests
    {
        [Test]
        public void BuildSnapshotReportsControlledVehicleCombatState()
        {
            var world = World.Create();
            var presenter = new BattlePlayerHudPresenter();

            try
            {
                var target = CreateTarget(world);
                CreatePlayerControlledVehicle(world, target);
                world.Commit();

                presenter.Bind(CreateSession(world));
                var snapshot = presenter.BuildSnapshot();

                Assert.IsTrue(snapshot.HasPlayer);
                Assert.IsTrue(snapshot.IsVehicle);
                Assert.AreEqual(501, snapshot.ControlledConfigId);
                Assert.AreEqual(72, snapshot.CurrentHealth);
                Assert.AreEqual(120, snapshot.MaxHealth);
                Assert.IsTrue(snapshot.HasWeapon);
                Assert.AreEqual(42, snapshot.WeaponConfigId);
                Assert.AreEqual(WeaponType.TankCannon, snapshot.WeaponType);
                Assert.AreEqual(1, snapshot.SelectedWeaponSlot);
                Assert.AreEqual(0.75f, snapshot.CooldownRemaining, 0.001f);
                Assert.AreEqual(2f, snapshot.CooldownDuration, 0.001f);
                Assert.AreEqual(8, snapshot.BallisticArmor);
                Assert.AreEqual(3, snapshot.EnergyArmor);
                Assert.AreEqual(5, snapshot.ExplosionArmor);
                Assert.IsTrue(snapshot.FirePressed);
                Assert.IsFalse(snapshot.InteractPressed);
                Assert.IsTrue(snapshot.HasTarget);
                Assert.IsFalse(snapshot.TargetIsVehicle);
                Assert.AreEqual(2001, snapshot.TargetConfigId);
                Assert.AreEqual(12, snapshot.TargetCurrentHealth);
                Assert.AreEqual(40, snapshot.TargetMaxHealth);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void BuildSnapshotReportsMissingPlayer()
        {
            var world = World.Create();
            var presenter = new BattlePlayerHudPresenter();

            try
            {
                presenter.Bind(CreateSession(world));
                var snapshot = presenter.BuildSnapshot();

                Assert.IsFalse(snapshot.HasPlayer);
            }
            finally
            {
                presenter.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = 1,
                    Height = 1
                },
                world);
        }

        private static void CreatePlayerControlledVehicle(World world, Entity target)
        {
            var entity = world.CreateEntity();
            world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
            world.GetStash<VehicleComponent>().Set(entity, new VehicleComponent
            {
                VehicleConfigId = 501,
                State = VehicleStateType.PlayerControlled
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = 72,
                Max = 120
            });
            world.GetStash<WeaponStatsComponent>().Set(entity, new WeaponStatsComponent
            {
                WeaponConfigId = 42,
                Type = WeaponType.TankCannon,
                Cooldown = 2f
            });
            world.GetStash<AttackCooldownComponent>().Set(entity, new AttackCooldownComponent
            {
                Value = 0.75f
            });
            world.GetStash<ArmorStatsComponent>().Set(entity, new ArmorStatsComponent
            {
                BallisticProtection = 8,
                EnergyProtection = 3,
                ExplosionProtection = 5
            });
            world.GetStash<PlayerInputComponent>().Set(entity, new PlayerInputComponent
            {
                MoveDirection = float2.zero,
                AimDirection = new float2(1f, 0f),
                FirePressed = true,
                InteractPressed = false,
                SelectedWeaponSlot = 1
            });
            world.GetStash<TargetComponent>().Set(entity, new TargetComponent
            {
                Target = target
            });
        }

        private static Entity CreateTarget(World world)
        {
            var entity = world.CreateEntity();
            world.GetStash<BotComponent>().Set(entity, new BotComponent
            {
                UnitConfigId = 2001
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = 12,
                Max = 40
            });
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
