using Il2CppInterop.Runtime;
using Il2CppTLD.WeatherParticle;
using System.Text;

namespace Architect
{
    // Weather snow is only removed inside "particle killer" boxes. Architect has them around walls and roofs,
    // this adds boxes over each building's interior.
    //
    // The game uses at most 32 active killers and reads its list from the end, so ours go last.
    // ParticleKillerInstance is a 40-byte struct (owner GameObject +0, bounds center +8, extents +20,
    // kills falling snow +32, kills blowing snow +33) that Il2CppInterop doesn't marshal correctly,
    // so the list entries are written directly. Check these offsets after a game update.
    internal static unsafe class SnowKillers
    {
        private const int maxBoxesPerBuilding = 8;
        private const int maxBoxes = 16;
        private const float wallMargin = 1.2f; // remove flakes before they reach the walls...
        private const float roofMargin = 1f;   // ...and the roof
        private const int listItemsOffset = 16, listSizeOffset = 24, arrayDataOffset = 32, entrySize = 40;

        private static GameObject? owner;
        private static List<Bounds> boxes = new List<Bounds>();
        private static string signature = "";

        public static void Reset()
        {
            boxes.Clear();
            signature = "";
        }

        private static bool IsAxisAligned(Transform t)
        {
            float y = Mathf.Repeat(t.eulerAngles.y, 90f);
            return y < 1f || y > 89f;
        }

        private static float Volume(Bounds b) => b.size.x * b.size.y * b.size.z;

        // Merges the pair whose union adds the least volume until at most maxCount boxes remain
        private static List<Bounds> Merge(List<Bounds> input, int maxCount)
        {
            List<Bounds> list = new List<Bounds>(input);
            while (list.Count > maxCount)
            {
                int bi = 0, bj = 1;
                float bestWaste = float.MaxValue;
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        Bounds u = list[i];
                        u.Encapsulate(list[j]);
                        float waste = Volume(u) - Volume(list[i]) - Volume(list[j]);
                        if (waste < bestWaste)
                        {
                            bestWaste = waste;
                            bi = i;
                            bj = j;
                        }
                    }
                }
                Bounds merged = list[bi];
                merged.Encapsulate(list[bj]);
                list[bi] = merged;
                list.RemoveAt(bj);
            }
            return list;
        }

        public static void Update(List<Structure> floors)
        {
            Dictionary<string, List<Structure>> byBuilding = new Dictionary<string, List<Structure>>();
            foreach (Structure f in floors)
            {
                if (!f || !f.interiorTrigger || !f.interiorTrigger.gameObject.activeInHierarchy) continue;
                if (!byBuilding.TryGetValue(f.interiorClusterID, out List<Structure>? list))
                {
                    list = new List<Structure>();
                    byBuilding[f.interiorClusterID] = list;
                }
                list.Add(f);
            }

            List<Bounds> newBoxes = new List<Bounds>();
            foreach (List<Structure> buildingFloors in byBuilding.Values)
            {
                List<Bounds> padded = new List<Bounds>();
                bool aligned = true;
                foreach (Structure f in buildingFloors)
                {
                    BoxCollider? bc = f.interiorTrigger.GetComponent<BoxCollider>();
                    if (bc == null) continue;
                    Bounds b = new Bounds();
                    b.SetMinMax(bc.bounds.min - new Vector3(wallMargin, 0.2f, wallMargin), bc.bounds.max + new Vector3(wallMargin, roofMargin, wallMargin));
                    padded.Add(b);
                    if (!IsAxisAligned(f.transform)) aligned = false;
                }
                if (padded.Count == 0) continue;
                // one box fits an axis-aligned building exactly, a rotated one gets a few
                newBoxes.AddRange(Merge(padded, aligned ? 1 : maxBoxesPerBuilding));
            }
            if (newBoxes.Count > maxBoxes) newBoxes = Merge(newBoxes, maxBoxes);

            // only touch the game's list when the boxes changed
            StringBuilder sb = new StringBuilder();
            foreach (Bounds b in newBoxes) sb.Append(b.center.ToString("F1")).Append(b.size.ToString("F1"));
            string sig = sb.ToString();
            if (sig == signature && owner) return;
            signature = sig;
            boxes = newBoxes;
            Sync();
        }

        private static byte* Entry(IntPtr list, int index)
        {
            IntPtr items = *(IntPtr*)((byte*)list + listItemsOffset);
            return (byte*)items + arrayDataOffset + index * entrySize;
        }

        private static int Size(IntPtr list) => *(int*)((byte*)list + listSizeOffset);

        public static void Sync()
        {
            try
            {
                WeatherParticleManager? manager = GameManager.GetUniStorm()?.m_WeatherParticleManager;
                if (manager == null || manager.m_AllParticleKillers == null) return;
                var killers = manager.m_AllParticleKillers;
                IntPtr lp = killers.Pointer;

                // our entries are recognized by their owner
                if (!owner) owner = new GameObject("Architect_InteriorSnowKillers");
                IntPtr ownerPtr = owner!.Pointer;

                for (int i = Size(lp) - 1; i >= 0; i--)
                {
                    if (*(IntPtr*)Entry(lp, i) == ownerPtr) killers.RemoveAt(i);
                }

                WeatherParticleManager.ParticleKillerInstance placeholder = new WeatherParticleManager.ParticleKillerInstance();
                foreach (Bounds box in boxes)
                {
                    killers.Add(placeholder); // grows the list, the new entry is overwritten below
                    IntPtr items = *(IntPtr*)((byte*)lp + listItemsOffset);
                    byte* e = Entry(lp, Size(lp) - 1);
                    IL2CPP.il2cpp_gc_wbarrier_set_field(items, (IntPtr)e, ownerPtr);
                    *(Vector3*)(e + 8) = box.center;
                    *(Vector3*)(e + 20) = box.extents;
                    e[32] = 1;
                    e[33] = 1;
                }
            }
            catch (Exception e)
            {
                LogOnce.Error("SnowKillers.Sync", e);
            }
        }
    }

    // The game rebuilds its killer list on scene init and when Architect refreshes it
    [HarmonyPatch(typeof(WeatherParticleManager), nameof(WeatherParticleManager.InitializeForScene))]
    internal static class WeatherParticleManagerInitPatch
    {
        private static void Postfix() => SnowKillers.Sync();
    }
}
