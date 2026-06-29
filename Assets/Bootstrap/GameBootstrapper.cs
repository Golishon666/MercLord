using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Game.StateMachine;
using MercLord.Game.StateMachine.States;
using UnityEngine;
using VContainer.Unity;

namespace MercLord.Bootstrap
{
    public sealed class GameBootstrapper : IStartable, IDisposable
    {
        private readonly GameStateMachine stateMachine;
        private readonly BootstrapState bootstrapState;
        private readonly MainMenuState mainMenuState;
        private readonly GenerateWorldState generateWorldState;
        private readonly LoadGlobalState loadGlobalState;
        private readonly GlobalMapState globalMapState;
        private readonly EnterBattleState enterBattleState;
        private readonly BattleState battleState;
        private readonly ExitBattleState exitBattleState;
        private readonly SaveLoadState saveLoadState;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        public GameBootstrapper(
            GameStateMachine stateMachine,
            BootstrapState bootstrapState,
            MainMenuState mainMenuState,
            GenerateWorldState generateWorldState,
            LoadGlobalState loadGlobalState,
            GlobalMapState globalMapState,
            EnterBattleState enterBattleState,
            BattleState battleState,
            ExitBattleState exitBattleState,
            SaveLoadState saveLoadState)
        {
            this.stateMachine = stateMachine;
            this.bootstrapState = bootstrapState;
            this.mainMenuState = mainMenuState;
            this.generateWorldState = generateWorldState;
            this.loadGlobalState = loadGlobalState;
            this.globalMapState = globalMapState;
            this.enterBattleState = enterBattleState;
            this.battleState = battleState;
            this.exitBattleState = exitBattleState;
            this.saveLoadState = saveLoadState;
        }

        public void Start()
        {
            RegisterStates();
            EnterBootstrapAsync().Forget();
        }

        public void Dispose()
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }

        private void RegisterStates()
        {
            stateMachine.Register(bootstrapState);
            stateMachine.Register(mainMenuState);
            stateMachine.Register(generateWorldState);
            stateMachine.Register(loadGlobalState);
            stateMachine.Register(globalMapState);
            stateMachine.Register(enterBattleState);
            stateMachine.Register(battleState);
            stateMachine.Register(exitBattleState);
            stateMachine.Register(saveLoadState);
        }

        private async UniTask EnterBootstrapAsync()
        {
            try
            {
                await stateMachine.ChangeStateAsync(GameStateId.Bootstrap, cancellationToken: cancellation.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
