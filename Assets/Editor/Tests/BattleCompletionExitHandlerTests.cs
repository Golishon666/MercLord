using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MercLord.Battle.Generation;
using MercLord.Battle.Rendering;
using MercLord.Game.StateMachine;
using NUnit.Framework;
using Scellecs.Morpeh;

namespace MercLord.Editor.Tests
{
    public sealed class BattleCompletionExitHandlerTests
    {
        [Test]
        public async Task TryRequestExitAsyncChangesStateWithCompletedResult()
        {
            var stateMachine = new FakeGameStateMachine();
            var handler = new BattleCompletionExitHandler(stateMachine);
            var world = World.Create();

            try
            {
                var session = CreateSession(world, sourceCellId: 21);
                var result = new BattleResult
                {
                    Outcome = BattleOutcome.AttackerVictory,
                    SourceCellId = 21,
                    WinnerFactionId = 4
                };
                session.Completion.Complete(result);

                var requested = await handler.TryRequestExitAsync(session);

                Assert.IsTrue(requested);
                Assert.IsTrue(handler.ExitRequested);
                Assert.AreEqual(1, stateMachine.ChangeCount);
                Assert.AreEqual(GameStateId.ExitBattle, stateMachine.LastStateId);
                Assert.IsInstanceOf<ExitBattleRequest>(stateMachine.LastPayload);

                var payload = (ExitBattleRequest)stateMachine.LastPayload;
                Assert.AreSame(result, payload.Result);
                Assert.IsTrue(payload.LoadGlobalScene);
            }
            finally
            {
                DisposeWorld(world);
            }
        }

        [Test]
        public async Task TryRequestExitAsyncIgnoresIncompleteSession()
        {
            var stateMachine = new FakeGameStateMachine();
            var handler = new BattleCompletionExitHandler(stateMachine);
            var world = World.Create();

            try
            {
                var requested = await handler.TryRequestExitAsync(CreateSession(world, sourceCellId: 22));

                Assert.IsFalse(requested);
                Assert.IsFalse(handler.ExitRequested);
                Assert.AreEqual(0, stateMachine.ChangeCount);
            }
            finally
            {
                DisposeWorld(world);
            }
        }

        [Test]
        public async Task TryRequestExitAsyncDoesNotRequestExitTwice()
        {
            var stateMachine = new FakeGameStateMachine();
            var handler = new BattleCompletionExitHandler(stateMachine);
            var world = World.Create();

            try
            {
                var session = CreateSession(world, sourceCellId: 23);
                session.Completion.Complete(new BattleResult
                {
                    Outcome = BattleOutcome.DefenderVictory,
                    SourceCellId = 23,
                    WinnerFactionId = 7
                });

                var firstRequested = await handler.TryRequestExitAsync(session);
                var secondRequested = await handler.TryRequestExitAsync(session);

                Assert.IsTrue(firstRequested);
                Assert.IsFalse(secondRequested);
                Assert.AreEqual(1, stateMachine.ChangeCount);
            }
            finally
            {
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world, int sourceCellId)
        {
            return new BattleSession(
                new BattleGenerationRequest
                {
                    SourceCellId = sourceCellId
                },
                new BattleModel
                {
                    Width = 1,
                    Height = 1
                },
                world);
        }

        private static void DisposeWorld(World world)
        {
            if (world != null && !world.IsDisposed)
            {
                world.Dispose();
            }
        }

        private sealed class FakeGameStateMachine : IGameStateMachine
        {
            public GameStateId? CurrentStateId { get; private set; }
            public int ChangeCount { get; private set; }
            public GameStateId LastStateId { get; private set; }
            public object LastPayload { get; private set; }

            public void Register(IGameState state)
            {
            }

            public UniTask ChangeStateAsync(
                GameStateId stateId,
                object payload = null,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ChangeCount++;
                LastStateId = stateId;
                LastPayload = payload;
                CurrentStateId = stateId;
                return UniTask.CompletedTask;
            }
        }
    }
}
