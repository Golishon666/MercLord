using System;
using System.Collections.Generic;

namespace MercLord.Player.Inventory
{
    [Serializable]
    public sealed class PlayerInventory
    {
        public List<ItemInstance> Items = new List<ItemInstance>();
    }

    [Serializable]
    public struct ItemInstance
    {
        public const int DurabilityNotTracked = 0;

        public int ConfigId;
        public int Amount;
        public int Durability;
    }

    public interface IInventoryService
    {
        void AddItem(PlayerInventory inventory, int itemConfigId, int amount, int durability);
    }

    public sealed class PlayerInventoryService : IInventoryService
    {
        public void AddItem(PlayerInventory inventory, int itemConfigId, int amount, int durability)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Inventory item amount must be positive.");
            }

            inventory.Items ??= new List<ItemInstance>();

            for (var itemIndex = 0; itemIndex < inventory.Items.Count; itemIndex++)
            {
                var item = inventory.Items[itemIndex];
                if (item.ConfigId != itemConfigId || item.Durability != durability)
                {
                    continue;
                }

                item.Amount = checked(item.Amount + amount);
                inventory.Items[itemIndex] = item;
                return;
            }

            inventory.Items.Add(new ItemInstance
            {
                ConfigId = itemConfigId,
                Amount = amount,
                Durability = durability
            });
        }
    }
}
