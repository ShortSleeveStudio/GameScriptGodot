using Godot;

namespace GameScript
{
    [Tool]
    public partial class GameScriptGodotPane : Control
    {
        [Export]
        public Button TestButton;

        [Export]
        public TextEdit OutputPathLabel;

        [Export]
        public Button OutputPathButton;

        [Export]
        public Resource Settings;

        public override void _Ready()
        {
            TestButton.Pressed += OnGenerateDBCode;
            OutputPathButton.Pressed += OnSelectOutputPath;
            Settings.Changed += OnSettingsChanged;
        }

        private void OnSelectOutputPath()
        {
            GD.Print("YEAH OK" + Settings);
            GameScriptSettings settings = (GameScriptSettings)Settings;
            GD.Print("YEAH it is");
            OutputPathButton.Text = settings.OutputPath;
        }

        private void OnSettingsChanged()
        {
            GD.Print("YEAH Settings Changed" + Settings);
            GameScriptSettings settings = (GameScriptSettings)Settings;
            GD.Print("YEAH it is");
            OutputPathButton.Text = settings.OutputPath;
        }

        private void OnGenerateDBCode()
        {
            string sqliteDatabasePath = "C:/Users/emful/Desktop/DATABASE/GameScript.db";
            string dbCodeDirectory = ProjectSettings.GlobalizePath(
                EditorConstants.k_GeneratedDBCodeFolder
            );
            DbCodeGeneratorResult result = DatabaseCodeGenerator.GenerateDatabaseCode(
                sqliteDatabasePath,
                dbCodeDirectory
            );

            if (!result.WasError)
                EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }
    }
}
