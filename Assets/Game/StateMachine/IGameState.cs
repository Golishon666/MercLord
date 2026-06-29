using System.Threading;
using Cysharp.Threading.Tasks;

namespace MercLord.Game.StateMachine
{
    public interface IGameState
    {
        GameStateId Id { get; }

        UniTask EnterAsync(GameStateContext context, CancellationToken cancellationToken);
        UniTask ExitAsync(CancellationToken cancellationToken);
    }
}
