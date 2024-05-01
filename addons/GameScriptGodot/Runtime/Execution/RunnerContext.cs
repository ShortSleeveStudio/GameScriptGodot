using System;
using System.Collections.Generic;

namespace GameScript
{
    public class RunnerContext
    {
        #region Constants
        private const int k_DefaultEdgeCapacity = 16;
        #endregion

        #region Static
        // 0 is reserved to represent uninitialized state
        private static uint s_NextContextId = 1;

        // 0 is reserved to represent uninitialized state
        private static uint s_NextSequenceNumber = 1;
        #endregion

        public uint ContextId { get; private set; }
        public uint SequenceNumber { get; private set; }

        private RunnerRoutineState m_RoutineState;
        private Conversation m_Conversation;
        private Node m_Node;
        private IRunnerListener m_Listener;
        private bool m_OnReadyCalled;
        private Action m_OnReady;
        private Node m_OnDecisionMadeValue;
        private bool m_OnDecisionMadeCalled;
        private Action<Node> m_OnDecisionMade;
        private MachineState m_CurrentState;
        private List<Node> m_AvailableNodes;
        private Lease m_DummyLease;
        private GameScriptSettings m_Settings;

        internal RunnerContext(GameScriptSettings settings)
        {
            ContextId = s_NextContextId++;
            m_OnReady = OnReady;
            m_CurrentState = MachineState.Idle;
            m_OnDecisionMade = OnDecisionMade;
            m_RoutineState = new(settings.MaxFlags, this);
            m_AvailableNodes = new(k_DefaultEdgeCapacity);
            m_DummyLease = Lease.DummyLease();
            m_Settings = settings;
        }

        #region Execution
        internal event Action<int> OnFlagRaised;

        internal void Start(Conversation conversation, IRunnerListener listener)
        {
            m_Node = conversation.RootNode;
            m_Listener = listener;
            m_Conversation = conversation;
            m_CurrentState = MachineState.ConversationEnter;
            SequenceNumber = s_NextSequenceNumber++;
        }

        internal void Stop()
        {
            // We'll still notify if possible
            m_Listener?.OnConversationExit(m_Conversation, new(SequenceNumber, this, m_OnReady));
            Reset();
        }

        /**Returns if the conversation is active*/
        internal bool Tick()
        {
            try
            {
                switch (m_CurrentState)
                {
                    case MachineState.Idle:
                    {
                        return false;
                    }
                    case MachineState.ConversationEnter:
                    {
                        m_Listener.OnConversationEnter(
                            m_Conversation,
                            new(SequenceNumber, this, m_OnReady)
                        );
                        m_CurrentState = MachineState.ConversationEnterWait;
                        goto case MachineState.ConversationEnterWait;
                    }
                    case MachineState.ConversationEnterWait:
                    {
                        if (!m_OnReadyCalled)
                            return true;
                        m_OnReadyCalled = false;
                        m_CurrentState = MachineState.NodeEnter;
                        goto case MachineState.NodeEnter;
                    }
                    case MachineState.NodeEnter:
                    {
                        m_Listener.OnNodeEnter(m_Node, new(SequenceNumber, this, m_OnReady));
                        m_CurrentState = MachineState.NodeEnterWait;
                        goto case MachineState.NodeEnterWait;
                    }
                    case MachineState.NodeEnterWait:
                    {
                        if (!m_OnReadyCalled)
                            return true;
                        m_OnReadyCalled = false;
                        m_CurrentState = MachineState.NodeExecute;
                        goto case MachineState.NodeExecute;
                    }
                    case MachineState.NodeExecute:
                    {
                        uint seq = SequenceNumber;
                        RoutineDirectory.Directory[m_Node.Code](this);
                        // Edge case:
                        // They stop the conversation inside of the routine.
                        if (seq != SequenceNumber)
                            return false;
                        if (!m_RoutineState.IsRoutineExecuted())
                            return true;
                        m_RoutineState.Reset();
                        m_CurrentState = MachineState.NodeExit;
                        goto case MachineState.NodeExit;
                    }
                    case MachineState.NodeExit:
                    {
                        m_Listener.OnNodeExit(m_Node, new(SequenceNumber, this, m_OnReady));
                        m_CurrentState = MachineState.NodeExitWait;
                        goto case MachineState.NodeExitWait;
                    }
                    case MachineState.NodeExitWait:
                    {
                        if (!m_OnReadyCalled)
                            return true;
                        m_OnReadyCalled = false;
                        m_CurrentState = MachineState.NodeDecision;
                        goto case MachineState.NodeDecision;
                    }
                    case MachineState.NodeDecision:
                    {
                        // Gather available edges
                        uint actorId = 0;
                        bool allEdgesSameActor = true;
                        byte priority = 0;
                        Node highestPriorityNode = null;
                        for (uint i = 0; i < m_Node.OutgoingEdges.Length; i++)
                        {
                            Edge edge = m_Node.OutgoingEdges[i];
                            // Conditions cannot be async
                            RoutineDirectory.Directory[edge.Target.Condition](this);
                            if (m_RoutineState.GetConditionResult())
                            {
                                // Retain a list of viable nodes
                                m_AvailableNodes.Add(edge.Target);

                                // See if all actors are the same
                                if (m_AvailableNodes.Count == 1)
                                    actorId = edge.Target.Actor.Id;
                                else if (allEdgesSameActor && actorId != edge.Target.Actor.Id)
                                {
                                    allEdgesSameActor = false;
                                }

                                // Track highest priority node
                                if (highestPriorityNode == null || priority < edge.Priority)
                                {
                                    priority = edge.Priority;
                                    highestPriorityNode = edge.Target;
                                }
                            }
                            m_RoutineState.Reset();
                        }

                        // Conversation Exit - No Available Edges
                        if (m_AvailableNodes.Count == 0)
                        {
                            m_Listener.OnConversationExit(
                                m_Conversation,
                                new(SequenceNumber, this, m_OnReady)
                            );
                            m_CurrentState = MachineState.ConversationExitWait;
                            goto case MachineState.ConversationExitWait;
                        }

                        // Node Decision
                        if (
                            // If we have multiple choices or
                            // we allow single node choices and there's a single choice with UI text
                            // and all nodes use the same actor and we're not preventing a response
                            // *phew*
                            (
                                (m_AvailableNodes.Count > 1)
                                || (
                                    m_AvailableNodes.Count == 1
                                    && !m_Settings.PreventSingleNodeChoices
                                    && m_AvailableNodes[0].UIResponseText != null
                                )
                            )
                            && allEdgesSameActor
                            && !m_Node.IsPreventResponse
                        )
                        {
                            m_Listener.OnNodeDecision(
                                m_AvailableNodes,
                                new(SequenceNumber, this, m_OnDecisionMade)
                            );
                            m_CurrentState = MachineState.NodeDecisionWait;
                            goto case MachineState.NodeDecisionWait;
                        }

                        // Node Enter
                        m_AvailableNodes.Clear();
                        m_Node = highestPriorityNode;
                        m_CurrentState = MachineState.NodeEnter;
                        goto case MachineState.NodeEnter;
                    }
                    case MachineState.NodeDecisionWait:
                    {
                        if (!m_OnDecisionMadeCalled)
                            return true;
                        m_Node = m_OnDecisionMadeValue;
                        m_AvailableNodes.Clear();
                        m_OnDecisionMadeCalled = false;
                        m_OnDecisionMadeValue = null;
                        m_CurrentState = MachineState.NodeEnter;
                        goto case MachineState.NodeEnter;
                    }
                    case MachineState.ConversationExitWait:
                    {
                        if (!m_OnReadyCalled)
                            return true;
                        return false; // Reset is called by Runner
                    }
                    default:
                    {
                        throw new Exception($"Invalid state machine state {m_CurrentState}");
                    }
                }
            }
            catch (Exception e)
            {
                m_Listener.OnError(m_Conversation, e);
                return false; // Reset is called by Runner
            }
        }

