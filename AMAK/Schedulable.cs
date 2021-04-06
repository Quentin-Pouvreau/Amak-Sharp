using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMAK
{
    public abstract class Schedulable
    {
        /// <summary>
        /// The <see cref="Scheduler"/> that holds this <see cref="Schedulable"/>.
        /// </summary>
        public Scheduler scheduler;

        /// <summary>
        /// The default time between scheduler cycle.
        /// </summary>
        public const int DefaultSleep = 0;

		/// <summary>
		/// Run a cycle of the schedulable system.
		/// </summary>
		public abstract void Cycle();

        /// <summary>
        /// Check if this <see cref="Schedulable"/> must be stopped by the <see cref="Scheduler"/>.
        /// For example, a stop condition can be "cycle == 5000" aiming at stopping the system
        /// at the cycle 5000 in order to extract results or simply debugging.
        /// </summary>
        /// <returns>
        /// True if the <see cref="Scheduler"/> must stops its execution.
        /// </returns>
        public abstract bool StopCondition { get; }

        /// <summary>
        /// This method is called when the <see cref="Scheduler"/> starts.
        /// </summary>
        public abstract void OnSchedulingStarts();

        /// <summary>
        /// This method is called when the <see cref="Scheduler"/> stops (by <see cref="StopCondition"/> or explicit stop).
        /// </summary>
        public abstract void OnSchedulingStops();
	}
}