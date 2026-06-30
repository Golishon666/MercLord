using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Battle.Generation;
using MercLord.Game.StateMachine;

namespace MercLord.Battle.Rendering
{
    public interface IBattleCompletionExitHandler
    {
        bool ExitRequested { get; }
        UniTask<bool> TryRequestExitAsync(BattleSession session, CancellationToken cancellationToken = default);
    }

    public sealed class BattleCompletionExitHandler : IBattleCompletionExitHandler
    {
        private readonly IGameStateMachine stateMachine;

        public BattleCompletionExitHandler(IGameStateMachine stateMachine)
        {
            this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        public bool ExitRequested { get; private set; }

        public async UniTask<bool> TryRequestExitAsync(
            BattleSession session,
            CancellationToken cancellationToken = default)
        {
            if (ExitRequested ||
                session == null ||
                !session.Completion.IsCompleted)
            {
                return false;
            }

            ExitRequested = true;
            await stateMachine.ChangeStateAsync(
                GameStateId.ExitBattle,
                new ExitBattleRequest(session.Completion.Result),
                cancellationToken);
            return true;
        }
    }
}
