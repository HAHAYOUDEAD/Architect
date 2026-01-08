using Il2Cpp;
using UnityEngine;
using static Architect.Snap;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Playables;
using static UnityEngine.GraphicsBuffer;
using static Il2CppRewired.Controller;
using Il2CppRewired;

namespace Architect
{
    public class Snap
    {
        public static readonly int snapTriggerLayer = vp_Layer.CharacterControllerCollideOnly;
        private static SnapPoint currentLookAtSnapPoint = SnapPoint.Unspecified;
        private static int currentCustomRotation = 0;
        public static bool requestUpdate = false;
        private static readonly float maxSnapDistance = 5f; // how far will object look for snap points
        private static readonly float intolerance = 0.004f; // how much the snap points should be embedded into the objects

        public static float nudgeInterval = 0.5f;
        public static float nudgeSpeedupDelay = 2.2f;
        public static float nudgeTimer = 0f;
        public static float nudgeHeldTimer = 0f;

        public enum SnapPoint
        {
            Unspecified = 0,
            Pivot,
            Zmax, // Z+ center
            Zmin, // Z- center
            Xmax, // X+ center
            Xmin, // X- center
            Ymin, // low center
            Ymid, // center
            Ymax, // top center
            ZpXp, // Z+ X+ corner (Y center)
            ZpXn, // Z+ X- corner (Y center)
            ZnXp, // Z- X+ corner (Y center)
            ZnXn, // Z- X- corner (Y center)
            YpXp, // top X+ corner (for walls)
            YpXn, // top X- corner (for walls)
            YnXp, // low X+ corner (for walls)
            YnXn, // low X- corner (for walls)
        }

        public enum SnapPattern
        {
            Undefined,
            Free,
            Special,
            Single,
            Floor,
            Wall,
            Roof,
            Stairs,
        }

        private enum SpecialPoint
        { 
            DoorPoint,
            DoorPointMirror,
            WindowPoint,
            StairsPoint,
            StairsRailingPoint,
            StairsRailingPointMirror,
            FencePoint,
            FencePointMirror,
        }
        public static void ResetObject()
        {
            currentLookAtSnapPoint = SnapPoint.Unspecified;
            forceShowCrosshair = false;
            //currentCustomRotation = 0;
        }

        public static Vector3 GetSnapPointPosition(Structure str, SnapPoint sp)
        {
            Mesh m = str.GetComponent<MeshFilter>().mesh;
            Vector3 c = str.transform.TransformPoint(m.bounds.center);
            Transform alt = str.transform.FindInactive("AltSnapMesh");
            if (alt)
            {
                m = alt.GetComponent<MeshFilter>().mesh;
                c = alt.transform.TransformPoint(m.bounds.center);
            }
            Vector3 x = str.transform.right * m.bounds.extents.x;
            Vector3 z = str.transform.forward * m.bounds.extents.z;
            Vector3 y = str.transform.up * m.bounds.extents.y;
            Vector3 xIt = str.transform.right * intolerance;
            Vector3 zIt = str.transform.forward * intolerance;
            Vector3 yIt = str.transform.up * intolerance;

            switch (sp)
            {
                default:
                    return Vector3.zero;
                case SnapPoint.Unspecified:
                    return Vector3.zero;
                case SnapPoint.Pivot:
                    return str.transform.position;
                case SnapPoint.Zmax:
                    return c + z - zIt;
                case SnapPoint.Zmin:
                    return c - z + zIt;
                case SnapPoint.Xmax:
                    return c + x - xIt;
                case SnapPoint.Xmin:
                    return c - x + xIt;
                case SnapPoint.Ymax:
                    return c + y - yIt;
                case SnapPoint.Ymin:
                    return c - y + yIt;
                case SnapPoint.Ymid:
                    return c;
                case SnapPoint.ZpXp:
                    return c + z + x - zIt - xIt;
                case SnapPoint.ZpXn:
                    return c + z - x - zIt + xIt;
                case SnapPoint.ZnXp:
                    return c - z + x + zIt - xIt;
                case SnapPoint.ZnXn:
                    return c - z - x + zIt - xIt;
                case SnapPoint.YpXp:
                    return c + y + x - yIt - xIt;
                case SnapPoint.YpXn:
                    return c + y - x - yIt + xIt;
                case SnapPoint.YnXp:
                    return c - y + x + yIt - xIt;
                case SnapPoint.YnXn:
                    return c - y - x + yIt + xIt;

            }
        }


