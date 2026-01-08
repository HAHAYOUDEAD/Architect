using Harmony;
using Il2Cpp;
using Il2CppSystem.Xml.Serialization;
using Il2CppTLD.Gameplay.Fishing;
using Il2CppTLD.Interactions;
using Il2CppTMPro;
using Il2CppVLB;
using UnityEngine;

namespace Architect
{
    internal class Interior
    {
        public static readonly float storeyHeight = 3f;
        public static readonly float floorDetectionRadius = 2f;

        public static float groundLevel = 0f;
        public static float highestRoof = 0f;
        public static float tempDeltaWalls = 0f;
        public static float tempDeltaFloors = 0f;

        public static HashSet<Structure> isolatedIsland = new();
        public static HashSet<Structure> isolatedIslandPerimeter = new();
        public static HashSet<Structure> isolatedIslandWalls = new();
        public static HashSet<Structure> isolatedIslandRoofs = new();
        public static HashSet<Structure> isolatedIslandPerimeterElevated = new();
        public static HashSet<Structure> interiorFloorTiles = new();

        public static Dictionary<string, HashSet<Structure>> currentRegionClusterSets = new();

        static readonly string[] rngA =
        {
            "Frozen", "Crooked", "Beautiful", "Desperate", "Quiet", "Dilapidated", 
            "Abandoned", "Creaking", "Forlorn", "Bleak", "Hopeful", "Haunted"
        };

        static readonly string[] rngB =
        {
            "Timber", "Corrugated", "Icy", "Wooden", "Rough", "Frostbitten", 
            "Crusty", "Splintered", "Snowy", "Masterwork", "Flawless", "Improvised"
        };
        
        static readonly string[] rngC =
        {
            "Cabin", "Shed", "Outpost", "Lookout", "Hut", "Joint", 
            "Crib", "Shack", "Room", "Corner", "Accomodation", "Quarters"
        };
        
        static readonly string[] rngD =
        {
            "Shelter", "Refuge", "Mistake", "Nuisance", "Perfection", "Thing", 
            "Idea", "Mess", "Afterthought", "Situation", "Resemblance", "Catastrophe"
        };

        static readonly Random rng = new Random();

        private static readonly Color debugBlue = new Color(0.5f, 0.67f, 1f, 1f);
        private static readonly Color debugLightBlue = new Color(0.75f, 0.84f, 1f, 1f);
        private static readonly Color debugGreen = new Color(0.5f, 1f, 0.67f, 1f);
        private static readonly Color debugLightGreen = new Color(0.7f, 1f, 0.8f, 1f);
        private static readonly Color debugYellow = new Color(1f, 0.8f, 0.5f, 1f);
        private static readonly Color debugRed = new Color(1f, 0.42f, 0.46f, 1f);

        private static readonly int debugFlashDuration = 5;

        private static float lastInteriorCheckTimeStamp = 0f;

        public static int coroutineRunning;

        public static string GenerateUniqueClusterName()
        {
            while (true)
            {
                string name =
                    $"{rngA[rng.Next(rngA.Length)]}" +
                    $"{rngB[rng.Next(rngB.Length)]}" +
                    $"{rngC[rng.Next(rngC.Length)]}" +
                    $"{rngD[rng.Next(rngD.Length)]}";

                if (!currentRegionClusterSets.ContainsKey(name))
                    return name;
            }
        }

        public static void CallPorchCheck()
        {
            List<Vector3> ws = new List<Vector3>();

            foreach (Structure w in isolatedIslandWalls)
            {
                if (Mathf.Abs(groundLevel - w.transform.position.y) <= 1f)
                {
                    ws.Add(Snap.GetSnapPointPosition(w, Snap.SnapPoint.Ymin));
                    //ws.Add(Snap.GetSnapPointPosition(w, Snap.SnapPoint.Xmax));
                }
            }

            //Vector3[] sorted = ExtractFarthestLoop(ws).ToArray();
            Vector3[] sorted = SortByNearestNeighbor(ExtractFarthestLoop(ws)).ToArray();

            /*
            for (int i = 0; i < sorted.Count(); i++)
            {
                MelonCoroutines.Start(FlashDebugSphere(sorted[i], i.ToString()));
            }
            */

            foreach (Structure floor in isolatedIsland)
            {
                Vector3 center = floor.renderer.bounds.center;
                bool isIn = IsInsidePerimeter(sorted, center);
                if (isIn)
                {
                    // 
                    interiorFloorTiles.Add(floor);
                }
                if (Settings.options.showDebugInfo)
                {
                    MelonCoroutines.Start(FlashDebugSphere(isIn ? debugGreen : debugRed, center + Vector3.up * 0.3f));
                }
                
            }
        }

