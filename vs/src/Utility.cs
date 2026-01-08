global using Architect;
global using HarmonyLib;
global using Il2Cpp;
global using Il2CppInterop.Runtime.Attributes;
global using Il2CppTLD.Placement;
global using LocalizationUtilities;
global using MelonLoader;
global using ModData;
global using System.Collections;
global using System.Collections.Generic;
global using System.Reflection;
//global using MelonLoader.TinyJSON;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using UnityEngine;
global using UnityEngine.AI;
global using static Architect.Utility;
global using static Architect.Values;
global using Data = Architect.StructureData;
global using Random = System.Random;
global using Utils = Il2Cpp.Utils;
using Il2CppTLD.Interactions;
using UnityEngine.Events;
using static Architect.StructureData;

namespace Architect
{
    public static class Utility
    {
        public static bool GetKeyDown(KeyCode key) => InputManager.HasContext(InputManager.m_CurrentContext) && Input.GetKeyDown(key);
        public static bool GetKeyUp(KeyCode key) => InputManager.HasContext(InputManager.m_CurrentContext) && Input.GetKeyUp(key);
        public static bool GetKeyHeld(KeyCode key) => InputManager.HasContext(InputManager.m_CurrentContext) && Input.GetKey(key);


        public static bool IsScenePlayable()
        {
            return !(string.IsNullOrEmpty(GameManager.m_ActiveScene) || GameManager.m_ActiveScene.Contains("MainMenu") || GameManager.m_ActiveScene == "Boot" || GameManager.m_ActiveScene == "Empty");
        }

        public static bool IsScenePlayable(string scene)
        {
            return !(string.IsNullOrEmpty(scene) || scene.Contains("MainMenu") || scene == "Boot" || scene == "Empty");
        }
        public static bool IsMainMenu(string scene)
        {
            return !string.IsNullOrEmpty(scene) && scene.Contains("MainMenu");
        }


        public static string? LoadEmbeddedJSON(string name)
        {
            name = resourcesFolder + name;

            string? result = null;

            MemoryStream memoryStream;
            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);
                result = reader.ReadToEnd(); 
            }

