using System.Threading;
using Cysharp.Threading.Tasks;

namespace MercLord.Game.StateMachine.States
{
    public abstract class GameStateBase : IGameState
    {
        public abstract GameStateId Id { get; }

        public virtual UniTask EnterAsync(GameStateContext context, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask ExitAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
