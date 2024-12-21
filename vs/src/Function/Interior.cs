namespace Architect
{
    internal class Interior
    {
        public static readonly float storeyHeight = 2f;
        public static readonly float floorDetectionRadius = 2f;

        public static List<Structure> isolatedIsland = new List<Structure>();
        public static List<Structure> isolatedIslandPerimeter = new List<Structure>();
        public static List<Structure> isolatedIslandWalls = new List<Structure>();
        public static List<Structure> isolatedIslandRoofs = new List<Structure>();

        public static int coroutineRunning;

        public static IEnumerator DetectInterior(Structure str)
        {
            isolatedIsland.Clear();
            isolatedIslandPerimeter.Clear();
            isolatedIslandWalls.Clear();
            isolatedIslandRoofs.Clear();

            MelonCoroutines.Start(GrabAdjacent(str, true));

            while (coroutineRunning > 0)
            {
                if (Settings.options.showDebugInfo) MelonLogger.Msg("Calculating... " + coroutineRunning);
                yield return new WaitForEndOfFrame();
            }

            if (Settings.options.showDebugInfo) MelonLogger.Msg("Found island of size {0} with {1} walls and {2} roofs. Perimeter {3}", 
                isolatedIsland.Count, isolatedIslandWalls.Count, isolatedIslandRoofs.Count, isolatedIslandPerimeter.Count );






            // !














            //периметр считается на нескольких этажах, так же как и общее количество плиток пола, надо либо вообще не учитывать второй этаж либо разделять















            // !

            if (isolatedIslandWalls.Count >= isolatedIslandPerimeter.Count * 0.8f && isolatedIslandRoofs.Count >= isolatedIsland.Count * 0.8f)
            {
                HUDMessage.AddMessage("Interior check passed");
            }
            else
            {
                HUDMessage.AddMessage("Interior check failed");
            }


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
                    Color c = str.isEnclosedFloorTile ? Color.red : Color.green;
                    MelonCoroutines.Start(Interior.FlashTile(str, c));
                }

            }

            foreach (Structure aStr in str.adjacentFloors)
            {
                if (isolatedIsland.Contains(aStr)) continue;

                isolatedIsland.Add(aStr);

                MelonCoroutines.Start(GrabAdjacent(aStr));

                if (!aStr.isEnclosedFloorTile && !isolatedIslandPerimeter.Contains(aStr))
                {
                    isolatedIslandPerimeter.Add(aStr);
                }

                if (Settings.options.showDebugInfo)
                {
                    Color c = aStr.isEnclosedFloorTile ? Color.red : Color.green;
                    MelonCoroutines.Start(Interior.FlashTile(aStr, c));
                }
            }

            foreach (Structure aStr in str.adjacentWalls)
            {
                if (isolatedIslandWalls.Contains(aStr)) continue;

                isolatedIslandWalls.Add(aStr);

                if (Settings.options.showDebugInfo)
                {
                    MelonCoroutines.Start(Interior.FlashTile(aStr, Color.yellow));
                }
            }

            foreach (Structure aStr in str.adjacentRoofs)
            {
                if (isolatedIslandRoofs.Contains(aStr)) continue;

                isolatedIslandRoofs.Add(aStr);

                if (Settings.options.showDebugInfo)
                {
                    MelonCoroutines.Start(Interior.FlashTile(aStr, Color.blue));
                }
            }

            coroutineRunning--;
            yield break;
        }


        public static IEnumerator FlashTile(Structure s, Color c)
        {
            s.insidePaintColor = c;
            s.outsidePaintColor = c;
            s.Finalize();

            yield return new WaitForSeconds(3f);

            s.insidePaintColor = Color.black;
            s.outsidePaintColor = Color.black;
            s.Finalize();

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


            yield return new WaitForSeconds(5f);

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
