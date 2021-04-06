using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;

namespace AMAK
{
    public class Scheduler
    {
        /// <summary>
        /// The schedulables object handled by the scheduler
        /// </summary>
        private HashSet<Schedulable> schedulables = new HashSet<Schedulable>();

        private SchedulingStates state;
        private readonly object stateLock = new object();

		// The idea is to prevent scheduler from launching
		// if the schedulables are not yet fully ready
		private int locked = 0;

		public Scheduler(params Schedulable[] schedulables)
        {
            foreach (var schedulable in schedulables)
            {
                Add(schedulable);
            }
            state = SchedulingStates.Idle;
        }

        /// <summary>
        /// The schedulables that must be added
        /// </summary>
        public Queue<Schedulable> PendingAdditionSchedulables { get; private set; } = new Queue<Schedulable>();

        /// <summary>
        /// The schedulables that must be removed
        /// </summary>
        public Queue<Schedulable> PendingRemovalSchedulables { get; private set; } = new Queue<Schedulable>();

        internal bool IsRunning => state == SchedulingStates.Running;

		/// <summary>
		/// The sleep time in milliseconds between each cycle.
		/// </summary>
		internal int SleepTime { get; set; }

        /// <summary>
        /// Plan to add a schedulable
        /// </summary>
        /// <param name="_schedulable">The schedulable to add</param>
        public void Add(Schedulable _schedulable)
        {
            PendingAdditionSchedulables.Enqueue(_schedulable);
		}

		/// <summary>
		/// Plan to remove a <see cref="Schedulable"/>.
		/// </summary>
		/// <param name="schedulable">
		/// The <see cref="Schedulable"/> to remove.
		/// </param>
		public void Remove(Schedulable schedulable)
		{
			PendingRemovalSchedulables.Enqueue(schedulable);
		}

		/// <summary>
		/// Effectively Add or Remove the schedulables that were added or removed
		/// during a cycle to avoid.
		/// </summary>
		private void TreatPendingSchedulables()
		{
			while (PendingAdditionSchedulables.Any())
				schedulables.Add(PendingAdditionSchedulables.Dequeue());
			while (PendingRemovalSchedulables.Any())
				schedulables.Remove(PendingRemovalSchedulables.Dequeue());
        }

        /// <summary>
        /// Start (or continue) with no delay between cycles.
        /// </summary>
        public void Start()
        {
            Start(Schedulable.DefaultSleep);
        }

        /// <summary>
        /// Start (or continue) with the specified delay between two cycles
        /// and launch the scheduler if it is not.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The delay between two cycles in milliseconds.
        /// </param>
        public void Start(int millisecondsTimeout)
        {
            if (locked <= 0)
            {
                SleepTime = millisecondsTimeout;
                lock (stateLock)
                {
                    if (state == SchedulingStates.Idle)
                    {
                        state = SchedulingStates.Running;
                        new Thread(new ThreadStart(Run)).Start();
                    }
                }
            }
            OnSpeedChanged();
        }

		/// <summary>
		/// Run a cycle of the schedulable system.
		/// </summary>
		public void Step()
        {
            if (locked <= 0)
            {
                SleepTime = 0;
                lock (stateLock)
                {
                    switch (state)
                    {
                        case SchedulingStates.Idle:
                            state = SchedulingStates.PendingStop;
                            new Thread(new ThreadStart(Run)).Start();
                            break;
                        default:
                            break;

                    }
                }
            }
            OnSpeedChanged();
        }

        /// <summary>
        /// Stop the scheduler if it is running.
        /// </summary>
        public void Stop()
		{
			lock(stateLock)
			{
                if (state == SchedulingStates.Running)
                {
                    state = SchedulingStates.PendingStop;
                }
            }
			OnSpeedChanged();
		}

		/// <summary>
		/// Threaded run method.
		/// </summary>
		public void Run()
        {
            TreatPendingSchedulables();

            foreach (Schedulable schedulable in schedulables)
            {
                schedulable.OnSchedulingStarts();
            }

            bool mustStop;
            do
            {
                foreach (Schedulable schedulable in schedulables)
                {
                    schedulable.Cycle();
                }
                if (SleepTime != 0)
                {
                    try
                    {
                        Thread.Sleep(SleepTime);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.StackTrace);
                    }
                }
                mustStop = false;
                foreach (Schedulable schedulable in schedulables)
                {
                    mustStop |= schedulable.StopCondition;
                }
            }
            while (state == SchedulingStates.Running && !mustStop);

            lock (stateLock)
            {
                state = SchedulingStates.Idle;
            }

            foreach (Schedulable schedulable in schedulables)
            {
                schedulable.OnSchedulingStops();
            }

            TreatPendingSchedulables();
            OnStop();
        }

		/// <summary>
		/// Soft lock the scheduler to avoid a too early running.
		/// </summary>
		public void Lock()
		{
			locked++;
		}

		/// <summary>
		/// Soft unlock the scheduler to avoid a too early running.
		/// </summary>
		public void Unlock()
		{
			locked--;
		}

        #region Events

        /// <summary>
        /// Raised when <see cref="SpeedTime"/> is changed.
        /// </summary>
        public event EventHandler SpeedChanged;
		private void OnSpeedChanged()
		{
			SpeedChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Raised when the scheduler stops.
		/// </summary>
		public event EventHandler Stopped;
        private void OnStop()
        {
            Stopped?.Invoke(this, EventArgs.Empty);
        } 

        #endregion
    }
}