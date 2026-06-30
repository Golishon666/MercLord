using System;
using System.Reflection;
using MercLord.Economy.Credits;
using MercLord.Economy.Trading;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Player.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class TradingServiceTests
    {
        [Test]
        public void SellTradeGoodAddsCreditsAndRemovesInventoryItems()
        {
            var tradeGood = CreateConfig<TradeGoodConfig>(101, "Scrap Metal");
            var item = CreateConfig<ItemConfig>(201, "Scrap Metal Item");
            var database = ScriptableObject.CreateInstance<ConfigDatabase>();

            try
            {
                SetField(tradeGood, "basePrice", 15);
                SetField(item, "category", ItemCategory.TradeGood);
                SetField(item, "tradeGood", tradeGood);
                SetField(database, "items", new[] { item });
                SetField(database, "tradeGoods", new[] { tradeGood });

                var inventoryService = new PlayerInventoryService();
                var saveModel = new SaveModel();
                saveModel.World.Player.Credits = 10;
                inventoryService.AddItem(
                    saveModel.Inventory,
                    item.Id,
                    3,
                    ItemInstance.DurabilityNotTracked);

                var tradingService = new TradingService(
                    database,
                    new CreditsService(),
                    inventoryService);

                var result = tradingService.SellTradeGood(saveModel, item.Id, 2);

                Assert.AreEqual(item.Id, result.ItemConfigId);
                Assert.AreEqual(2, result.SoldAmount);
                Assert.AreEqual(30, result.CreditsEarned);
                Assert.AreEqual(1, result.RemainingAmount);
                Assert.AreEqual(40, saveModel.World.Player.Credits);
                Assert.AreEqual(
                    1,
                    inventoryService.CountItem(saveModel.Inventory, item.Id, ItemInstance.DurabilityNotTracked));
            }
            finally
            {
                DestroyAssets(database, item, tradeGood);
            }
        }

        [Test]
        public void BuyItemSpendsCreditsAndAddsInventoryItem()
        {
            var item = CreateConfig<ItemConfig>(301, "Med Kit");
            var database = ScriptableObject.CreateInstance<ConfigDatabase>();

            try
            {
                SetField(item, "category", ItemCategory.Consumable);
                SetField(item, "price", 25);
                SetField(database, "items", new[] { item });

                var inventoryService = new PlayerInventoryService();
                var saveModel = new SaveModel();
                saveModel.World.Player.Credits = 80;

                var tradingService = new TradingService(
                    database,
                    new CreditsService(),
                    inventoryService);

                var result = tradingService.BuyItem(saveModel, item.Id, 2);

                Assert.AreEqual(item.Id, result.ItemConfigId);
                Assert.AreEqual(2, result.PurchasedAmount);
                Assert.AreEqual(50, result.CreditsSpent);
                Assert.AreEqual(30, result.RemainingCredits);
                Assert.AreEqual(30, saveModel.World.Player.Credits);
                Assert.AreEqual(
                    2,
                    inventoryService.CountItem(saveModel.Inventory, item.Id, ItemInstance.DurabilityNotTracked));
            }
            finally
            {
                DestroyAssets(database, item);
            }
        }

        private static T CreateConfig<T>(int id, string displayName)
            where T : ScriptableObject
        {
            var config = ScriptableObject.CreateInstance<T>();
            SetField(config, "id", id);
            SetField(config, "displayName", displayName);
            return config;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new InvalidOperationException($"Field '{fieldName}' was not found on {target.GetType().Name}.");
        }

        private static void DestroyAssets(params UnityEngine.Object[] assets)
        {
            for (var assetIndex = 0; assetIndex < assets.Length; assetIndex++)
            {
                if (assets[assetIndex] != null)
                {
                    UnityEngine.Object.DestroyImmediate(assets[assetIndex]);
                }
            }
        }
    }
}
