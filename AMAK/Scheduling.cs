namespace AMAK
{
    internal enum SchedulingStates
	{
		/// <summary>
		/// The scheduler is running
		/// </summary>
		Running,
		/// <summary>
		/// The scheduler is paused
		/// </summary>
		Idle,
		/// <summary>
		/// The scheduler is expected to stop at the end at the current cycle
		/// </summary>
		PendingStop
	}
}