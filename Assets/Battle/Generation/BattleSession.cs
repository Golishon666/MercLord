using System;
using MercLord.Battle.ECS.Components;
using Scellecs.Morpeh;

namespace MercLord.Battle.Generation
{
    public sealed class BattleSession
    {
        public BattleSession(
            BattleGenerationRequest request,
            BattleModel model,
            World world)
        {
            Request = request;
            Model = model ?? throw new ArgumentNullException(nameof(model));
            World = world ?? throw new ArgumentNullException(nameof(world));
            Completion = new BattleCompletionState();
            ObjectiveStates = CreateObjectiveStates(Model);
        }

        public BattleGenerationRequest Request { get; }
        public BattleModel Model { get; }
        public World World { get; }
        public BattleCompletionState Completion { get; }
        public BattleObjectiveRuntimeState[] ObjectiveStates { get; }

        private static BattleObjectiveRuntimeState[] CreateObjectiveStates(BattleModel model)
        {
            var objectives = model.Objectives ?? Array.Empty<BattleObjectiveZone>();
            var states = new BattleObjectiveRuntimeState[objectives.Length];
            for (var objectiveIndex = 0; objectiveIndex < objectives.Length; objectiveIndex++)
            {
                states[objectiveIndex] = new BattleObjectiveRuntimeState
                {
                    Type = objectives[objectiveIndex].Type,
                    Priority = objectives[objectiveIndex].Priority
                };
            }

            return states;
        }
    }

    public sealed class BattleCompletionState
    {
        public bool IsCompleted => Result != null && Result.Outcome != BattleOutcome.None;
        public BattleResult Result { get; private set; }

        public void Complete(BattleResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.Outcome == BattleOutcome.None)
            {
                throw new InvalidOperationException("Completed battle result must have a non-empty outcome.");
            }

            if (IsCompleted)
            {
                return;
            }

            Result = result;
        }
    }

    public sealed class BattleObjectiveRuntimeState
    {
        public BattleObjectiveType Type;
        public int Priority;
        public bool HasOwner;
        public BattleTeamType OwnerTeam;
        public bool HasCaptureTeam;
        public BattleTeamType CaptureTeam;
        public float CaptureProgress;
        public bool IsContested;
        public int AttackerPresence;
        public int DefenderPresence;
    }
}
