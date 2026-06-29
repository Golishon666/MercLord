using System;
using MercLord.Global.Cells;
using MercLord.Player.Equipment;
using MercLord.Player.Inventory;

namespace MercLord.Game.Save
{
    [Serializable]
    public sealed class SaveModel
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public WorldModel World = new WorldModel();
        public PlayerInventory Inventory = new PlayerInventory();
        public PlayerEquipment Equipment = new PlayerEquipment();
    }

    public interface ISaveService
    {
        SaveModel Current { get; }
        void SetCurrent(SaveModel saveModel);
        SaveModel CreateNew(int seed);
        SaveModel CreateNew(WorldModel world);
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

        public SaveModel CreateNew(WorldModel world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var saveModel = new SaveModel
            {
                Version = SaveModel.CurrentVersion,
                World = world
            };

            Current = saveModel;
            return saveModel;
        }
    }
}
