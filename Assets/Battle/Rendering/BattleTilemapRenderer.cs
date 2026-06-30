using System;
using MercLord.Battle.Generation;

namespace MercLord.Battle.Rendering
{
    public interface IBattleTilemapRenderer
    {
        void Render(BattleSession session);
        void Clear();
    }

    public sealed class BattleTilemapRenderer : IBattleTilemapRenderer
    {
        private readonly BattleTilemapView view;

        public BattleTilemapRenderer(BattleTilemapView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Render(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            view.Render(session.Model);
        }

        public void Clear()
        {
            view.Clear();
        }
    }
}
