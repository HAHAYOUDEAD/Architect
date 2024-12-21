namespace Architect
{
    internal class Values
    {
        public static bool gameStarted;

        public static readonly string modPrefix = "ArchitectMod_";
        public static readonly string commonSeparator = "_@_";

        public static AssetBundle meshBundle;

        public const string dllVersion = "0.7.0";
        public const string resourcesFolder = "Architect.Resources.";
        public const string modName = "Architect";

        public static ModDataManager dataManager = new ModDataManager(modName);
        public static readonly string saveDataTag = "structures";

        public static readonly string placeholderIconName = "ICO_Architect_Placeholder";
        public static readonly string emptyIconName = "ico_Empty";

        public static int doorRotationDegree = 110;
        public static float doorOpenTime = 2.6f;
        public static float doorCloseTime = 1.3f;

        public static PlaceMeshRules placeRules = PlaceMeshRules.None;// | PlaceMeshRules.AllowFloorPlacement | PlaceMeshRules.AllowWallPlacement | PlaceMeshRules.IgnoreCloseObjects;

        public static string lastSelectedBreakDownTool;


        public static bool isInBuildingMode = false;
    }
}