        public static bool CallInteriorCheck(Vector3 rayOrigin)
        {
            int layerMask = 0;
            layerMask |= (1 << vp_Layer.InteractiveProp);
            layerMask |= (1 << vp_Layer.Buildings);
            RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, 0.6f, Vector3.down, 2f, layerMask, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                Structure floor = hit.transform.GetComponent<Structure>();
                if (floor && floor.buildPart == Data.BuildPart.Floor)
                {
                    groundLevel = floor.transform.position.y;
                    highestRoof = 0f;
                    MelonCoroutines.Start(DetectInterior(floor));
                    return true;
                }
            }
            if (Settings.options.showDebugInfo)
            {
                MelonLogger.Msg(ConsoleColor.Red, $"Interior check failed: ");
                MelonLogger.Msg("  No floor tile detected");
            }
            HUDMessage.AddMessage(Localization.Get("ARC_InteriorCheckFailed"));
            return false;
        }

        public static IEnumerator DetectInterior(Structure str)
        {
            if (Settings.options.showDebugInfo)
            {
                if (Time.time - lastInteriorCheckTimeStamp <= debugFlashDuration)
                {
                    GameAudioManager.PlayGUIError();
                    yield break;
                }
            }
            else
            {
                if (Time.time - lastInteriorCheckTimeStamp <= 0.5f)
                {
                    GameAudioManager.PlayGUIError();
                    yield break;
                }
            }

            isolatedIsland.Clear();
            isolatedIslandPerimeter.Clear();
            isolatedIslandPerimeterElevated.Clear();
            isolatedIslandWalls.Clear();
            isolatedIslandRoofs.Clear();
            interiorFloorTiles.Clear();

            MelonCoroutines.Start(GrabAdjacent(str, true));

            while (coroutineRunning > 0)
            {
                Log(ConsoleColor.Gray, "Calculating... " + coroutineRunning);
                yield return new WaitForEndOfFrame();
            }

            CallPorchCheck(); // find floor tiles enclosed by walls, discard "porch"

            float interiorCount = interiorFloorTiles.Count * 0.8f; // used with roof calculation
            float outerWallCount = (isolatedIslandPerimeter.Count + isolatedIslandPerimeterElevated.Count) * 0.8f; // used with wall calculation
            float wallCount = isolatedIslandWalls.Count;
            float roofCount = isolatedIslandRoofs.Count;

            foreach (var f in isolatedIslandPerimeter)
            {
                if (f.name.ToLower().Contains("quarter")) outerWallCount -= 0.75f;
                else if (f.name.ToLower().Contains("half") || f.name.ToLower().Contains("narrow")) outerWallCount -= 0.5f;
            }               
            foreach (var f in isolatedIsland)
            {
                if (f.name.ToLower().Contains("quarter")) interiorCount -= 0.75f;
                else if (f.name.ToLower().Contains("half") || f.name.ToLower().Contains("narrow")) interiorCount -= 0.5f;
            }           
            foreach (var w in isolatedIslandWalls)
            {
                if (w.name.ToLower().Contains("halfnarrow")) wallCount -= 0.75f;
                else if (w.name.ToLower().Contains("half") || w.name.ToLower().Contains("narrow")) wallCount -= 0.5f;
            }
            foreach (var r in isolatedIslandRoofs)
            {
                if (r.name.ToLower().Contains("quarter")) roofCount -= 0.75f;
                else if (r.name.ToLower().Contains("half") || r.name.ToLower().Contains("narrow")) roofCount -= 0.5f;
            }

            if (isolatedIsland.Count() > 0 && wallCount >= 3 + outerWallCount && roofCount >= 1 + interiorCount)
            {
                int i = CreateInteriorTriggers(interiorFloorTiles.ToArray());
                int c = CleanupInteriorTriggers(isolatedIsland.ToArray());

                // create or lookup cluster

                if (Settings.options.showDebugInfo)
                {
                    MelonLogger.Msg(ConsoleColor.Green, $"Interior check passed: ");
                    MelonLogger.Msg("  Found interior of size {0} with {1} walls and {2} roofs. Perimeter {3}",
                        interiorFloorTiles.Count, wallCount, roofCount, isolatedIslandPerimeter.Count + isolatedIslandPerimeterElevated.Count);
                    if (i <= 0) MelonLogger.Msg("  No new interior tiles were created");
                    else MelonLogger.Msg($"  Created {i} new interior tiles");
                    if (c > 0) MelonLogger.Msg($"  Cleaned up {c} old interior tiles");
                }
                HUDMessage.AddMessage(Localization.Get("ARC_InteriorCheckPassed"));
            }
            else
            {
                if (Settings.options.showDebugInfo)
                {
                    MelonLogger.Msg(ConsoleColor.Red, $"Interior check failed: ");
                    if (isolatedIsland.Count() <= 0) MelonLogger.Msg("  No floor tile detected");
                    if (roofCount < 1 + interiorCount) MelonLogger.Msg($"  Expected roof count > {1 + interiorCount}, got {roofCount}");
                    if (wallCount < 3 + outerWallCount) MelonLogger.Msg($"  Expected wall count > {3 + outerWallCount}, got {wallCount}");
                }

                HUDMessage.AddMessage(Localization.Get("ARC_InteriorCheckFailed"));
            }

            lastInteriorCheckTimeStamp = Time.time;

            yield break;
        }

