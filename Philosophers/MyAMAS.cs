using AMAK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Philosophers
{
    internal class MyAMAS : Amas<Table>
    {
        public MyAMAS(Table env, bool doSyncAgents = true)
            : base (env, doSyncAgents)
        {
        }

        public override bool StopCondition =>
            agents.All(a => a is Philosopher philosopher && philosopher.eatenPastas >= 5);

        internal void Start()
        {
            scheduler.Start();
        }

        protected override void OnInitialAgentsCreation()
        {
            Fork[] forks = Environment.Forks;
            Philosopher[] philosophers = new Philosopher[forks.Length];
            bool areSynchronous = (bool)Parameters[0];
            //Create one agent per fork
            for (int i = 0; i < forks.Length - 1; i++)
            {
                philosophers[i] = new Philosopher(i, this, forks[i], forks[i + 1])
                { isSynchronous = areSynchronous };
            }

            //Let the last philosopher takes the first fork (round table)
            philosophers[forks.Length - 1] = new Philosopher(forks.Length - 1, this, forks[forks.Length - 1], forks[0])
            { isSynchronous = areSynchronous };

            //Add neighborhood
            for (int i = 1; i < philosophers.Length; i++)
            {
                philosophers[i].AddNeighbor(philosophers[i - 1]);
                philosophers[i - 1].AddNeighbor(philosophers[i]);
            }
            philosophers[0].AddNeighbor(philosophers[philosophers.Length - 1]);
            philosophers[philosophers.Length - 1].AddNeighbor(philosophers[0]);
        }

        protected override void OnCycleEnd()
        {
            if ((bool)Parameters[0])
            {
                int[] indexes = new int[agents.Count];
                int[] eatenPastas = new int[agents.Count];
                char[] states = new char[agents.Count];
                int[] hungerDurations = new int[agents.Count];
                foreach (Philosopher philosopher in agents.Cast<Philosopher>())
                {
                    indexes[philosopher.Id] = philosopher.Id;
                    eatenPastas[philosopher.Id] = philosopher.eatenPastas;
                    states[philosopher.Id] = philosopher.state.ToString()[0];
                    hungerDurations[philosopher.Id] = philosopher.hungerDuration;
                }

                Console.WriteLine("\n Cycle : " + nbCycles);
                Console.WriteLine("======================");
                Console.WriteLine("Philosophers :\t\t" + string.Join("  |  ", indexes));
                Console.WriteLine("Eaten Pastas :\t\t" + string.Join("  |  ", eatenPastas));
                Console.WriteLine("States :\t\t" + string.Join("  |  ", states));
                Console.WriteLine("Hunger Durations :\t" + string.Join("  |  ", hungerDurations));
                Console.WriteLine();
            }
            else
            {
                List<Philosopher> wantedAgents = agents.Where(a => a is Philosopher p && p.eatenPastas < 5).Cast<Philosopher>().ToList();
                if (wantedAgents.Count() == 1)
                {
                }
            }
        }
    }
}