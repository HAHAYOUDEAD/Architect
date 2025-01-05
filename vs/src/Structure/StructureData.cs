using static Architect.StructureData;

namespace Architect
{

    public class StructurePreset
    {
        public string prefabName;
        public BuildMaterial bMat;
        public BuildPart bPart;
        public ResourcesPreset res;
        public DoorVariant door = DoorVariant.Regular;
        public string loKey = "placeholderLocalizationKey";
        public int numVar = 1; // number of variants, must be suffixed with _A through _E, max is 5
        public int paintable = 2; // 0 - not, 1 - interior, 2 - both
        public string icoName = placeholderIconName;
        //public bool isDoor;
    }

    public class StructureBuildInfo
    {
        public GameObject[] yields;
        public int[] yieldsNum;
        public GameObject[] requirements;
        public int[] requirementsNum;
        public GameObject[] tools;
        public float buildTime = 2f;
        public float breakTime = 1f;
        public string buildAudio = "PLAY_CRAFTINGWOOD";
        public string breakAudio = "PLAY_CRAFTINGWOOD";
    }


    public class ToolEfficiency
    {
        public bool yieldExtra;
        public float buildTime;
        public float breakTime;
        public float yieldAmount;
    
    }

    //[RegisterTypeInIl2Cpp]
    public class StructureData 
    {

        //public StructureData(IntPtr intPtr) : base(intPtr) { }

        public enum BuildMaterial
        {
            Undefined,
            WoodPlank,
            WoodLog,
            Stone,
            MetalSheet
        }

        public enum BuildPart
        {
            Undefined,
            Wall,
            Floor,
            Roof,
            Pillar,
            Stairs,
            Door,
            Deco,
            Misc
        }

        public enum MaterialName
        {
            Undefined,
            Ghost,
            GhostlyColor, 
            PlankFresh,
            PlankReclaimed,
            PlankPainted,
            Stone,
            StonePainted,
            LogCedar,
            LogFir,
            LogPainted
        }

        public enum BuildPartSide
        {
            Inside,
            Outside,
            Both
        }

        public enum DoorVariant
        {
            Regular,
            Fence,
            Window,
            RoofWindow
        }

        public enum ResourcesPreset
        {
            Regular,
            Half,
            Quarter,
            Double,
            ExtraAux,
            HalfExtraAux,
            Fancy,
            Scaffolding,
            Singular,
        }

        public static int doorRotationDegree = 110;
        public static int windowRotationDegree = 90;
        public static int roofWindowRotationDegree = 60;
        public static float doorOpenTime = 2.6f;
        public static float doorCloseTime = 1.3f;

        public static readonly string placeholderGearName = "GEAR_GoldNugget";
        public static readonly string nailsGearName = "GEAR_ARC_nails";
        public static readonly string nailsBundleGearName = "GEAR_ARC_nailbox";
        public static readonly int nailsBundleSize = 20;
        public static readonly string planksGearName = "GEAR_ARC_plank";
        public static readonly string planksBundleGearName = "GEAR_ARC_plankbundle";
        public static readonly int planksBundleSize = 8;
        public static readonly string logsGearName = "GEAR_ARC_log";
        public static readonly string logsBundleGearName = "GEAR_ARC_logbundle";
        public static readonly int logsBundleSize = 3;

        public static Dictionary<string, ToolEfficiency> toolSpecs = new()
        {
            { "GEAR_SimpleTools", new ToolEfficiency() { yieldExtra = false, yieldAmount = 1.0f, buildTime = 1.0f, breakTime = 1.0f } },
            { "GEAR_HighQualityTools", new ToolEfficiency() { yieldExtra = true, yieldAmount = 1.0f, buildTime = 0.6f, breakTime = 0.8f }},
            { "GEAR_Hatchet", new ToolEfficiency() { yieldExtra = false, yieldAmount = 0.8f, buildTime = 1.5f, breakTime = 1.0f } },
            { "GEAR_HatchetImprovised", new ToolEfficiency() { yieldExtra = false, yieldAmount = 0.5f, buildTime = 2.0f, breakTime = 1.5f } },
            { "GEAR_Hammer", new ToolEfficiency() { yieldExtra = false, yieldAmount = 0.66f, buildTime = 1.5f, breakTime = 1.5f } },
            { "GEAR_Prybar", new ToolEfficiency() { yieldExtra = true, yieldAmount = 1.0f, buildTime = 2.0f, breakTime = 0.6f } },
            { "GEAR_WoodworkingTools", new ToolEfficiency() { yieldExtra = true, yieldAmount = 1.0f, buildTime = 0.5f, breakTime = 0.6f } },
        };


