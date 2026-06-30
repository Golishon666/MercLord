using MercLord.Battle.Generation;
using NUnit.Framework;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleObjectiveResolverTests
    {
        [Test]
        public void TryResolvePrimaryControlPointTargetUsesHighestPriorityControlPoint()
        {
            var model = new BattleModel
            {
                Objectives = new[]
                {
                    new BattleObjectiveZone
                    {
                        Type = BattleObjectiveType.ControlPoint,
                        Area = new RectInt(1, 1, 2, 2),
                        Priority = 1
                    },
                    new BattleObjectiveZone
                    {
                        Type = BattleObjectiveType.ControlPoint,
                        Area = new RectInt(6, 2, 4, 2),
                        Priority = 5
                    }
                }
            };

            var found = BattleObjectiveResolver.TryResolvePrimaryControlPointTarget(model, out var target);

            Assert.IsTrue(found);
            Assert.AreEqual(8f, target.x, 0.001f);
            Assert.AreEqual(3f, target.y, 0.001f);
        }

        [Test]
        public void TryResolvePrimaryControlPointTargetReturnsFalseWithoutControlPoint()
        {
            var model = new BattleModel
            {
                Objectives = new[]
                {
                    new BattleObjectiveZone
                    {
                        Type = BattleObjectiveType.EliminateEnemies,
                        Area = new RectInt(1, 1, 2, 2),
                        Priority = 10
                    }
                }
            };

            Assert.IsFalse(BattleObjectiveResolver.TryResolvePrimaryControlPointTarget(model, out _));
        }
    }
}
