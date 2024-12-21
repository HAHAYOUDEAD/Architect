using Il2Cpp;
using UnityEngine;

namespace Architect
{
    internal class Snap
    {

        public static readonly int snapTriggerLayer = vp_Layer.CharacterControllerCollideOnly;

        public enum SnapPoint
        {
            Unspecified = 0,
            Pivot,
            Zmax,
            Zmin,
            Xmax,
            Xmin,
            Ymin,
            Ymid,
            Ymax,
            ZpXp, // Z+ X+ corner
            ZpXn, // Z+ X- corner
            ZnXp, // Z- X+ corner
            ZnXn, // Z- X- corner
            YpXp, // top X+ corner
            YpXn, // top X- corner
            YnXp, // low X+ corner
            YnXn, // low X- corner
        }

        public static Vector3 GetSnapPoint(Structure str, SnapPoint sp)
        {
            Renderer r = str.renderer;
            Vector3 c = r.bounds.center;
            Vector3 x = r.transform.right * r.bounds.extents.x;
            Vector3 z = r.transform.forward * r.bounds.extents.z;
            Vector3 y = r.transform.up * r.bounds.extents.y;

            switch (sp)
            {
                default:
                    return Vector3.zero;
                case SnapPoint.Unspecified:
                    return Vector3.zero;
                case SnapPoint.Pivot:
                    return str.transform.position;
                case SnapPoint.Zmax:
                    return c + z;
                case SnapPoint.Zmin:
                    return c - z;
                case SnapPoint.Xmax:
                    return c + x;
                case SnapPoint.Xmin:
                    return c - x;
                case SnapPoint.Ymax:
                    return c + y;
                case SnapPoint.Ymin:
                    return c - y;
                case SnapPoint.Ymid:
                    return c;
                case SnapPoint.ZpXp:
                    return c + z + x;
                case SnapPoint.ZpXn:
                    return c + z - x;
                case SnapPoint.ZnXp:
                    return c - z + x;
                case SnapPoint.ZnXn:
                    return c - z - x;
                case SnapPoint.YpXp:
                    return c + y + x;
                case SnapPoint.YpXn:
                    return c + y - x;
                case SnapPoint.YnXp:
                    return c - y + x;
                case SnapPoint.YnXn:
                    return c - y - x;

            }
        }


        public static Dictionary<(Data.BuildPart heldStructure, Data.BuildPart lookStructure, SnapPoint lookPoint), SnapPoint> snapPointMatrix = new()
        {
            { (heldStructure: Data.BuildPart.Floor, lookStructure: Data.BuildPart.Floor, lookPoint: SnapPoint.Ymin), SnapPoint.Pivot },


        
        
        
        };

        /*
        public static Vector3 GetDefaultSnapPoint(Structure curStr, SnapPoint toSp, Structure toStr)
        {

            switch (toSp)
            {
                default:
                    return Vector3.zero;
                case SnapPoint.Pivot:
                    return GetSnapPoint(curStr, SnapPoint.Pivot);
                case SnapPoint.Zmax:
                    if (curStr.buildPart == Data.BuildPart.Floor)
                    {
                        return GetSnapPoint(curStr, SnapPoint.Zmin);
                    }

                    if (curStr.buildPart == Data.BuildPart.Wall)
                    {
                        return GetSnapPoint(curStr, SnapPoint.Ymin);
                    }
                    break;
                case SnapPoint.Zmin:
                    if (curStr.buildPart == Data.BuildPart.Floor)
                    {
                        return GetSnapPoint(curStr, SnapPoint.Zmin);
                    }

                    if (curStr.buildPart == Data.BuildPart.Wall)
                    {
                        return GetSnapPoint(curStr, SnapPoint.Ymid);
                    }
                    break;
                case SnapPoint.Xmax:
                    return r.bounds.center + r.transform.right * r.bounds.extents.x;
                case SnapPoint.Xmin:
                    return r.bounds.center - r.transform.right * r.bounds.extents.x;
                case SnapPoint.Ymax:
                    return r.bounds.center + r.transform.up * r.bounds.extents.y;
                case SnapPoint.Ymin:
                    return r.bounds.center - r.transform.up * r.bounds.extents.y;
                case SnapPoint.Ymid:
                    return r.bounds.center;
                case SnapPoint.ZpXp:
                    return r.bounds.center + r.transform.forward * r.bounds.extents.z + r.transform.right * r.bounds.extents.x;
                case SnapPoint.ZpXn:
                    return r.bounds.center + r.transform.forward * r.bounds.extents.z - r.transform.right * r.bounds.extents.x;
                case SnapPoint.ZnXp:
                    return r.bounds.center - r.transform.forward * r.bounds.extents.z + r.transform.right * r.bounds.extents.x;
                case SnapPoint.ZnXn:
                    return r.bounds.center - r.transform.forward * r.bounds.extents.z - r.transform.right * r.bounds.extents.x;
            }




            return Vector3.zero;
        }
        */

        public static void SnapToTriggerRelatedPoint(Structure heldStr)
        {
            Ray ray = GameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition);
            int layerMask = 0;

            layerMask |= (1 << snapTriggerLayer);

            if (Physics.Raycast(ray, out RaycastHit hit, 100, layerMask))
            {
                Enum.TryParse(hit.transform.name, out SnapPoint sp);
                if (sp == SnapPoint.Unspecified) return;

                Structure hitStr = hit.transform.parent.parent.GetComponent<Structure>();
                if (!hitStr) return;
                
                Vector3 v1 = GetSnapPoint(hitStr, sp);
                HUDMessage.AddMessage("Point: " + hit.transform.gameObject.name, 0.4f, true, true);
                MelonCoroutines.Start(Interior.FlashDebugRay(v1, v1 + Vector3.up, Color.red));

                heldStr.transform.rotation = hitStr.transform.rotation;

                SnapPoint heldSnap;

                switch (sp)
                {

                }
                

                //heldStr.
                // хз как поворачивать текущий объект
            }


        }

    }
}
