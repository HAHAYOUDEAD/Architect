namespace Architect
{
    // Heat model numbers for DynamicTemperature (converted to its InteriorSpaceConfig by DynamicTemperatureBridge)
    internal class ThermalProfile
    {
        public bool fallback = true; // not analyzed yet
        public float square, conductivity, volume, density, heatCapacity, airVolume, windowSquare;
    }

    // One building = all floor tiles with the same interiorClusterID
    internal class BuildingInfo
    {
        public string id = "";
        public string spaceName = ""; // DynamicTemperature space name
        public ThermalProfile thermal = new ThermalProfile();
        public List<Structure> doors = new List<Structure>(); // doors and window shutters
        public AudioOcclusionLevel materialOcclusion = AudioOcclusionLevel.MediumOcclusion;
        public float analyzedAt;

        public bool AnyDoorOpen()
        {
            foreach (Structure door in doors)
            {
                if (!door) continue;
                OpenClose? oc = door.GetComponent<OpenClose>();
                if (oc != null && oc.IsOpen()) return true;
            }
            return false;
        }
    }

    // Finds walls, roof, doors and windows around a building's floor tiles
    internal static class BuildingAnalyzer
    {
        // U-value (W/m2K) and heat capacity per m2 (kJ/K); planks match DynamicTemperature's small wooden cabins
        private static float UValue(Data.BuildMaterial m) => m switch
        {
            Data.BuildMaterial.WoodLog => 0.75f,
            Data.BuildMaterial.Stone => 1.4f,
            Data.BuildMaterial.MetalSheet => 5f,
            _ => 1.5f,
        };

        private static float HeatCapacityPerM2(Data.BuildMaterial m) => m switch
        {
            Data.BuildMaterial.WoodLog => 105f,
            Data.BuildMaterial.Stone => 220f,
            Data.BuildMaterial.MetalSheet => 15f,
            _ => 60f,
        };

        private static AudioOcclusionLevel Occlusion(Data.BuildMaterial m) => m switch
        {
            Data.BuildMaterial.WoodLog or Data.BuildMaterial.Stone => AudioOcclusionLevel.HeavyOcclusion,
            Data.BuildMaterial.MetalSheet => AudioOcclusionLevel.MildOcclusion,
            _ => AudioOcclusionLevel.MediumOcclusion,
        };

        private const float windowArea = 1f;
        private const float minCeilingHeight = 1.2f;
        private static readonly Vector3 wallMargin = new Vector3(0.8f, 0.5f, 0.8f);
        private static readonly Vector3 roofMarginBottom = new Vector3(1.5f, 0f, 1.5f);
        private static readonly Vector3 roofMarginTop = new Vector3(1.5f, 2.5f, 1.5f);

        // Area of a wall or roof panel: its two largest dimensions
        private static float SurfaceArea(Structure s)
        {
            Vector3 size = Vector3.zero;
            MeshFilter? mf = s.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh)
            {
                size = Vector3.Scale(mf.sharedMesh.bounds.size, s.transform.lossyScale);
                size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            }
            else if (s.renderer) size = s.renderer.bounds.size;

            float[] d = { size.x, size.y, size.z };
            Array.Sort(d);
            float area = d[2] * d[1];
            return area > 0.01f ? area : 4f;
        }

        private static Vector3 CenterOf(Structure s) => s.renderer ? s.renderer.bounds.center : s.transform.position;

        public static BuildingInfo Analyze(string id, List<Structure> floors)
        {
            BuildingInfo info = new BuildingInfo
            {
                id = id,
                spaceName = DynamicTemperatureBridge.SpaceNameFor(id),
                analyzedAt = Time.realtimeSinceStartup
            };

            // interior volume
            bool any = false;
            Bounds interior = new Bounds();
            float floorArea = 0f;
            HashSet<IntPtr> ownFloors = new HashSet<IntPtr>();
            foreach (Structure f in floors)
            {
                if (!f || f.interiorClusterID != id || !f.interiorTrigger) continue;
                BoxCollider? bc = f.interiorTrigger.GetComponent<BoxCollider>();
                if (bc == null) continue;
                if (!any) interior = bc.bounds;
                else interior.Encapsulate(bc.bounds);
                any = true;
                floorArea += bc.size.x * bc.size.z;
                ownFloors.Add(f.Pointer);
            }
            if (!any) return info;

            Bounds wallZone = new Bounds();
            wallZone.SetMinMax(interior.min - wallMargin, interior.max + wallMargin);
            Bounds roofZone = new Bounds();
            roofZone.SetMinMax(interior.min - roofMarginBottom, interior.max + roofMarginTop);
            float ceilingMinY = interior.min.y + minCeilingHeight;

            float square = 0f, ua = 0f, capacity = 0f, windows = 0f;
            Dictionary<Data.BuildMaterial, float> wallAreaByMaterial = new Dictionary<Data.BuildMaterial, float>();

            foreach (Structure s in StructureManager.structures)
            {
                if (!s || !s.isBuilt || s.isBeingPlaced) continue;
                Data.BuildPart part = s.buildPart;
                bool isWall = part == Data.BuildPart.Wall || part == Data.BuildPart.Door;
                // floor tiles of the storey above count as ceiling
                bool isRoof = part == Data.BuildPart.Roof || (part == Data.BuildPart.Floor && !ownFloors.Contains(s.Pointer));
                if (!isWall && !isRoof) continue;

                Vector3 c = CenterOf(s);
                if (isWall && !wallZone.Contains(c)) continue;
                if (isRoof && (!roofZone.Contains(c) || c.y < ceilingMinY)) continue;

                if (part == Data.BuildPart.Door)
                {
                    if (s.doorType == Data.DoorVariant.Fence) continue;
                    info.doors.Add(s);
                }

                float a = SurfaceArea(s);
                Data.BuildMaterial m = s.buildMaterial;
                square += a;
                ua += a * UValue(m);
                capacity += a * HeatCapacityPerM2(m) * 1000f;

                if (isWall)
                {
                    wallAreaByMaterial.TryGetValue(m, out float prev);
                    wallAreaByMaterial[m] = prev + a;
                    if (part == Data.BuildPart.Wall && s.gameObject.name.ToLowerInvariant().Contains("window")) windows += windowArea;
                }
            }

            if (square < 1f) return info;

            // sound follows the main wall material
            Data.BuildMaterial main = Data.BuildMaterial.WoodPlank;
            float best = -1f;
            foreach (KeyValuePair<Data.BuildMaterial, float> kv in wallAreaByMaterial)
            {
                if (kv.Value > best)
                {
                    best = kv.Value;
                    main = kv.Key;
                }
            }
            info.materialOcclusion = Occlusion(main);

            // mixed materials packed into DynamicTemperature's single-material model (thickness = 1)
            ThermalProfile p = info.thermal;
            p.fallback = false;
            p.square = square;
            p.conductivity = ua / square;
            p.volume = square;
            p.density = 1f;
            p.heatCapacity = capacity / (square * 1000000f);
            p.airVolume = Mathf.Max(floorArea * interior.size.y, 1f);
            p.windowSquare = windows;
            return info;
        }
    }
}
