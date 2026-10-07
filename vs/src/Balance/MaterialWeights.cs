using Il2CppTLD.Gear;
using Il2CppTLD.IntBackedUnit;

namespace Architect
{
    // Weight of logs, planks and their bundles. Realistic values assume the lightest wood (cedar, 0.5 kg a piece):
    // log bundle 9 x 0.5 = 4.5 kg, log 4.5 / 3; plank bundle 8 x 0.5 = 4 kg, plank 4 / 8
    internal static class MaterialWeights
    {
        public enum Mode { Architect, Realistic } // Settings.balanceMaterialWeight

        private static readonly string[] items = { "GEAR_ARC_log", "GEAR_ARC_logbundle", "GEAR_ARC_plank", "GEAR_ARC_plankbundle" };
        private static readonly float[] realisticKg = { 1.5f, 4.5f, 0.5f, 4f };

        private static readonly Dictionary<string, ItemWeight> original = new Dictionary<string, ItemWeight>();
        private static readonly Dictionary<string, ItemWeight> target = new Dictionary<string, ItemWeight>();
        private static Mode? appliedMode;

        // New items take the weight from GearItemData, existing ones have their own copy
        public static void Apply()
        {
            try
            {
                Mode mode = (Mode)Settings.options.balanceMaterialWeight;
                for (int i = 0; i < items.Length; i++)
                {
                    GearItem? prefab = GearItem.LoadGearItemPrefab(items[i]);
                    if (prefab == null || prefab.GearItemData == null) continue;
                    GearItemData data = prefab.GearItemData;
                    if (!original.ContainsKey(items[i])) original[items[i]] = data.m_BaseWeight;
                    data.m_BaseWeight = mode == Mode.Realistic ? ItemWeight.FromKilograms(realisticKg[i]) : original[items[i]];
                    target[items[i]] = data.m_BaseWeight;
                }
                if (target.Count == 0) return;

                foreach (GearItem gi in Resources.FindObjectsOfTypeAll<GearItem>()) FixInstance(gi);

                if (appliedMode != mode) MelonLogger.Msg("Material weight: " + mode);
                appliedMode = mode;
            }
            catch (Exception e)
            {
                LogOnce.Error("MaterialWeights.Apply", e);
            }
        }

        public static void FixInstance(GearItem gi)
        {
            if (!gi || target.Count == 0) return;
            string name = gi.name.Replace("(Clone)", "").Trim();
            if (target.TryGetValue(name, out ItemWeight weight)) gi.WeightKG = weight;
        }
    }

    // Loading a save restores each item's saved weight
    [HarmonyPatch(typeof(GearItem), nameof(GearItem.Deserialize), new Type[] { typeof(GearItemSaveDataProxy), typeof(bool) })]
    internal static class GearItemDeserializeWeightPatch
    {
        private static void Postfix(GearItem __instance)
        {
            try
            {
                MaterialWeights.FixInstance(__instance);
            }
            catch (Exception e)
            {
                LogOnce.Error("GearItemDeserializeWeightPatch", e);
            }
        }
    }
}
