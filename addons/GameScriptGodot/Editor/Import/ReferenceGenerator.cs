using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Microsoft.Data.Sqlite;

namespace GameScript
{
    static class ReferenceGenerator
    {
        public static ReferenceGeneratorResult GenerateAssetReferences(
            string dbPath,
            string outputDirectory,
            string refDir
        )
        {
            ReferenceGeneratorResult result = new();
            string oldDir;
            string tmpDir = null;
            try
            {
                // Ensure other directories are in the proper state
                tmpDir = PathCombine(outputDirectory, EditorConstants.k_ReferencesTmpFolder);
                if (Directory.Exists(tmpDir))
                    throw new Exception(
                        $"Temporary folder from a previous import still exists: {tmpDir}"
                    );
                oldDir = PathCombine(outputDirectory, EditorConstants.k_ReferencesOldFolder);
                if (Directory.Exists(oldDir))
                    throw new Exception(
                        $"Broken references folder from a previous import still exists: {oldDir}"
                    );

                // Create temporary directory
                Directory.CreateDirectory(tmpDir);

                // Load all previous references in lookup tables
                ReferenceTables referenceTables = LoadAllReferences(refDir);

                // Create the references
                ReferenceGenerateResult genResult = CreateReferences(
                    dbPath,
                    tmpDir,
                    referenceTables
                );

                // Preserve the old directory if there are broken references
                if (genResult.BrokenReferences)
                {
                    GD.PushWarning(
                        $"Found {RuntimeConstants.k_AppName} references that no longer"
                            + $" exist. They will be left in the following directory: {oldDir}"
                    );
                    Directory.Move(refDir, oldDir);
                }
                // Delete the old heirarchy since it's empty of references
                else if (Directory.Exists(refDir))
                {
                    Directory.Delete(refDir, true);
                }

                // The temporary directory becomes the new references directory
                Directory.Move(tmpDir, refDir);
            }
            catch (Exception e)
            {
                GD.PushError(e);
                if (tmpDir != null && Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);
                result.WasError = true;
            }
            return result;
        }

        private static ReferenceGenerateResult CreateReferences(
            string dbPath,
            string tmpDir,
            ReferenceTables referenceTables
        )
        {
            using (SqliteConnection connection = new(DbHelper.SqlitePathToURI(dbPath)))
            {
                connection.Open();
                ReferenceGenerateResult convResult = CreateConversationReferences(
                    connection,
                    tmpDir,
                    referenceTables
                );
                ReferenceGenerateResult localeResult = CreateLocaleReferences(
                    connection,
                    tmpDir,
                    referenceTables
                );
                ReferenceGenerateResult actorResult = CreateActorReferences(
                    connection,
                    tmpDir,
                    referenceTables
                );

                return new()
                {
                    BrokenReferences =
                        convResult.BrokenReferences
                        || localeResult.BrokenReferences
                        || actorResult.BrokenReferences,
                };
            }
        }

