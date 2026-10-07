namespace Architect
{
    // Fire light comes with a shadowless fill light that shines through walls.
    // Point and spot lights inside buildings get shadows; the original setting is restored when they leave.
    internal static class LightFixes
    {
        private const float interval = 3f;
        private static float nextRun;
        private static readonly Dictionary<IntPtr, (Light light, LightShadows shadows)> changed = new Dictionary<IntPtr, (Light, LightShadows)>();

        public static HashSet<IntPtr> insideLights = new HashSet<IntPtr>();

        public static void Reset()
        {
            changed.Clear();
            insideLights = new HashSet<IntPtr>();
            nextRun = 0f;
        }

        // Lights that move with the player or camera make every shadow swim, so they're left alone
        private static bool IsExcluded(Light l)
        {
            if (l.gameObject.name.Contains("IndoorCore")) return true;
            Transform? player = GameManager.GetPlayerTransform();
            if (player != null && l.transform.IsChildOf(player)) return true;
            Camera? cam = Camera.main;
            return cam != null && l.transform.IsChildOf(cam.transform);
        }

        public static void Tick()
        {
            float now = Time.realtimeSinceStartup;
            if (now < nextRun) return;
            nextRun = now + interval;
            try
            {
                // restore lights that left a building or were destroyed
                foreach (IntPtr key in changed.Keys.ToList())
                {
                    (Light light, LightShadows shadows) = changed[key];
                    if (!light) changed.Remove(key);
                    else if (!BuildingTracker.IsInsideAnyBuilding(light.transform.position) || IsExcluded(light))
                    {
                        light.shadows = shadows;
                        changed.Remove(key);
                    }
                }

                HashSet<IntPtr> inside = new HashSet<IntPtr>();
                if (BuildingTracker.interiorTiles.Count > 0)
                {
                    foreach (Light l in UnityEngine.Object.FindObjectsOfType<Light>())
                    {
                        if (!l || !l.isActiveAndEnabled || (l.type != LightType.Point && l.type != LightType.Spot)) continue;
                        if (!BuildingTracker.IsInsideAnyBuilding(l.transform.position) || IsExcluded(l)) continue;
                        inside.Add(l.Pointer);
                        if (l.shadows != LightShadows.None || changed.ContainsKey(l.Pointer)) continue;
                        changed[l.Pointer] = (l, l.shadows);
                        l.shadows = LightShadows.Soft;
                    }
                }
                insideLights = inside;
            }
            catch (Exception e)
            {
                LogOnce.Error("LightFixes.Tick", e);
            }
        }
    }

    // LightDancing randomizes shadow strength (letting light through walls) and jitters the light's position.
    // Inside buildings: full shadows, and the jitter is damped around a slowly moving average.
    [HarmonyPatch(typeof(LightDancing), nameof(LightDancing.Update))]
    internal static class FireFlickerPatch
    {
        public enum Mode { Off, Weak, Auto, Vanilla } // Settings.indoorFireFlicker

        private const float weakJitterKept = 0.15f;
        private const float weakAverageRate = 0.05f;
        private const float offAverageRate = 0.002f; // so slow the light practically stays still
        private static readonly Dictionary<IntPtr, Vector3> average = new Dictionary<IntPtr, Vector3>();

        private static void Postfix(LightDancing __instance)
        {
            try
            {
                Mode mode = (Mode)Settings.options.indoorFireFlicker;
                if (LightFixes.insideLights.Count == 0 || mode == Mode.Vanilla)
                {
                    average.Clear();
                    return;
                }
                Light? l = __instance.m_MyLight;
                if (l == null || !LightFixes.insideLights.Contains(l.Pointer)) return;
                l.shadowStrength = 1f;

                // Auto: weak while the player is inside, off while outside
                bool flicker = mode == Mode.Weak || (mode == Mode.Auto && BuildingTracker.current != null);
                float jitterKept = flicker ? weakJitterKept : 0f;
                float averageRate = flicker ? weakAverageRate : offAverageRate;

                Transform t = __instance.transform;
                Vector3 pos = t.localPosition;
                Vector3 avg = average.TryGetValue(__instance.Pointer, out Vector3 prev) ? prev : pos;
                avg = Vector3.Lerp(avg, pos, averageRate);
                average[__instance.Pointer] = avg;
                t.localPosition = avg + (pos - avg) * jitterKept;
            }
            catch (Exception e)
            {
                LogOnce.Error("FireFlickerPatch", e);
            }
        }
    }
}
