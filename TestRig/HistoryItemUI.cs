using Godot;

public partial class HistoryItemUI : Node
{
    #region Inspector Variables
    [Export]
    public Label ActorText;

    [Export]
    public Label VoiceText;
    #endregion

    #region API
    public void SetActorName(string actorName)
    {
        ActorText.Text = actorName;
    }

    public void SetVoiceText(string voiceText)
    {
        VoiceText.Text = voiceText;
    }
    #endregion
}
