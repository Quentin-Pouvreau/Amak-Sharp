using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AMAK
{
    public abstract class Amas<E> : Schedulable
        where E : Environment
    {
        public object[] Parameters { get; }
        public E Environment { get; }

        public List<Agent<E>> agents;
        public int nbCycles;

        public ExecutionPolicies executionPolicy = ExecutionPolicies.OnePhase;

        /// <summary>
        /// Agents that must be removed from the AMAS at the end of the cycle
        /// </summary>
        private Queue<Agent<E>> agentsPendingRemoval = new Queue<Agent<E>>();

        /// <summary>
        /// Agents that must be added to the AMAS at the end of the 
        /// </summary>
        private Queue<Agent<E>> agentsPendingAddition = new Queue<Agent<E>>();


        private List<Agent<E>> runningAsyncAgents = new List<Agent<E>>();

        /// <summary>
        /// This semaphore is meant to synchronize the agents after the <see cref="AgentPhases.Perception"/> phase.
        /// </summary>
        protected Semaphore perceptionPhaseSemaphore;
        /// <summary>
        /// This semaphore is meant to synchronize the agents after the <see cref="AgentPhases.DecisionAction"/> phase.
        /// </summary>
        protected Semaphore decisionActionPhaseSemaphore;

        public Amas(E environment, params object[] parameters)
        {
            agents = new List<Agent<E>>();
            scheduler = new Scheduler(this);
            scheduler.Lock();
            Parameters = parameters;
            Environment = environment;
            OnInitialConfiguration();
            OnInitialAgentsCreation();
            AddPendingAgents();
            OnReady();
            scheduler.Unlock();
        }

        /// <summary>
        /// Add an agent to the MAS.
        /// This method is called by the agent itself during its creation.
        /// </summary>
        /// <param name="agent">
        /// The agent to add to the system
        /// </param>
        public void AddAgent(Agent<E> agent)
        {
            lock (agentsPendingAddition)
            {
                agentsPendingAddition.Enqueue(agent);
            }
        }

        /// <summary>
        /// Remove an agent from the MAS.
        /// </summary>
        /// <param name="agent">
        /// The agent to remove from the system
        /// </param>
        public void RemoveAgent(Agent<E> agent)
        {
            lock (agentsPendingRemoval)
            {
                agentsPendingRemoval.Enqueue(agent);
            }
        }

        /// <summary>
        /// Effectively add agent to the system
        /// </summary>
        protected void AddPendingAgents()
        {
            // The double loop is required as the method onReady should only be called when
            // all the agents have been added
            lock (agentsPendingAddition)
            {
                foreach (var agent in agentsPendingAddition)
                {
                    agents.Add(agent);
                }
                while (agentsPendingAddition.Any())
                {
                    var agent = agentsPendingAddition.Dequeue();
                    agent.OnBeforeReady();
                    agent.OnReady();
                    if (!agent.isSynchronous)
                    {
                        scheduler.SpeedChanged += (s, e) =>
                        {
                            Scheduler scheduler = s as Scheduler;
                            if (scheduler.IsRunning && !runningAsyncAgents.Contains(agent))
                            {
                                StartRunningAsyncAgent(agent);
                            }
                        };
                        StartRunningAsyncAgent(agent);
                    }
                }
            }
        }

        protected void RemovePendingAgents()
        {
            lock(agentsPendingRemoval)
            {
                while (agentsPendingRemoval.Any())
                    agents.Remove(
                        agentsPendingRemoval.Dequeue());
            }
        }

        private void StartRunningAsyncAgent(Agent<E> agent)
        {
            runningAsyncAgents.Add(agent);
            RunAsynchronousAgent(agent);
        }

        private void RunAsynchronousAgent(Agent<E> agent)
        {
            ThreadPool.QueueUserWorkItem((target) =>
            {
                agent.Cycle(ExecutionPolicies.OnePhase);
                if (scheduler.IsRunning && agents.Contains(agent))
                {
                    Thread.Sleep(scheduler.SleepTime);
                    RunAsynchronousAgent(agent);
                }
                else
                {
                    try
                    {
                        runningAsyncAgents.Remove(agent);
                    }
                    catch (Exception)
                    {
                        
                    }
                }
            });
        }

        public override void Cycle()
        {
            nbCycles++;
            List<Agent<E>> synchronousAgents = agents.Where(a => a.isSynchronous).ToList();
            synchronousAgents.Sort();

            OnCycleBegin();

            if (synchronousAgents.Any())
            {
                perceptionPhaseSemaphore = new Semaphore(0, synchronousAgents.Count);
                decisionActionPhaseSemaphore = new Semaphore(0, synchronousAgents.Count);

                switch (executionPolicy)
                {
                    case ExecutionPolicies.OnePhase:
                        foreach (Agent<E> agent in synchronousAgents)
                        {
                            ThreadPool.QueueUserWorkItem((state) =>
                            {
                                agent.Perceived += Agent_Perceived;
                                agent.Acted += Agent_Acted;
                                agent.Cycle(executionPolicy);
                            });
                        }
                        for (int i = 0; i < synchronousAgents.Count; i++)
                        {
                            perceptionPhaseSemaphore.WaitOne();
                            decisionActionPhaseSemaphore.WaitOne();
                        }
                        break;
                    case ExecutionPolicies.TwoPhases:
                        foreach (Agent<E> agent in synchronousAgents)
                        {
                            ThreadPool.QueueUserWorkItem((state) =>
                            {
                                agent.Perceived += Agent_Perceived;
                                agent.Cycle(executionPolicy);
                            });
                        }
                        for (int i = 0; i < synchronousAgents.Count; i++)
                        {
                            perceptionPhaseSemaphore.WaitOne();
                        }
                        foreach (Agent<E> agent in synchronousAgents)
                        {
                            ThreadPool.QueueUserWorkItem((state) =>
                            {
                                agent.Acted += Agent_Acted;
                                agent.Cycle(executionPolicy);
                            });
                        }
                        for (int i = 0; i < synchronousAgents.Count; i++)
                        {
                            decisionActionPhaseSemaphore.WaitOne();
                        }
                        break;
                    default:
                        break;
                }

                perceptionPhaseSemaphore.Dispose();
                decisionActionPhaseSemaphore.Dispose();
            }

            RemovePendingAgents();
            AddPendingAgents();
            OnCycleEnd();
        }

        private void Agent_Perceived(object sender, EventArgs e)
        {
            (sender as Agent<E>).Perceived -= Agent_Perceived;
            try
            {
                perceptionPhaseSemaphore?.Release();
            }
            catch (SemaphoreFullException)
            {
                Console.WriteLine("Un Release en trop pour la perception.");
            }
        }

        private void Agent_Acted(object sender, EventArgs e)
        {
            (sender as Agent<E>).Acted -= Agent_Acted;
            try
            {
                decisionActionPhaseSemaphore?.Release();
            }
            catch (SemaphoreFullException)
            {
                Console.WriteLine("Un Release en trop pour la décision et l'action.");
            }
        }

        #region Schedulable Implementation

        public override bool StopCondition => false;

        public override void OnSchedulingStarts() { }

        public override void OnSchedulingStops() { }

        #endregion

        #region Methods to override

        protected virtual void OnInitialConfiguration() { }

        protected virtual void OnInitialAgentsCreation() { }

        protected virtual void OnReady() { }

        protected virtual void OnCycleBegin() { }

        protected virtual void OnCycleEnd() { }

        #endregion
    }
}