        public static IEnumerator GrabAdjacent(Structure str, bool isFirst = false)
        {
            coroutineRunning++;

            str.SphereCastAdjacentStructures();

            if (isFirst)
            {
                isolatedIsland.Add(str);

                if (!str.isEnclosedFloorTile && !isolatedIslandPerimeter.Contains(str))
                {
                    isolatedIslandPerimeter.Add(str);
                }

                if (Settings.options.showDebugInfo)
                {
                    Color c = str.isEnclosedFloorTile ? debugLightGreen : debugGreen;
                    MelonCoroutines.Start(Interior.FlashTile(str, c));
                }
            }

            foreach (Structure aStr in str.adjacentFloors)
            {
                if (isolatedIsland.Contains(aStr)) continue;

                isolatedIsland.Add(aStr);

                if (aStr.buildMaterial == Data.BuildMaterial.WoodPlank)
                {
                    tempDeltaFloors += 5f;
                }
                if (aStr.buildMaterial == Data.BuildMaterial.WoodLog)
                {
                    if (aStr.name.ToLower().Contains("elevated")) tempDeltaWalls += 12f;
                    else tempDeltaFloors += 8f;
                }
                if (aStr.buildMaterial == Data.BuildMaterial.Stone)
                {
                    tempDeltaFloors += 10f;
                }
                if (aStr.buildMaterial == Data.BuildMaterial.MetalSheet)
                {
                    tempDeltaFloors += 3f;
                }

                MelonCoroutines.Start(GrabAdjacent(aStr));

                if (!aStr.isEnclosedFloorTile && !isolatedIslandPerimeter.Contains(aStr))
                {
                    isolatedIslandPerimeter.Add(aStr);
                }

                if (Settings.options.showDebugInfo)
                {
                    Color c = aStr.isEnclosedFloorTile ? debugLightGreen : debugGreen;
                    MelonCoroutines.Start(Interior.FlashTile(aStr, c));
                }
            }
            str.adjacentFloors.Clear();
            
            foreach (Structure aStr in str.elevatedFloors)
            {
                if (isolatedIslandPerimeterElevated.Contains(aStr)) continue;

                if (!aStr.isEnclosedFloorTile) isolatedIslandPerimeterElevated.Add(aStr);

                if (Settings.options.showDebugInfo)
                {
                    MelonCoroutines.Start(Interior.FlashTile(aStr, debugLightBlue));
                }
            }                
            str.elevatedFloors.Clear();

            foreach (Structure aStr in str.adjacentWalls)
            {
                if (isolatedIslandWalls.Contains(aStr)) continue;

                isolatedIslandWalls.Add(aStr);

                if (aStr.buildMaterial == Data.BuildMaterial.WoodPlank)
                {
                    tempDeltaWalls += 5f;
                }                
                if (aStr.buildMaterial == Data.BuildMaterial.WoodLog)
                {
                    tempDeltaWalls += 8f;
                }                
                if (aStr.buildMaterial == Data.BuildMaterial.Stone)
                {
                    tempDeltaWalls += 10f;
                }                
                if (aStr.buildMaterial == Data.BuildMaterial.MetalSheet)
                {
                    tempDeltaWalls += 3f;
                }

                if (Settings.options.showDebugInfo)
                {
                    MelonCoroutines.Start(Interior.FlashTile(aStr, debugYellow));
                }
            }
            str.adjacentWalls.Clear();

            foreach (Structure aStr in str.adjacentRoofs)
            {
                if (isolatedIslandRoofs.Contains(aStr)) continue;

                isolatedIslandRoofs.Add(aStr);

                if (aStr.transform.position.y > highestRoof || highestRoof == 0f)
                {
                    highestRoof = aStr.transform.position.y;
                }

                if (Settings.options.showDebugInfo)
                {
                    MelonCoroutines.Start(Interior.FlashTile(aStr, debugBlue));
                }
            }
            str.adjacentRoofs.Clear();

            coroutineRunning--;
            yield break;
        }

