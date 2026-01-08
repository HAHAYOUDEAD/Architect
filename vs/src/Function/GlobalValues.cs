namespace Architect
{
    internal class Values
    {
        public static bool gameStarted;

        public static readonly string modPrefix = "ArchitectMod_";
        public static readonly string commonSeparator = "_@_";

        public static AssetBundle meshBundle;

        public const string dllVersion = "0.7.9";
        public const string resourcesFolder = "Architect.Resources.";
        public const string modName = "Architect";

        public static ModDataManager dataManager = new ModDataManager(modName);
        public static readonly string saveDataTag = "structures";

        public static readonly string placeholderIconName = "ICO_Architect_Placeholder";
        public static readonly string emptyIconName = "ico_Empty";

        public static bool forceShowCrosshair = false;

        public static readonly string interiorTriggerName = "InteriorTrigger";

        public static PlaceMeshRules placeRules = PlaceMeshRules.None;// | PlaceMeshRules.AllowFloorPlacement | PlaceMeshRules.AllowWallPlacement | PlaceMeshRules.IgnoreCloseObjects;

        public static string lastSelectedBreakDownTool = "";


        public static bool isInBuildingMode = false;
    }
}
