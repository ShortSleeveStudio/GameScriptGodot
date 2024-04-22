using System;
using Godot;

public partial class ChoiceUI : Button
{
    #region API
    public void SetButtonText(string text) => Text = text;

    public void RegisterButtonHandler(Action onButtonPress) => Pressed += onButtonPress;

    public void UnregisterButtonHandler(Action onButtonPress) => Pressed -= onButtonPress;
    #endregion
}
