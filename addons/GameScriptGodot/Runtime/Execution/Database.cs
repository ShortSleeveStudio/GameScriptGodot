using System;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Godot;

namespace GameScript
{
    public class Database
    {
        #region Singleton
        private static GameData m_Instance;
        public static GameData Instance
        {
            get
            {
                if (m_Instance == null)
                    throw new Exception("Must call Initialize() on database before use");
                return m_Instance;
            }
        }

        public static void Initialize(GameScriptSettings settings)
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
                        m_Instance = (GameData)serializer.Deserialize(zipStream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                    }
                }
            }
        }
        #endregion

        #region Static State
        private static Locale s_BinarySearchLocale = new();
        private static Localization s_BinarySearchLocalization = new();
        private static Conversation s_BinarySearchConversation = new();
        private static EmptyProperty s_BinarySearchProperty = new("");
        #endregion

        #region Static Methods
        public static Localization FindLocalization(uint localizationId) =>
            Find(localizationId, s_BinarySearchLocalization, Instance.Localizations);

        public static Locale FindLocale(uint localeId) =>
            Find(localeId, s_BinarySearchLocale, Instance.Locales);

        public static Conversation FindConversation(uint conversationId) =>
            Find(conversationId, s_BinarySearchConversation, Instance.Conversations);

        public static Property FindProperty(Property[] properties, string propertyName)
        {
            s_BinarySearchProperty.SetName(propertyName);
            int index = Array.BinarySearch(properties, s_BinarySearchProperty);
            if (index == -1)
                return null;
            return properties[index];
        }

        private static T Find<T>(uint id, T searchBuddy, T[] arr)
            where T : BaseData<T>
        {
            searchBuddy.Id = id;
            int index = Array.BinarySearch(arr, searchBuddy);
            return arr[index];
        }
        #endregion
    }
}
