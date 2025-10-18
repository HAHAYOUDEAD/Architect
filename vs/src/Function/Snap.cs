using Il2Cpp;
using UnityEngine;
using static Architect.Snap;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Playables;
using static UnityEngine.GraphicsBuffer;
using static Il2CppRewired.Controller;

namespace Architect
{
    public class Snap
    {
        public static readonly int snapTriggerLayer = vp_Layer.CharacterControllerCollideOnly;
        private static SnapPoint currentLookAtSnapPoint = SnapPoint.Unspecified;
        private static int currentCustomRotation = 0;
        public static bool requestUpdate = false;
        private static readonly float intolerance = 0.004f; // how much the snap points should be embedded into the objects

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

            // Floor>Floor 
            // basic only

            // Floor>Wall
            { (held: SnapPattern.Floor, look: SnapPattern.Wall, sp: SnapPoint.Unspecified), SnapPoint.Zmax },

            // Wall>Floor
            { (held: SnapPattern.Wall, look: SnapPattern.Floor, sp: SnapPoint.Unspecified), SnapPoint.Ymin }, 

            // Wall>Wall
            //{ (held: SnapPattern.Floor, look: SnapPattern.Floor, sp: SnapPoint.Unspecified), SnapPoint.Ymin },
            { (held: SnapPattern.Wall, look: SnapPattern.Wall, sp: SnapPoint.Ymin), SnapPoint.Unspecified }, 
            { (held: SnapPattern.Wall, look: SnapPattern.Wall, sp: SnapPoint.Ymid), SnapPoint.Ymin },
            { (held: SnapPattern.Wall, look: SnapPattern.Wall, sp: SnapPoint.Ymax), SnapPoint.Ymin },

            // Single>Any
            { (held: SnapPattern.Single, look: SnapPattern.Undefined, sp: SnapPoint.Unspecified), SnapPoint.Pivot },

            // Single>Floor
            { (held: SnapPattern.Single, look: SnapPattern.Floor, sp: SnapPoint.Unspecified), SnapPoint.Ymin },


        };

        public static SnapPattern PartToPattern(Data.BuildPart bp)
        { 
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

        public static SnapPoint GetDefaultSnapPoint(SnapPattern sp)
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
            }
        }
        public static bool GetOptimalRotation(Structure heldStr, SnapPoint lookAtSP, Structure targetStr, out float result) // true if needs rotation
        {
            result = 0f;

            if (PartToPattern(heldStr.buildPart) == SnapPattern.Wall)
            {
                if (PartToPattern(targetStr.buildPart) == SnapPattern.Floor)
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
            return false;
        }
        public static SnapPoint GetOptimalSnapPoint(Structure heldStr, SnapPoint lookAtSP, Structure targetStr)
        {
            SnapPoint final = GetDefaultSnapPoint(PartToPattern(heldStr.buildPart)); //failsafe

            if (snapPointMatrix.TryGetValue(
                (PartToPattern(heldStr.buildPart), PartToPattern(targetStr.buildPart), lookAtSP), out SnapPoint result)) // look for specific rule
            {
                return result;
            }
            else if (snapPointMatrix.TryGetValue(
                (PartToPattern(heldStr.buildPart), PartToPattern(targetStr.buildPart), SnapPoint.Unspecified), out SnapPoint resultGeneral)) // look for general rule
            {
                return resultGeneral;
            }
            else if (snapPointMatrix.TryGetValue(
                (PartToPattern(heldStr.buildPart), SnapPattern.Undefined, SnapPoint.Unspecified), out SnapPoint refultUniform)) // look for uniform rule
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

        public static bool GetSpecialPoint(Structure str, SnapPoint sp, out Vector3 pos, out Quaternion rot) // true if exists
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;

            if (HasSpecialPoint(str))
            {
                foreach (var t in str.transform.Find("SpecialAttachPoints").GetComponentsInChildren<Transform>())
                {
                    if (Enum.TryParse(t.name, out SpecialPoint point))
                    {
                        if (point == SpecialPoint.WindowPoint && sp == SnapPoint.Ymid)
                        {
                            pos = t.position;
                            rot = t.rotation;
                            return true;
                        }
                        if (point == SpecialPoint.DoorPoint && (
                            sp == SnapPoint.Ymid ||
                            sp == SnapPoint.Ymin ||
                            sp == SnapPoint.YnXn))
                        {
                            pos = t.position;
                            rot = t.rotation;
                            return true;
                        }
                        if (point == SpecialPoint.DoorPointMirror && sp == SnapPoint.YnXp)
                        {
                            pos = t.position;
                            rot = t.rotation;
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

        public static bool SnapToTriggerRelatedPoint(Structure heldStr) // true if should override vanilla placing handler
        {
            

            Ray ray = GameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition);
            int layerMask = 0;

            layerMask |= (1 << snapTriggerLayer);

            if (Physics.Raycast(ray, out RaycastHit hit, 100, layerMask))
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
                        if (GetSpecialPoint(hitStr, sp, out Vector3 pos, out Quaternion rot))
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

                AlignToGenericSnapPoint(heldStr, heldSnap, hitStr, sp);

                // DEBUG
                Vector3 v1 = GetSnapPointPosition(hitStr, sp);
                HUDMessage.AddMessage("Point: " + hit.transform.gameObject.name + " > " + heldSnap, 0.4f, true, true);
                MelonCoroutines.Start(Interior.FlashDebugRay(v1, v1 + heldStr.transform.up, Color.green));
                MelonCoroutines.Start(Interior.FlashDebugRay(v1, v1 + heldStr.transform.forward, Color.blue));
                MelonCoroutines.Start(Interior.FlashDebugRay(v1, v1 + heldStr.transform.right, Color.red));

                return true;
            }

            return false;


        }

    }
}