        public static int CreateInteriorTriggers(Structure[] floors)
        {
            int i = 0;
            foreach (Structure f in floors)
            {
                if (f.transform.Find(interiorTriggerName) == null)
                {
                    Mesh m = f.GetComponent<MeshFilter>().mesh;
                    GameObject interior = new(interiorTriggerName);
                    interior.transform.SetParent(f.transform);
                    interior.transform.localPosition = Vector3.zero;
                    interior.transform.localRotation = Quaternion.identity;
                    interior.layer = vp_Layer.TriggerIgnoreRaycast;
                    BoxCollider box = interior.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(m.bounds.size.x, highestRoof - groundLevel, m.bounds.size.z);
                    box.center = new Vector3(m.bounds.center.x, (highestRoof - groundLevel) / 2f, m.bounds.center.z);
                    IndoorSpaceTrigger ist = interior.AddComponent<IndoorSpaceTrigger>();
                    ist.m_UseOutdoorTemperature = true;
                    ist.m_TemperatureDeltaCelsius = 10f;
                    ist.m_AllowDropTravois = true;
                    if (f.buildMaterial == Data.BuildMaterial.Stone) ist.m_AllowCampfires = true;
                    f.interiorTrigger = ist;
                    i++;
                }
            }
            return i;
        }
        public static int CleanupInteriorTriggers(Structure[] floors)
        {
            int i = 0;
            foreach (Structure f in floors)
            {
                GameObject? trigger = f.transform.Find(interiorTriggerName)?.gameObject;
                if (trigger && !interiorFloorTiles.Contains(f))
                {
                    GameObject.Destroy(trigger);
                    i++;
                }
            }
            return i;
        }

