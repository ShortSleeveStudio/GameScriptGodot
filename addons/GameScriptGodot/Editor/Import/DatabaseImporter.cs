using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Microsoft.Data.Sqlite;

namespace GameScript
{
    static class DatabaseImporter
    {
        #region Variables
        public static bool IsImporting { get; private set; } = false;
        #endregion

        #region API
        public static string GetDatabaseVersion(string sqliteDatabasePath)
        {
            using (SqliteConnection connection = new(DbHelper.SqlitePathToURI(sqliteDatabasePath)))
            {
                // Open connection
                connection.Open();

                // Fetch table names
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = $"SELECT version FROM {Version.TABLE_NAME};";
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            return reader.GetString(0);
                    }
                }
            }
            return null;
        }

        public static async void ImportDatabase(GameScriptSettings settings)
        {
            try
            {
                if (IsImporting)
                    return;
                IsImporting = true;

                // Ensure Directory Structure
                if (EnsureDirectoryStructure(settings))
                    GD.PushError($"Output path was invalid: {settings.OutputPathAbsolute}");

                TranspilerResult transpilerResult = default;
                ConversationDataGeneratorResult conversationResult = default;
                ReferenceGeneratorResult assetResult = default;
                await Task.Run(() =>
                {
                    transpilerResult = Transpiler.Transpile(
                        settings.DatabasePath,
                        settings.CodePathAbsolute
                    );
                    if (transpilerResult.WasError)
                        return;
                    conversationResult = ConversationDataGenerator.GenerateConversationData(
                        settings.DatabasePath,
                        settings.DataPathAbsolute,
                        transpilerResult.RoutineIdToIndex
                    );
                });

                // Check for errors
                if (transpilerResult.WasError || conversationResult.WasError)
                    return;

                // Create asset references (must be main thread)
                assetResult = ReferenceGenerator.GenerateAssetReferences(
                    settings.DatabasePath,
                    settings.OutputPathAbsolute,
                    settings.ReferencesPathAbsolute
                );
                if (assetResult.WasError)
                    return;

                // Update Settings
                settings.MaxFlags = transpilerResult.MaxFlags;

                // Refresh Database
                EditorInterface.Singleton.GetResourceFilesystem().Scan();
            }
            catch (Exception e)
            {
                GD.PushError(e);
            }
            finally
            {
                IsImporting = false;
            }
        }

        private static bool EnsureDirectoryStructure(GameScriptSettings settings)
        {
            if (!Directory.Exists(settings.OutputPathAbsolute))
                return false;

            if (!Directory.Exists(settings.CodePathAbsolute))
                Directory.CreateDirectory(settings.CodePathAbsolute);

            if (!Directory.Exists(settings.DataPathAbsolute))
                Directory.CreateDirectory(settings.DataPathAbsolute);

            if (!Directory.Exists(settings.ReferencesPathAbsolute))
                Directory.CreateDirectory(settings.ReferencesPathAbsolute);

            return true;
        }
        #endregion
    }
}
