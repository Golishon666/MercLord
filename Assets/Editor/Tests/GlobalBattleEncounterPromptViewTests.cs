using MercLord.Game.StateMachine;
using MercLord.Global.UI;
using NUnit.Framework;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class GlobalBattleEncounterPromptViewTests
    {
        [Test]
        public void ShowBindsEncounterTextAndButtonCallbacks()
        {
            var view = GlobalBattleEncounterPromptView.CreateRuntime();
            try
            {
                var attackClicked = false;
                var cancelClicked = false;
                var encounter = new GlobalBattleEncounter(
                    cellId: 7,
                    opponentArmyId: 42,
                    playerFactionId: 1,
                    opponentFactionId: 2,
                    playerUnitCount: 12,
                    opponentUnitCount: 18);

                view.Show(
                    encounter,
                    "Enemy Faction",
                    () => attackClicked = true,
                    () => cancelClicked = true);

                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual(42, view.CurrentEncounter.OpponentArmyId);
                StringAssert.Contains("Enemy army 42", view.TitleLabel.text);
                StringAssert.Contains("Enemy Faction", view.BodyLabel.text);
                StringAssert.Contains("12 vs 18", view.BodyLabel.text);

                view.AttackButton.onClick.Invoke();
                Assert.IsTrue(attackClicked);
                Assert.IsTrue(view.IsVisible);

                view.CancelButton.onClick.Invoke();
                Assert.IsTrue(cancelClicked);
                Assert.IsFalse(view.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }
    }
}
