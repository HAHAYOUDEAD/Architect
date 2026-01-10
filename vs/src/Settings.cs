using Description = ModSettings.DescriptionAttribute;
using ModSettings;
using System.ComponentModel;

namespace Architect
{
    internal class ArcModSettings : JsonModSettings
    {
        [Section("Main settings")]

        [Name("Building menu key")]
        [Description("Bring up radial menu to choose building parts from")]
        public KeyCode menuKey = KeyCode.B;

        [Name("Drop yields")]
        [Description("Drop yields from dismantling on the ground instead of straight to inventory")]
        public bool dropYields = false;

        [Section("Nudge settings")]

        /*
        [Name("Nudge direction")]
        [Description("Local space - along the local mesh axis\n\nFrom player - aligned along axis where FORWARD points from player to held object\n\nDefault: Local space")]
        [Choice(new string[]
        {
            "Local space",
            "From player"
        })]
        public int nudgeDirection = 0;
        */

        [Name("Nudge amount")]
        [Description("Distance in centimeters")]
        [Slider(1, 20)]
        public int nudgeAmount = 4;

        [Name("Nudge right")]
        [Description("\n\nDefault: Right Arrow")]
        public KeyCode nudgeXpKey = KeyCode.None;

        [Name("Nudge left")]
        [Description("\n\nDefault: Left Arrow")]
        public KeyCode nudgeXnKey = KeyCode.None;

        [Name("Nudge forwards")]
        [Description("\n\nDefault: Up Arrow")]
        public KeyCode nudgeZpKey = KeyCode.None;

        [Name("Nudge backwards")]
        [Description("\n\nDefault: Down Arrow")]
        public KeyCode nudgeZnKey = KeyCode.None;

        [Name("Nudge up")]
        [Description("\n\nDefault: Page Up")]
        public KeyCode nudgeYpKey = KeyCode.None;

        [Name("Nudge down")]
        [Description("\n\nDefault: Page Down")]
        public KeyCode nudgeYnKey = KeyCode.None;

        [Section("Visual settings")]

        [Name("Terrain blending")]
        [Description("Allow snow texture to propogate onto structures\n\nWill only appear on the 'outside' part of every building piece\n\nWill still appear inside your house on certain occasions")]
        public bool terrainBlending = true;

        [Name("Withering")]
        [Description("Planks will become grey with time")]
        public bool woodWithering = true;

        [Section("Performance settings (currently not implemented)")]

        [Name("Building size limit")]
        [Description("Limit how far to look when checking building integrity, in unity units")]
        [Slider(10, 200)]
        public int interiorSearchLimit = 30;

        [Name("Interior finder precision")]
        [Description("1 is not very precise\n\n2 is ideal in most cases\n\n3 you should try if detection is not precise enough for your particular case")]
        [Slider(1, 3)]
        public int interiorFinderPrecision = 2;


        [Section("Debug settings")]

        [Name("Show debug info")]
        [Description("Highlight tiles when calculating indoor volume, send other debug info to console")]
        public bool showDebugInfo = false;


        [Section("Cheats for cheaters")]

        [Name("Building time multiplier")]
        [Description("...")]
        [Slider(0, 2, 21)]
        public float buildingTimeMult = 1f;

        [Name("No requirements")]
        [Description("For testing purposes, will be removed when out of beta")]
        public bool noRequirements = false;

        [Name("Campfires anywhere")]
        [Description("Allow campfires on wooden floors")]
        public bool campfiresOnWood = false;

        protected override void OnConfirm()
        {
            base.OnConfirm();
        }
    }

    internal static class Settings
    {
        public static ArcModSettings options;

        public static void OnLoad()
        {
            options = new ArcModSettings();
            options.AddToModSettings(modName);
        }
    }
}
