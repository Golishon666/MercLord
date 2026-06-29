using System;
using MercLord.Global.Cells;
using MercLord.Player.Equipment;
using MercLord.Player.Inventory;

namespace MercLord.Game.Save
{
    [Serializable]
    public sealed class SaveModel
    {
        public int Version = 1;
        public WorldModel World = new WorldModel();
        public PlayerInventory Inventory = new PlayerInventory();
        public PlayerEquipment Equipment = new PlayerEquipment();
    }

    public interface ISaveService
    {
        SaveModel Current { get; }
        void SetCurrent(SaveModel saveModel);
        SaveModel CreateNew(int seed);
    }

    public sealed class SaveService : ISaveService
    {
        public SaveModel Current { get; private set; }

        public void SetCurrent(SaveModel saveModel)
        {
            Current = saveModel ?? throw new ArgumentNullException(nameof(saveModel));
        }

        public SaveModel CreateNew(int seed)
        {
            var saveModel = new SaveModel();
            saveModel.World.Seed = seed;
            Current = saveModel;
            return saveModel;
        }
    }
}
