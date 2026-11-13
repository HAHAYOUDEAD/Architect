namespace Architect
{
    // Tracks Architect buildings and the one the player is in, and updates all indoor features 4 times a second
    internal static class BuildingTracker
    {
        private const float tickInterval = 0.25f;
        private const float floorRefreshInterval = 2f;
        private const float reanalyzeInterval = 10f;

        private static float nextTick;
        private static float nextFloorRefresh;
        private static readonly List<Structure> floors = new List<Structure>();
        private static readonly Dictionary<string, BuildingInfo> infos = new Dictionary<string, BuildingInfo>();
        private static readonly HashSet<string> seenBuildings = new HashSet<string>();

        public static string? current; // id of the building the player is in
        public static List<Bounds> interiorTiles = new List<Bounds>();

        public static void ResetScene()
        {
            floors.Clear();
            infos.Clear();
            seenBuildings.Clear();
            interiorTiles = new List<Bounds>();
            current = null;
            nextTick = 0f;
            nextFloorRefresh = 0f;
            SoundInsulation.Update(null);
            SnowKillers.Reset();
            SnowHider.Reset();
            LightFixes.Reset();
        }

        private static bool InGame()
        {
            if (!GameManager.GetPlayerTransform()) return false;
            string scene = GameManager.m_ActiveScene;
            if (string.IsNullOrEmpty(scene)) return false;
            scene = scene.ToLowerInvariant();
            return !scene.Contains("menu") && !scene.Contains("boot") && !scene.Contains("empty");
        }

        // Architect floor tile the trigger belongs to, or null
        public static Structure? ArchitectFloorOf(IndoorSpaceTrigger? ist)
        {
            if (ist == null || ist.transform.parent == null) return null;
            Structure? s = ist.transform.parent.GetComponent<Structure>();
            return s != null && !string.IsNullOrEmpty(s.interiorClusterID) ? s : null;
        }

        public static bool IsInsideAnyBuilding(Vector3 pos)
        {
            foreach (Bounds tile in interiorTiles)
            {
                Bounds b = tile;
                b.Expand(0.2f);
                if (b.Contains(pos)) return true;
            }
            return false;
        }

        private static void RefreshFloors()
        {
            floors.Clear();
            List<Bounds> tiles = new List<Bounds>();
            List<string> discovered = new List<string>();

            foreach (Structure s in StructureManager.structures)
            {
                if (!s || s.buildPart != Data.BuildPart.Floor) continue;
                if (string.IsNullOrEmpty(s.interiorClusterID) || !s.interiorTrigger) continue;
                floors.Add(s);
                if (DynamicTemperatureBridge.active) DynamicTemperatureBridge.RemoveArchitectBonus(s.interiorTrigger);
                if (seenBuildings.Add(s.interiorClusterID)) discovered.Add(s.interiorClusterID);

                if (!s.interiorTrigger.gameObject.activeInHierarchy) continue;
                BoxCollider? bc = s.interiorTrigger.GetComponent<BoxCollider>();
                if (bc != null) tiles.Add(bc.bounds);
            }
            interiorTiles = tiles;

            if (DynamicTemperatureBridge.HandlesCurrentScene)
            {
                foreach (string id in discovered) DynamicTemperatureBridge.BuildingDiscovered(GetInfo(id, true));
            }

            SnowKillers.Update(floors);
        }

        public static bool HasFloorsOf(string id)
        {
            if (floors.Count == 0) RefreshFloors();
            return floors.Exists(f => f && f.interiorClusterID == id);
        }

        public static string? FindPlayerBuilding()
        {
            Transform? pt = GameManager.GetPlayerTransform();
            if (pt == null) return null;
            Vector3 p = pt.position + Vector3.up * 0.8f;
            foreach (Structure f in floors)
            {
                if (!f || !f.interiorTrigger || !f.interiorTrigger.gameObject.activeInHierarchy) continue;
                BoxCollider? bc = f.interiorTrigger.GetComponent<BoxCollider>();
                if (bc == null) continue;
                Bounds b = bc.bounds;
                b.Expand(new Vector3(0.3f, 0.4f, 0.3f));
                if (b.Contains(p)) return f.interiorClusterID;
            }
            return null;
        }

        public static BuildingInfo GetInfo(string id, bool reanalyze)
        {
            if (!reanalyze && infos.TryGetValue(id, out BuildingInfo? cached)) return cached;
            BuildingInfo info = BuildingAnalyzer.Analyze(id, floors);
            infos[id] = info;
            if (DynamicTemperatureBridge.active) DynamicTemperatureBridge.ProfileUpdated(info);
            return info;
        }

        public static void Tick(bool force)
        {
            float now = Time.realtimeSinceStartup;
            if (!force && now < nextTick) return;
            nextTick = now + tickInterval;

            if (!InGame())
            {
                current = null;
                SnowHider.Update(false);
                SoundInsulation.Update(null);
                return;
            }

            if (force || now >= nextFloorRefresh)
            {
                nextFloorRefresh = now + floorRefreshInterval;
                RefreshFloors();
            }

            LightFixes.Tick();

            string? building = FindPlayerBuilding();
            bool changed = building != current;
            current = building;

            BuildingInfo? inside = null;
            if (building != null) inside = GetInfo(building, changed || now - GetInfo(building, false).analyzedAt > reanalyzeInterval);

            if (DynamicTemperatureBridge.HandlesCurrentScene)
            {
                if (inside != null) DynamicTemperatureBridge.Enter(inside);
                else if (changed) DynamicTemperatureBridge.Leave();
            }
            SnowHider.Update(inside != null && !inside.AnyDoorOpen());
            SoundInsulation.Update(inside);
        }
    }
}
