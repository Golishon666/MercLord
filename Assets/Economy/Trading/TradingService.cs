using System;
using MercLord.Economy.Credits;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Player.Inventory;

namespace MercLord.Economy.Trading
{
    public readonly struct TradeSaleResult
    {
        public TradeSaleResult(int itemConfigId, int soldAmount, int creditsEarned, int remainingAmount)
        {
            ItemConfigId = itemConfigId;
            SoldAmount = soldAmount;
            CreditsEarned = creditsEarned;
            RemainingAmount = remainingAmount;
        }

        public int ItemConfigId { get; }
        public int SoldAmount { get; }
        public int CreditsEarned { get; }
        public int RemainingAmount { get; }
    }

    public readonly struct TradePurchaseResult
    {
        public TradePurchaseResult(int itemConfigId, int purchasedAmount, int creditsSpent, int remainingCredits)
        {
            ItemConfigId = itemConfigId;
            PurchasedAmount = purchasedAmount;
            CreditsSpent = creditsSpent;
            RemainingCredits = remainingCredits;
        }

        public int ItemConfigId { get; }
        public int PurchasedAmount { get; }
        public int CreditsSpent { get; }
        public int RemainingCredits { get; }
    }

    public interface ITradingService
    {
        TradeSaleResult SellTradeGood(SaveModel saveModel, int itemConfigId, int amount);
        bool TrySellTradeGood(SaveModel saveModel, int itemConfigId, int amount, out TradeSaleResult result);
        TradePurchaseResult BuyItem(SaveModel saveModel, int itemConfigId, int amount);
        bool TryBuyItem(SaveModel saveModel, int itemConfigId, int amount, out TradePurchaseResult result);
    }

    public sealed class TradingService : ITradingService
    {
        private readonly ConfigDatabase configDatabase;
        private readonly CreditsService creditsService;
        private readonly IInventoryService inventoryService;

        public TradingService(
            ConfigDatabase configDatabase,
            CreditsService creditsService,
            IInventoryService inventoryService)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
            this.creditsService = creditsService ?? throw new ArgumentNullException(nameof(creditsService));
            this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        }

        public TradeSaleResult SellTradeGood(SaveModel saveModel, int itemConfigId, int amount)
        {
            if (!TrySellTradeGood(saveModel, itemConfigId, amount, out var result))
            {
                throw new InvalidOperationException("Trade good sale failed. Check item category, amount, and inventory contents.");
            }

            return result;
        }

        public bool TrySellTradeGood(SaveModel saveModel, int itemConfigId, int amount, out TradeSaleResult result)
        {
            result = default;

            if (saveModel == null)
            {
                throw new ArgumentNullException(nameof(saveModel));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Trade sale amount must be positive.");
            }

            EnsureSaveGraph(saveModel);
            if (!TryGetTradeGoodItem(itemConfigId, out var itemConfig, out var tradeGoodConfig))
            {
                return false;
            }

            var durability = ItemInstance.DurabilityNotTracked;
            if (!inventoryService.TryRemoveItem(saveModel.Inventory, itemConfig.Id, amount, durability))
            {
                return false;
            }

            var creditsEarned = checked(tradeGoodConfig.BasePrice * amount);
            saveModel.World.Player.Credits = creditsService.AddCredits(
                saveModel.World.Player.Credits,
                creditsEarned);

            result = new TradeSaleResult(
                itemConfig.Id,
                amount,
                creditsEarned,
                inventoryService.CountItem(saveModel.Inventory, itemConfig.Id, durability));
            return true;
        }

        public TradePurchaseResult BuyItem(SaveModel saveModel, int itemConfigId, int amount)
        {
            if (!TryBuyItem(saveModel, itemConfigId, amount, out var result))
            {
                throw new InvalidOperationException("Item purchase failed. Check item price, amount, and player credits.");
            }

            return result;
        }

        public bool TryBuyItem(SaveModel saveModel, int itemConfigId, int amount, out TradePurchaseResult result)
        {
            result = default;

            if (saveModel == null)
            {
                throw new ArgumentNullException(nameof(saveModel));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Trade purchase amount must be positive.");
            }

            EnsureSaveGraph(saveModel);
            if (!configDatabase.TryGetItem(itemConfigId, out var itemConfig) || itemConfig.Price <= 0)
            {
                return false;
            }

            var creditsSpent = checked(itemConfig.Price * amount);
            if (!creditsService.TrySpendCredits(saveModel.World.Player.Credits, creditsSpent, out var remainingCredits))
            {
                return false;
            }

            saveModel.World.Player.Credits = remainingCredits;
            inventoryService.AddItem(
                saveModel.Inventory,
                itemConfig.Id,
                amount,
                ItemInstance.DurabilityNotTracked);

            result = new TradePurchaseResult(
                itemConfig.Id,
                amount,
                creditsSpent,
                remainingCredits);
            return true;
        }

        private bool TryGetTradeGoodItem(
            int itemConfigId,
            out ItemConfig itemConfig,
            out TradeGoodConfig tradeGoodConfig)
        {
            itemConfig = null;
            tradeGoodConfig = null;

            if (!configDatabase.TryGetItem(itemConfigId, out itemConfig) ||
                itemConfig.Category != ItemCategory.TradeGood ||
                itemConfig.TradeGood == null)
            {
                return false;
            }

            tradeGoodConfig = itemConfig.TradeGood;
            return configDatabase.TryGetTradeGood(tradeGoodConfig.Id, out _);
        }

        private static void EnsureSaveGraph(SaveModel saveModel)
        {
            saveModel.World ??= new MercLord.Global.Cells.WorldModel();
            saveModel.Inventory ??= new PlayerInventory();
            saveModel.World.Player ??= new MercLord.Global.Cells.PlayerGlobalData();
        }
    }
}
