using AMAK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Philosophers
{
    static class Program
    {
        static Semaphore amasStopSemaphore = new Semaphore(0, 1);

        static void Main(string[] args)
        {
            //Random rand = new Random(Guid.NewGuid().GetHashCode());
            Console.WriteLine("Synchroniser les agents ? (O/n) :");
            bool doSyncAgents = Console.ReadLine() != "n";
            ExecutionPolicies exePolicy = ExecutionPolicies.OnePhase;
            if (doSyncAgents)
            {
                Console.WriteLine("Cycler en 2 phases ? (o/N) :");
                exePolicy = Console.ReadLine() == "o" ?
                    ExecutionPolicies.TwoPhases : ExecutionPolicies.OnePhase;
            }

            do
            {
                //Console.WriteLine(rand.Next(101));

                MyAMAS amas = new MyAMAS(new Table(), doSyncAgents);
                if (doSyncAgents)
                    amas.executionPolicy = exePolicy;
                amas.scheduler.Stopped += Scheduler_Stopped;
                amas.Start();
                amasStopSemaphore.WaitOne();
                Summary(amas);
                Console.Write("Relancer ? (O/n) : ");
            } while (Console.ReadLine() != "n");
            
        }

        private static void Summary(MyAMAS amas)
        {
            int[] indexes = new int[amas.agents.Count];
            int[] eatenPastas = new int[amas.agents.Count];
            double[] meanHungerDurations = new double[amas.agents.Count];
            foreach (Philosopher philosopher in amas.agents.Cast<Philosopher>())
            {
                indexes[philosopher.Id] = philosopher.Id;
                eatenPastas[philosopher.Id] = philosopher.eatenPastas;
                meanHungerDurations[philosopher.Id] = philosopher.hungerDurations.Average();
            }

            Console.WriteLine("\nTotal Cycles : " + amas.nbCycles);
            Console.WriteLine("======================");
            Console.WriteLine("Philosophers :\t\t" + string.Join("  |  ", indexes));
            Console.WriteLine("Eaten Pastas :\t\t" + string.Join("  |  ", eatenPastas));
            Console.WriteLine("Hunger Durations :\t" + string.Join("  |  ", meanHungerDurations));
            Console.WriteLine();
        }

        private static void Scheduler_Stopped(object sender, EventArgs e)
        {
            (sender as Scheduler).Stopped += Scheduler_Stopped;
            amasStopSemaphore.Release();
        }
    }
}
