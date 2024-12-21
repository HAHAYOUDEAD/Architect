using static Architect.StructureData;

using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Architect.StructureData;
using static Il2CppNodeCanvas.Tasks.Actions.DebugLogText;

namespace Architect
{
    internal class Materials
    {


        public static Dictionary<MaterialName, string> textureNameDictionary = new()
        {
            { MaterialName.PlankFresh, "TEX_woodNew2" },
            { MaterialName.PlankReclaimed, "TEX_woodReclaimed" },
            { MaterialName.PlankPainted, "TEX_woodPainted_white" },
            { MaterialName.Stone, "" },
            { MaterialName.StonePainted, "" },
            { MaterialName.LogCedar, "TEX_logBase" },
            { MaterialName.LogFir, "TEX_logAlt" },
            { MaterialName.LogPainted, "TEX_logPainted" }

        };

        public static Shader vanillaSkinnedShader = Shader.Find("Shader Forge/TLD_StandardSkinned");
        public static Shader vanillaDefaultShader = Shader.Find("Shader Forge/TLD_StandardDiffuse");

        public static float ghostPower = 1f;


        public static void AssignMaterial(Structure sc, BuildPartSide bps = BuildPartSide.Both, bool ghost = false, bool painted = false, bool gradual = false) // painted also used for SOLID ghost color
        {
            Material mat = new Material(vanillaDefaultShader);
            Material matPainted = new Material(vanillaDefaultShader);
            Material matOverride = new Material(vanillaDefaultShader);
            GameObject go = sc.gameObject;
            MeshRenderer? ghostMR = meshBundle.LoadAsset<GameObject>("Ghost")?.GetComponent<MeshRenderer>();
            string textureName = "";
            string paintedTextureName = "";

            if (sc.ghostCoroutine != null) MelonCoroutines.Stop(sc.ghostCoroutine);
            if (ghost)
            {
                if (!painted) // alpha ghost material
                {
                    mat = new Material(ghostMR?.materials[0].shader);
                    matOverride = new Material(ghostMR?.materials[0].shader);
                    mat.CopyPropertiesFromMaterial(ghostMR?.materials[0]);
                    if (gradual) mat.SetFloat("_Power", 0f);
                    matOverride.CopyPropertiesFromMaterial(mat);
                }
            }

            else
            {
                switch (sc.buildMaterial)
                {
                    case BuildMaterial.WoodPlank:
                        if (painted) paintedTextureName = textureNameDictionary[MaterialName.PlankPainted];
                        if (sc.isAltMaterial) textureName = textureNameDictionary[MaterialName.PlankReclaimed];
                        else textureName = textureNameDictionary[MaterialName.PlankFresh];
                        break;
                    case BuildMaterial.WoodLog:
                        if (painted) paintedTextureName = textureNameDictionary[MaterialName.LogPainted];
                        if (sc.isAltMaterial) textureName = textureNameDictionary[MaterialName.LogFir];
                        else textureName = textureNameDictionary[MaterialName.LogCedar];
                        break;
                    case BuildMaterial.Stone:
                        if (painted) paintedTextureName = "";
                        if (sc.isAltMaterial) textureName = "";
                        else textureName = "";
                        break;
                }

                if (painted) matPainted.mainTexture = meshBundle.LoadAsset<Texture>("Assets/Textures/" + paintedTextureName + ".png");
                mat.mainTexture = meshBundle.LoadAsset<Texture>("Assets/Textures/" + textureName + ".png");
            }

            Material[] matArray = go.GetComponent<MeshRenderer>().materials;

            Material matInner = new Material(mat) { name = "MTL_Inner" };
            Material matOuter = new Material(mat) { name = "MTL_Outer" };
            Material matPaintedInner = new Material(matPainted) { name = "MTL_Inner_Painted" };
            Material matPaintedOuter = new Material(matPainted) { name = "MTL_Outer_Painted" };

            if (ghost && painted) // solid ghost color
            {
                matInner.CopyPropertiesFromMaterial(ghostMR?.materials[1]);
                matOuter.CopyPropertiesFromMaterial(ghostMR?.materials[1]);
                matPaintedInner.CopyPropertiesFromMaterial(ghostMR?.materials[1]);
                matPaintedOuter.CopyPropertiesFromMaterial(ghostMR?.materials[1]);
            }

            for (int i = 0; i < matArray.Length; i++)
            {


                if (matArray[i].name.ToLower().Contains("inner"))
                {
                    if (bps == BuildPartSide.Inside || bps == BuildPartSide.Both)
                    {
                        Log(ConsoleColor.Red, matArray[i].name + " - " + sc.name + " " + i);
                        matArray[i] = painted ? matPaintedInner : matInner;
                        Log(ConsoleColor.Blue, matArray[i].name + " - " + mat.name);
                    }
                }
                else if (matArray[i].name.ToLower().Contains("outer"))
                {
                    if (bps == BuildPartSide.Outside || bps == BuildPartSide.Both)
                    {
                        Log(ConsoleColor.Green, matArray[i].name + " - " + sc.name + " " + i);
                        matArray[i] = painted ? matPaintedOuter : matOuter;
                    }
                }
                else
                {
                    Log(ConsoleColor.Yellow, matArray[i].name + " - " + sc.name + " " + i);
                    if (matArray[i].name.ToLower().Contains("logcore"))
                    {
                        matOverride.mainTexture = meshBundle.LoadAsset<Texture>("Assets/Textures/" + "TEX_logCore" + ".png");
                    }
                    else if (matArray[i].name.ToLower().Contains("logframe"))
                    {
                        matOverride.mainTexture = meshBundle.LoadAsset<Texture>("Assets/Textures/" + "TEX_logAlt" + ".png");
                    }
                    else
                    {
                        matOverride.mainTexture = meshBundle.LoadAsset<Texture>("Assets/Textures/" + textureName + ".png");
                    }
                    if (ghost)
                    {
                        matOverride.mainTexture = null;
                    }

                    Material matOverrideInstance = new Material(matOverride) { name = matArray[i].name.Replace(" (Instance)", "") };
                    if (ghost && painted) // solid ghost color
                    {
                        matOverrideInstance.CopyPropertiesFromMaterial(ghostMR?.materials[1]);
                    }
                    matArray[i] = matOverrideInstance;
                }
            }

            go.GetComponent<MeshRenderer>().materials = matArray;

            if (gradual && ghost && !painted)
            {
                sc.ghostCoroutine = MelonCoroutines.Start(sc.LerpGhostTransparency(ghostPower));
            }
        }
    }
}