        public static Dictionary<string, int> nailsYieldPerObject = new()
        {
            { "CratesB", 2 },
            { "PalletA", 2 },
            { "BoxCrateB", 2 },
            { "BoxCrateC", 2 },
            { "BoxCratePlane", 2 },
            { "BoxCrateA", 4 },
            { "PalletPileC", 6 },
            { "PalletPileB", 8 }
        };

        public static StructureBuildInfo GetStructureResources(ResourcesPreset rp, BuildMaterial bm)
        {
            StructureBuildInfo sr = new StructureBuildInfo();
            GameObject mainRes; 
            GameObject auxRes;
            GameObject brokenRes;
            int mainResNum = 1;
            int auxResNum = 1;
            float buildingTime = 2f;
            float breakdownTimeMult = 0.6f;
            List<GameObject> tools = new List<GameObject>();
            foreach (string s in toolSpecs.Keys)
            {
                tools.Add(GetPrefab(s));
            }
            sr.tools = tools.ToArray();
            // should be gearItem.m_BreakDownItem, otherwise need to patch Panel_BreakDown.RefreshTools()

            switch (bm)
            {
                case BuildMaterial.WoodPlank:
                    mainRes = GetPrefab("GEAR_ARC_plank");
                    auxRes = GetPrefab("GEAR_ARC_nails");
                    brokenRes = GetPrefab("GEAR_ReclaimedWoodB");
                    break;
                case BuildMaterial.WoodLog:
                    mainRes = GetPrefab("GEAR_ARC_log");
                    auxRes = GetPrefab(placeholderGearName); // empty
                    brokenRes = GetPrefab("GEAR_ReclaimedWoodB");
                    break;
                case BuildMaterial.Stone:
                    mainRes = GetPrefab("GEAR_Stone");
                    auxRes = GetPrefab("GEAR_OldMansBeardHarvested");
                    brokenRes = GetPrefab("GEAR_Stone");
                    break;
                case BuildMaterial.MetalSheet:
                    mainRes = GetPrefab("GEAR_ScrapMetal");
                    auxRes = GetPrefab("GEAR_ScrapMetal");
                    brokenRes = GetPrefab("GEAR_ScrapMetal");
                    break;
                default:
                    mainRes = GetPrefab("GEAR_Stick");
                    auxRes = GetPrefab("GEAR_Stick");
                    brokenRes = GetPrefab("GEAR_Stick");
                    break;
            }

            switch (rp)
            {
                case ResourcesPreset.Regular:
                    mainResNum = 8;
                    if (bm == BuildMaterial.WoodLog) mainResNum = 6;
                    auxResNum = 2;
                    buildingTime = 1.0f;
                    break;
                case ResourcesPreset.Half:
                    mainResNum = 4;
                    if (bm == BuildMaterial.WoodLog) mainResNum = 3;
                    auxResNum = 2;
                    buildingTime = 0.8f;
                    break;
                case ResourcesPreset.Quarter:
                    mainResNum = 2;
                    auxResNum = 1;
                    buildingTime = 0.6f;
                    break;
                case ResourcesPreset.Double:
                    mainResNum = 12;
                    if (bm == BuildMaterial.WoodLog) mainResNum = 10;
                    auxResNum = 2;
                    buildingTime = 1.6f;
                    break;
                case ResourcesPreset.ExtraAux:
                    mainResNum = 8;
                    if (bm == BuildMaterial.WoodLog) mainResNum = 6;
                    auxResNum = 3;
                    buildingTime = 2.0f;
                    break;
                case ResourcesPreset.HalfExtraAux:
                    mainResNum = 4;
                    if (bm == BuildMaterial.WoodLog) mainResNum = 3;
                    auxResNum = 3;
                    buildingTime = 1.6f;
                    break;
                case ResourcesPreset.Fancy:
                    mainResNum = 10;
                    if (bm == BuildMaterial.WoodLog) mainResNum = 8;
                    auxResNum = 4;
                    buildingTime = 4.0f;
                    break;
                case ResourcesPreset.Scaffolding:
                    mainResNum = 6;
                    auxResNum = 3;
                    buildingTime = 0.6f;
                    break;
                case ResourcesPreset.Singular:
                    mainResNum = 1;
                    auxResNum = 0;
                    buildingTime = 0.2f;
                    break;
            }

            buildingTime *= Settings.options.buildingTimeMult;

            sr.requirements = [ mainRes, auxRes ];
            sr.requirementsNum = [ mainResNum, auxResNum];
            sr.yields = [mainRes, auxRes, brokenRes ];
            sr.yieldsNum = [mainResNum, auxResNum, 0 ];
            sr.buildTime = buildingTime;
            sr.breakTime = buildingTime * breakdownTimeMult;

            return sr;
        }

