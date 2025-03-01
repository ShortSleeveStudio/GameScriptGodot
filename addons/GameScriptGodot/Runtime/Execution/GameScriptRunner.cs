using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace GameScript
{
    public partial class GameScriptRunner : Godot.Node
    {
        #region Inspector Variables
        [Export]
        public int m_ExecutionOrder = 10;

        [Export]
        public GameScriptSettings m_Settings;
        #endregion

        #region State
        // Using linked lists so we can iterate and add
        private LinkedList<RunnerContext> m_ContextsActive;
        private LinkedList<RunnerContext> m_ContextsInactive;
        private Thread m_MainThread;
        private GameScriptDatabase m_Database;
        #endregion

        #region Public API
        public GameScriptDatabase Database => m_Database;

        // TODO: make this async
        public void Initialize()
        {
            // Deserialize database
            if (m_Database != null)
                throw new Exception("Tried to initialize GameScript more than once");
            m_Database = new();
            m_Database.Initialize(m_Settings);
        }

        public ActiveConversation StartConversation(
            ConversationReference conversationRef,
            IGameScriptListener listener
        ) => StartConversation(conversationRef.Id, listener);

        public ActiveConversation StartConversation(
            uint conversationId,
            IGameScriptListener listener
        )
        {
            Conversation conversation = m_Database.FindConversation(conversationId);
            return StartConversation(conversation, listener);
        }

        public ActiveConversation StartConversation(
            Conversation conversation,
            IGameScriptListener listener
        )
        {
            EnsureMainThread();
            RunnerContext context = ContextAcquire();
            context.Start(conversation, listener);
            return new(this, context.SequenceNumber, context.ContextId);
        }

        public void SetFlag(ActiveConversation active, int flag)
        {
            EnsureMainThread();
            RunnerContext ctx = FindContextActive(active);
            if (ctx == null)
                throw new Exception(
                    "You can't set a flag for conversations that have already ended"
                );
            ctx.SetFlag(flag);
        }

        public void SetFlagForAll(int flag)
        {
            LinkedListNode<RunnerContext> node = m_ContextsActive.First;
            while (node != null)
            {
                LinkedListNode<RunnerContext> next = node.Next;
                node.Value.SetFlag(flag);
                node = next;
            }
        }

        public void RegisterFlagListener(ActiveConversation active, Action<int> listener)
        {
            EnsureMainThread();
            RunnerContext ctx = FindContextActive(active);
            if (ctx == null)
                throw new Exception(
                    "You can't register a flag listener on a conversation that's already ended"
                );
            ctx.OnFlagRaised += listener;
        }

        public void UnregisterFlagListener(ActiveConversation active, Action<int> listener)
        {
            EnsureMainThread();
            RunnerContext ctx = FindContextActive(active);
            if (ctx == null)
                return;
            ctx.OnFlagRaised -= listener;
        }

        public bool IsActive(ActiveConversation active)
        {
            EnsureMainThread();
            RunnerContext ctx = FindContextActive(active);
            return ctx != null;
        }

        public void StopConversation(ActiveConversation active)
        {
            EnsureMainThread();
            RunnerContext ctx = FindContextActive(active);
            if (ctx == null)
                // we assume the conversation is already ended. Thus this call is idempotent.
                return;
            ContextRelease(ctx);
        }

        public void StopAllConversations()
        {
            EnsureMainThread();
            LinkedListNode<RunnerContext> node = m_ContextsActive.First;
            while (node != null)
            {
                LinkedListNode<RunnerContext> next = node.Next;
                ContextRelease(node);
                node = next;
            }
        }
        #endregion

        #region Godot Lifecycle Methods
        public override void _Ready()
        {
            // Initialize runtime state
            m_MainThread = Thread.CurrentThread;
            m_ContextsActive = new();
            m_ContextsInactive = new();
            for (uint i = 0; i < m_Settings.InitialConversationPool; i++)
            {
                m_ContextsInactive.AddLast(new RunnerContext(m_Settings));
            }
        }

        public override void _Process(double delta)
        {
            if (m_ContextsActive.Count == 0)
                return;
            LinkedListNode<RunnerContext> iterator = m_ContextsActive.First;
            do
            {
                // Grab value
                RunnerContext runnerContext = iterator.Value;

                // Iterate
                iterator = iterator.Next;

                // Handle current context
                bool isConversationActive = runnerContext.Tick();
                if (!isConversationActive)
                    ContextRelease(runnerContext);
            } while (iterator != null);
        }
        #endregion

        #region Helpers
        private RunnerContext ContextAcquire()
        {
            RunnerContext context;
            if (m_ContextsInactive.Count == 0)
            {
                context = new(m_Settings);
                m_ContextsActive.AddLast(context);
            }
            else
            {
                // We add to and remove from the end of the inactive list so that the
                // "primed" contexts are used more frequently. It's more likely that
                // the oft-used contexts will have more room for signals/blocks.
                LinkedListNode<RunnerContext> node = m_ContextsInactive.Last;
                m_ContextsInactive.RemoveLast();
                m_ContextsActive.AddLast(node);
                context = node.Value;
            }
            return context;
        }

        private void ContextRelease(RunnerContext context)
        {
            LinkedListNode<RunnerContext> node = m_ContextsActive.Find(context);
            // Idempotent
            if (node != null)
                ContextRelease(node);
        }

        private void ContextRelease(LinkedListNode<RunnerContext> node)
        {
            node.Value.Stop();
            m_ContextsActive.Remove(node);
            m_ContextsInactive.AddLast(node);
        }

        private RunnerContext FindContextActive(ActiveConversation active)
        {
            LinkedListNode<RunnerContext> node = m_ContextsActive.First;
            while (node != null)
            {
                LinkedListNode<RunnerContext> next = node.Next;
                if (node.Value.ContextId == active.ContextId)
                {
                    if (node.Value.SequenceNumber != active.SequenceNumber)
                        return null;
                    return node.Value;
                }
                node = next;
            }
            return null;
        }

        private void EnsureMainThread()
        {
            if (m_MainThread != Thread.CurrentThread)
                throw new Exception("Runner APIs can only be used from the main thread");
        }
        #endregion
    }
}
