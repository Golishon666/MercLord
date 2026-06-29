namespace MercLord.Game.StateMachine.States
{
    public sealed class BootstrapState : GameStateBase
    {
        public override GameStateId Id => GameStateId.Bootstrap;
    }

    public sealed class MainMenuState : GameStateBase
    {
        public override GameStateId Id => GameStateId.MainMenu;
    }

    public sealed class GenerateWorldState : GameStateBase
    {
        public override GameStateId Id => GameStateId.GenerateWorld;
    }

    public sealed class LoadGlobalState : GameStateBase
    {
        public override GameStateId Id => GameStateId.LoadGlobal;
    }

    public sealed class GlobalMapState : GameStateBase
    {
        public override GameStateId Id => GameStateId.GlobalMap;
    }

    public sealed class EnterBattleState : GameStateBase
    {
        public override GameStateId Id => GameStateId.EnterBattle;
    }

    public sealed class BattleState : GameStateBase
    {
        public override GameStateId Id => GameStateId.Battle;
    }

    public sealed class ExitBattleState : GameStateBase
    {
        public override GameStateId Id => GameStateId.ExitBattle;
    }

    public sealed class SaveLoadState : GameStateBase
    {
        public override GameStateId Id => GameStateId.SaveLoad;
    }
}
