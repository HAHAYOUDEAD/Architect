using static Architect.StructureData;
using UnityEngine.UIElements.UIR;
using System.Text.Json;

namespace Architect
{
    public static class StructureManager
    {
        public static List<Structure> structures = new List<Structure>();

        public static void Reset() => structures.Clear();

        public static void Add(Structure s)
        {
            if (!structures.Contains(s)) structures.Add(s);
        }

        public static void Remove(Structure s)
        {
            if (structures.Contains(s)) structures.Remove(s);
        }

        public static string SerializeAll()
        {
            StructureSaveProxy[] allData = new StructureSaveProxy[structures.Count];

            for (int i = 0; i < structures.Count; i++)
            {
                if (structures[i])
                {
                    allData[i] = structures[i].Serialize();
                }
            }

            //return JSON.Dump(allData);
            return JsonSerializer.Serialize(allData);
        }

        public static IEnumerator PostInitialization()
        {
            bool initialized = false;

            while (!initialized)
            {
                initialized = true;

                yield return new WaitForSeconds(0.5f);

                foreach (Structure sc in structures)
                {
                    if (!sc.initialized) initialized = false;
                }                
            }
            RadialActions.RefreshParticleKillers();

            yield break;
        }


        public static void DeserializeAll(string[] list)
        {
            foreach (string s in list)
            {
                StructureSaveProxy? structure = null;

                //if (!string.IsNullOrEmpty(s)) JSON.MakeInto(JSON.Load(s), out structure);
                if (!string.IsNullOrEmpty(s)) structure = JsonSerializer.Deserialize<StructureSaveProxy>(s, Jsoning.GetDefaultOptions());

                if (structure != null && structure.prefabName.Length > 0)
                {
                    GameObject wallPart = UnityEngine.Object.Instantiate(meshBundle.LoadAsset<GameObject>(structure.prefabName));
                    wallPart.name = structure.prefabName;
                    Structure component = wallPart.GetComponent<Structure>();
                    component.Restore(structure);
                }
            }
            MelonCoroutines.Start(PostInitialization());
        }
        public static void DeserializeAll(StructureSaveProxy[] list)
        {
            foreach (StructureSaveProxy s in list)
            {
                if (s != null && s.prefabName.Length > 0)
                {
                    GameObject wallPart = UnityEngine.Object.Instantiate(meshBundle.LoadAsset<GameObject>(s.prefabName));
                    wallPart.name = s.prefabName;
                    Structure component = wallPart.GetComponent<Structure>();
                    component.Restore(s);
                }
            }
            MelonCoroutines.Start(PostInitialization());
        }


    }
}