        public static Dictionary<string, StructurePreset> allStructures = new()
        {
            // wood plank
                // wall
            { "ARC_plank_halfWall", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Wall, res = ResourcesPreset.Half, loKey = "ARC_WallHalf", numVar = 3 } },
            { "ARC_plank_narrowWall", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Wall, res = ResourcesPreset.Half, loKey = "ARC_WallNarrow", numVar = 2 } },
            { "ARC_plank_regularWall", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Wall, res = ResourcesPreset.Regular, loKey = "ARC_WallRegular", numVar = 3 } },
            { "ARC_plank_doorWall", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Wall,res = ResourcesPreset.Regular, loKey = "ARC_WallDoor" } },
            { "ARC_plank_windowWall", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Wall, res = ResourcesPreset.Regular, loKey = "ARC_WallWindow" } },
            { "ARC_plank_halfNarrowWall", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Wall, res = ResourcesPreset.Quarter, loKey = "ARC_WallQuarter", numVar = 2 } },
                // floor
            { "ARC_plank_regularFloor", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Floor, res = ResourcesPreset.Regular, loKey = "ARC_FloorRegular", numVar = 3 } },
            { "ARC_plank_quarterFloor", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Floor, res = ResourcesPreset.Quarter, loKey = "ARC_FloorQuarter" } },
            { "ARC_plank_stairsFloor", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Floor, res = ResourcesPreset.Regular, loKey = "ARC_FloorStairs" } },
                // roof
            { "ARC_plank_regularRoof", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Roof, res = ResourcesPreset.Regular, loKey = "ARC_RoofRegular", numVar = 3 } },
            { "ARC_plank_triangleWallLeft", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Wall, res = ResourcesPreset.Half, loKey = "ARC_WallTriangleLeft" } },
            { "ARC_plank_triangleWallRight", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Wall, res = ResourcesPreset.Half, loKey = "ARC_WallTriangleRight" } },
                // pillar
            { "ARC_plank_railingEnd", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Pillar, res = ResourcesPreset.Singular, loKey = "ARC_RailingEnd", numVar = 3 } },
            { "ARC_plank_railingShort", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Pillar, res = ResourcesPreset.Quarter, loKey = "ARC_RailingShort", numVar = 2 } },
            { "ARC_plank_railingLong", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Pillar, res = ResourcesPreset.Half, loKey = "ARC_RailingLong" } },
            { "ARC_plank_railingSlanted", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Pillar, res = ResourcesPreset.Half, loKey = "ARC_RailingSlanted" } },
            { "ARC_plank_pillarHalf", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Pillar, res = ResourcesPreset.Singular, loKey = "ARC_PillarHalf", numVar = 2 } },
            { "ARC_plank_pillar", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Pillar, res = ResourcesPreset.Singular, loKey = "ARC_Pillar", numVar = 2 } },
                // stairs
            { "ARC_plank_regularStairs", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Stairs, res = ResourcesPreset.Half, loKey = "ARC_StairsHalf" } },
            { "ARC_plank_longStairs", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Stairs, res = ResourcesPreset.Fancy, loKey = "ARC_StairsFull" } },
                // door
            { "ARC_plank_doorFancy", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Door, res = ResourcesPreset.Fancy, loKey = "ARC_DoorFancy" } },
            { "ARC_plank_doorSimple", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Door, res = ResourcesPreset.ExtraAux, loKey = "ARC_DoorSimple" } },
            { "ARC_plank_windowShutter", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Door, res = ResourcesPreset.Regular, loKey = "ARC_WindowShutter", door = DoorVariant.Window } },
                // deco
                // misc
            { "ARC_plank_scaffolding", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Misc, res = ResourcesPreset.Scaffolding, loKey = "ARC_Scaffolding" } },
            { "ARC_plank_single", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Misc, res = ResourcesPreset.Singular, loKey = "ARC_Singular", numVar = 3 } },
            { "ARC_plank_board", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Misc, res = ResourcesPreset.Quarter, loKey = "ARC_PatchBoard", numVar = 2 } },
            { "ARC_plank_beam", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Misc, res = ResourcesPreset.Singular, loKey = "ARC_Beam", numVar = 2 } },
            { "ARC_plank_beamHalf", new StructurePreset() { bMat = BuildMaterial.WoodPlank, bPart = BuildPart.Misc, res = ResourcesPreset.Singular, loKey = "ARC_BeamHalf", numVar = 2 } },
            
            
            // wood log
                // wall
            { "ARC_log_regularWall", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Wall, res = ResourcesPreset.Regular, loKey = "ARC_WallRegular", numVar = 2 } },
            { "ARC_log_narrowWall", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Wall, res = ResourcesPreset.Half, loKey = "ARC_WallNarrow", numVar = 2 } },
            { "ARC_log_halfWall", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Wall, res = ResourcesPreset.Half, loKey = "ARC_WallHalf", numVar = 2 } },
            { "ARC_log_halfNarrowWall", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Wall, res = ResourcesPreset.Quarter, loKey = "ARC_WallQuarter", numVar = 2 } },
            { "ARC_log_windowWall", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Wall, res = ResourcesPreset.Regular, loKey = "ARC_WallWindow" } },
            { "ARC_log_doorWall", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Wall, res = ResourcesPreset.Regular, loKey = "ARC_WallDoor" } },
            { "ARC_log_largeWall", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Wall, res = ResourcesPreset.Double, loKey = "ARC_WallLong" } },
                // floor
            { "ARC_log_regularFloor", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Floor, res = ResourcesPreset.Regular, loKey = "ARC_FloorRegular", numVar = 2 } },
            { "ARC_log_elevatedFloor", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Floor, res = ResourcesPreset.Fancy, loKey = "ARC_FloorElevated", numVar = 2 } },
            { "ARC_log_foundation", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Floor, res = ResourcesPreset.Regular, loKey = "ARC_FoundationElevated", numVar = 2 } },
                // roof
            { "ARC_log_triangleWallLeft", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Wall, res = ResourcesPreset.Half, loKey = "ARC_WallTriangleLeft" } },
            { "ARC_log_triangleWallRight", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Wall, res = ResourcesPreset.Half, loKey = "ARC_WallTriangleRight" } },
                // pillar
            { "ARC_log_pillar", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Pillar, res = ResourcesPreset.Singular, loKey = "ARC_Pillar", numVar = 2 } },
            { "ARC_log_pillarFancy", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Pillar, res = ResourcesPreset.Half, loKey = "ARC_PillarFancy" } },
            { "ARC_log_fence", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Pillar, res = ResourcesPreset.Half, loKey = "ARC_Fence", numVar = 2 } },
            { "ARC_log_fencePost", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Pillar, res = ResourcesPreset.Singular, loKey = "ARC_FencePost" } },
            
                // stairs
            { "ARC_log_ramp", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Stairs, res = ResourcesPreset.Regular, loKey = "ARC_Ramp", numVar = 2 } },
            { "ARC_log_regularStairs", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Stairs, res = ResourcesPreset.Half, loKey = "ARC_StairsHalf" } },
            { "ARC_log_longStairs", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Stairs, res = ResourcesPreset.Fancy, loKey = "ARC_StairsFull" } },
                // door
            { "ARC_log_fenceGate", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Door, res = ResourcesPreset.ExtraAux, loKey = "ARC_FenceGate", door = DoorVariant.Fence } },
                // deco
                // misc
            { "ARC_log_beam", new StructurePreset() { bMat = BuildMaterial.WoodLog, bPart = BuildPart.Pillar, res = ResourcesPreset.Singular, loKey = "ARC_Beam", numVar = 2 } },


            // stone
                // wall
                // floor
                // roof
                // pillar
                // stairs
                // door
                // deco
                // misc
        };
    }
}
