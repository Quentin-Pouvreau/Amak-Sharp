using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMAK
{
    public abstract class Environment : Schedulable
    {
        public Random Random { get; } = new Random();

        public Environment()
        {
			scheduler = new Scheduler(this);
            lock (scheduler)
            {
                OnInitialization();
                OnInitialEntitiesCreation();
            }
        }

        public override void Cycle()
        {
            OnCycle();
        }

        public override bool StopCondition => true;

        public override void OnSchedulingStarts() { }

        public override void OnSchedulingStops() { }

        public virtual void OnInitialization() { }

        public virtual void OnInitialEntitiesCreation() { }

        protected virtual void OnCycle() { }
    }
}