namespace Architect
{
    // Optional DynamicTemperature integration: each building becomes a DynamicTemperature interior space and
    // Architect's own temperature bonus is turned off. DynamicTemperature is accessed through reflection,
    // so Architect doesn't need it to build or run.
    internal static class DynamicTemperatureBridge
    {
        public const string spacePrefix = "ARC#"; // saved in DynamicTemperature's data, don't change

        public static bool active;

        // DynamicTemperature handles interior scenes itself
        public static bool HandlesCurrentScene
        {
            get
            {
                if (!active) return false;
                Weather? w = GameManager.GetWeatherComponent();
                return w == null || !w.IsIndoorScene();
            }
        }

        public static string SpaceNameFor(string buildingId) => spacePrefix + GameManager.m_ActiveScene + "#" + buildingId;

        public static void RemoveArchitectBonus(IndoorSpaceTrigger trigger)
        {
            trigger.m_TemperatureDeltaCelsius = 0f;
            trigger.m_UseOutdoorTemperature = false;
        }

        // ---------------- DynamicTemperature members ----------------

        private const BindingFlags anyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        private static Type configType = null!;                 // InteriorSpaceConfig
        private static FieldInfo spaceManagerField = null!;     // DynamicTemperature.spaceManager
        private static FieldInfo currentSpaceField = null!;     // InteriorSpaceManager.CurrentSpace
        private static FieldInfo currentSpaceMetaField = null!; // InteriorSpaceManager.CurrentSpaceMeta
        private static FieldInfo currentWallsField = null!;     // InteriorSpaceManager.CurrentWalls
        private static FieldInfo trackedSpacesField = null!;    // InteriorSpaceManager.TrackedSpaces
        private static MethodInfo leaveMethod = null!;          // InteriorSpaceManager.Leave()
        private static FieldInfo profileField = null!;          // WarmingWalls.Profile
        private static MethodInfo removeShadowHeaters = null!;  // WarmingWalls.RemoveShadowHeaters()
        private static MethodInfo getWallsComponent = null!;    // GameObject.GetComponent<WarmingWalls>()
        private static MethodInfo transferProperties = null!;   // InteriorSpaceConfig.TransferProperties()
        private static readonly Dictionary<string, FieldInfo> configFields = new Dictionary<string, FieldInfo>();

        private static readonly Dictionary<string, object> profileCache = new Dictionary<string, object>(); // space name -> InteriorSpaceConfig

        public static void TryEnable(HarmonyLib.Harmony harmony)
        {
            Assembly? dt = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "DynamicTemperature");
            if (dt != null)
            {
                try
                {
                    Enable(harmony, dt);
                    active = true;
                }
                catch (Exception e)
                {
                    MelonLogger.Error("DynamicTemperature integration failed: " + e);
                }
            }
            MelonLogger.Msg("DynamicTemperature integration: " + (active ? "on" : "off"));
        }

        private static Type GetDtType(Assembly dt, string name) =>
            dt.GetType("DynamicTemperature." + name) ?? throw new TypeLoadException("DynamicTemperature." + name + " not found");

        private static FieldInfo GetField(Type t, string name) =>
            t.GetField(name, anyMember) ?? throw new MissingFieldException(t.Name, name);

        private static MethodInfo GetMethod(Type t, string name, params Type[] args) =>
            t.GetMethod(name, anyMember, null, args, null) ?? throw new MissingMethodException(t.Name, name);

        private static void Enable(HarmonyLib.Harmony harmony, Assembly dt)
        {
            Type mod = GetDtType(dt, "DynamicTemperature");
            Type manager = GetDtType(dt, "InteriorSpaceManager");
            Type walls = GetDtType(dt, "WarmingWalls");
            configType = GetDtType(dt, "InteriorSpaceConfig");

            spaceManagerField = GetField(mod, "spaceManager");
            currentSpaceField = GetField(manager, "CurrentSpace");
            currentSpaceMetaField = GetField(manager, "CurrentSpaceMeta");
            currentWallsField = GetField(manager, "CurrentWalls");
            trackedSpacesField = GetField(manager, "TrackedSpaces");
            leaveMethod = GetMethod(manager, "Leave");
            profileField = GetField(walls, "Profile");
            removeShadowHeaters = GetMethod(walls, "RemoveShadowHeaters");
            transferProperties = GetMethod(configType, "TransferProperties");
            foreach (string name in new[] { "Name", "MaterialTag", "SizeTag", "Square", "Thickness", "Conductivity", "InsulationRValue", "Volume",
                "Density", "HeatCapacity", "AirVolume", "AirChangesPerHour", "WindowSquare", "DeltaTemperature", "AuroraPower", "AuroraHeaterPower" })
            {
                configFields[name] = GetField(configType, name);
            }

            MethodInfo getComponent = typeof(GameObject).GetMethods()
                .First(m => m.Name == "GetComponent" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            getWallsComponent = getComponent.MakeGenericMethod(walls);

            Patch(harmony, GetMethod(manager, "GetIndoorSpaceName", typeof(IndoorSpaceTrigger)), nameof(GetIndoorSpaceNamePrefix));
            Patch(harmony, GetMethod(manager, "EnterIndoorSpace", typeof(IndoorSpaceTrigger)), nameof(EnterIndoorSpacePrefix));
            Patch(harmony, leaveMethod, nameof(LeavePrefix));
            Patch(harmony, GetMethod(mod, "LoadSceneConfig", typeof(string)), nameof(LoadSceneConfigPrefix), configType);
            Patch(harmony, GetMethod(mod, "GetOutsideTemperature"), nameof(GetOutsideTemperaturePrefix));
        }

        private static void Patch(HarmonyLib.Harmony harmony, MethodBase target, string prefix, Type? genericArg = null)
        {
            MethodInfo p = typeof(DynamicTemperatureBridge).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic)!;
            if (genericArg != null) p = p.MakeGenericMethod(genericArg);
            harmony.Patch(target, new HarmonyMethod(p));
        }

