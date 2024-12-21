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
