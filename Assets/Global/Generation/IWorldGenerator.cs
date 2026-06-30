using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    public interface IWorldGenerator
    {
        WorldModel Generate(WorldGenerationRequest request);
        UniTask<WorldModel> GenerateAsync(WorldGenerationRequest request, CancellationToken cancellationToken = default);
    }
}
