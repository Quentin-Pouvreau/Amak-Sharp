using AMAK;
using System;
using System.Collections.Generic;

namespace Philosophers
{
    internal class Philosopher : Agent<Table>
    {
        private static Random random = new Random();

        private Fork left;
        private Fork right;

        internal PhilosopherStates state = PhilosopherStates.Think;
        internal int hungerDuration;
        internal int eatenPastas;

        public List<int> hungerDurations = new List<int>();

        public Philosopher(int id, MyAMAS myAMAS, Fork left, Fork right)
            : base(id, myAMAS)
        {
            this.left = left;
            this.right = right;
        }

        protected override void OnDecideAndAct()
        {
            PhilosopherStates nextState = state;
            switch (state)
            {
                case PhilosopherStates.Think:
                    if (random.Next(101) > 50)
                    {
                        hungerDurations.Add(hungerDuration);
                        hungerDuration = 0;
                        nextState = PhilosopherStates.Hungry;
                    }
                    break;
                case PhilosopherStates.Hungry:
                    hungerDuration++;
                    if (GetMostCriticalNeighbor(true) == this)
                    {
                        if (left.TryTake(this) && right.TryTake(this))
                            nextState = PhilosopherStates.Eating;
                        else
                        {
                            left.Release(this);
                            right.Release(this);
                        }
                    }
                    else
                    {
                        left.Release(this);
                        right.Release(this);
                    }
                    break;
                case PhilosopherStates.Eating:
                    eatenPastas++;
                    if (random.Next(101) > 50)
                    {
                        left.Release(this);
                        right.Release(this);
                        nextState = PhilosopherStates.Think;
                    }
                    break;
                default:
                    break;
            }

            state = nextState;
        }

        protected override double ComputeCriticality()
        {
            return hungerDuration;
        }

        protected override void OnAgentCycleBegin()
        {
            if (eatenPastas < 5)
            {

            }
        }
    }
}