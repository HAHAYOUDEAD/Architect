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
            string[] allData = new string[structures.Count];

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
                if (!string.IsNullOrEmpty(s)) structure = JsonSerializer.Deserialize<StructureSaveProxy>(s.Replace("\\", ""), Jsoning.GetDefaultOptions()) ?? throw new ArgumentException("Could not parse recipe data from the text.", nameof(s));

                if (structure != null && structure.prefabName.Length > 0)
                {
                    GameObject wallPart = UnityEngine.Object.Instantiate(meshBundle.LoadAsset<GameObject>(structure.prefabName));
                    wallPart.name = structure.prefabName;
                    //Structure component = wallPart.AddComponent<Structure>();
                    Structure component = wallPart.GetComponent<Structure>();
                    component.Restore(structure);
                }
            }
            MelonCoroutines.Start(PostInitialization());
        }


    }
}
