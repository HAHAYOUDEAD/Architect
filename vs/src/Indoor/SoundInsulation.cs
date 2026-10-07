namespace Architect
{
    // Muffles outdoor sounds inside buildings using the game's occlusion states, and turns the wind down
    internal static class SoundInsulation
    {
        public enum Level { ByMaterial, Weak, Medium, Strong } // Settings.indoorSoundLevel

        // weakest to strongest
        private static readonly AudioOcclusionLevel[] steps =
        {
            AudioOcclusionLevel.NoOcclusion,
            AudioOcclusionLevel.MildOcclusion,
            AudioOcclusionLevel.MediumOcclusion,
            AudioOcclusionLevel.HeavyOcclusion,
        };

        public static AudioOcclusionLevel desired = AudioOcclusionLevel.NoOcclusion;
        public static float windFactor = 1f;

        private static float WindFactorFor(AudioOcclusionLevel level) => level switch
        {
            AudioOcclusionLevel.HeavyOcclusion => 0.35f,
            AudioOcclusionLevel.MediumOcclusion => 0.6f,
            AudioOcclusionLevel.MildOcclusion => 0.85f,
            _ => 1f,
        };

        private static bool IsBlizzard()
        {
            Weather? w = GameManager.GetWeatherComponent();
            return w != null && w.IsBlizzard();
        }

        private static AudioOcclusionLevel LevelFor(BuildingInfo building)
        {
            Level setting = (Level)Settings.options.indoorSoundLevel;
            int step = setting == Level.ByMaterial ? Array.IndexOf(steps, building.materialOcclusion) : (int)setting;
            if (Settings.options.indoorStrongerInBlizzard && IsBlizzard()) step = steps.Length - 1;
            if (building.AnyDoorOpen()) step--;
            return steps[Math.Max(step, 0)];
        }

        // inside: building the player is in, null when outside
        public static void Update(BuildingInfo? inside)
        {
            desired = inside == null ? AudioOcclusionLevel.NoOcclusion : LevelFor(inside);

            float windVolume = inside == null ? 1f : Settings.options.indoorWindVolume / 100f;
            float factor = WindFactorFor(desired) * windVolume;
            if (factor != windFactor)
            {
                windFactor = factor;
                WindSoundPatch.Reapply();
            }
            ApplyState();
        }

        // The game only sets the occlusion state when an occlusion trigger is entered, so we set it ourselves
        private static void ApplyState()
        {
            if (desired == AudioOcclusionLevel.NoOcclusion) return;
            GameAudioManager? gam = GameAudioManager.Instance;
            if (gam == null || gam.m_CurrentAudioOcclusionLevel == desired) return;
            switch (desired)
            {
                case AudioOcclusionLevel.HeavyOcclusion: gam.SetHeavyOcclusion(); break;
                case AudioOcclusionLevel.MediumOcclusion: gam.SetMediumOcclusion(); break;
                case AudioOcclusionLevel.MildOcclusion: gam.SetMildOcclusion(); break;
            }
        }
    }

    // UpdateAudioOcclusion resets the state when all occlusion trigger counters are zero.
    // While inside, our level is counted for the duration of Update only, so the state can't get stuck after leaving.
    [HarmonyPatch(typeof(GameAudioManager), nameof(GameAudioManager.Update))]
    internal static class OcclusionCounterPatch
    {
        private static void Adjust(GameAudioManager gam, AudioOcclusionLevel level, int delta)
        {
            switch (level)
            {
                case AudioOcclusionLevel.HeavyOcclusion: gam.m_InsideHeavyOcclusionTriggerCount += delta; break;
                case AudioOcclusionLevel.MediumOcclusion: gam.m_InsideMediumOcclusionTriggerCount += delta; break;
                case AudioOcclusionLevel.MildOcclusion: gam.m_InsideMildOcclusionTriggerCount += delta; break;
            }
        }

        private static void Prefix(GameAudioManager __instance, out AudioOcclusionLevel __state)
        {
            __state = SoundInsulation.desired;
            try
            {
                Adjust(__instance, __state, 1);
            }
            catch (Exception e)
            {
                __state = AudioOcclusionLevel.NoOcclusion;
                LogOnce.Error("OcclusionCounterPatch", e);
            }
        }

        private static void Postfix(GameAudioManager __instance, AudioOcclusionLevel __state)
        {
            try
            {
                Adjust(__instance, __state, -1);
            }
            catch (Exception e)
            {
                LogOnce.Error("OcclusionCounterPatch", e);
            }
        }
    }

    // Scales the wind sound parameters that Wind.UpdateProceduralWind sets (ids from SoundbanksInfo.xml)
    [HarmonyPatch(typeof(GameAudioManager), nameof(GameAudioManager.SetRTPCValue))]
    internal static class WindSoundPatch
    {
        private static readonly uint[] windParameters =
        {
            0xC0F0529F, // WindIntensityBlend, most of the loudness
            0xD93040CA, // WindActualSpeed
            0xA58F4439, // WindGustIntensity
        };

        private static readonly Dictionary<uint, float> lastValues = new Dictionary<uint, float>();
        private static bool bypass;

        private static void Prefix(uint rtpcID, ref float rtpcValue)
        {
            if (bypass || Array.IndexOf(windParameters, rtpcID) < 0) return;
            lastValues[rtpcID] = rtpcValue;
            rtpcValue *= SoundInsulation.windFactor;
        }

        // Applies a new factor right away instead of waiting for the next wind update
        public static void Reapply()
        {
            bypass = true;
            try
            {
                foreach (KeyValuePair<uint, float> kv in lastValues)
                {
                    GameAudioManager.SetRTPCValue(kv.Key, kv.Value * SoundInsulation.windFactor, null);
                }
            }
            catch (Exception e)
            {
                LogOnce.Error("WindSoundPatch.Reapply", e);
            }
            finally
            {
                bypass = false;
            }
        }
    }
}
