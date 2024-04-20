#if TOOLS
using Godot;

namespace GameScript
{
    public partial class GameScriptSettingsInspector : EditorInspectorPlugin
    {
        public override bool _CanHandle(GodotObject @object)
        {
            return @object is GameScriptSettings;
        }

        public override bool _ParseProperty(
            GodotObject @object,
            Variant.Type type,
            string name,
            PropertyHint hintType,
            string hintString,
            PropertyUsageFlags usageFlags,
            bool wide
        )
        {
            if (name == "Import")
            {
                AddPropertyEditor(name, new GameScriptSettingsInspectorButton());
                return true;
            }
            else if (name == "MaxFlags")
                return true;
            // Resource resource = @object as Resource;
            // GameScriptSettings settings = resource as GameScriptSettings;

            // if (type == Variant.Type.String)
            // {
            //     // Create an instance of the custom property editor and register
            //     // it to a specific property path.
            //     // AddPropertyEditor(name, new RandomIntEditor());
            //     // Inform the editor to remove the default property editor for
            //     // this property type.
            //     return true;
            // }
            // else if (type == Variant.Type.Int)
            // {
            //     if (name == "MaxFlags")
            //     {
            //         return true;
            //     }
            // }
            return false;
        }
    }
}
#endif
