using System;

namespace MercLord.Economy.Credits
{
    public sealed class CreditsService
    {
        public int Add(int currentCredits, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Use Spend for negative credit changes.");
            }

            return checked(currentCredits + amount);
        }

        public bool CanSpend(int currentCredits, int amount)
        {
            return amount >= 0 && currentCredits >= amount;
        }

        public bool TrySpend(int currentCredits, int amount, out int result)
        {
            if (!CanSpend(currentCredits, amount))
            {
                result = currentCredits;
                return false;
            }

            result = currentCredits - amount;
            return true;
        }
    }
}