        #region Conversations & Localizations
        private static ReferenceGenerateResult CreateConversationReferences(
            SqliteConnection connection,
            string tmpDir,
            ReferenceTables referenceTables
        )
        {
            // Create conversations directory
            string conversationsDir = PathCombine(
                tmpDir,
                EditorConstants.k_ReferencesConversationsFolder
            );
            Directory.CreateDirectory(conversationsDir);

            // Grab all filter names
            string[] filterNames = null;
            ImportHelpers.ReadTable(
                connection,
                Filters.TABLE_NAME,
                (uint count) => filterNames = new string[count],
                (uint index, SqliteDataReader filterReader) =>
                {
                    Filters filter = Filters.FromReader(filterReader);
                    filterNames[index] = filter.name;
                }
            );

            ImportHelpers.ReadTable(
                connection,
                Conversations.TABLE_NAME,
                null,
                (uint index, SqliteDataReader convReader) =>
                {
                    // Load conversation
                    Conversations conversation = Conversations.FromReader(convReader);

                    // Construct directory heirarchy
                    // +1 for root directory
                    string[] pathSegments = new string[conversation.filters.Length + 1];
                    pathSegments[0] = conversationsDir;
                    string containingDirectory = conversationsDir;
                    for (int i = 0; i < conversation.filters.Length; i++)
                    {
                        string filterValue = conversation.filters[i];
                        if (string.IsNullOrEmpty(filterValue))
                            filterValue = "";
                        string pathSegment = filterNames[i] + "_" + SanitizeFileName(filterValue);
                        pathSegments[i + 1] = pathSegment;
                        string newDir = PathCombine(containingDirectory, pathSegment);
                        if (!Directory.Exists(newDir))
                            Directory.CreateDirectory(newDir);
                        containingDirectory = newDir;
                    }

                    // Construct directory and file names
                    string conversationName = CreateConversationName(conversation);
                    string conversationLocFolderName =
                        conversationName + EditorConstants.k_LocalizationReferenceFolderSuffix;
                    string conversationPath =
                        PathCombine(containingDirectory, conversationName) + ".tres";
                    string conversationLocFolderPath = PathCombine(
                        containingDirectory,
                        conversationLocFolderName
                    );

                    // Attempt to create the conversation reference or move it if it already exists
                    MoveOrCreateAsset(
                        referenceTables.IdToConversationRef,
                        (uint)conversation.id,
                        conversationPath
                    );

                    // Load all localizations for this conversation that aren't system created
                    bool locFolderCreated = false;
                    ImportHelpers.ReadTable(
                        connection,
                        Localizations.TABLE_NAME,
                        null,
                        (uint index, SqliteDataReader locReader) =>
                        {
                            // Grab localization
                            Localizations localization = Localizations.FromReader(locReader);
                            string localizationName = CreateLocalizationName(localization);

                            // Create new containing directory
                            if (!locFolderCreated)
                            {
                                locFolderCreated = true;
                                Directory.CreateDirectory(conversationLocFolderPath);
                            }

                            // Create localization reference
                            MoveOrCreateAsset(
                                referenceTables.IdToLocalizationRef,
                                (uint)localization.id,
                                PathCombine(conversationLocFolderPath, localizationName) + ".tres"
                            );
                        },
                        $"WHERE parent = {conversation.id} AND is_system_created = false"
                    );
                }
            );

            // Handle global localizations
            string globalLocalizationFolder = PathCombine(
                conversationsDir,
                EditorConstants.k_LocalizationGlobalFolder
            );
            Directory.CreateDirectory(globalLocalizationFolder);
            ImportHelpers.ReadTable(
                connection,
                Localizations.TABLE_NAME,
                null,
                (uint index, SqliteDataReader locReader) =>
                {
                    Localizations localization = Localizations.FromReader(locReader);
                    string localizationName = CreateLocalizationName(localization);
                    string localizationPath =
                        PathCombine(globalLocalizationFolder, localizationName) + ".tres";
                    MoveOrCreateAsset(
                        referenceTables.IdToLocalizationRef,
                        (uint)localization.id,
                        localizationPath
                    );
                },
                "WHERE parent IS NULL and is_system_created = false"
            );

            // See if we have any broken references
            return new()
            {
                BrokenReferences =
                    referenceTables.IdToConversationRef.Count > 0
                    || referenceTables.IdToLocalizationRef.Count > 0
            };
        }
        #endregion

        #region Locales
        private static ReferenceGenerateResult CreateLocaleReferences(
            SqliteConnection connection,
            string tmpDir,
            ReferenceTables referenceTables
        )
        {
            // Create locale directory
            string localesDir = PathCombine(tmpDir, EditorConstants.k_ReferencesLocalesFolder);
            Directory.CreateDirectory(localesDir);

            ImportHelpers.ReadTable(
                connection,
                Locales.TABLE_NAME,
                null,
                (uint index, SqliteDataReader actorReader) =>
                {
                    // Load locale
                    Locales locale = Locales.FromReader(actorReader);

                    // Construct path
                    string actorPath = PathCombine(localesDir, CreateLocaleName(locale)) + ".tres";

                    // Attempt to create the asset or move it if it already exists
                    MoveOrCreateAsset(referenceTables.IdToLocaleRef, (uint)locale.id, actorPath);
                }
            );

            return new() { BrokenReferences = referenceTables.IdToLocaleRef.Count > 0 };
        }
        #endregion

