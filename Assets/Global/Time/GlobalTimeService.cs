using UnityEngine;

namespace MercLord.Global.Time
{
    public enum GlobalTimeMode
    {
        Paused,
        PlayerMoving,
        Waiting
    }

    public sealed class GlobalTimeService
    {
        public GlobalTimeMode Mode { get; private set; } = GlobalTimeMode.Paused;
        public float SimulationSpeed { get; private set; } = 1f;
        public bool IsSimulationActive => Mode != GlobalTimeMode.Paused;

        public void SetPaused()
        {
            Mode = GlobalTimeMode.Paused;
            SimulationSpeed = 0f;
        }

        public void SetPlayerMoving(float simulationSpeed)
        {
            Mode = GlobalTimeMode.PlayerMoving;
            SimulationSpeed = Mathf.Max(0f, simulationSpeed);
        }

        public void SetWaiting(float simulationSpeed)
        {
            Mode = GlobalTimeMode.Waiting;
            SimulationSpeed = Mathf.Max(0f, simulationSpeed);
        }
    }
}