        private static object SpaceManager() => spaceManagerField.GetValue(null)!;

        private static string? CurrentSpace() => (string?)currentSpaceField.GetValue(SpaceManager());

        // WarmingWalls of a space DynamicTemperature already tracks
        private static object? GetTrackedWalls(string spaceName)
        {
            Dictionary<string, GameObject> tracked = (Dictionary<string, GameObject>)trackedSpacesField.GetValue(SpaceManager())!;
            if (!tracked.TryGetValue(spaceName, out GameObject? go) || !go) return null;
            return getWallsComponent.Invoke(go, null);
        }

        // ---------------- InteriorSpaceConfig ----------------

        private static object NewConfig(string spaceName)
        {
            object cfg = Activator.CreateInstance(configType)!;
            configFields["Name"].SetValue(cfg, spaceName);
            return cfg;
        }

        private static object FallbackProfile(string spaceName)
        {
            object cfg = NewConfig(spaceName);
            transferProperties.Invoke(cfg, null);
            configFields["AirVolume"].SetValue(cfg, 100f);
            configFields["AirChangesPerHour"].SetValue(cfg, 0.5f);
            configFields["DeltaTemperature"].SetValue(cfg, 0f);
            return cfg;
        }

        private static object ToConfig(BuildingInfo info)
        {
            ThermalProfile p = info.thermal;
            if (p.fallback) return FallbackProfile(info.spaceName);

            object cfg = NewConfig(info.spaceName);
            configFields["MaterialTag"].SetValue(cfg, "ARCHITECT");
            configFields["SizeTag"].SetValue(cfg, "ARCHITECT");
            configFields["Square"].SetValue(cfg, p.square);
            configFields["Thickness"].SetValue(cfg, 1f);
            configFields["Conductivity"].SetValue(cfg, p.conductivity);
            configFields["InsulationRValue"].SetValue(cfg, 0f);
            configFields["Volume"].SetValue(cfg, p.volume);
            configFields["Density"].SetValue(cfg, p.density);
            configFields["HeatCapacity"].SetValue(cfg, p.heatCapacity);
            configFields["AirVolume"].SetValue(cfg, p.airVolume);
            configFields["AirChangesPerHour"].SetValue(cfg, 0.5f);
            configFields["WindowSquare"].SetValue(cfg, p.windowSquare);
            configFields["DeltaTemperature"].SetValue(cfg, 0f);
            configFields["AuroraPower"].SetValue(cfg, 0f);
            configFields["AuroraHeaterPower"].SetValue(cfg, 0f);
            return cfg;
        }

        private static object ConfigFor(BuildingInfo info)
        {
            if (!profileCache.TryGetValue(info.spaceName, out object? cfg))
            {
                cfg = ToConfig(info);
                profileCache[info.spaceName] = cfg;
            }
            return cfg;
        }

        // Profile of a building in the current scene, else the last known one, else a neutral one
        private static object GetProfile(string spaceName)
        {
            string prefix = spacePrefix + GameManager.m_ActiveScene + "#";
            if (spaceName.StartsWith(prefix))
            {
                string id = spaceName.Substring(prefix.Length);
                if (BuildingTracker.HasFloorsOf(id)) return ConfigFor(BuildingTracker.GetInfo(id, true));
            }
            return profileCache.TryGetValue(spaceName, out object? cached) ? cached : FallbackProfile(spaceName);
        }

        // ---------------- Called by BuildingTracker ----------------

        public static void ProfileUpdated(BuildingInfo info) => profileCache[info.spaceName] = ToConfig(info);

