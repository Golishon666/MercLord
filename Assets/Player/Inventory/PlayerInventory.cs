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
        int CountItem(PlayerInventory inventory, int itemConfigId, int durability);
        bool TryRemoveItem(PlayerInventory inventory, int itemConfigId, int amount, int durability);
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

        public int CountItem(PlayerInventory inventory, int itemConfigId, int durability)
        {
            if (inventory == null || inventory.Items == null)
            {
                return 0;
            }

            var count = 0;
            for (var itemIndex = 0; itemIndex < inventory.Items.Count; itemIndex++)
            {
                var item = inventory.Items[itemIndex];
                if (item.ConfigId == itemConfigId && item.Durability == durability)
                {
                    count = checked(count + item.Amount);
                }
            }

            return count;
        }

        public bool TryRemoveItem(PlayerInventory inventory, int itemConfigId, int amount, int durability)
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
            if (CountItem(inventory, itemConfigId, durability) < amount)
            {
                return false;
            }

            var remainingAmount = amount;
            for (var itemIndex = inventory.Items.Count - 1; itemIndex >= 0 && remainingAmount > 0; itemIndex--)
            {
                var item = inventory.Items[itemIndex];
                if (item.ConfigId != itemConfigId || item.Durability != durability)
                {
                    continue;
                }

                var removedAmount = Math.Min(item.Amount, remainingAmount);
                item.Amount -= removedAmount;
                remainingAmount -= removedAmount;

                if (item.Amount <= 0)
                {
                    inventory.Items.RemoveAt(itemIndex);
                }
                else
                {
                    inventory.Items[itemIndex] = item;
                }
            }

            return true;
        }
    }
}
