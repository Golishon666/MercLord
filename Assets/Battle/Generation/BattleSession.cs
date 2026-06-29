using System;
using Scellecs.Morpeh;

namespace MercLord.Battle.Generation
{
    public sealed class BattleSession
    {
        public BattleSession(
            BattleGenerationRequest request,
            BattleModel model,
            World world)
        {
            Request = request;
            Model = model ?? throw new ArgumentNullException(nameof(model));
            World = world ?? throw new ArgumentNullException(nameof(world));
        }

        public BattleGenerationRequest Request { get; }
        public BattleModel Model { get; }
        public World World { get; }
    }
}
