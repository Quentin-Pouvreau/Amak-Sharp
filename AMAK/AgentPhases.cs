using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMAK
{
    public enum AgentPhases
	{
		/// <summary>
		/// Agent haven't started to perceive, decide or act
		/// </summary>
		Initialization,
		/// <summary>
		/// Agent is perceiving
		/// </summary>
		Perception,
		/// <summary>
		/// Agent is ready to decide
		/// </summary>
		PerceptionDone,
		/// <summary>
		/// Agent is deciding and acting
		/// </summary>
		DecisionAction,
		/// <summary>
		/// Agent is ready to perceive or die
		/// </summary>
		DecisionActionDone
	}
}