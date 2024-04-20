#if TOOLS
using Godot;

namespace GameScript
{
    public partial class GameScriptSettingsInspectorButton : EditorProperty
    {
        private Button _ImportButton;

        public GameScriptSettingsInspectorButton()
        {
            _ImportButton = new() { Text = "Import Database" };
            AddChild(_ImportButton);
            _ImportButton.Pressed += OnImport;
        }

        private void OnImport()
        {
            if (GetEditedObject() is GameScriptSettings settings)
            {
                DatabaseImporter.ImportDatabase(settings);
            }
        }

        #region Developement Only
        private void GenerateDBCode()
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
        #endregion
    }
}
#endif
