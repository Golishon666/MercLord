using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MercLord.Game.StateMachine
{
    public sealed class GameStateMachine : IGameStateMachine
    {
        private readonly Dictionary<GameStateId, IGameState> states = new Dictionary<GameStateId, IGameState>();
        private IGameState currentState;

        public GameStateId? CurrentStateId { get; private set; }

        public void Register(IGameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (states.ContainsKey(state.Id))
            {
                throw new InvalidOperationException($"Game state already registered: {state.Id}");
            }

            states.Add(state.Id, state);
        }

        public async UniTask ChangeStateAsync(
            GameStateId stateId,
            object payload = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!states.TryGetValue(stateId, out var nextState))
            {
                throw new KeyNotFoundException($"Game state is not registered: {stateId}");
            }

            if (CurrentStateId == stateId)
            {
                return;
            }

            var fromState = CurrentStateId;

            if (currentState != null)
            {
                await currentState.ExitAsync(cancellationToken);
            }

            currentState = nextState;
            CurrentStateId = stateId;

            await currentState.EnterAsync(new GameStateContext(stateId, fromState, payload), cancellationToken);
        }
    }
}
