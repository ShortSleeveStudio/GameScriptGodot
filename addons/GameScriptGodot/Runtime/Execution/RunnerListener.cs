using System;
using System.Collections.Generic;

namespace GameScript
{
    /**
     * Call OnReady to signal that the conversation runner can proceed.
     * IsValid() tells you if the conversation was stopped.
     */
    public struct ReadyNotifier
    {
        private bool m_Signalled;
        private Action m_OnReady;
        private RunnerContext m_Context;
        private uint m_OriginalSequenceNumber;

        internal ReadyNotifier(uint sequenceNumber, RunnerContext context, Action onReady)
        {
            m_Signalled = false;
            m_OnReady = onReady;
            m_Context = context;
            m_OriginalSequenceNumber = sequenceNumber;
        }

        public bool IsValid() =>
            m_Context.SequenceNumber == m_OriginalSequenceNumber && !m_Signalled;

        public void OnReady()
        {
            if (!IsValid())
            {
                return;
            }
            m_OnReady();
        }
    }

    /**
     * Call OnDecisionMade to signal that the conversation runner can proceed with the selected
     * node.
     * IsValid() tells you if the conversation was stopped.
     */
    public struct DecisionNotifier
    {
        private bool m_Signalled;
        private Action<Node> m_OnDecisionMade;
        private RunnerContext m_Context;
        private uint m_OriginalSequenceNumber;

        internal DecisionNotifier(
            uint sequenceNumber,
            RunnerContext context,
            Action<Node> onDecisionMade
        )
        {
            m_Signalled = false;
            m_OnDecisionMade = onDecisionMade;
            m_Context = context;
            m_OriginalSequenceNumber = sequenceNumber;
        }

        public bool IsValid() =>
            m_Context.SequenceNumber == m_OriginalSequenceNumber && !m_Signalled;

        public void OnDecisionMade(Node node)
        {
            if (!IsValid())
                return;
            m_Signalled = true;
            m_OnDecisionMade(node);
        }
    }

    /**Runner listeners can react to changes in conversation runner state.*/
    public interface IRunnerListener
    {
        /**
         * Called before the conversation starts.
         */
        public void OnConversationEnter(Conversation conversation, ReadyNotifier readyNotifier);

        /**
         * Called before a node's routine is executed.
         */
        public void OnNodeEnter(Node node, ReadyNotifier readyNotifier);

        /**
         * Called when a decision must be made to proceed with the conversation.
         */
        public void OnNodeDecision(List<Node> nodes, DecisionNotifier decisionNotifier);

        /**
         * Called before proceeding to find the next available node.
         */
        public void OnNodeExit(Node node, ReadyNotifier readyNotifier);

        /**
         * Called before a conversation ends.
         */
        public void OnConversationExit(Conversation conversation, ReadyNotifier readyNotifier);

        /**
         * Called when an error occurs.
         */
        public void OnError(Conversation conversation, Exception e);
    }
}
