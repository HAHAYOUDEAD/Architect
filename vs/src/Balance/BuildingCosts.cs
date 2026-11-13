namespace Architect
{
    // Log and plank cost of each building part; nails are unchanged, dismantling returns the same amount.
    // GetStructureResources only knows the cost group, not the part, so callers set Context first.
    internal static class BuildingCosts
    {
        public enum Mode { Architect, Adjusted } // Settings.balanceBuildingCost

        private static readonly Dictionary<string, int> logs = new Dictionary<string, int>
        {
            { "ARC_WallRegular", 6 },
            { "ARC_WallWindow", 5 },
            { "ARC_WallDoor", 4 },
            { "ARC_WallLong", 8 },
            { "ARC_WallNarrow", 3 },
            { "ARC_WallHalf", 3 },
            { "ARC_WallQuarter", 2 },
            { "ARC_WallTriangleLeft", 2 },
            { "ARC_WallTriangleRight", 2 },
            { "ARC_FloorRegular", 3 },
            { "ARC_FloorElevated", 4 },
            { "ARC_FoundationElevated", 5 },
            { "ARC_Pillar", 1 },
            { "ARC_PillarFancy", 2 },
            { "ARC_Beam", 1 },
            { "ARC_Fence", 2 },
            { "ARC_FencePost", 1 },
            { "ARC_FenceGate", 3 },
            { "ARC_Ramp", 3 },
            { "ARC_StairsHalf", 3 },
            { "ARC_StairsFull", 5 },
        };

        // one plank bundle (8) = one regular wall
        private static readonly Dictionary<string, int> planks = new Dictionary<string, int>
        {
            { "ARC_WallRegular", 8 },
            { "ARC_WallWindow", 7 },
            { "ARC_WallDoor", 5 },
            { "ARC_WallNarrow", 4 },
            { "ARC_WallHalf", 4 },
            { "ARC_WallQuarter", 2 },
            { "ARC_WallTriangleLeft", 2 },
            { "ARC_WallTriangleRight", 2 },
            { "ARC_FloorRegular", 4 },
            { "ARC_FloorQuarter", 1 },
            { "ARC_FloorStairs", 3 },
            { "ARC_RoofRegular", 4 },
            { "ARC_RoofOverhangLeft", 2 },
            { "ARC_RoofOverhangRight", 2 },
            { "ARC_RoofOverhangTrim", 2 },
            { "ARC_StairsHalf", 4 },
            { "ARC_StairsFull", 6 },
            { "ARC_DoorFancy", 4 },
            { "ARC_DoorSimple", 3 },
            { "ARC_WindowShutter", 2 },
            { "ARC_RailingLong", 3 },
            { "ARC_RailingSlanted", 2 },
            { "ARC_RailingShort", 2 },
            { "ARC_RailingEnd", 1 },
            { "ARC_Scaffolding", 8 },
            { "ARC_Pillar", 1 },
            { "ARC_PillarHalf", 1 },
            { "ARC_Beam", 1 },
            { "ARC_BeamHalf", 1 },
            { "ARC_Singular", 1 },
            { "ARC_PatchBoard", 2 },
        };

        [ThreadStatic] public static Structure? context; // part whose cost is being requested

        private static Dictionary<string, int>? TableFor(Data.BuildMaterial m) => m switch
        {
            Data.BuildMaterial.WoodLog => logs,
            Data.BuildMaterial.WoodPlank => planks,
            _ => null,
        };

        public static void Adjust(StructureBuildInfo? info)
        {
            Structure? s = context;
            if (s == null || info == null || string.IsNullOrEmpty(s.localizationKey)) return;
            if ((Mode)Settings.options.balanceBuildingCost != Mode.Adjusted) return;
            Dictionary<string, int>? table = TableFor(s.buildMaterial);
            if (table == null || !table.TryGetValue(s.localizationKey, out int cost)) return;
            if (info.requirementsNum?.Length > 0) info.requirementsNum[0] = cost;
            if (info.yieldsNum?.Length > 0) info.yieldsNum[0] = cost;
        }

        // Recalculates costs of already placed parts after the setting changes
        public static void RefreshPlaced()
        {
            try
            {
                foreach (Structure s in StructureManager.structures)
                {
                    if (!s || !s.breakdown || TableFor(s.buildMaterial) == null) continue;
                    if (s.isBuilt) s.breakdown.InitializeForBreakdown();
                    else s.breakdown.InitializeForBuilding();
                }
            }
            catch (Exception e)
            {
                LogOnce.Error("BuildingCosts.RefreshPlaced", e);
            }
        }
    }

    [HarmonyPatch(typeof(StructureData), nameof(StructureData.GetStructureResources))]
    internal static class GetStructureResourcesPatch
    {
        private static void Postfix(StructureBuildInfo __result)
        {
            try
            {
                BuildingCosts.Adjust(__result);
            }
            catch (Exception e)
            {
                LogOnce.Error("GetStructureResourcesPatch", e);
            }
        }
    }

    [HarmonyPatch(typeof(Interfaces), nameof(Interfaces.InitializeForBuilding))]
    internal static class InitializeForBuildingPatch
    {
        private static void Prefix(BreakDown bd) => BuildingCosts.context = bd ? bd.GetComponent<Structure>() : null;
        private static void Postfix() => BuildingCosts.context = null;
    }

    [HarmonyPatch(typeof(Interfaces), nameof(Interfaces.InitializeForBreakdown))]
    internal static class InitializeForBreakdownPatch
    {
        private static void Prefix(BreakDown bd) => BuildingCosts.context = bd ? bd.GetComponent<Structure>() : null;
        private static void Postfix() => BuildingCosts.context = null;
    }

    // Architect recalculates the dismantle yield in its own prefix of this method; ours runs first
    [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.UpdateDurationLabel))]
    internal static class BreakDownPanelContextPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Panel_BreakDown __instance) => BuildingCosts.context = __instance.m_BreakDown ? __instance.m_BreakDown.GetComponent<Structure>() : null;

        private static void Postfix() => BuildingCosts.context = null;
    }
}
