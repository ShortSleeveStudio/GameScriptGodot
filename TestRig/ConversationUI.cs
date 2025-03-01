using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameScript;
using Godot;

public partial class ConversationUI : Godot.Node, IGameScriptListener
{
    #region Constants
    private const int k_ReadTimeMillis = 1000;
    #endregion

    #region Inspector Variables
    [Export]
    public Godot.Node HistoryContent;

    [Export]
    public Godot.Node ChoiceContent;
    #endregion

    #region State
    private Action<ConversationUI> m_OnComplete;
    private ActiveConversation m_ActiveConversation;
    private GameScriptRunner m_GameScriptRunner;
    private PackedScene m_HistoryItemPackedScene;
    private PackedScene m_ChoiceItemPackedScene;
    private TestRigSettings m_TestSettings;
    #endregion

    #region Initialization
    public void Initialize(
        uint conversationId,
        Action<ConversationUI> onComplete,
        PackedScene historyItemPackedScene,
        PackedScene choiceItemPackedScene,
        TestRigSettings TestSettings,
        GameScriptRunner Runner
    )
    {
        m_GameScriptRunner = Runner;
        m_OnComplete = onComplete;
        m_HistoryItemPackedScene = historyItemPackedScene;
        m_ChoiceItemPackedScene = choiceItemPackedScene;
        m_TestSettings = TestSettings;
        m_ActiveConversation = m_GameScriptRunner.StartConversation(conversationId, this);
    }
    #endregion

    #region Handlers
    public void Stop()
    {
        m_GameScriptRunner.StopConversation(m_ActiveConversation);
        m_OnComplete(this);
    }
    #endregion

    #region Runner Listerner
    public void OnConversationEnter(Conversation conversation, ReadyNotifier readyNotifier)
    {
        readyNotifier.OnReady();
    }

    public void OnConversationExit(Conversation conversation, ReadyNotifier readyNotifier)
    {
        int childCount = HistoryContent.GetChildCount() - 1;
        for (int i = childCount; i >= 0; i--)
        {
            HistoryContent.GetChild(i).QueueFree();
        }
        readyNotifier.OnReady();
        m_OnComplete(this);
    }

    public void OnNodeEnter(GameScript.Node node, ReadyNotifier readyNotifier)
    {
        if (node.VoiceText != null)
        {
            HistoryItemUI historyItem = m_HistoryItemPackedScene.Instantiate() as HistoryItemUI;
            string actorName =
                node.Actor.LocalizedName != null
                    ? node.Actor.LocalizedName.GetLocalization(m_TestSettings.CurrentLocale)
                    : "<Player Name Missing>";
            string voiceText = node.VoiceText.GetLocalization(m_TestSettings.CurrentLocale);
            historyItem.SetVoiceText(voiceText);
            historyItem.SetActorName(actorName);
            HistoryContent.AddChild(historyItem);
            Delay(k_ReadTimeMillis, readyNotifier);
        }
        else
            readyNotifier.OnReady();
    }

    public void OnNodeExit(List<GameScript.Node> nodes, DecisionNotifier decisionNotifier)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            GameScript.Node node = nodes[i];
            ChoiceUI choiceUI = m_ChoiceItemPackedScene.Instantiate() as ChoiceUI;
            string buttonText = "";
            if (node.UIResponseText != null)
                buttonText = node.UIResponseText.GetLocalization(m_TestSettings.CurrentLocale);
            choiceUI.SetButtonText(buttonText);
            choiceUI.RegisterButtonHandler(() =>
            {
                decisionNotifier.OnDecisionMade(node);
            });
            ChoiceContent.AddChild(choiceUI);
        }
    }

    public void OnNodeExit(GameScript.Node node, ReadyNotifier readyNotifier)
    {
        int childCount = ChoiceContent.GetChildCount() - 1;
        for (int i = childCount; i >= 0; i--)
        {
            ChoiceContent.GetChild(i).QueueFree();
        }
        readyNotifier.OnReady();
    }

    public void OnError(Conversation conversation, Exception e) => GD.PushError(e);
    #endregion

    #region Helpers
    private async void Delay(int millis, ReadyNotifier readyNotifier)
    {
        await Task.Delay(millis);
        readyNotifier.OnReady();
    }
    #endregion
}
