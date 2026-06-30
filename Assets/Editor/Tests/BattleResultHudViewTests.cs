using MercLord.Battle.Generation;
using MercLord.Battle.UI;
using MercLord.Global.Cells;
using NUnit.Framework;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattleResultHudViewTests
    {
        [Test]
        public void ShowBindsResultSummaryAndContinueCallback()
        {
            var view = BattleResultHudView.CreateRuntime();
            try
            {
                var continued = false;
                var result = new BattleResult
                {
                    Outcome = BattleOutcome.AttackerVictory,
                    PlayerSurvived = true,
                    CreditsReward = 25,
                    Loot = new[]
                    {
                        new BattleLootEntry { ItemConfigId = 10, Amount = 2 },
                        new BattleLootEntry { ItemConfigId = 11, Amount = 1 }
                    },
                    PlayerParty = new[]
                    {
                        new SquadData { UnitConfigId = 100, Count = 3 },
                        new SquadData { UnitConfigId = 101, Count = 0 }
                    }
                };

                view.Show(result, () => continued = true);

                Assert.IsTrue(view.IsVisible);
                StringAssert.Contains("Attacker victory", view.TitleLabel.text);
                StringAssert.Contains("Player survived: Yes", view.SummaryLabel.text);
                StringAssert.Contains("Credits: 25", view.SummaryLabel.text);
                StringAssert.Contains("Loot items: 3", view.SummaryLabel.text);
                StringAssert.Contains("Surviving party squads: 1", view.SummaryLabel.text);

                view.ContinueButton.onClick.Invoke();
                Assert.IsTrue(continued);
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }
    }
}
