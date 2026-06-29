using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    public interface IWorldGenerator
    {
        WorldModel Generate(WorldGenerationRequest request);
    }
}