        internal void OnReady()
        {
            m_OnReadyCalled = true;
        }

        internal void OnDecisionMade(Node node)
        {
            m_OnDecisionMadeValue = node;
            m_OnDecisionMadeCalled = true;
        }

        private void Reset()
        {
            OnFlagRaised = null;
            SequenceNumber = 0;
            m_Node = null;
            m_Listener = null;
            m_Conversation = null;
            m_CurrentState = MachineState.Idle;
            m_RoutineState.Reset();
            m_OnReadyCalled = false;
            m_AvailableNodes.Clear();
            m_OnDecisionMadeValue = null;
            m_OnDecisionMadeCalled = false;
        }
        #endregion

        #region Conversation
        public Node GetCurrentNode(uint sequenceNumber)
        {
            // If the conversation ended, send back null
            if (sequenceNumber != SequenceNumber)
                return null;
            return m_Node;
        }
        #endregion

        #region Routines
        public void SetConditionResult(bool result) => m_RoutineState.SetConditionResult(result);
        #endregion

        #region Scheduled Blocks
        public void SetBlocksInUse(int blockCount) => m_RoutineState.SetBlocksInUse(blockCount);

        public bool IsBlockExecuted(int blockIndex) => m_RoutineState.IsBlockExecuted(blockIndex);

        public void SetBlockExecuted(int blockIndex) => m_RoutineState.SetBlockExecuted(blockIndex);

        public bool HaveBlockFlagsFired(int blockIndex) =>
            m_RoutineState.HaveBlockFlagsFired(blockIndex);

        public void SetBlockFlagsFired(int blockIndex) =>
            m_RoutineState.SetBlockFlagsFired(blockIndex);

        public Lease AcquireLease(int blockIndex, uint sequenceNumber)
        {
            // If the conversation ended, send back a dummy lease
            if (sequenceNumber != SequenceNumber)
                return m_DummyLease;
            return m_RoutineState.AcquireLease(blockIndex);
        }

        public bool HaveBlockSignalsFired(int blockIndex) =>
            m_RoutineState.HaveBlockSignalsFired(blockIndex);
        #endregion

        #region Flags
        public void SetFlag(int flag)
        {
            m_RoutineState.SetFlag(flag);
            OnFlagRaised?.Invoke(flag);
        }

        public bool IsFlagSet(int flag) => m_RoutineState.IsFlagSet(flag);

        public void SetFlags(int[] flags)
        {
            for (int i = 0; i < flags.Length; i++)
            {
                SetFlag(flags[i]);
            }
        }

        public bool AreFlagsSet(int[] flags)
        {
            for (int i = 0; i < flags.Length; i++)
            {
                if (!IsFlagSet(flags[i]))
                    return false;
            }
            return true;
        }
        #endregion

        #region States
        private enum MachineState
        {
            Idle,
            ConversationEnter,
            ConversationEnterWait,
            NodeEnter,
            NodeEnterWait,
            NodeExecute,
            NodeExit,
            NodeExitWait,
            NodeDecision,
            NodeDecisionWait,
            ConversationExit,
            ConversationExitWait,
        }
        #endregion
    }
}
