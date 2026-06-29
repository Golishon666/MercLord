using System;

namespace MercLord.Global.Cells
{
    public interface IInfluenceService
    {
        int GetDominantFactionSlot(Influence4 influence);
        Influence4 AddInfluence(Influence4 influence, int factionSlot, float amount);
        Influence4 CreateSingleFactionInfluence(int factionSlot, float amount);
    }

    public sealed class InfluenceService : IInfluenceService
    {
        public int GetDominantFactionSlot(Influence4 influence)
        {
            return influence.DominantFactionSlot;
        }

        public Influence4 AddInfluence(Influence4 influence, int factionSlot, float amount)
        {
            influence.Set(factionSlot, Math.Max(0f, influence.Get(factionSlot) + amount));
            return influence;
        }

        public Influence4 CreateSingleFactionInfluence(int factionSlot, float amount)
        {
            return AddInfluence(default, factionSlot, amount);
        }
    }
}
