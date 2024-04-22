using GameScript;
using Godot;

public partial class Tester : Godot.Node
{
    #region Inspector Variables
    [Export]
    public Godot.Node ConversationContent;

    [Export]
    public TestRigSettings TestSettings;

    [Export]
    public ConversationReference ConversationReference;

    [Export]
    public LocaleReference LocaleReference;
    #endregion

    #region Private State
    private PackedScene m_ChoiceItemPackedScene;
    private PackedScene m_HistoryItemPackedScene;
    private PackedScene m_ConversationPackedScene;
    #endregion

    #region Godot Lifecycle Methods
    public override void _Ready()
    {
        // Preload packed scenes
        m_ChoiceItemPackedScene = GD.Load<PackedScene>("res://TestRig/Nodes/ChoiceItem.tscn");
        m_HistoryItemPackedScene = GD.Load<PackedScene>("res://TestRig/Nodes/HistoryItem.tscn");
        m_ConversationPackedScene = GD.Load<PackedScene>("res://TestRig/Nodes/Conversation.tscn");

        // TODO - make this selectable
        OnLocaleSelected();
    }
    #endregion

    #region Handlers
    public void OnStartPressed()
    {
        ConversationReference conversation = ConversationReference;
        uint conversationId = conversation.Id;

        // Add Conversation to UI
        ConversationUI conversationUI = m_ConversationPackedScene.Instantiate() as ConversationUI;
        conversationUI.Initialize(
            conversationId,
            OnConversationFinished,
            m_HistoryItemPackedScene,
            m_ChoiceItemPackedScene,
            TestSettings
        );
        ConversationContent.AddChild(conversationUI);
    }

    public void OnLocaleSelected()
    {
        TestSettings.CurrentLocale = Database.FindLocale(LocaleReference.Id);
    }

    public void OnConversationFinished(ConversationUI conversationUI)
    {
        conversationUI.QueueFree();
    }
    #endregion
}
