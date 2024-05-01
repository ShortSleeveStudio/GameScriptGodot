using Godot;

namespace GameScript
{
    [Tool]
    [GlobalClass]
    public partial class GameScriptSettings : Resource
    {
        #region Runtime Settings
        [Export]
        public uint MaxFlags
        {
            get => _MaxFlags;
            set
            {
                _MaxFlags = value;
                EmitChanged();
            }
        }
        private uint _MaxFlags;

        [Export]
        public uint InitialConversationPool
        {
            get => _InitialConversationPool;
            set
            {
                _InitialConversationPool = value;
                EmitChanged();
            }
        }
        private uint _InitialConversationPool = 1;

        [Export]
        public bool PreventSingleNodeChoices
        {
            get => _PreventSingleNodeChoices;
            set
            {
                _PreventSingleNodeChoices = value;
                EmitChanged();
            }
        }
        private bool _PreventSingleNodeChoices;
        #endregion

        #region Editor Settings
        [Export]
        public string OutputPath
        {
            get => _OutputPath;
            set
            {
                if (!value.StartsWith(RuntimeConstants.k_ResourcePrefix))
                    value = RuntimeConstants.k_ResourcePrefix;
                _OutputPath = value;

                // Absolute Paths
                _OutputPathAbsolute = ProjectSettings.GlobalizePath(value);
                _DataPathAbsolute = _OutputPathAbsolute + "/" + RuntimeConstants.k_DataSubDirectory;
                _CodePathAbsolute = _OutputPathAbsolute + "/" + RuntimeConstants.k_CodeSubDirectory;
                _ReferencesPathAbsolute =
                    _OutputPathAbsolute + "/" + RuntimeConstants.k_ReferencesSubDirectory;

                // Relative Paths
                _OutputPathRelative = value.Substring(RuntimeConstants.k_ResourcePrefix.Length);
                _DataPathRelative = _OutputPathRelative + "/" + RuntimeConstants.k_DataSubDirectory;
                _CodePathRelative = _OutputPathRelative + "/" + RuntimeConstants.k_CodeSubDirectory;
                _DataFilePathRelative =
                    _DataPathRelative + "/" + RuntimeConstants.k_ConversationDataFilename;
                _ReferencesPathRelative =
                    _OutputPathRelative + "/" + RuntimeConstants.k_ReferencesSubDirectory;

                EmitChanged();
            }
        }
        private string _OutputPath;

        private string _OutputPathAbsolute;
        private string _DataPathAbsolute;
        private string _CodePathAbsolute;
        private string _ReferencesPathAbsolute;
        public string DataPathAbsolute => _DataPathAbsolute;
        public string CodePathAbsolute => _CodePathAbsolute;
        public string OutputPathAbsolute => _OutputPathAbsolute;
        public string ReferencesPathAbsolute => _ReferencesPathAbsolute;

        private string _DataPathRelative;
        private string _CodePathRelative;
        private string _OutputPathRelative;
        private string _DataFilePathRelative;
        private string _ReferencesPathRelative;
        public string DataPathRelative => _DataPathRelative;
        public string CodePathRelative => _CodePathRelative;
        public string OutputPathRelative => _OutputPathRelative;
        public string DataFilePathRelative => _DataFilePathRelative;
        public string ReferencesPathRelative => _ReferencesPathRelative;

        [Export]
        public string DatabasePath
        {
            get => _DatabasePath;
            set
            {
                _DatabasePath = value;
                EmitChanged();
            }
        }
        private string _DatabasePath;

        [Export]
        public bool Import;
        #endregion
    }
}
