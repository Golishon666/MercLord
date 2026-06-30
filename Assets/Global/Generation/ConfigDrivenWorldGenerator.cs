using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Game.Configs;
using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    public sealed class ConfigDrivenWorldGenerator : IWorldGenerator
    {
        private readonly SphericalWorldGenerator generator;

        public ConfigDrivenWorldGenerator(ConfigDatabase configDatabase, IInfluenceService influenceService)
        {
            if (configDatabase == null)
            {
                throw new ArgumentNullException(nameof(configDatabase));
            }

            generator = new SphericalWorldGenerator(configDatabase, influenceService);
        }

        public WorldModel Generate(WorldGenerationRequest request)
        {
            return generator.Generate(request);
        }

        public UniTask<WorldModel> GenerateAsync(
            WorldGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            return generator.GenerateAsync(request, cancellationToken);
        }
    }
}
