using System;
using MercLord.Player.Inventory;

namespace MercLord.Player.Equipment
{
    [Serializable]
    public sealed class PlayerEquipment
    {
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
    }
}
