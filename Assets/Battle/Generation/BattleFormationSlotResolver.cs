using System;
using Unity.Mathematics;

namespace MercLord.Battle.Generation
{
    public static class BattleFormationSlotResolver
    {
        public static float2 ResolveLineSlot(
            int slotIndex,
            int squadSize,
            float2 forwardDirection,
            float slotSpacing = 1f,
            int maxColumns = 20)
        {
            if (slotIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            if (squadSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(squadSize));
            }

            if (slotIndex >= squadSize)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            if (slotSpacing <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(slotSpacing));
            }

            if (maxColumns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxColumns));
            }

            var forward = math.normalizesafe(forwardDirection, new float2(1f, 0f));
            var lateral = new float2(-forward.y, forward.x);
            var columns = math.min(squadSize, maxColumns);
            var row = slotIndex / columns;
            var column = slotIndex % columns;
            var centeredColumn = column - (columns - 1) * 0.5f;

            return lateral * centeredColumn * slotSpacing - forward * row * slotSpacing;
        }
    }
}
