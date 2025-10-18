using Il2Cpp;
using Il2CppVLB;
using UnityEngine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Architect
{
    internal class Patches
    {
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
        internal class ShowRadial
        {
            private static void Postfix()
            {
                RadialMenuManager.MaybeShowMenu();
            }
        }


        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Start))]
        internal class OnGameStart
        {
            private static void Postfix()
            {
                gameStarted = true;
            }
        }

        [HarmonyPatch(typeof(GearItem), nameof(GearItem.Awake))]
        internal class MakeToolsBreakDownItem
        {
            private static void Postfix(ref GearItem __instance)
            {
                foreach (string s in Data.toolSpecs.Keys)
                {
                    if (__instance.name.Contains(s))
                    {
                        BreakDownItem bdi = __instance.GetOrAddComponent<BreakDownItem>();
                        __instance.m_BreakDownItem = bdi;
                    }
                }

            }
        }

        [HarmonyPatch(typeof(BreakDown), nameof(BreakDown.Start))]
        internal class StopChangingMyFuckingLayers
        {
            private static void Postfix(ref BreakDown __instance)
            {
                Structure sc = __instance.GetComponent<Structure>();
                if (sc) sc.SetLayerAndVisuals();
            }
        }
        
        [HarmonyPatch(typeof(BreakDown), nameof(BreakDown.PerformInteraction))]
        internal class BreakDownInteraction
        {
            public static bool shouldShowBuildInsteadOfBreakDown;
            private static bool Prefix(ref BreakDown __instance)
            {
                Structure sc = __instance.GetComponent<Structure>();
                if (sc && Settings.options.noRequirements)
                {
                    sc.isBuilt = !sc.isBuilt;
                    sc.Finalize(true);
                    return false;
                }
                if (sc && !sc.isBuilt)
                {
                    shouldShowBuildInsteadOfBreakDown = true;
                }
                return true;
            }

            private static void Postfix(ref BreakDown __instance)
            {
                Structure sc = __instance.GetComponent<Structure>();
                if (sc)
                {
                    shouldShowBuildInsteadOfBreakDown = false;
                }
            }
        }
        
        [HarmonyPatch(typeof(Localization), nameof(Localization.Get))]
        internal class ReplaceHudMessage
        {
            private static void Prefix(ref string key)
            {
                if (BreakDownInteraction.shouldShowBuildInsteadOfBreakDown)
                {
                    key = key.Replace("GAMEPLAY_RequiresToolToBreakDown", "ARC_Interface_RequiresToolToBreakDown");
                }
            }
        }
        
        
        [HarmonyPatch(typeof(BreakDown), nameof(BreakDown.Awake))]
        internal class AddingNailsToYields
        {
            private static void Postfix(ref BreakDown __instance)
            {
                int i = 0;

                foreach (KeyValuePair<string, int> entry in Data.nailsYieldPerObject)
                {
                    if (__instance.name.Contains(entry.Key)) i = entry.Value;
                }

                if (i == 0) return;

                GameObject nails = GetPrefab(Data.nailsGearName);
                if (nails)
                {
                    //List<GameObject> yields = __instance.m_YieldObject.ToList();
                    List<GameObject> tools = __instance.m_UsableTools.ToList();
                    //List<int> yieldsNum = __instance.m_YieldObjectUnits.ToList();
                    //yields.Add(nails.gameObject);
                    tools.Add(GetPrefab("GEAR_Prybar"));
                    //yieldsNum.Add(i);
                    //__instance.m_YieldObject = yields.ToArray();
                    //__instance.m_YieldObjectUnits = yieldsNum.ToArray();
                    __instance.m_UsableTools = tools.ToArray();

                }
                else
                {
                    Log(ConsoleColor.Red, "Missing Architect.modcomponent");
                }

            }
        }

        [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.Enable))]
        internal class BreakDownLabels1
        {
            private static void Postfix(ref Panel_BreakDown __instance, ref bool enable)
            {
                Structure sc = __instance.m_BreakDown.GetComponent<Structure>();
                
                if (sc && sc.enabled && !sc.isBeingPlaced)
                {
                    if (enable)
                    {
                        if (!sc.isBuilt)
                        {
                            //Interfaces.UpdateRequirementLabels(sc);
                            Interfaces.ChangeBreakDownLabelsToBuild(true);
                        }
                    }
                    else
                    {
                        //Interfaces.ResetRequirementLabels(sc);
                        Interfaces.ChangeBreakDownLabelsToBuild(false);
                        Interfaces.ResetRequirementLabels(sc.breakdown);
                        lastSelectedBreakDownTool = "";
                        CancelActionIfNotEnoughMaterials.showNotEnoughItemsLabel = false;
                        __instance.m_RequiresToolLabel.GetComponent<UILocalize>().key = "GAMEPLAY_RequiresTool";
                        __instance.m_RequiresToolLabel.GetComponent<UILocalize>().OnLocalize();

                    }
                }
            }
        }

        [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.OnBreakDown))]
        internal class CancelActionIfNotEnoughMaterials
        {
            public static bool showNotEnoughItemsLabel;
            private static bool Prefix(ref Panel_BreakDown __instance)
            {
                Structure sc = __instance.m_BreakDown.GetComponent<Structure>();

                if (sc && sc.enabled && !sc.isBeingPlaced)
                {
                    if (!sc.isBuilt)
                    {
                        int n = 0;
                        for (int i = 0; i < sc.breakdown.m_YieldObject.Count; i++)
                        {
                            if (!PlayerHasEnoughItems(sc.breakdown.m_YieldObject[i].GetComponent<GearItem>(), sc.breakdown.m_YieldObjectUnits[i]))
                            {
                                n++;
                            }
                        }

                        if (n > 0)
                        {
                            showNotEnoughItemsLabel = true;
                            __instance.m_RequiresToolLabel.GetComponent<UILocalize>().key = "ARC_Interface_NotEnoughMaterials";
                            __instance.m_RequiresToolLabel.GetComponent<UILocalize>().OnLocalize();
                            GameAudioManager.PlayGUIError();
                            //HUDMessage.AddMessage(Localization.Get("ARC_Interface_NotEnoughMaterials"), false, true);
                            
                            return false;
                        }
                    }
                }
                return true;
            }
        }
        
        
        [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.Update))]
        internal class ShowMessageIfNotEnoughMaterials
        {
            private static void Postfix(ref Panel_BreakDown __instance)
            {
                if (CancelActionIfNotEnoughMaterials.showNotEnoughItemsLabel)
                {
                    __instance.m_RequiresToolLabel.active = true;
                }
            }
        }


        [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.UpdateDurationLabel))]
        internal class UpdateYieldForCustomTools
        {
            private static bool Prefix(ref Panel_BreakDown __instance)
            {

                Structure sc = __instance.m_BreakDown.GetComponent<Structure>();

                if (sc && sc.enabled && !sc.isBeingPlaced)
                {
                    GearItem tool = __instance.GetSelectedTool();

                    __instance.m_DurationHours = __instance.m_BreakDown.m_TimeCostHours;

                    if (tool)
                    {
                        ToolEfficiency te = Data.toolSpecs[tool.name];

                        __instance.m_DurationHours *= sc.isBuilt ? te.breakTime : te.buildTime;

                        if (lastSelectedBreakDownTool != tool.name) // prevent unnecessary updates for yield updates
                        {
                            StructureBuildInfo sr = Data.GetStructureResources(sc.resources, sc.buildMaterial);

                            if (sc.isBuilt) // break down
                            {
                                sc.breakdown.m_YieldObject = sr.yields;

                                if (sc.resources == Data.ResourcesPreset.Scaffolding) // scaffolding yields aren't affected by tool and always 100%
                                {
                                    sc.breakdown.m_YieldObject = sr.yields.Take(sr.yields.Count() - 1).ToArray(); // skip broken materials
                                    sc.breakdown.m_YieldObjectUnits = sr.yieldsNum;
                                }
                                else
                                {
                                    List<GameObject> yields = sr.yields.ToList();
                                    List<int> yieldsNum = sr.yieldsNum.ToList();
                                    if (te.yieldAmount == 1f || sr.yieldsNum[0] <= 1) // if 100% efficient tool or singular structure
                                    {
                                        yields.RemoveAt(2);
                                        yieldsNum.RemoveAt(2);
                                    }
                                    else // if not 100% efficient tool - add broken materials to yield
                                    {
                                        yieldsNum[2] = sr.yieldsNum[0] - Mathf.FloorToInt(sr.yieldsNum[0] * te.yieldAmount);
                                        yieldsNum[0] = Mathf.FloorToInt(sr.yieldsNum[0] * te.yieldAmount); // main resource
                                    }
                                    
                                    if (sr.yields[1].name.Contains(Data.nailsGearName)) // secondary resource
                                    {
                                        yieldsNum[1] = te.yieldExtra ? sr.yieldsNum[1] : 0;
                                        if (yieldsNum[1] == 0)
                                        {
                                            yields.RemoveAt(1);
                                            yieldsNum.RemoveAt(1);
                                        }
                                    }
                                    if (sr.yields[1].name.Contains(Data.placeholderGearName)) // if no secondary resource
                                    {
                                        yields.RemoveAt(1);
                                        yieldsNum.RemoveAt(1);
                                    }
                                    sc.breakdown.m_YieldObject = yields.ToArray();
                                    sc.breakdown.m_YieldObjectUnits = yieldsNum.ToArray();
                                    __instance.RefreshYield();
                                }

                                Interfaces.ResetRequirementLabels(sc.breakdown);
                            }
                            else // build
                            {
                                int n = 1; // skip broken
                                if (sr.yields[1].name.Contains(Data.placeholderGearName)) n = 2; // skip secondary resource if not using
                                if (sr.yieldsNum[1] == 0) n = 2; // skip secondary resource if 0
                                sc.breakdown.m_YieldObject = sr.yields.Take(sr.yields.Length - n).ToArray(); 
                                sc.breakdown.m_YieldObjectUnits = sr.yieldsNum;
                                __instance.RefreshYield();
                                Interfaces.UpdateRequirementLabels(sc.breakdown);
                            }
                        }

                        lastSelectedBreakDownTool = tool.name;
                    }
                    if (__instance.m_DurationHours > 0f)
                    {
                        __instance.GetPanelInfo().m_DurationLabel.text = Il2Cpp.Utils.GetExpandedDurationString(Mathf.RoundToInt(__instance.m_DurationHours * 60f));
                        return false;
                    }
                    __instance.GetPanelInfo().m_DurationLabel.text = "0 " + Localization.Get("GAMEPLAY_hours");
                    return false;
                }

                else // nail yields from crates/pallets
                {
                    int y = 0;

                    foreach (KeyValuePair<string, int> entry in Data.nailsYieldPerObject)
                    {
                        if (__instance.m_BreakDown.name.Contains(entry.Key)) y = entry.Value;
                    }

                    if (y == 0) return true;
                    
                    GearItem tool = __instance.GetSelectedTool();
                    
                    if (tool?.name != lastSelectedBreakDownTool) // prevent unnecessary updates
                    {
                        List<GameObject> yields = __instance.m_BreakDown.m_YieldObject.ToList();
                        List<int> yieldsNum = __instance.m_BreakDown.m_YieldObjectUnits.ToList();

                        if (tool?.name.Contains("Prybar") == true) // only get nails when using prybar
                        {

                            yields.Add(GetPrefab(Data.nailsGearName));
                            yieldsNum.Add(y);
                            if (tool?.m_BreakDownItem.m_BreakDownTimeModifier == 0f)
                            {
                                tool.m_BreakDownItem.m_BreakDownTimeModifier = Data.toolSpecs[tool.name].breakTime;
                            }

                        }
                        else
                        {
                            for (int i = 0; i < yields.Count; i++)
                            {
                                if (yields[i].name.Contains(Data.nailsGearName))
                                {
                                    yields.RemoveAt(i);
                                    yieldsNum.RemoveAt(i);
                                }
                            }
                        }

                        __instance.m_BreakDown.m_YieldObject = yields.ToArray();
                        __instance.m_BreakDown.m_YieldObjectUnits = yieldsNum.ToArray();

                        __instance.RefreshYield();

                        lastSelectedBreakDownTool = tool ? tool.name : "";
                    }
                }


                return true;
            }
        }


        [HarmonyPatch(typeof(AccelTimePopup), nameof(AccelTimePopup.SetActive))]
        internal class BreakDownLabels2
        {
            private static void Postfix(ref AccelTimePopup __instance, ref bool active)
            {

                if (InterfaceManager.GetPanel<Panel_BreakDown>().IsEnabled() && InterfaceManager.GetPanel<Panel_BreakDown>().IsBreakingDown())
                {
                    Structure sc = InterfaceManager.GetPanel<Panel_BreakDown>().m_BreakDown.GetComponent<Structure>();

                    if (sc && sc.enabled && !sc.isBeingPlaced)
                    {
                        if (active)
                        {
                            if (!sc.isBuilt)
                            {
                                __instance.m_Label_Action.text = Localization.Get("ARC_Interface_BreakingDownProgress");
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BreakDown), nameof(BreakDown.DoBreakDown))]
        internal class BreakDownExecuted
        {
            private static bool Prefix(ref BreakDown __instance, ref bool spawnYieldObjects)
            {
                Structure sc = __instance.GetComponent<Structure>();

                if (sc && sc.enabled && !sc.isBeingPlaced)
                {
                    if (sc.isBuilt) // break down
                    {
                        if (Settings.options.dropYields) __instance.SpawnYieldObjectsAndStickToGround();
                        else __instance.SpawnYieldObjectsAndAddToInventory();
                        lastSelectedBreakDownTool = "";
                        sc.ProcessDeleteInteraction();
                    }
                    else // build
                    {
                        sc.isBuilt = true;
                        if (sc.constructionDate == 0f) sc.constructionDate = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                        for (int i = 0; i < sc.breakdown.m_YieldObject.Count; i++)
                        {
                            UnpackBundlesAndDeductItems(sc.breakdown.m_YieldObject[i].GetComponent<GearItem>(), sc.breakdown.m_YieldObjectUnits[i]);
                            
                        }
                    }
                    
                    __instance.StickSurfaceObjectsToGround(false);
                    sc.Finalize(true);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(InputManager), nameof(InputManager.ExecuteAltFire))] // interrupted by BetterBases
        internal class ProcessStructureRightClick
        {
            private static bool skip;

            private static void Prefix() // prevent picking up same object again while already in placement mode
            {
                if (!gameStarted) return;

                if (GameManager.GetPlayerManagerComponent().IsInPlacementMode())
                {
                    GameManager.GetPlayerManagerComponent().CancelPlaceMesh();
                    skip = true;
                }
            }

            private static void Postfix()
            {
                if (!gameStarted) return;

                if (skip)
                {
                    skip = false;
                    return;
                }

                Structure? sc = GetInteractiveGameObjectUnderCrosshair()?.GetComponent<Structure>();
                if (sc && sc.enabled && !sc.isBeingPlaced)
                {
                    sc.ProcessAltInteraction();
                }
            }
        }

        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.SetHoverText))]
        public class ShowButtonsAndHoverText
        {
            public static void Prefix(ref string hoverText, ref GameObject itemUnderCrosshairs)
            {

                if (!gameStarted) return;

                //Structure? str = GetInteractiveGameObjectUnderCrosshair()?.GetComponent<Structure>();
                Structure? str = itemUnderCrosshairs?.GetComponent<Structure>();
                if (!str || InterfaceManager.s_IsOverlayActive) return;

                if (isInBuildingMode)
                {
                    EquipItemPopup eip = InterfaceManager.GetPanel<Panel_HUD>().m_EquipItemPopup;

                    eip.enabled = true;

                    if (!str.isBuilt)
                    {

                        if (InputManager.GetSprintDown(InputManager.m_CurrentContext))
                        {
                            eip.ShowGenericPopupWithDefaultActions(Localization.Get("ARC_InteractionBuild"), Localization.Get("ARC_InteractionMovePrecise"));
                            eip.m_ButtonPromptScrollWheel.ShowPromptForKey(Localization.Get("ARC_InteractionDelete"), "Scroll");
                        }
                        else
                        {
                            eip.ShowGenericPopupWithDefaultActions(Localization.Get("ARC_InteractionBuild"), Localization.Get("ARC_InteractionMove"));
                            eip.m_ButtonPromptScrollWheel.ShowPromptForKey(Localization.Get("ARC_InteractionClone"), "Scroll");
                        }
                    }
                    else 
                    {
                        eip.ShowGenericPopupWithDefaultActions(Localization.Get("ARC_InteractionBreakdown"), Localization.Get("ARC_InteractionPaint"));

                        eip.m_ButtonPromptScrollWheel.ShowPromptForKey(Localization.Get("ARC_InteractionClone"), "Scroll");
                    }
                }

                if (string.IsNullOrEmpty(hoverText))
                {
                    if (!isInBuildingMode && str.isBuilt && str.buildPart == Data.BuildPart.Door) return; // doors have their own hover text from OpenClose component
                    hoverText = itemUnderCrosshairs?.GetComponent<Structure>().localizedName;
                }
            }
        }


        [HarmonyPatch(typeof(InputManager), nameof(InputManager.GetRadialButtonHeldDown))]
        internal class PreventRadialClosing
        {
            private static void Postfix(ref bool __result, ref MonoBehaviour context)
            {
                if (InputManager.HasContext(context))
                {
                    foreach (CustomRadialMenu radialMenu in RadialMenuManager.radialMenuList)
                    {
                        if (radialMenu.enabled)
                        {
                            if (Utility.GetKeyHeld(radialMenu.keycode))
                            {
                                __result = true;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RadialMenuArm), nameof(RadialMenuArm.SetRadialInfo))]
        internal class InjectName
        {
            private static void Postfix(ref RadialMenuArm __instance)
            {
                if (!__instance) return;

                string unprocessedName = __instance.m_Sprite.spriteName;

                if (unprocessedName.Contains(modPrefix) && __instance.m_RadialInfo != null)
                {
                    string[] processedName = unprocessedName.Replace(modPrefix, "").Split(commonSeparator);
                    string displayNameLoKey = processedName[0];
                    string iconName = processedName[1];

                    __instance.OverrideHoverText(Localization.Get(displayNameLoKey));

                    __instance.m_Sprite.enabled = false;
                    __instance.m_Texture.enabled = true;

                    if (!__instance.m_Texture.mainTexture.name.Contains(modPrefix + iconName))
                    {
                        __instance.m_Texture.mainTexture = Utility.LoadEmbeddedIcon(iconName + ".png");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveSceneData))]
        private static class SaveStructures
        {
            internal static void Postfix(ref string sceneSaveName)
            {
                dataManager.Save(StructureManager.SerializeAll(), saveDataTag + commonSeparator + sceneSaveName);
            }
        }


        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.LoadSceneData))]
        private static class LoadStructures
        {
            internal static void Postfix(ref string sceneSaveName)
            {

                string? serializedSaveData = dataManager.Load(saveDataTag + commonSeparator + sceneSaveName);
                //if (string.IsNullOrEmpty(serializedSaveData)) serializedSaveData = dataManager.Load(saveDataTag);
                string[]? structureList = null;

                //if (!string.IsNullOrEmpty(serializedSaveData)) JSON.MakeInto(JSON.Load(serializedSaveData), out structureList);
                if (!string.IsNullOrEmpty(serializedSaveData)) structureList = JsonSerializer.Deserialize<string[]?>(serializedSaveData);

                if (structureList != null && structureList.Length > 0)
                {
                    StructureManager.DeserializeAll(structureList);
                }
            }
        }


        [HarmonyPatch(typeof(GameManager), nameof(GameManager.ResetLists))]
        private static class ResetStructureManagerOnSceneLoad
        {
            internal static void Postfix()
            {
                StructureManager.Reset();

            }
        }


        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.UpdateObjectToPlaceTransform), [typeof(Vector3), typeof(Quaternion)])]
        private static class KeepRotationOnPlace
        {
            internal static void Prefix(PlayerManager __instance, ref Quaternion rotation)
            {
                Structure sc = GameManager.GetPlayerManagerComponent().m_ObjectToPlace?.GetComponent<Structure>();
                if (sc)
                {
                    if (sc.lastPlacedRotation != Quaternion.identity)
                    {
                        // done in position check instead
                        //rotation = sc.lastPlacedRotation;
                        //sc.lastPlacedRotation = Quaternion.identity;
                    }
                }

            }

            // snapping part


            
            internal static bool Prefix(PlayerManager __instance)
            {
                Structure sc = GameManager.GetPlayerManagerComponent().m_ObjectToPlace?.GetComponent<Structure>();
                if (sc && Snap.PartToPattern(sc.buildPart) != Snap.SnapPattern.Free && !InputManager.GetSprintDown(InputManager.m_CurrentContext))
                {
                    if (Snap.SnapToTriggerRelatedPoint(sc))
                    {
                        forceShowCrosshair = true;
                        Snap.SetCustomRotation(Input.mouseScrollDelta.y); // rotate with mouse wheel
                        sc.ToggleSnapTriggers(false);
                        return false;
                    }                   
                }

                Snap.ResetObject();
                return true;
            }
            
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.StartPlaceMesh), [typeof(GameObject), typeof(float), typeof(PlaceMeshFlags), typeof(PlaceMeshRules)])]
        private static class ManagePlacement
        {
            private static Structure? sc;

            internal static void Prefix(PlayerManager __instance, ref GameObject objectToPlace)
            {
                sc = objectToPlace?.GetComponent<Structure>();
                if (sc != null)
                {
                    Snap.ResetObject();
                    sc.lastPlacedRotation = sc.transform.rotation;
                }
                    
            }

            internal static void Postfix(PlayerManager __instance, ref GameObject objectToPlace)
            {
                sc = objectToPlace?.GetComponent<Structure>();
                if (sc != null)
                {
                    
                    sc.isBeingPlaced = true;
                    if (!sc.isBuilt) Materials.AssignMaterial(sc, Data.BuildPartSide.Both, true, true);
                    //Utils.m_PhysicalCollisionLayerMask &= ~(1 << vp_Layer.IgnoreRaycast); // disable surface conform
                    sc.lastPlacedRotation = sc.transform.rotation;
                }

            }
        }   
        
        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.TintPreviewRenderers))]
        private static class DisableTint
        {
            internal static bool Prefix(PlayerManager __instance)
            {
                Structure? sc = __instance.GetObjectToPlace()?.GetComponent<Structure>();
                if (sc != null)
                {
                    return false;
                }
                return true;
                    
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.ExitMeshPlacement))]
        private static class ManagePostPlacement
        {
            private static bool? isArc;
            private static GameObject arcObject;
            private static Structure sc;

            internal static void Prefix(PlayerManager __instance)
            {
                if (__instance.GetObjectToPlace()?.GetComponent<Structure>() != null)
                {
                    isArc = true;
                    arcObject = __instance.GetObjectToPlace();
                    
                }
            }

            internal static void Postfix(PlayerManager __instance)
            {
                if (isArc == true)
                {
                    isArc = false;

                    if (arcObject)
                    {
                        if (arcObject.transform.position == Vector3.zero)
                        {
                            GameObject.Destroy(arcObject);
                            return;

                        }

                        sc = arcObject.GetComponent<Structure>();

                        if (!sc.isBuilt) Materials.AssignMaterial(sc, Data.BuildPartSide.Both, true, false, true);
                        sc.SetLayerAndVisuals();

                        sc.ToggleSnapTriggers(true);

                        sc.isBeingPlaced = false;

                        //Utils.m_PhysicalCollisionLayerMask |= 1 << vp_Layer.IgnoreRaycast; // reset surface conform
                    }

                    RadialActions.RefreshParticleKillers();
                }
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.GetLayerMaskForPlacementPosition))]
        private static class ChangeLayerMaskForGhostStructures
        {
            internal static void Postfix(ref int __result)
            {
                PlayerManager pm = GameManager.GetPlayerManagerComponent();

                if (pm.m_ObjectToPlace?.GetComponent<Structure>() != null)
                {
                    __result |= (1 << vp_Layer.InteractivePropNoCollidePlayer);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.DoPositionCheck))]
        [HarmonyPriority(Priority.Low)]
        private static class AllowPlacingAnywhere
        {
            internal static bool Prefix(ref PlayerManager __instance, ref MeshLocationCategory __result)
            {
                Structure sc = __instance.m_ObjectToPlace?.GetComponent<Structure>();
                if (sc)
                {
                    __result = MeshLocationCategory.Valid;
                    Quaternion rotation = Quaternion.identity;
                    if (sc.lastPlacedRotation != Quaternion.identity)
                    { 
                        rotation = sc.lastPlacedRotation;
                    }
                    int layerMask = PlayerManager.GetLayerMaskForPlacementPosition();
                    if (Physics.Raycast(GameManager.GetVpFPSCamera().transform.position, GameManager.GetVpFPSCamera().transform.forward, out RaycastHit hit, float.PositiveInfinity, layerMask))
                    {
                        __instance.UpdateObjectToPlaceTransform(hit.point, rotation);
                    }
                        
                    return false;
                }
                return true;
            }
        }

        /*
        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.DoPositionCheck))]
        private static class SecondaryRaycastForStructureComponent
        {
            [HarmonyPriority(Priority.Low)]
            internal static void Postfix(ref PlayerManager __instance, ref MeshLocationCategory __result)
            {
                if (__result == MeshLocationCategory.Invalid)
                {
                    int num = PlayerManager.GetLayerMaskForPlacementPosition();
                    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;
                    RaycastHit raycastHit;
                    if (Physics.Raycast(GameManager.GetVpFPSCamera().transform.position, GameManager.GetVpFPSCamera().transform.forward, out raycastHit, float.PositiveInfinity, num, queryTriggerInteraction))
                    {
                        if (!__instance.m_ObjectToPlace && __instance.m_DecalToPlace != null)
                        {
                            if (raycastHit.collider != null)
                            {
                                if (__instance.m_DecalToPlace.m_DecalProjectorType == DecalProjectorType.SprayPaint && raycastHit.collider.gameObject.GetComponent<Structure>())
                                {
                                    __instance.m_DecalToPlace.m_Pos = raycastHit.point; 
                                    __instance.m_DecalToPlace.m_Normal = raycastHit.normal;
                                    __result = MeshLocationCategory.Valid;
                                }
                            }
                        }
                    }
                }
            }
        }*/

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.ShouldSuppressCrosshairs))]
        public class ShowCrosshairWhenSnapOn
        {
            private static void Postfix(ref bool __result)
            {
                if (forceShowCrosshair) __result = false;
            }
        }
    }
}

