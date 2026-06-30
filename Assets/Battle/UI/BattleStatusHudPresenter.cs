using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;

namespace MercLord.Battle.UI
{
    public sealed class BattleStatusHudPresenter : IDisposable
    {
        private BattleSession session;
        private World world;
        private Filter unitFilter;
        private Filter playerFilter;
        private Stash<TeamComponent> teams;
        private Stash<HealthComponent> health;
        private Stash<DeadComponent> dead;

        public void Bind(BattleSession session)
        {
            DisposeFilters();

            this.session = session ?? throw new ArgumentNullException(nameof(session));
            world = session.World ?? throw new InvalidOperationException("BattleStatusHudPresenter requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleStatusHudPresenter cannot bind to a disposed Morpeh world.");
            }

            unitFilter = world.Filter
                .With<TeamComponent>()
                .With<HealthComponent>()
                .Build();
            playerFilter = world.Filter
                .With<PlayerControlledComponent>()
                .With<TeamComponent>()
                .With<HealthComponent>()
                .Build();
            teams = world.GetStash<TeamComponent>();
            health = world.GetStash<HealthComponent>();
            dead = world.GetStash<DeadComponent>();
        }

        public BattleStatusHudSnapshot BuildSnapshot()
        {
            if (session == null ||
                world == null ||
                world.IsDisposed ||
                unitFilter == null)
            {
                return new BattleStatusHudSnapshot(
                    default,
                    default,
                    false,
                    BattleTeamType.Attacker,
                    0,
                    0,
                    false,
                    BattleOutcome.None);
            }

            CountTeams(
                out var attackerTotal,
                out var attackerAlive,
                out var defenderTotal,
                out var defenderAlive);
            TryGetPlayerHealth(
                out var hasPlayer,
                out var playerTeam,
                out var playerCurrentHealth,
                out var playerMaxHealth);

            return new BattleStatusHudSnapshot(
                new BattleTeamHudSnapshot(attackerTotal, attackerAlive),
                new BattleTeamHudSnapshot(defenderTotal, defenderAlive),
                hasPlayer,
                playerTeam,
                playerCurrentHealth,
                playerMaxHealth,
                session.Completion.IsCompleted,
                session.Completion.Result?.Outcome ?? BattleOutcome.None);
        }

        public void Dispose()
        {
            DisposeFilters();
            session = null;
            world = null;
            teams = null;
            health = null;
            dead = null;
        }

        private void CountTeams(
            out int attackerTotal,
            out int attackerAlive,
            out int defenderTotal,
            out int defenderAlive)
        {
            attackerTotal = 0;
            attackerAlive = 0;
            defenderTotal = 0;
            defenderAlive = 0;

            foreach (var entity in unitFilter)
            {
                var team = teams.Get(entity).Value;
                var isAlive = IsAlive(entity);
                if (team == BattleTeamType.Attacker)
                {
                    attackerTotal++;
                    if (isAlive)
                    {
                        attackerAlive++;
                    }
                }
                else
                {
                    defenderTotal++;
                    if (isAlive)
                    {
                        defenderAlive++;
                    }
                }
            }
        }

        private bool TryGetPlayerHealth(
            out bool hasPlayer,
            out BattleTeamType playerTeam,
            out int currentHealth,
            out int maxHealth)
        {
            hasPlayer = false;
            playerTeam = BattleTeamType.Attacker;
            currentHealth = 0;
            maxHealth = 0;

            foreach (var entity in playerFilter)
            {
                hasPlayer = true;
                playerTeam = teams.Get(entity).Value;
                var healthComponent = health.Get(entity);
                currentHealth = Math.Max(0, healthComponent.Current);
                maxHealth = Math.Max(0, healthComponent.Max);
                return true;
            }

            return false;
        }

        private bool IsAlive(Entity entity)
        {
            if (dead.Has(entity))
            {
                return false;
            }

            return health.Get(entity).Current > 0;
        }

        private void DisposeFilters()
        {
            if (world != null && !world.IsDisposed)
            {
                unitFilter?.Dispose();
                playerFilter?.Dispose();
            }

            unitFilter = null;
            playerFilter = null;
        }
    }
}
