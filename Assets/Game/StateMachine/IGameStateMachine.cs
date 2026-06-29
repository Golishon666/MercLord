using System.Threading;
using Cysharp.Threading.Tasks;

namespace MercLord.Game.StateMachine
{
    public interface IGameStateMachine
    {
        GameStateId? CurrentStateId { get; }

        void Register(IGameState state);
        UniTask ChangeStateAsync(GameStateId stateId, object payload = null, CancellationToken cancellationToken = default);
    }
}