        // One DynamicTemperature space per building, not per floor tile
        public static void Enter(BuildingInfo info)
        {
            object manager = SpaceManager();
            if (CurrentSpace() != info.spaceName)
            {
                currentSpaceField.SetValue(manager, info.spaceName);
                currentSpaceMetaField.SetValue(manager, null);
                currentWallsField.SetValue(manager, null);
            }
            object? walls = GetTrackedWalls(info.spaceName);
            object cfg = ConfigFor(info);
            if (walls != null && profileField.GetValue(walls) != cfg) profileField.SetValue(walls, cfg);
        }

        public static void Leave()
        {
            string? space = CurrentSpace();
            if (space != null && space.StartsWith(spacePrefix)) leaveMethod.Invoke(SpaceManager(), null);
        }

        // First time a building is seen after loading: real fires replace DynamicTemperature's saved "shadow heaters"
        public static void BuildingDiscovered(BuildingInfo info)
        {
            object? walls = GetTrackedWalls(info.spaceName);
            if (walls == null) return;
            removeShadowHeaters.Invoke(walls, null);
            profileField.SetValue(walls, ConfigFor(info));
        }

        // ---------------- Patches on DynamicTemperature ----------------

        // DynamicTemperature names spaces by ObjectGuid, which Architect triggers (and some other mods' triggers) don't have
        private static bool GetIndoorSpaceNamePrefix(IndoorSpaceTrigger ist, ref string __result)
        {
            try
            {
                Structure? floor = BuildingTracker.ArchitectFloorOf(ist);
                if (floor != null)
                {
                    __result = SpaceNameFor(floor.interiorClusterID);
                    return false;
                }
                if (ist && !ist.GetComponent<ObjectGuid>())
                {
                    for (Transform t = ist.transform; t.parent; t = t.parent)
                    {
                        if (t.parent.gameObject.name.ToLowerInvariant().Contains("cave"))
                        {
                            __result = "SmallCave";
                            return false;
                        }
                    }
                    __result = "NOGUID#" + ist.gameObject.name;
                    return false;
                }
            }
            catch (Exception e)
            {
                LogOnce.Error("DynamicTemperatureBridge.GetIndoorSpaceName", e);
            }
            return true;
        }

        // Architect buildings are entered by BuildingTracker
        private static bool EnterIndoorSpacePrefix(IndoorSpaceTrigger ist)
        {
            try
            {
                if (BuildingTracker.ArchitectFloorOf(ist) == null) return true;
                BuildingTracker.Tick(true);
            }
            catch (Exception e)
            {
                LogOnce.Error("DynamicTemperatureBridge.EnterIndoorSpace", e);
            }
            return false;
        }

        // Moving between floor tiles of one building fires an exit; stay in the space while still inside
        private static bool LeavePrefix()
        {
            try
            {
                string? space = CurrentSpace();
                if (space == null || !space.StartsWith(spacePrefix)) return true;
                return BuildingTracker.FindPlayerBuilding() == null;
            }
            catch (Exception e)
            {
                LogOnce.Error("DynamicTemperatureBridge.Leave", e);
                return true;
            }
        }

        // Generic so __result can be typed as InteriorSpaceConfig without referencing it
        private static bool LoadSceneConfigPrefix<T>(string spaceName, ref T __result) where T : class
        {
            if (spaceName == null || !spaceName.StartsWith(spacePrefix)) return true;
            try
            {
                __result = (T)GetProfile(spaceName);
            }
            catch (Exception e)
            {
                LogOnce.Error("DynamicTemperatureBridge.LoadSceneConfig", e);
                __result = (T)FallbackProfile(spaceName);
            }
            return false;
        }

        // DynamicTemperature adds the blizzard drop (the game subtracts it) and reads m_CurrentWindChill (no longer updated)
        private static bool GetOutsideTemperaturePrefix(ref float __result)
        {
            try
            {
                Weather w = GameManager.GetWeatherComponent();
                ExperienceModeManager em = GameManager.GetExperienceModeManagerComponent();
                __result = w.m_BaseTemperature
                    - em.GetOutdoorTempDropCelcius(GameManager.GetTimeOfDayComponent().GetDayNumber())
                    - w.m_CurrentBlizzardDegreesDrop
                    + w.GetCurrentWindchill();
                return false;
            }
            catch (Exception e)
            {
                LogOnce.Error("DynamicTemperatureBridge.GetOutsideTemperature", e);
                return true;
            }
        }
    }

    // Architect's temperature bonus is also applied on trigger enter; zero it while DynamicTemperature is active
    [HarmonyPatch(typeof(IndoorSpaceTrigger), nameof(IndoorSpaceTrigger.OnTriggerEnter))]
    internal static class ArchitectBonusPatch
    {
        private static void Prefix(IndoorSpaceTrigger __instance)
        {
            try
            {
                if (DynamicTemperatureBridge.active && BuildingTracker.ArchitectFloorOf(__instance) != null) __instance.m_TemperatureDeltaCelsius = 0f;
            }
            catch (Exception e)
            {
                LogOnce.Error("ArchitectBonusPatch", e);
            }
        }
    }
}
