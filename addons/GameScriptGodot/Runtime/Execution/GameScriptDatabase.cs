using System;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Godot;

namespace GameScript
{
    public class GameScriptDatabase
    {
        #region State
        private Locale m_BinarySearchLocale;
        private GameData m_GameData;
        private Localization m_BinarySearchLocalization;
        private Conversation m_BinarySearchConversation;
        private Actor m_BinarySearchActor;
        private EmptyProperty m_BinarySearchProperty;
        #endregion

        #region Constructor
        internal GameScriptDatabase()
        {
            m_BinarySearchLocale = new();
            m_BinarySearchProperty = new("");
            m_BinarySearchLocalization = new();
            m_BinarySearchConversation = new();
            m_BinarySearchActor = new();
        }
        #endregion

        #region Public API
        public GameData GameData => m_GameData;

        public Localization FindLocalization(uint localizationId) =>
            Find(localizationId, m_BinarySearchLocalization, m_GameData.Localizations);

        public Locale FindLocale(uint localeId) =>
            Find(localeId, m_BinarySearchLocale, m_GameData.Locales);

        public Conversation FindConversation(uint conversationId) =>
            Find(conversationId, m_BinarySearchConversation, m_GameData.Conversations);

        public Actor FindActor(uint actorId) =>
            Find(actorId, m_BinarySearchActor, m_GameData.Actors);

        public Property FindProperty(Property[] properties, string propertyName)
        {
            m_BinarySearchProperty.SetName(propertyName);
            int index = Array.BinarySearch(properties, m_BinarySearchProperty);
            if (index == -1)
                return null;
            return properties[index];
        }
        #endregion

        #region Internal API
        internal void Initialize(GameScriptSettings settings)
        {
            using (
                FileAccess file = FileAccess.Open(
                    settings.DataFilePathRelative,
                    FileAccess.ModeFlags.Read
                )
            )
            {
                // Compose Response
                byte[] result = file.GetBuffer((long)file.GetLength());
                BinaryFormatter serializer = new() { Binder = new CustomSerializationBinder() };
                using (System.IO.MemoryStream dataStream = new System.IO.MemoryStream(result))
                {
                    using (GZipStream zipStream = new(dataStream, CompressionMode.Decompress))
                    {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                        m_GameData = (GameData)serializer.Deserialize(zipStream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                    }
                }
            }
        }
        #endregion

        #region Private API
        private T Find<T>(uint id, T searchBuddy, T[] arr)
            where T : BaseData<T>
        {
            searchBuddy.Id = id;
            int index = Array.BinarySearch(arr, searchBuddy);
            return arr[index];
        }
        #endregion
    }
}
