using System;

namespace Philosophers
{
    internal class Fork
    {
        private Philosopher takenBy;

        public bool TryTake(Philosopher asker)
        {
            if (takenBy != null)
                return false;

            takenBy = asker;
            return true;
        }

        public void Release(Philosopher asker)
        {
            if (takenBy == asker)
                takenBy = null;
        }

        public bool IsOwnedBy(Philosopher asker)
        {
            return takenBy == asker;
        }
    }
}