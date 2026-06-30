using System;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using UnityEngine;
using VContainer;

namespace MercLord.Battle.Rendering
{
    public sealed class BattleSceneRoot : MonoBehaviour
    {
        [SerializeField] private Transform unitViewRoot;

        private IBattleSessionService battleSessionService;
        private IBattlePlayerSpawner battlePlayerSpawner;
        private IBattleVehicleSpawner battleVehicleSpawner;
        private IBattleViewSpawner battleViewSpawner;
        private IBattleSystemRunner battleSystemRunner;

        public Transform UnitViewRoot => unitViewRoot;

        [Inject]
        public void Construct(
            IBattleSessionService battleSessionService,
            IBattlePlayerSpawner battlePlayerSpawner,
            IBattleVehicleSpawner battleVehicleSpawner,
            IBattleViewSpawner battleViewSpawner,
            IBattleSystemRunner battleSystemRunner)
        {
            this.battleSessionService = battleSessionService ?? throw new ArgumentNullException(nameof(battleSessionService));
            this.battlePlayerSpawner = battlePlayerSpawner ?? throw new ArgumentNullException(nameof(battlePlayerSpawner));
            this.battleVehicleSpawner = battleVehicleSpawner ?? throw new ArgumentNullException(nameof(battleVehicleSpawner));
            this.battleViewSpawner = battleViewSpawner ?? throw new ArgumentNullException(nameof(battleViewSpawner));
            this.battleSystemRunner = battleSystemRunner ?? throw new ArgumentNullException(nameof(battleSystemRunner));
        }

        private void Start()
        {
            if (unitViewRoot == null)
            {
                throw new InvalidOperationException("BattleSceneRoot requires a unit view root.");
            }

            var session = battleSessionService?.Current
                ?? throw new InvalidOperationException("BattleSceneRoot requires an active battle session.");
            battlePlayerSpawner.SpawnPlayer(session);
            battleVehicleSpawner.SpawnVehicles(session);
            battleViewSpawner.SpawnMissingViews(session, unitViewRoot);
            battleSystemRunner.Start(session);
        }

        private void Update()
        {
            battleSystemRunner?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            battleSystemRunner?.Stop();
            battleViewSpawner?.ReleaseAll();
        }
    }
}
