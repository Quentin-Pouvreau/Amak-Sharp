using AMAK;
using System.Collections.Generic;

namespace Philosophers
{
    internal class Table : Environment
    {
        public Fork[] Forks { get; private set; }

        public Table()
        {
        }

        public override void OnInitialization()
        {
            // Set 10 forks on the table
            Forks = new Fork[8];
            for (int i = 0; i < Forks.Length; i++)
                Forks[i] = new Fork();
        }
    }
}