        #region Actors
        private static ReferenceGenerateResult CreateActorReferences(
            SqliteConnection connection,
            string tmpDir,
            ReferenceTables referenceTables
        )
        {
            // Create actor directory
            string actorsDir = PathCombine(tmpDir, EditorConstants.k_ReferencesActorsFolder);
            Directory.CreateDirectory(actorsDir);

            ImportHelpers.ReadTable(
                connection,
                Actors.TABLE_NAME,
                null,
                (uint index, SqliteDataReader actorReader) =>
                {
                    // Load actor
                    Actors actor = Actors.FromReader(actorReader);

                    // Construct path
                    string actorPath = PathCombine(actorsDir, CreateActorName(actor)) + ".tres";

                    // Attempt to create the asset or move it if it already exists
                    MoveOrCreateAsset(referenceTables.IdToActorRef, (uint)actor.id, actorPath);
                }
            );

            return new() { BrokenReferences = referenceTables.IdToActorRef.Count > 0 };
        }
        #endregion

        #region Helpers
        private static ReferenceTables LoadAllReferences(string path)
        {
            ReferenceTables referenceTables = new();
            foreach (
                string file in Directory.EnumerateFiles(path, "*.tres", SearchOption.AllDirectories)
            )
            {
                Resource resource = ResourceLoader.Singleton.Load(file);
                switch (resource)
                {
                    case ActorReference actorReference:
                        referenceTables.IdToActorRef[actorReference.Id] = new(actorReference, file);
                        break;
                    case ConversationReference conferenceReference:
                        referenceTables.IdToConversationRef[conferenceReference.Id] = new(
                            conferenceReference,
                            file
                        );
                        break;
                    case LocaleReference localeReference:
                        referenceTables.IdToLocaleRef[localeReference.Id] = new(
                            localeReference,
                            file
                        );
                        break;
                    case LocalizationReference localizationReference:
                        referenceTables.IdToLocalizationRef[localizationReference.Id] = new(
                            localizationReference,
                            file
                        );
                        break;
                }
            }
            return referenceTables;
        }

        private static string CreateLocalizationName(Localizations localization)
        {
            string localizationName = string.IsNullOrEmpty(localization.name)
                ? ""
                : SanitizeFileName(localization.name);
            return "l" + localization.id + "_" + localizationName;
        }

        private static string CreateConversationName(Conversations conversation)
        {
            string conversationName = string.IsNullOrEmpty(conversation.name)
                ? ""
                : SanitizeFileName(conversation.name);
            return "c" + conversation.id + "_" + conversationName;
        }

        private static string CreateLocaleName(Locales locale)
        {
            string localeName = string.IsNullOrEmpty(locale.name)
                ? ""
                : SanitizeFileName(locale.name);
            return "L" + locale.id + "_" + localeName;
        }

        private static string CreateActorName(Actors actor)
        {
            string actorName = string.IsNullOrEmpty(actor.name) ? "" : SanitizeFileName(actor.name);
            return "a" + actor.id + "_" + actorName;
        }

        private static void MoveOrCreateAsset<T>(
            Dictionary<uint, AssetInfo<T>> map,
            uint assetId,
            string outputPath
        )
            where T : Reference, new()
        {
            AssetInfo<T> refInfo;
            map.TryGetValue(assetId, out refInfo);
            if (refInfo != null)
            {
                File.Move(refInfo.AssetPath, outputPath);
            }
            else
            {
                T resource = new() { Id = assetId };
                ResourceSaver.Save(resource, outputPath);
            }

            // Clear out the map as we go so we know about references that no longer exist at the
            // end
            map.Remove(assetId);
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalids = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries))
                .TrimEnd('.');
        }

        private static string PathCombine(params string[] paths)
        {
            return string.Join('/', paths);
        }
        #endregion

        #region Helper Classes
        private class ReferenceGenerateResult
        {
            public bool BrokenReferences;
        }

        private class AssetInfo<T>
            where T : Reference
        {
            public AssetInfo(T asset, string assetPath)
            {
                Asset = asset;
                AssetPath = assetPath;
            }

            public T Asset;
            public string AssetPath;
        }

        private class ReferenceTables
        {
            public ReferenceTables()
            {
                IdToActorRef = new();
                IdToLocaleRef = new();
                IdToConversationRef = new();
                IdToLocalizationRef = new();
            }

            public Dictionary<uint, AssetInfo<ActorReference>> IdToActorRef;
            public Dictionary<uint, AssetInfo<LocaleReference>> IdToLocaleRef;
            public Dictionary<uint, AssetInfo<ConversationReference>> IdToConversationRef;
            public Dictionary<uint, AssetInfo<LocalizationReference>> IdToLocalizationRef;
        }
        #endregion
    }
}
