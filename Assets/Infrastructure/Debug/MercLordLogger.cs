using UnityEngine;

namespace MercLord.Infrastructure.Debugging
{
    public static class MercLordLogger
    {
        private const string Prefix = "[MercLord]";

        public static void Log(string message)
        {
            Debug.Log($"{Prefix} {message}");
        }

        public static void Warning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        public static void Error(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }
    }
}