            return result;
        }


        public static Texture2D LoadEmbeddedIcon(string name)
        {
            name = resourcesFolder + "Icons." + name;

            Texture2D result = new Texture2D(2, 2, TextureFormat.RGBA4444, false);
            result.name = modPrefix + name;

            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);


            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                result.LoadImage(ms.ToArray());
            }

            return result;
            
        }

        public static AssetBundle LoadEmbeddedAssetBundle(string name)
        {
            /*
            MemoryStream memoryStream;
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcesFolder + name);
            memoryStream = new MemoryStream((int)stream.Length);
            stream.CopyTo(memoryStream);

            return AssetBundle.LoadFromMemory(memoryStream.ToArray());
            */
            using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcesFolder + name))
            {
                MemoryStream? memory = new((int)stream.Length);
                stream!.CopyTo(memory);

                Il2CppSystem.IO.MemoryStream memoryStream = new(memory.ToArray());
                return AssetBundle.LoadFromStream(memoryStream);
            };
        }


        public static GameObject GetInteractiveGameObjectUnderCrosshair()
        {
            if (!gameStarted) return null;
            GameObject go = null;
            PlayerManager pm = GameManager.GetPlayerManagerComponent();
            
            float maxPickupRange = GameManager.GetGlobalParameters().m_MaxPickupRange;
            float maxRange = pm.ComputeModifiedPickupRange(maxPickupRange);
            if (pm.GetControlMode() == PlayerControlMode.InFPCinematic)
            {
                maxRange = 50f;
            }

            go = GameManager.GetPlayerManagerComponent().GetInteractiveObjectUnderCrosshairs(maxRange); // breaks here when going to main menu

            return go;
        }


        public static Transform Zero(this Transform t)
        {
            t.position = Vector3.zero;
            t.rotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        public static Transform ZeroLocal(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        public static iTweenEvent Initialize(this iTweenEvent ite, DoorVariant type, string name, float rotation, float speed = 2.2f)
        {
            ite.animating = false;
            ite.playAutomatically = false;
            ite.tweenName = name;
            ite.type = iTweenEvent.TweenType.RotateBy;
            switch (type)
            {
                default:
                    ite.vector3s = new Vector3[] { new Vector3(0, rotation, 0) };
                    break;
                case DoorVariant.Window:
                    ite.vector3s = new Vector3[] { new Vector3(-rotation, 0, 0) };
                    break;
                case DoorVariant.RoofWindow:
                    ite.vector3s = new Vector3[] { new Vector3(-rotation, 0, 0) };
                    break;
            }
            ite.floats = new float[] { speed };
            ite.keys = new string[] { "amount", "time" };
            ite.indexes = new int[] { 0, 0 };

            return ite;
        }

        public static OpenClose Initialize(this OpenClose oc, DoorVariant type, ObjectAnim oa, Structure str)
        {
            switch (type)
            {
                default:
                    oc.m_LocalizedDisplayName = new LocalizedString() { m_LocalizationID = "GAMEPLAY_Door" };
                    break;
                case DoorVariant.Window:
                    oc.m_LocalizedDisplayName = new LocalizedString() { m_LocalizationID = "ARC_WindowShutter" };
                    break;
                case DoorVariant.Fence:
                    oc.m_LocalizedDisplayName = new LocalizedString() { m_LocalizationID = "ARC_FenceGate" };
                    break;
            }
            oc.m_CloseAudioEvent = new Il2CppAK.Wwise.Event() { m_playingId = Il2CppAK.EVENTS.PLAY_SNDMECHDOORWOODCLOSE1 };
            oc.m_OpenAudioEvent = new Il2CppAK.Wwise.Event() { m_playingId = Il2CppAK.EVENTS.PLAY_SNDMECHDOORWOODOPEN1 };
            oc.m_ObjectAnim = oa;
            oc.MaybeCreateNavMeshObstacle();
            OpenCloseManager.Add(oc);
            oc.m_StartHasBeenCalled = true;
            oc.m_StartOpened = false;
            oc.m_OpenAudio = "";
            oc.m_CloseAudio = "";
            //oc.AddEventCallback(InteractionEventType.CloseEnd, (UnityAction<BaseInteraction>)(check => Interior.CallInteriorCheck(str.renderer.bounds.center)));

            return oc;
        }

        public static ObjectAnim Initialize(this ObjectAnim oa, GameObject go)
        {
            oa.m_Target = go;
            oa.Start();

            return oa;
        } 

        public static GameObject GetPrefab(string name) => GearItem.LoadGearItemPrefab(name).gameObject;

        public static bool PlayerHasEnoughItems(GearItem gi, int num)
        {
            int totalInInventory = 0;

            if (gi.name.Contains(Data.nailsGearName))
            {
                totalInInventory += GameManager.GetInventoryComponent().NumGearInInventory(Data.nailsGearName);
                totalInInventory += GameManager.GetInventoryComponent().NumGearInInventory(Data.nailsBundleGearName) * Data.nailsBundleSize;
            }
            else if (gi.name.Contains(Data.planksGearName))
            {
                totalInInventory += GameManager.GetInventoryComponent().NumGearInInventory(Data.planksGearName);
                totalInInventory += GameManager.GetInventoryComponent().NumGearInInventory(Data.planksBundleGearName) * Data.planksBundleSize;
            }
            else if (gi.name.Contains(Data.logsGearName))
            {
                totalInInventory += GameManager.GetInventoryComponent().NumGearInInventory(Data.logsGearName);
                totalInInventory += GameManager.GetInventoryComponent().NumGearInInventory(Data.logsBundleGearName) * Data.logsBundleSize;
            }
            else
            {
                totalInInventory += GameManager.GetInventoryComponent().NumGearInInventory(gi.name);
            }

            return totalInInventory >= num;
        }

        public static void UnpackBundlesAndDeductItems(GearItem gi, int num)
        {
            int dif = 0;
            int bundlesToUnpack = 0;

            string bundleName = "";
            int bundleSize = 0;

            if (gi.name.Contains(Data.nailsGearName))
            {
                dif = GameManager.GetInventoryComponent().NumGearInInventory(Data.nailsGearName) - num;
                bundleName = Data.nailsBundleGearName;
                bundleSize = Data.nailsBundleSize;
            }
            else if (gi.name.Contains(Data.planksGearName))
            {
                dif = GameManager.GetInventoryComponent().NumGearInInventory(Data.planksGearName) - num;
                bundleName = Data.planksBundleGearName;
                bundleSize = Data.planksBundleSize;
            }
            else if (gi.name.Contains(Data.logsGearName))
            {
                dif = GameManager.GetInventoryComponent().NumGearInInventory(Data.logsGearName) - num;
                bundleName = Data.logsBundleGearName;
                bundleSize = Data.logsBundleSize;
            }

            if (dif < 0)
            {
                bundlesToUnpack = Mathf.CeilToInt((float)Mathf.Abs(dif) / bundleSize);

                Utility.Log(ConsoleColor.Gray, $"Required: {num}, unpacking {bundlesToUnpack} of {bundleName} into {bundlesToUnpack * bundleSize} of {gi.name}");
                GameManager.GetInventoryComponent().RemoveGearFromInventory(bundleName, bundlesToUnpack);
                gi.GetComponent<StackableItem>().m_ShareStackWithGear = new (0); // can be removed after MC fix TEMP
                GameManager.GetPlayerManagerComponent().InstantiateItemInPlayerInventory(gi, bundlesToUnpack * bundleSize);
            }

            GameManager.GetInventoryComponent().RemoveGearFromInventory(gi.name, num);
        }

        public static NavMeshObstacle Initialize(this NavMeshObstacle nvmo)
        {
            if (!nvmo)
            {
                //MelonLogger.Msg("how");
                return null;
            }
            nvmo.carveOnlyStationary = true;
            nvmo.carving = true;
            nvmo.shape = NavMeshObstacleShape.Box;

            return nvmo;
        }

        public static void Split<T>(T[] array, int index, out T[] first, out T[] second)
        {
            first = array.Take(index).ToArray();
            second = array.Skip(index).ToArray();
        }

        public static void SplitMidPoint<T>(T[] array, out T[] first, out T[] second)
        {
            Split(array, array.Length / 2, out first, out second);
        }

        public static Transform FindInactive(this Transform parent, string childName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == childName)
                    return child;
            return null;
        }

        public static void Log(ConsoleColor color, string message)
        {
            if (Settings.options.showDebugInfo || color == ConsoleColor.Red)
            {
                MelonLogger.Msg(color, message);
            }
        }
    }
}
