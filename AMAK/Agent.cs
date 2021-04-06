using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMAK
{
    public abstract class Agent<E> : IComparable<Agent<E>>
        where E : Environment
    {
        public int Id { get; }

        protected E environment;
        // Last calculated criticality of the agent
        private double criticality;
        private int executionOrder;
        // Current phase of the agent
        protected AgentPhases currentPhase = AgentPhases.Initialization;
        public bool isSynchronous = true;


        public Agent(int id, Amas<E> amas)
        {
            Id = id;
            Neighborhood = new List<Agent<E>>();
            Criticalities = new Dictionary<Agent<E>, double>();
            Neighborhood.Add(this);

            environment = amas.Environment;

            if (amas != null)
            {
                amas.AddAgent(this);
            }
        }

        // Criticalities of the neighbors (and it self) as perceived at the beginning of the agent's cycle
        protected Dictionary<Agent<E>, double> Criticalities { get; }

        // Neighborhood of the agent ; must refer to the same couple amas, environment
        protected List<Agent<E>> Neighborhood { get; }

        public void AddNeighbor(params Agent<E>[] agents)
        {
            foreach (var agent in agents)
            {
                if (agent != null)
                {
                    Neighborhood.Add(agent);
                    Criticalities.Add(agent, double.NegativeInfinity);
                }
            }
        }

        protected abstract double ComputeCriticality();

        /// <summary>
        /// Compute the execution order from the layer and a random value.
        /// </summary>
        /// <returns>A number used by amak to determine which agent executes first.</returns>
        private int ComputeExecutionOrder()
        {
            return ComputeExecutionOrderLayer() * 10000 + environment.Random.Next(10000);
        }

        /// <summary>
        /// This method must be overriden if you need to specify an execution order layer
        /// </summary>
        /// <returns>The execution order layer</returns>
        protected virtual int ComputeExecutionOrderLayer()
        {
            return 0;
        }

        /// <summary>
        /// Called by the framework when all initial agents have been created
        /// and are almost ready to be started
        /// </summary>
        internal void OnBeforeReady()
        {
            criticality = ComputeCriticality();
            executionOrder = ComputeExecutionOrder();
        }

        /// <summary>
        /// Called when all initial agents have been created and are ready to be started
        /// </summary>
        internal virtual void OnReady() { }

        public void Cycle(ExecutionPolicies executionPolicy)
        {
            OnAgentCycleBegin();
            if (executionPolicy == ExecutionPolicies.OnePhase)
            {
                OnePhaseCycle();
            }
            else if (executionPolicy == ExecutionPolicies.TwoPhases)
            {
                currentPhase = NextPhase;
                switch (currentPhase)
                {
                    case AgentPhases.Perception:
                        RunPerceptionPhase();
                        break;
                    case AgentPhases.DecisionAction:
                        RunDesicionActionPhase();
                        break;
                    default:
                        //Log.defaultLog.fatal("AMAK", "An agent is being run in an invalid phase (%s)", currentPhase);
                        break;
                }
            }
            OnAgentCycleEnd();
        }

        /// <summary>
        /// Determine which phase comes after <see cref="currentPhase"/>.
        /// </summary>
        private AgentPhases NextPhase
        {
            get
            {
                switch (currentPhase)
                {
                    case AgentPhases.Perception:
                        return AgentPhases.PerceptionDone;
                    case AgentPhases.PerceptionDone:
                        return AgentPhases.DecisionAction;
                    case AgentPhases.DecisionAction:
                        return AgentPhases.DecisionActionDone;
                    default:
                        return AgentPhases.Perception;
                }
            }
        }

        public void OnePhaseCycle()
        {
            RunPerceptionPhase();
            RunDesicionActionPhase();
        }

        protected void RunPerceptionPhase()
        {
            currentPhase = AgentPhases.Perception;
            Perceive();
        }

        protected void RunDesicionActionPhase()
        {
            currentPhase = AgentPhases.DecisionAction;
            DecideAndAct();

            executionOrder = ComputeExecutionOrder();
            criticality = ComputeCriticality();
            Criticalities[this] = criticality;

            OnExpose();
        }

        private void Perceive()
        {
            OnPerceive();
            foreach (Agent<E> agent in Neighborhood)
            {
                Criticalities[agent] = agent.criticality;
            }
            currentPhase = AgentPhases.PerceptionDone;
            OnPerceived();
        }

        /// <summary>
        /// A combination of decision and action as called by the framework
        /// </summary>
        private void DecideAndAct()
        {
            OnDecideAndAct();
            criticality = ComputeCriticality();
            currentPhase = AgentPhases.DecisionActionDone;
            OnActed();
        }

        protected Agent<E> GetMostCriticalNeighbor(bool includingMe)
        {
            List<Agent<E>> criticalest = new List<Agent<E>>();
            double maxCriticality = double.NegativeInfinity;

            if (includingMe)
            {
                criticalest.Add(this);
                maxCriticality = Criticalities.Max(neighbor => neighbor.Value);
            }
            lock (Criticalities)
            {
                foreach (KeyValuePair<Agent<E>, double> pair in Criticalities)
                {
                    if (pair.Value > maxCriticality)
                    {
                        criticalest.Clear();
                        maxCriticality = pair.Value;
                        criticalest.Add(pair.Key);
                    }
                    else if (pair.Value == maxCriticality)
                    {
                        criticalest.Add(pair.Key);
                    }
                } 
            }
            if (criticalest.Any())
                return criticalest[environment.Random.Next(criticalest.Count)];

            return null;
        }

        public int CompareTo(Agent<E> other) => executionOrder - other.executionOrder;

        protected virtual void OnAgentCycleBegin() { }

        protected virtual void OnPerceive() { }

        /// <summary>
        /// Decide and act These two phases can often be grouped
        /// </summary>
        protected virtual void OnDecideAndAct()
        {
            OnDecide();
            OnAct();
        }

        protected virtual void OnAct() { }

        protected virtual void OnDecide() { }

        protected virtual void OnExpose() { }

        protected virtual void OnAgentCycleEnd() { }

        #region Events

        public event EventHandler Perceived;
        private void OnPerceived() => Perceived?.Invoke(this, EventArgs.Empty);

        public event EventHandler Acted;
        private void OnActed() => Acted?.Invoke(this, EventArgs.Empty);

        #endregion
    }
}