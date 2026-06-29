using MercLord.Global.Cells;

namespace MercLord.Game.StateMachine
{
    public readonly struct NewGameRequest
    {
        public NewGameRequest(int? seed = null, int cultureId = WorldIds.None)
        {
            Seed = seed;
            CultureId = cultureId;
        }

        public int? Seed { get; }
        public int CultureId { get; }
    }

    public readonly struct LoadGlobalRequest
    {
        public LoadGlobalRequest(bool loadScene)
        {
            LoadScene = loadScene;
        }

        public bool LoadScene { get; }
    }
}
