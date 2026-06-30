using System;
using System.Collections.Generic;
using System.Reflection;
using MercLord.Battle.Combat;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Game.Configs;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;
using EcsDamageSystem = MercLord.Battle.ECS.Systems.DamageSystem;

namespace MercLord.Editor.Tests
{
    public sealed class DamageSystemTests
    {
        [Test]
        public void DamageSystemIgnoresDriverTargetInsideVehicle()
        {
            using var configSet = new TestConfigSet();
            var world = World.Create();
            var system = new EcsDamageSystem(configSet.Database, new MercLord.Battle.Combat.DamageSystem());

            try
            {
                var driver = CreateDriver(world, new float2(1f, 1f));
                CreateDamageRequest(world, driver, amount: 20);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);
                world.Commit();

                Assert.AreEqual(10, world.GetStash<HealthComponent>().Get(driver).Current);
                Assert.IsFalse(world.GetStash<DeadComponent>().Has(driver));
                Assert.IsTrue(world.GetStash<DriverComponent>().Has(driver));
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        [Test]
        public void LethalDamageToVehicleKillsDriverAtVehiclePosition()
        {
            using var configSet = new TestConfigSet();
            var world = World.Create();
            var system = new EcsDamageSystem(configSet.Database, new MercLord.Battle.Combat.DamageSystem());

            try
            {
                var driver = CreateDriver(world, new float2(1f, 1f));
                var vehicle = CreateVehicle(world, driver, new float2(4f, 3f));
                CreateDamageRequest(world, vehicle, amount: 20);
                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);
                world.Commit();

                var dead = world.GetStash<DeadComponent>();
                var drivers = world.GetStash<DriverComponent>();
                var vehicleComponent = world.GetStash<VehicleComponent>().Get(vehicle);
                var driverPosition = world.GetStash<PositionComponent>().Get(driver).Value;

                Assert.IsTrue(dead.Has(vehicle));
                Assert.IsTrue(dead.Has(driver));
                Assert.IsFalse(drivers.Has(driver));
                Assert.AreEqual(VehicleStateType.Destroyed, vehicleComponent.State);
                Assert.IsFalse(world.Has(vehicleComponent.Driver));
                Assert.AreEqual(4f, driverPosition.x, 0.001f);
                Assert.AreEqual(3f, driverPosition.y, 0.001f);
            }
            finally
            {
                system.Dispose();
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = 8,
                    Height = 8
                },
                world);
        }

        private static Entity CreateDriver(World world, float2 position)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = 10,
                Max = 10
            });
            world.GetStash<BotStateComponent>().Set(entity, new BotStateComponent
            {
                Value = BotStateType.Idle
            });
            world.GetStash<DriverComponent>().Set(entity, new DriverComponent());
            return entity;
        }

        private static Entity CreateVehicle(World world, Entity driver, float2 position)
        {
            var entity = world.CreateEntity();
            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = position
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = 10,
                Max = 10
            });
            world.GetStash<VehicleComponent>().Set(entity, new VehicleComponent
            {
                VehicleConfigId = 1,
                Driver = driver,
                State = VehicleStateType.PlayerControlled
            });
            world.GetStash<DriverComponent>().Get(driver).ControlledVehicle = entity;
            return entity;
        }

        private static void CreateDamageRequest(World world, Entity target, int amount)
        {
            var entity = world.CreateEntity();
            world.GetStash<DamageRequestComponent>().Set(entity, new DamageRequestComponent
            {
                Target = target,
                HitPosition = float2.zero,
                DamageType = DamageType.Ballistic,
                Amount = amount
            });
        }

        private static void DisposeWorld(World world)
        {
            if (world != null && !world.IsDisposed)
            {
                world.Dispose();
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new InvalidOperationException($"Field '{fieldName}' was not found on {target.GetType().Name}.");
        }

        private sealed class TestConfigSet : IDisposable
        {
            private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

            public TestConfigSet()
            {
                Database = Create<ConfigDatabase>();
                var combatBalance = Create<CombatBalanceConfig>();
                SetField(combatBalance, "damageFormula", new DamageFormula
                {
                    MinimumDamage = 1
                });
                SetField(Database, "combatBalance", combatBalance);
            }

            public ConfigDatabase Database { get; }

            public void Dispose()
            {
                for (var assetIndex = assets.Count - 1; assetIndex >= 0; assetIndex--)
                {
                    if (assets[assetIndex] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(assets[assetIndex]);
                    }
                }
            }

            private T Create<T>()
                where T : ScriptableObject
            {
                var asset = ScriptableObject.CreateInstance<T>();
                assets.Add(asset);
                return asset;
            }
        }
    }
}
