using System;

namespace MercLord.Player.Inventory
{
    [Serializable]
    public sealed class PlayerInventory
    {
        public ItemInstance[] Items = Array.Empty<ItemInstance>();
    }

    [Serializable]
    public struct ItemInstance
    {
        public string InstanceId;
        public int ItemConfigId;
        public int Count;
    }
}
