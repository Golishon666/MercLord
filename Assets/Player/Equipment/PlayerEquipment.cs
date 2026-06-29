using System;
using MercLord.Player.Inventory;

namespace MercLord.Player.Equipment
{
    [Serializable]
    public sealed class PlayerEquipment
    {
        public const int WeaponSlotCount = 4;

        public ItemInstance BodyArmor;
        public ItemInstance Helmet;
        public ItemInstance WeaponSlot1;
        public ItemInstance WeaponSlot2;
        public ItemInstance WeaponSlot3;
        public ItemInstance WeaponSlot4;
        public ItemInstance SpecialSlot1;
        public ItemInstance SpecialSlot2;
        public ItemInstance SpecialSlot3;
        public ItemInstance SpecialSlot4;

        public bool TryGetWeaponSlot(int slotIndex, out ItemInstance item)
        {
            switch (slotIndex)
            {
                case 0:
                    item = WeaponSlot1;
                    return true;
                case 1:
                    item = WeaponSlot2;
                    return true;
                case 2:
                    item = WeaponSlot3;
                    return true;
                case 3:
                    item = WeaponSlot4;
                    return true;
                default:
                    item = default;
                    return false;
            }
        }
    }
}
