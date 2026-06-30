using System;
using Unity.Mathematics;

namespace MercLord.Battle.Generation
{
    public static class BattleObjectiveResolver
    {
        public static bool TryResolvePrimaryControlPoint(BattleModel model, out BattleObjectiveZone objective)
        {
            objective = default;
            var objectives = model?.Objectives ?? Array.Empty<BattleObjectiveZone>();
            var hasObjective = false;
            var bestPriority = int.MinValue;
            for (var objectiveIndex = 0; objectiveIndex < objectives.Length; objectiveIndex++)
            {
                var candidate = objectives[objectiveIndex];
                if (candidate.Type != BattleObjectiveType.ControlPoint ||
                    (hasObjective && candidate.Priority <= bestPriority))
                {
                    continue;
                }

                hasObjective = true;
                bestPriority = candidate.Priority;
                objective = candidate;
            }

            return hasObjective;
        }

        public static bool TryResolvePrimaryControlPointTarget(BattleModel model, out float2 target)
        {
            if (TryResolvePrimaryControlPoint(model, out var objective))
            {
                target = objective.Center;
                return true;
            }

            target = default;
            return false;
        }
    }
}
