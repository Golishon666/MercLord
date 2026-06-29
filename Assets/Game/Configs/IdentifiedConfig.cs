using UnityEngine;

namespace MercLord.Game.Configs
{
    public interface IIdentifiedConfig
    {
        int Id { get; }
        string DisplayName { get; }
    }

    public abstract class IdentifiedConfig : ScriptableObject, IIdentifiedConfig
    {
        [SerializeField] private int id;
        [SerializeField] private string displayName;

        public int Id => id;
        public string DisplayName => displayName;
    }
}