        public static Dictionary<(SnapPattern held, SnapPattern look, SnapPoint sp), SnapPoint> snapPointMatrix = new()
        {
            // Unspecified lookat = general rule, not overriden by basic rule
            // Unspecified output = no snap
            // Undefined lookat = uniform rule for any target, overrides basic rule
            // If no override - checks basic rule, if no basic rule - no snap

            // Basic rule
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.Zmax), SnapPoint.Zmin },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.Zmin), SnapPoint.Zmax },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.Xmax), SnapPoint.Xmin },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.Xmin), SnapPoint.Xmax },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.ZpXp), SnapPoint.ZnXn },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.ZpXn), SnapPoint.ZnXp },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.ZnXp), SnapPoint.ZpXn },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.ZnXn), SnapPoint.ZpXp },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.YnXn), SnapPoint.YnXp },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.YnXp), SnapPoint.YnXn },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.YpXn), SnapPoint.YpXp },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.YpXp), SnapPoint.YpXn },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.Ymin), SnapPoint.Ymax },
            { (held: SnapPattern.Undefined, look: SnapPattern.Undefined, sp: SnapPoint.Ymax), SnapPoint.Ymin },

            // Floor>Floor 
            // basic

            // Floor>Wall
            { (held: SnapPattern.Floor, look: SnapPattern.Wall, sp: SnapPoint.Unspecified), SnapPoint.Zmax },

            // Floor>Roof
            { (held: SnapPattern.Floor, look: SnapPattern.Roof, sp: SnapPoint.YpXn), SnapPoint.Zmax }, // top
            { (held: SnapPattern.Floor, look: SnapPattern.Roof, sp: SnapPoint.YnXp), SnapPoint.Zmax }, // bottom

            // Wall>Floor
            { (held: SnapPattern.Wall, look: SnapPattern.Floor, sp: SnapPoint.Unspecified), SnapPoint.Ymin },                         
            { (held: SnapPattern.Wall, look: SnapPattern.Floor, sp: SnapPoint.ZpXp), SnapPoint.YnXp },
            { (held: SnapPattern.Wall, look: SnapPattern.Floor, sp: SnapPoint.ZpXn), SnapPoint.YnXp },
            { (held: SnapPattern.Wall, look: SnapPattern.Floor, sp: SnapPoint.ZnXp), SnapPoint.YnXp },
            { (held: SnapPattern.Wall, look: SnapPattern.Floor, sp: SnapPoint.ZnXn), SnapPoint.YnXp },

            // Wall>Wall
            //{ (held: SnapPattern.Floor, look: SnapPattern.Floor, sp: SnapPoint.Unspecified), SnapPoint.Ymin },
            { (held: SnapPattern.Wall, look: SnapPattern.Wall, sp: SnapPoint.Ymin), SnapPoint.Ymax }, 
            { (held: SnapPattern.Wall, look: SnapPattern.Wall, sp: SnapPoint.Ymid), SnapPoint.Ymin },
            { (held: SnapPattern.Wall, look: SnapPattern.Wall, sp: SnapPoint.Ymax), SnapPoint.Ymin },

            //Wall>Roof
            { (held: SnapPattern.Wall, look: SnapPattern.Roof, sp: SnapPoint.Unspecified), SnapPoint.Unspecified }, 


            // Single>Any
            { (held: SnapPattern.Single, look: SnapPattern.Undefined, sp: SnapPoint.Unspecified), SnapPoint.Pivot },

            // Single>Floor
            { (held: SnapPattern.Single, look: SnapPattern.Floor, sp: SnapPoint.Unspecified), SnapPoint.Ymin },

            // Roof>Any
            { (held: SnapPattern.Roof, look: SnapPattern.Undefined, sp: SnapPoint.Unspecified), SnapPoint.Unspecified },

            // Roof>Wall
            { (held: SnapPattern.Roof, look: SnapPattern.Wall, sp: SnapPoint.Unspecified), SnapPoint.Unspecified },
            { (held: SnapPattern.Roof, look: SnapPattern.Wall, sp: SnapPoint.Ymax), SnapPoint.YnXp },
            { (held: SnapPattern.Roof, look: SnapPattern.Wall, sp: SnapPoint.YpXp), SnapPoint.YnXp },
            { (held: SnapPattern.Roof, look: SnapPattern.Wall, sp: SnapPoint.YpXn), SnapPoint.YnXp },

            // Roof>Floor
            { (held: SnapPattern.Roof, look: SnapPattern.Floor, sp: SnapPoint.Zmax), SnapPoint.YpXn },
            { (held: SnapPattern.Roof, look: SnapPattern.Floor, sp: SnapPoint.Zmin), SnapPoint.YpXn },
            { (held: SnapPattern.Roof, look: SnapPattern.Floor, sp: SnapPoint.Xmax), SnapPoint.YpXn },
            { (held: SnapPattern.Roof, look: SnapPattern.Floor, sp: SnapPoint.Xmin), SnapPoint.YpXn },

            // Roof>Roof
            // basic override
            { (held: SnapPattern.Roof, look: SnapPattern.Roof, sp: SnapPoint.Zmax), SnapPoint.Zmin },
            { (held: SnapPattern.Roof, look: SnapPattern.Roof, sp: SnapPoint.Zmin), SnapPoint.Zmax },
            { (held: SnapPattern.Roof, look: SnapPattern.Roof, sp: SnapPoint.YpXn), SnapPoint.YpXn },

            // Stairs>Floor
            // overriden by SpecialPoint

            // Stairs>Any
            { (held: SnapPattern.Stairs, look: SnapPattern.Undefined, sp: SnapPoint.Unspecified), SnapPoint.Pivot },



        };

        public static Dictionary<(string name, SnapPattern look, SnapPoint sp), SnapPoint> snapPointMatrixSpecialOverride = new()
        {
            { (name: "ARC_plank_overhangEdgeRoof", look: SnapPattern.Undefined, sp: SnapPoint.Unspecified), SnapPoint.Unspecified },
            { (name: "ARC_plank_overhangEdgeRoof", look: SnapPattern.Floor, sp: SnapPoint.Zmax), SnapPoint.Xmax },
            { (name: "ARC_plank_overhangEdgeRoof", look: SnapPattern.Floor, sp: SnapPoint.Zmin), SnapPoint.Xmax },
            { (name: "ARC_plank_overhangEdgeRoof", look: SnapPattern.Floor, sp: SnapPoint.Xmax), SnapPoint.Xmax },
            { (name: "ARC_plank_overhangEdgeRoof", look: SnapPattern.Floor, sp: SnapPoint.Xmin), SnapPoint.Xmax },
            { (name: "ARC_plank_overhangEdgeRoof", look: SnapPattern.Wall, sp: SnapPoint.Ymax), SnapPoint.Xmax },
            { (name: "ARC_plank_overhangEdgeRoof", look: SnapPattern.Wall, sp: SnapPoint.Ymid), SnapPoint.Xmax },
            { (name: "ARC_plank_overhangEdgeRoof", look: SnapPattern.Wall, sp: SnapPoint.Ymin), SnapPoint.Xmax },

            { (name: "ARC_plank_overhangEdgeRoofLeft", look: SnapPattern.Undefined, sp: SnapPoint.Unspecified), SnapPoint.Unspecified },
            { (name: "ARC_plank_overhangEdgeRoofLeft", look: SnapPattern.Roof, sp: SnapPoint.Zmax), SnapPoint.Zmin },
            { (name: "ARC_plank_overhangEdgeRoofLeft", look: SnapPattern.Roof, sp: SnapPoint.Zmin), SnapPoint.Zmax },

            { (name: "ARC_plank_overhangEdgeRoofRight", look: SnapPattern.Undefined, sp: SnapPoint.Unspecified), SnapPoint.Unspecified },
            { (name: "ARC_plank_overhangEdgeRoofRight", look: SnapPattern.Roof, sp: SnapPoint.Zmax), SnapPoint.Zmin },
            { (name: "ARC_plank_overhangEdgeRoofRight", look: SnapPattern.Roof, sp: SnapPoint.Zmin), SnapPoint.Zmax },

            { (name: "ARC_plank_quarterFloor", look: SnapPattern.Floor, sp: SnapPoint.ZpXp), SnapPoint.ZpXn },
            { (name: "ARC_plank_quarterFloor", look: SnapPattern.Floor, sp: SnapPoint.ZpXn), SnapPoint.ZpXp },
            { (name: "ARC_plank_quarterFloor", look: SnapPattern.Floor, sp: SnapPoint.ZnXp), SnapPoint.ZnXn },
            { (name: "ARC_plank_quarterFloor", look: SnapPattern.Floor, sp: SnapPoint.ZnXn), SnapPoint.ZnXp },

            { (name: "ARC_log_elevatedFloor", look: SnapPattern.Floor, sp: SnapPoint.Ymid), SnapPoint.Ymin },
            { (name: "ARC_log_elevatedFloor", look: SnapPattern.Floor, sp: SnapPoint.Ymin), SnapPoint.Ymid },
            { (name: "ARC_log_foundation", look: SnapPattern.Floor, sp: SnapPoint.Ymid), SnapPoint.Ymin },
            { (name: "ARC_log_foundation", look: SnapPattern.Floor, sp: SnapPoint.Ymin), SnapPoint.Ymid },



        };

        public static SnapPattern PartToPattern(Structure str)
        {
            if (str.name.ToLower().Contains("ramp")) return SnapPattern.Roof;

            Data.BuildPart bp = str.buildPart;
            switch (bp)
            {
                default:
                    return SnapPattern.Free;
                case Data.BuildPart.Door:
                    return SnapPattern.Special;
                case Data.BuildPart.Floor:
                    return SnapPattern.Floor;
                case Data.BuildPart.Stairs:
                    return SnapPattern.Stairs;
                case Data.BuildPart.Wall:
                    return SnapPattern.Wall;
                case Data.BuildPart.Roof:
                    return SnapPattern.Roof;
                case Data.BuildPart.Pillar:
                    return SnapPattern.Single;
            }
        }

        public static SnapPoint GetFallbackSnapPoint(SnapPattern sp)
        {
            switch (sp)
            {
                default:
                    return SnapPoint.Unspecified;
                case SnapPattern.Floor:
                    return SnapPoint.Zmax;
                case SnapPattern.Wall:
                    return SnapPoint.Xmin;
                case SnapPattern.Roof:
                    return SnapPoint.Ymin;
                case SnapPattern.Single:
                    return SnapPoint.Pivot;
                case SnapPattern.Stairs:
                    return SnapPoint.Pivot;
            }
        }
        public static bool GetOptimalRotation(Structure heldStr, SnapPoint lookAtSP, Structure targetStr, out float result) // true if needs rotation, can't manually rotate if true
        {
            result = 0f;

            if (PartToPattern(heldStr) == SnapPattern.Floor)
            {
                if (PartToPattern(targetStr) == SnapPattern.Floor)
                {
                    if (heldStr.name.ToLower().Contains("quarter") && targetStr.name.ToLower().Contains("quarter")) // small floor to small floor
                    {

                        //

                    }

                }
            }


            if (PartToPattern(heldStr) == SnapPattern.Wall)
            {
                if (PartToPattern(targetStr) == SnapPattern.Floor)
                {
                    switch (lookAtSP)
                    {
                        default:
                            return false;
                        case SnapPoint.Zmax:
                            return true;
                        case SnapPoint.Zmin:
                            result = 180f;
                            return true; 
                        case SnapPoint.Xmax:
                            result = 90f;
                            return true; 
                        case SnapPoint.Xmin:
                            result = -90f;
                            return true;
                    }
                }
            }
            if (PartToPattern(heldStr) == SnapPattern.Roof)
            {
                if (PartToPattern(targetStr) == SnapPattern.Roof)
                {
                    switch (lookAtSP)
                    {
                        default:
                            return false;
                        case SnapPoint.Zmax:
                            return true;
                        case SnapPoint.Zmin:
                            return true;                        
                        case SnapPoint.YpXn:
                            result = 180f;
                            return true;
                    }
                }

                if (PartToPattern(targetStr) == SnapPattern.Floor && heldStr.name.ToLower().Contains("edgeroof"))// only for overhang trim
                {
                    if (heldStr.name.ToLower().Contains("right"))
                    {
                        result = 90f;
                        return true;
                    }
                    else if (heldStr.name.ToLower().Contains("left"))
                    {
                        result = -90f;
                        return true;
                    }
                    switch (lookAtSP)
                    {
                        case SnapPoint.Zmax:
                            result = 90f;
                            return true;
                        case SnapPoint.Zmin:
                            result = -90f;
                            return true;
                        case SnapPoint.Xmax:
                            result = 180f;
                            return true;
                        case SnapPoint.Xmin:
                            return true;
                    }
                }
            }
            if (PartToPattern(heldStr) == SnapPattern.Stairs)
            {
                if (PartToPattern(targetStr) == SnapPattern.Floor && targetStr.name.ToLower().Contains("stairs")) // only for stairs cutout floor
                {
                    switch (lookAtSP)
                    {
                        default:
                            return false;
                        case SnapPoint.Zmin:
                            return true;
                    }
                }               
                if (PartToPattern(targetStr) == SnapPattern.Wall && targetStr.name.ToLower().Contains("door")) // only for doorframes
                {
                    switch (lookAtSP)
                    {
                        default:
                            return false;
                        case SnapPoint.Ymin:
                            result = 180f;
                            return false;
                    }
                }

            }
            return false;
        }
        public static SnapPoint GetOptimalSnapPoint(Structure heldStr, SnapPoint lookAtSP, Structure targetStr)
        {
            SnapPoint final = GetFallbackSnapPoint(PartToPattern(heldStr)); //failsafe

            if (snapPointMatrixSpecialOverride.TryGetValue((heldStr.name[0..^2], PartToPattern(targetStr), lookAtSP), out SnapPoint specialResult))
            {
                return specialResult;
            }            
            else if (snapPointMatrixSpecialOverride.TryGetValue((heldStr.name[0..^2], SnapPattern.Undefined, SnapPoint.Unspecified), out SnapPoint specialUnifromResult))
            {
                return specialUnifromResult;
            }
            else if (snapPointMatrix.TryGetValue(
                (PartToPattern(heldStr), PartToPattern(targetStr), lookAtSP), out SnapPoint result)) // look for specific rule
            {
                return result;
            }
            else if (snapPointMatrix.TryGetValue(
                (PartToPattern(heldStr), PartToPattern(targetStr), SnapPoint.Unspecified), out SnapPoint resultGeneral)) // look for general rule
            {
                return resultGeneral;
            }
            else if (snapPointMatrix.TryGetValue(
                (PartToPattern(heldStr), SnapPattern.Undefined, SnapPoint.Unspecified), out SnapPoint refultUniform)) // look for uniform rule
            {
                return refultUniform;
            }
            else snapPointMatrix.TryGetValue(
                (SnapPattern.Undefined, SnapPattern.Undefined, lookAtSP), out final); // fallback to basic rule

            if (heldStr.buildPart == Data.BuildPart.Door)
            {
                if (HasSpecialPoint(targetStr))
                {
                    final = SnapPoint.Pivot;
                }
            }
            return final;
        }

        public static bool HasSpecialPoint(Structure str) => str.transform.Find("SpecialAttachPoints");

        public static bool GetSpecialPoint(Structure targetStr, SnapPoint sp, Structure heldStr, ref SnapPoint overrideSp, out Vector3 pos, out Quaternion rot) // true if exists
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;

            if (HasSpecialPoint(targetStr))
            {
                foreach (var t in targetStr.transform.Find("SpecialAttachPoints").GetComponentsInChildren<Transform>())
                {
                    if (Enum.TryParse(t.name, out SpecialPoint point))
                    {
                        if (heldStr.buildPart == Data.BuildPart.Door)
                        {
                            if (point == SpecialPoint.WindowPoint && sp == SnapPoint.Ymid && heldStr.doorType == Data.DoorVariant.Window)
                            {
                                pos = t.position;
                                rot = t.rotation;
                                return true;
                            }
                            if (point == SpecialPoint.DoorPoint && heldStr.doorType == Data.DoorVariant.Regular && (
                                    sp == SnapPoint.Ymid || sp == SnapPoint.Ymin || sp == SnapPoint.YnXn))
                            {
                                pos = t.position;
                                rot = t.rotation;
                                return true;
                            }
                            if (point == SpecialPoint.DoorPointMirror && sp == SnapPoint.YnXp && heldStr.doorType == Data.DoorVariant.Regular)
                            {
                                pos = t.position;
                                rot = t.rotation;
                                return true;
                            }
                            if (heldStr.doorType == Data.DoorVariant.Fence)
                            {
                                return false;
                            }
                        }
                        if (point == SpecialPoint.StairsPoint &&
                            heldStr.buildPart == Data.BuildPart.Stairs && 

                            ((targetStr.buildPart == Data.BuildPart.Floor && 
                            (sp == SnapPoint.Zmin || sp == SnapPoint.Ymid || sp == SnapPoint.ZnXp || sp == SnapPoint.ZnXn))

                            ||

                            (targetStr.buildPart == Data.BuildPart.Wall &&
                            (sp == SnapPoint.Ymid || sp == SnapPoint.Ymin))))
                        {
                            pos = t.position;
                            rot = t.rotation;
                            overrideSp = SnapPoint.YpXp;
                            return true;
                        }

                    }
                }
            }
            return false;
        }

        public static void AlignToGenericSnapPoint(Structure heldStr, SnapPoint currentSP, Structure targetStr, SnapPoint targetSP)
        {
            heldStr.transform.rotation = targetStr.transform.rotation;

            if (GetOptimalRotation(heldStr, targetSP, targetStr, out float rotation))
            {
                heldStr.transform.Rotate(Vector3.up, rotation);
            }
            else
            {
                if (currentCustomRotation != 0f) // been rotated by user
                {
                    heldStr.transform.Rotate(Vector3.up, currentCustomRotation);
                }
            }

            Vector3 offset = GetSnapPointPosition(heldStr, currentSP) - heldStr.transform.position;

            heldStr.transform.position = GetSnapPointPosition(targetStr, targetSP) - offset;
        }

        public static void AlignToSpecialSnapPoint(Structure heldStr, SnapPoint currentSP, Structure targetStr, Vector3 pos, Quaternion rot)
        {
            heldStr.transform.rotation = rot;
            Vector3 offset = GetSnapPointPosition(heldStr, currentSP) - heldStr.transform.position;
            heldStr.transform.position = pos - offset;
        }


        public static void SetCustomRotation(float angle)
        {
            if (angle < 0f)
            {
                if (currentCustomRotation == 270) currentCustomRotation = 0;
                else currentCustomRotation += 90;
                requestUpdate = true;
            }
            else if (angle > 0f)
            {
                if (currentCustomRotation == 0) currentCustomRotation = 270;
                else currentCustomRotation -= 90;
                requestUpdate = true;
            }
        }

        public static void Nudge(Structure str, Vector3 direction)
        {
            float nudgeAmt = Settings.options.nudgeAmount / 100f;

            if (direction.y != 0)
            {
                direction = str.transform.TransformDirection(direction);
            }
            else
            {
                // player local > world
                Vector3 playerWorldDir = GameManager.GetPlayerTransform().TransformDirection(direction);

                // world > object local
                Vector3 objectLocalDir = str.transform.InverseTransformDirection(playerWorldDir);

                if (Mathf.Abs(objectLocalDir.x) > Mathf.Abs(objectLocalDir.z))
                {
                    objectLocalDir = new Vector3(Mathf.Sign(objectLocalDir.x), 0, 0);
                }
                else
                {
                    objectLocalDir = new Vector3(0, 0, Mathf.Sign(objectLocalDir.z));
                }
                    

                // object local > world
                direction = str.transform.TransformDirection(objectLocalDir);
            }

            /*
            if (Settings.options.nudgeDirection == 1) // from player perspective
            {
                Vector3 toTarget = (str.transform.position - GameManager.GetPlayerTransform().position);
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.0001f) toTarget = GameManager.GetPlayerTransform().forward;
                toTarget.Normalize();

                Quaternion rotation = Quaternion.LookRotation(toTarget, Vector3.up);
                direction = rotation * direction;
            }
            else // in local space but relative to player
            {
                direction = str.transform.TransformDirection(direction);
            }
            */
            str.transform.position += direction * nudgeAmt;
        }

        public static bool SnapToTriggerRelatedPoint(Structure heldStr) // true if should override vanilla placing handler
        {
            Ray ray = GameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition);
            int layerMask = 0;

            layerMask |= (1 << snapTriggerLayer);

            if (Physics.Raycast(ray, out RaycastHit hit, maxSnapDistance, layerMask))
            {
                Enum.TryParse(hit.transform.name, out SnapPoint sp);
                if (sp == SnapPoint.Unspecified) return false;

                Structure hitStr = hit.transform.parent.parent.GetComponent<Structure>();
                if (!hitStr || hitStr == heldStr) return false;

                SnapPoint heldSnap = GetOptimalSnapPoint(heldStr, sp, hitStr);

                if (sp == currentLookAtSnapPoint && !requestUpdate) return true; // wait for change before calculating transform again
                currentLookAtSnapPoint = sp;
                requestUpdate = false;

                if (heldSnap == SnapPoint.Unspecified) return false;

                if (heldStr.buildPart == Data.BuildPart.Door)
                { 
                    if (HasSpecialPoint(hitStr))
                    {
                        if (GetSpecialPoint(hitStr, sp, heldStr, ref heldSnap, out Vector3 pos, out Quaternion rot))
                        {
                            AlignToSpecialSnapPoint(heldStr, heldSnap, hitStr, pos, rot);
                            return true;
                        }
                        return false;
                    }   
                    else // don't snap door if not looking at door frame
                    {
                        return false;
                    }
                }
                if (heldStr.buildPart == Data.BuildPart.Stairs)
                {
                    if (HasSpecialPoint(hitStr))
                    {
                        if (GetSpecialPoint(hitStr, sp, heldStr, ref heldSnap, out Vector3 pos, out Quaternion rot))
                        {
                            AlignToSpecialSnapPoint(heldStr, heldSnap, hitStr, pos, rot);
                            return true;
                        }
                        return false;
                    }
                }


                AlignToGenericSnapPoint(heldStr, heldSnap, hitStr, sp);

                // DEBUG
                /*
                Vector3 v1 = GetSnapPointPosition(hitStr, sp);
                HUDMessage.AddMessage("Point: " + hit.transform.gameObject.name + " > " + heldSnap, 0.4f, true, true);
                MelonCoroutines.Start(Interior.FlashDebugRay(v1, v1 + heldStr.transform.up, Color.green));
                MelonCoroutines.Start(Interior.FlashDebugRay(v1, v1 + heldStr.transform.forward, Color.blue));
                MelonCoroutines.Start(Interior.FlashDebugRay(v1, v1 + heldStr.transform.right, Color.red));
                */
                return true;
            }

            return false;


        }

    }
}
