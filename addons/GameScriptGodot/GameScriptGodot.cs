#if TOOLS
using Godot;

namespace GameScript
{
    [Tool]
    public partial class GameScriptGodot : EditorPlugin
    {
        private GameScriptSettingsInspector _Plugin;

        public override void _EnterTree()
        {
            _Plugin = new GameScriptSettingsInspector();
            AddInspectorPlugin(_Plugin);
        }

        public override void _ExitTree()
        {
            RemoveInspectorPlugin(_Plugin);
        }
    }
}
#endif
