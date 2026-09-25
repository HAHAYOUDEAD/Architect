using Description = ModSettings.DescriptionAttribute;
using ModSettings;
using System.ComponentModel;

namespace Architect
{
    // All texts are localization keys (Resources/Localization.json). ModSettings' description holder has its
    // Localize flag inverted, so descriptions are given without it to be translated.
    internal class ArcModSettings : JsonModSettings
    {
        [Section("ARC_Set_Section_Main", Localize = true)]

        [Name("ARC_Set_MenuKey", Localize = true)]
        [Description("ARC_Set_MenuKey_Desc")]
        public KeyCode menuKey = KeyCode.B;

        [Name("ARC_Set_DropYields", Localize = true)]
        [Description("ARC_Set_DropYields_Desc")]
        public bool dropYields = false;

        [Section("ARC_Set_Section_Nudge", Localize = true)]

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

        [Name("ARC_Set_NudgeAmount", Localize = true)]
        [Description("ARC_Set_NudgeAmount_Desc")]
        [Slider(1, 20)]
        public int nudgeAmount = 4;

        [Name("ARC_Set_NudgeRight", Localize = true)]
        [Description("ARC_Set_NudgeRight_Desc")]
        public KeyCode nudgeXpKey = KeyCode.None;

        [Name("ARC_Set_NudgeLeft", Localize = true)]
        [Description("ARC_Set_NudgeLeft_Desc")]
        public KeyCode nudgeXnKey = KeyCode.None;

        [Name("ARC_Set_NudgeForward", Localize = true)]
        [Description("ARC_Set_NudgeForward_Desc")]
        public KeyCode nudgeZpKey = KeyCode.None;

        [Name("ARC_Set_NudgeBackward", Localize = true)]
        [Description("ARC_Set_NudgeBackward_Desc")]
        public KeyCode nudgeZnKey = KeyCode.None;

        [Name("ARC_Set_NudgeUp", Localize = true)]
        [Description("ARC_Set_NudgeUp_Desc")]
        public KeyCode nudgeYpKey = KeyCode.None;

        [Name("ARC_Set_NudgeDown", Localize = true)]
        [Description("ARC_Set_NudgeDown_Desc")]
        public KeyCode nudgeYnKey = KeyCode.None;

        [Section("ARC_Set_Section_Visual", Localize = true)]

        [Name("ARC_Set_TerrainBlending", Localize = true)]
        [Description("ARC_Set_TerrainBlending_Desc")]
        public bool terrainBlending = true;

        [Name("ARC_Set_Withering", Localize = true)]
        [Description("ARC_Set_Withering_Desc")]
        public bool woodWithering = true;

        [Section("ARC_Indoor_Section", Localize = true)]

        [Name("ARC_Indoor_FireFlicker", Localize = true)]
        [Description("ARC_Indoor_FireFlicker_Desc")]
        [Choice(new string[] { "ARC_Indoor_FireFlicker_Off", "ARC_Indoor_FireFlicker_Weak", "ARC_Indoor_FireFlicker_Auto", "ARC_Indoor_FireFlicker_Vanilla" }, Localize = true)]
        public int indoorFireFlicker = 2;           // FireFlickerPatch.Mode

        [Name("ARC_Indoor_Sound", Localize = true)]
        [Description("ARC_Indoor_Sound_Desc")]
        [Choice(new string[] { "ARC_Indoor_Sound_ByMaterial", "ARC_Indoor_Sound_Weak", "ARC_Indoor_Sound_Medium", "ARC_Indoor_Sound_Strong" }, Localize = true)]
        public int indoorSoundLevel = 0;            // SoundInsulation.Level

        [Name("ARC_Indoor_StrongerInBlizzard", Localize = true)]
        [Description("ARC_Indoor_StrongerInBlizzard_Desc")]
        public bool indoorStrongerInBlizzard = true;

        [Name("ARC_Indoor_WindVolume", Localize = true)]
        [Description("ARC_Indoor_WindVolume_Desc")]
        [Slider(0, 100)]
        public int indoorWindVolume = 100;          // percent

        [Section("ARC_Balance_Section", Localize = true)]

        [Name("ARC_Balance_Weight", Localize = true)]
        [Description("ARC_Balance_Weight_Desc")]
        [Choice(new string[] { "ARC_Balance_Weight_Architect", "ARC_Balance_Weight_Realistic" }, Localize = true)]
        public int balanceMaterialWeight = 1;       // MaterialWeights.Mode

        [Name("ARC_Balance_Cost", Localize = true)]
        [Description("ARC_Balance_Cost_Desc")]
        [Choice(new string[] { "ARC_Balance_Cost_Architect", "ARC_Balance_Cost_Adjusted" }, Localize = true)]
        public int balanceBuildingCost = 1;         // BuildingCosts.Mode

        [Section("ARC_Set_Section_Performance", Localize = true)]

        [Name("ARC_Set_SizeLimit", Localize = true)]
        [Description("ARC_Set_SizeLimit_Desc")]
        [Slider(10, 200)]
        public int interiorSearchLimit = 30;

        [Name("ARC_Set_FinderPrecision", Localize = true)]
        [Description("ARC_Set_FinderPrecision_Desc")]
        [Slider(1, 3)]
        public int interiorFinderPrecision = 2;


        [Section("ARC_Set_Section_Debug", Localize = true)]

        [Name("ARC_Set_DebugInfo", Localize = true)]
        [Description("ARC_Set_DebugInfo_Desc")]
        public bool showDebugInfo = false;


        [Section("ARC_Set_Section_Cheats", Localize = true)]

        [Name("ARC_Set_BuildTime", Localize = true)]
        [Description("ARC_Set_BuildTime_Desc")]
        [Slider(0, 2, 21)]
        public float buildingTimeMult = 1f;

        [Name("ARC_Set_NoRequirements", Localize = true)]
        [Description("ARC_Set_NoRequirements_Desc")]
        public bool noRequirements = false;

        [Name("ARC_Set_CampfiresAnywhere", Localize = true)]
        [Description("ARC_Set_CampfiresAnywhere_Desc")]
        public bool campfiresOnWood = false;

        protected override void OnConfirm()
        {
            base.OnConfirm();
            MaterialWeights.Apply();
            BuildingCosts.RefreshPlaced();
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
