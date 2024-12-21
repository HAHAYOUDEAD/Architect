
namespace Architect
{
    //[RegisterTypeInIl2Cpp]
    public class StructureSaveProxy
    {
        public string prefabName;
        //public string localizationKey;
        public Vector3 position;
        public Quaternion rotation;
        //public Vector3 scale;
        //public bool isDoor;
        public bool isBuilt = false;
        //public Data.BuildMaterial material;
        //public Data.BuildPart part;
        public bool hasSnow;
        public Color insidePaintColor;
        public Color outsidePaintColor;
        public bool isAltMaterial = false; // for reclaimed wood or fir logs
        public bool doorState; // true = opened, false = closed
    }
}