        /*
        private static List<Vector3> SortByNearestNeighbor(List<Vector3> points) // GPT
        {
            if (points == null || points.Count == 0)
                return new List<Vector3>();

            var sorted = new List<Vector3>();
            var remaining = new List<Vector3>(points);

            // Start from the first point (or find the leftmost if you want consistency)
            var current = remaining[0];
            sorted.Add(current);
            remaining.RemoveAt(0);

            while (remaining.Count > 0)
            {
                // Find nearest remaining point
                float bestDist = float.MaxValue;
                int bestIndex = -1;

                for (int i = 0; i < remaining.Count; i++)
                {
                    float dist = Vector3.SqrMagnitude(remaining[i] - current);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIndex = i;
                    }
                }

                current = remaining[bestIndex];
                sorted.Add(current);
                remaining.RemoveAt(bestIndex);
            }

            return sorted;
        }

        */
        private static List<Vector3> SortByNearestNeighbor(List<Vector3> points, float epsilon = 0.01f) // GPT
        {
            if (points == null || points.Count == 0)
                return new List<Vector3>();

            var sorted = new List<Vector3>();
            var remaining = new List<Vector3>(points);

            // Start from the first point
            var current = remaining[0];
            sorted.Add(current);
            remaining.RemoveAt(0);

            while (remaining.Count > 0)
            {
                float bestDist = float.MaxValue;
                int bestIndex = -1;

                for (int i = 0; i < remaining.Count; i++)
                {
                    float dist = Vector3.SqrMagnitude(remaining[i] - current);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIndex = i;
                    }
                }

                // Only add if farther than epsilon
                if (bestDist > epsilon * epsilon)
                    sorted.Add(remaining[bestIndex]);

                current = remaining[bestIndex];
                remaining.RemoveAt(bestIndex);

                // Remove any other points within epsilon of current
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    if (Vector3.SqrMagnitude(remaining[i] - current) <= epsilon * epsilon)
                        remaining.RemoveAt(i);
                }
            }

            return sorted;
        }
        private static bool IsInsidePerimeter(Vector3[] walls, Vector3 point) // GPT
        {
            if (walls == null || walls.Length < 3)
                return false;

            int intersections = 0;
            float px = point.x;
            float pz = point.z;

            for (int i = 0; i < walls.Length; i++)
            {
                Vector3 a = walls[i];
                Vector3 b = walls[(i + 1) % walls.Length];

                float ax = a.x;
                float az = a.z;
                float bx = b.x;
                float bz = b.z;

                // Check if the horizontal ray at point.z crosses this edge
                bool crosses = (az > pz) != (bz > pz);
                if (!crosses) continue;

                // Find where that edge intersects the ray (the X position)
                float xCross = (bx - ax) * (pz - az) / (bz - az) + ax;

                // If the intersection is to the right of the point, count it
                if (px < xCross)
                    intersections++;
            }

            // Odd = inside, Even = outside
            return (intersections % 2) == 1;
        }

        private static List<Vector3> ExtractFarthestLoop(List<Vector3> points, float returnThreshold = 2.5f) // GPT
        {
            if (points == null || points.Count < 3)
                return new List<Vector3>();

            // find centroid
            Vector3 centroid = Vector3.zero;
            foreach (var p in points) centroid += p;
            centroid /= points.Count;

            // find start, farthest from centroid
            Vector3 start = points
                .OrderByDescending(p => (p - centroid).sqrMagnitude)
                .First();

            List<Vector3> result = new() { start };
            //List<Vector3> resultOpposite = new();
            HashSet<int> visited = new();
            visited.Add(points.IndexOf(start));

            int grace = points.Count() / 6;
            int iteration = 0;

            Vector3 current = start;

            while (true)
            {
                float bestDist = -1f;
                int bestIndex = -1;
                

                // find farthest point from current
                for (int i = 0; i < points.Count; i++)
                {
                    if (visited.Contains(i)) continue;

                    float d = (points[i] - current).sqrMagnitude;

                    if (d > bestDist)
                    {
                        bestDist = d;
                        bestIndex = i;
                    }
                }

                // no more valid candidates
                if (bestIndex == -1)
                    break;

                Vector3 next = points[bestIndex];

                // check if we returned close to start after grace period
                if (iteration > grace && (next - start).sqrMagnitude < returnThreshold * returnThreshold)
                    break;

                // add the next perimeter point
                //if (iteration % 2 == 0) resultOpposite.Add(next); // only works for square 
                //else result.Add(next);
                result.Add(next);
                visited.Add(bestIndex);
                current = next;
                iteration++;
            }
            //result.AddRange(resultOpposite);

            return result;
        }



        public static IEnumerator FlashTile(Structure s, Color c)
        {
            Color i = s.insidePaintColor;
            Color o = s.outsidePaintColor;

            s.insidePaintColor = c;
            s.outsidePaintColor = c;
            s.Finalize();

            yield return new WaitForSeconds(debugFlashDuration);

            s.insidePaintColor = i;
            s.outsidePaintColor = o;
            s.Finalize();

        }


        public static IEnumerator FlashDebugSphere(Color c, Vector3 position, string text = "", float radius = 0.2f)
        {
            float t = 0f;

            if (c == Color.black)
            {
                c = debugGreen;
            }

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * radius * 2f; // scale based on radius

            Shader s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Sprites/Default");
            Material mat = new Material(s);
            mat.color = c * new Color(1f,1f,1f,0.6f);
            var renderer = sphere.GetComponent<Renderer>();
            renderer.material = mat;

            GameObject textObj = new GameObject("Text");

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.font = GameManager.GetFontManager().m_LatinTMPFont;
            tmp.color = debugGreen;
            tmp.fontSize = 8;
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.lineSpacing = -30f;
            tmp.text = text;
            //text.fontSize = 50;

            textObj.transform.SetParent(sphere.transform);
            textObj.transform.localPosition = new Vector3(0f, radius + 0.01f, 0f);
            textObj.transform.rotation = Quaternion.identity;
            textObj.GetComponent<Renderer>().material.renderQueue = 4000;

            GameObject.Destroy(sphere.GetComponent<Collider>());

            while (t < debugFlashDuration)
            {
                t += Time.deltaTime;
                //textObj.transform.forward = (GameManager.GetPlayerTransform().position + Vector3.up * 1.8f) - textObj.transform.position;
                textObj.transform.rotation = Quaternion.LookRotation(textObj.transform.position - (GameManager.GetPlayerTransform().position + Vector3.up * 1.8f));
                yield return new WaitForEndOfFrame();
            }

            
            //yield return new WaitForSeconds(debugFlashDuration);

            GameObject.Destroy(sphere);
        }

        public static IEnumerator FlashDebugRay(Vector3 v1, Vector3 v2, Color c)
        {
            LineRenderer debugLine1;
            GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);

            cube1.transform.localScale = new Vector3(0f, 0f, 0f);

            cube1.transform.position = v1;

            debugLine1 = cube1.AddComponent<LineRenderer>();
            debugLine1.material = new Material(Shader.Find("Sprites/Default"));
            debugLine1.widthMultiplier = 0.02f;
            float alpha = 1.0f;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(c, 0.0f), new GradientColorKey(c, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
            );
            debugLine1.colorGradient = gradient;

            debugLine1.SetPosition(0, v1);// + Vector3.up * 0.2f);
            debugLine1.SetPosition(1, v2);// + Vector3.up * 0.2f);
            /*
                int layerMask = 0;
    
                layerMask |= (1 << vp_Layer.InteractiveProp);
                layerMask |= (1 << vp_Layer.InteractivePropNoCollidePlayer);
    
                //layerMask ^= (1 << layerToExclude);
                //layerMask |= (1 << layerToInclude);
    
                if (Physics.SphereCast(v1 + Vector3.up, 0.3f, Vector3.down, out RaycastHit hit1, 2, layerMask))
                {
                    //MelonLogger.Msg("hit1: " + hit1.collider?.gameObject?.name);
                    MelonCoroutines.Start(FlashTile(hit1.collider?.gameObject?.GetComponent<Structure>(), c));
                }
                if (Physics.SphereCast(v2 + Vector3.up, 0.3f, Vector3.down, out RaycastHit hit2, 2, layerMask))
                {
                    //MelonLogger.Msg("hit2: " + hit2.collider?.gameObject?.name);
                    MelonCoroutines.Start(FlashTile(hit2.collider?.gameObject?.GetComponent<Structure>(), c));
                }
                */


            yield return new WaitForSeconds(debugFlashDuration);

            GameObject.Destroy(cube1);

            yield break;

        }


    }



    




    /*
    static float adjacentDistance = 2.6f;

    static int compareX(Vector3 a, Vector3 b)
    {
        return a.x.CompareTo(b.x);
    }

    static int compareY(Vector3 a, Vector3 b)
    {
        return a.y.CompareTo(b.y);
    }

    static int compareZ(Vector3 a, Vector3 b)
    {
        return a.z.CompareTo(b.z);
    }


    public static int FindClosestIndex(Vector3[] v, Vector3 target, Vector3 direction) // find closest to given coordinate in array 
    {
        int closest = int.MaxValue;
        float minDifference = float.MaxValue;

        if (direction.x > 0) // X
        {
            for (int i = 0; i < v.Length; i++)
            {
                float difference = Math.Abs(v[i].x - target.x);
                if (minDifference > difference)
                {
                    minDifference = difference;
                    closest = i;
                }
            }
            if (minDifference > adjacentDistance) closest = -1;
        }
        if (direction.z > 0) // Z
        {
            for (int i = 0; i < v.Length; i++)
            {
                float difference = Math.Abs(v[i].z - target.z);
                if (minDifference > difference)
                {
                    minDifference = difference;
                    closest = i;
                }
            }
            if (minDifference > adjacentDistance) closest = -1;
        }
        return closest;
    }

    public static Vector3[] SiftAdjacent(Vector3[] v, Vector3 startPoint, bool drawDebug = false) // go through array and remove entries further away from each other than threshold
    {
        if (v.Length == 0) return new Vector3[0];
        if (FindClosestIndex(v, startPoint, Vector3.right) == -1 || FindClosestIndex(v, startPoint, Vector3.forward) == -1) return new Vector3[0];

        Array.Sort(v, compareX);

        for (int i = FindClosestIndex(v, startPoint, Vector3.right); i < v.Length; i++) // sift right side of the array
        {
            if (i < v.Length - 1)
            {
                if (Mathf.Abs(v[i + 1].x - v[i].x) > adjacentDistance)
                {
                    v = v.Take(i + 1).ToArray();
                    break;
                }
                else
                {
                    if (Settings.options.showDebugInfo && drawDebug) MelonCoroutines.Start(CreateRay(v[i + 1], v[i], Color.white));
                }
            }
        }
        for (int i = FindClosestIndex(v, startPoint, Vector3.right); i >= 0; i--) // sift left side of the array
        {
            if (i > 0)
            {
                if (Mathf.Abs(v[i - 1].x - v[i].x) > adjacentDistance)
                {
                    v = v.Skip(i).ToArray();
                    break;
                }
                else
                {
                    if (Settings.options.showDebugInfo && drawDebug) MelonCoroutines.Start(CreateRay(v[i - 1], v[i], Color.gray));
                }
            }
        }
        Array.Sort(v, compareZ);

        for (int i = FindClosestIndex(v, startPoint, Vector3.forward); i < v.Length; i++) // sift right side of the array
        {
            if (i < v.Length - 1)
            {
                if (Mathf.Abs(v[i + 1].z - v[i].z) > adjacentDistance)
                {
                    v = v.Take(i + 1).ToArray();
                    break;
                }
                else
                {
                    if (Settings.options.showDebugInfo && drawDebug) MelonCoroutines.Start(CreateRay(v[i + 1], v[i], Color.cyan));
                }
            }
        }
        for (int i = FindClosestIndex(v, startPoint, Vector3.forward); i >= 0; i--) // sift left side of the array
        {
            if (i > 0)
            {
                if (Mathf.Abs(v[i - 1].z - v[i].z) > adjacentDistance)
                {
                    v = v.Skip(i).ToArray();
                    break;
                }
                else
                {
                    if (Settings.options.showDebugInfo && drawDebug) MelonCoroutines.Start(CreateRay(v[i - 1], v[i], Color.blue));
                }
            }
        }
        if (Settings.options.showDebugInfo && drawDebug)
        {
            foreach (Vector3 vv in v)
            {
                MelonCoroutines.Start(CreateRay(vv, vv + Vector3.up, Color.green));
            }
        }
        return v;
    }

    public static IEnumerator DrawDebugCircle(Vector3 start, float r, Color c)
    {
        LineRenderer debugLine1;
        GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);

        int segments = (int)r * 4;

        cube1.transform.localScale = new Vector3(0f, 0f, 0f);
        cube1.transform.position = start;

        debugLine1 = cube1.AddComponent<LineRenderer>();
        debugLine1.material = new Material(Shader.Find("Sprites/Default"));
        debugLine1.widthMultiplier = 0.1f;
        float alpha = 1.0f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(c, 0.0f), new GradientColorKey(c, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
        );
        debugLine1.colorGradient = gradient;
        debugLine1.SetVertexCount(segments + 1);


        float x;
        float y;
        float z;

        float angle = 0f;

        for (int i = 0; i < (segments + 1); i++)
        {
            x = start.x + Mathf.Sin(Mathf.Deg2Rad * angle) * r;
            z = start.z + Mathf.Cos(Mathf.Deg2Rad * angle) * r;

            debugLine1.SetPosition(i, new Vector3(x, start.y, z));

            angle += (360f / segments);
            yield return new WaitForEndOfFrame();
        }


        yield return new WaitForSeconds(5f);

        GameObject.Destroy(cube1);

        yield break;
    }
    */
}
