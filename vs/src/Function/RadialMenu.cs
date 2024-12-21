using LocalizationUtilities;

namespace Architect
{

    public class RadialArm
    {
        public enum Direction
        {
            /// <remarks> ↑ </remarks>
            North = 0,
            /// <remarks> ↗ </remarks>
            NorthEast = 5,
            /// <remarks> → </remarks>
            East = 1,
            /// <remarks> ↘ </remarks>
            SouthEast = 7,
            /// <remarks> ↓ </remarks>
            South = 3,
            /// <remarks> ↙ </remarks>
            SouthWest = 6,
            /// <remarks> ← </remarks>
            West = 2,
            /// <remarks> ↖ </remarks>
            NorthWest = 4,
            /// <remarks> • </remarks>
            Middle = 8
        }

        public Panel_ActionsRadial.RadialInfo? radialInfo;
        public Action? action;
    }

    public static class RadialBuilder
    {
        public static RadialArm[] New()
        {
            RadialArm[] menu = new RadialArm[9];
            RadialArm ra = CreateRadialArm("NaN", "", null, Panel_ActionsRadial.RadialType.Empty);

            Array.Fill(menu, ra);

            return menu;
        }

        public static RadialArm[] SetSpecific(this RadialArm[] array, RadialArm radialArm, RadialArm.Direction direction)
        {
            array[(int)direction] = radialArm;
            
            return array;
        }



        public static RadialArm CreateRadialArm(string localizationKey, string iconName, Action? action = null, Panel_ActionsRadial.RadialType radialType = Panel_ActionsRadial.RadialType.Navigation)
        {
            if (iconName == "") iconName = placeholderIconName;

            return new RadialArm()
            {
                radialInfo = new Panel_ActionsRadial.RadialInfo()
                {
                    m_RadialElement = radialType,
                    m_SpriteName = modPrefix + localizationKey + commonSeparator + iconName,
                },
                action = action
            };
        }

        public static RadialArm CreateEmptyRadialArm()
        {
            return new RadialArm()
            {
                radialInfo = new Panel_ActionsRadial.RadialInfo()
                {
                    m_RadialElement = Panel_ActionsRadial.RadialType.Empty,
                    m_SpriteName = modPrefix + "" + commonSeparator + emptyIconName,
                },
                action = null,
            };
        }

        public static RadialArm CreateRadialArmToPlaceWall(string name)
        {

            StructurePreset sp = Data.allStructures[name];
            sp.prefabName = name;
            //GameObject go = ArcMain.meshBundle.LoadAsset<GameObject>(name + "_A");
            //Structure sc = ArcMain.meshBundle.LoadAsset<GameObject>(name + "_A").GetComponent<Structure>();
            //if (sc) MelonLogger.Msg(" test " + sc.name);
            //string icoName = Data.iconDict[sc.inj_bm.Get()];
            //string loKey = sc.inj_lk.Get();
            //MelonLogger.Msg(loKey);

            return CreateRadialArm(sp.loKey, sp.icoName, () => MelonCoroutines.Start(RadialActions.StartPlacing(sp))); 
        }


    }


    public class RadialActions
    {

        public static IEnumerator StartPlacing(StructurePreset sp)
        {
            string[] variant = { "A", "B", "C", "D", "E" };
            Random r = new Random();
            string variantName = sp.prefabName + "_" + variant[r.Next(sp.numVar)];

            GameObject wallPart = GameObject.Instantiate(meshBundle.LoadAsset<GameObject>(variantName));
            wallPart.name = variantName;
            Structure sc = wallPart.GetComponent<Structure>();
            sc.localizationKey = sp.loKey;
            sc.localizedName = Localization.Get(sp.loKey);
            sc.buildMaterial = sp.bMat;
            sc.buildPart = sp.bPart;
            sc.resources = sp.res;
            sc.isPaintable = sp.paintable;
            //sc.isDoor = sp.isDoor;
            //sc.SetupBreakDown();

            yield return new WaitForEndOfFrame(); // give custom component time to initialize

            GameManager.GetPlayerManagerComponent().StartPlaceMesh(wallPart, PlaceMeshFlags.None, placeRules);
        }

        public static void RefreshParticleKillers() => GameManager.GetUniStorm().m_WeatherParticleManager.InitializeForScene();

        public static void ToggleBuildingMode(bool enable)
        {
            isInBuildingMode = enable;

            if (!enable) RefreshParticleKillers();
        }

    }

    public class CustomRadialMenu
    {

        public KeyCode keycode;
        internal bool enabled = true;

        public CustomRadialMenu(KeyCode keyCode, bool enabled = true)
        {
            this.keycode = keyCode;
            this.enabled = enabled;
            RadialMenuManager.AddToList(this);
        }

        private void FinalizeRadial(RadialArm[] radialArray, Action a, bool clearQueue = false)
        {
            Panel_ActionsRadial radial = InterfaceManager.GetPanel<Panel_ActionsRadial>();
            if (clearQueue) radial.m_Queue.Clear();
            radial.m_Queue.Add(a);
            radial.Enable(true, false);

            foreach (RadialArm ra in radialArray)
            {
                radial.AddRadialSelection(ra.radialInfo, ra.action);
            }
        }

        public void ShowMainRadial()
        {
            RadialArm[] rArray = RadialBuilder.New();

            if (isInBuildingMode)
            {
                rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_PlankMaterialType", "ico_Material__Plank", () => ShowPartSelection(Data.BuildMaterial.WoodPlank)), RadialArm.Direction.East);
                rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_LogMaterialType", "ico_Material__Log", () => ShowPartSelection(Data.BuildMaterial.WoodLog)), RadialArm.Direction.West);
                //rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_StoneMaterialType", "ico_Material__Stone", () => ShowPartSelection(Data.BuildMaterial.Stone)), RadialArm.Direction.North);

                rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_ToggleBuildingModeOFF", "ICO_Architect_EnableBuild", () => RadialActions.ToggleBuildingMode(false)), RadialArm.Direction.Middle);
            }
            else
            {
                rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_ToggleBuildingModeON", "ICO_Architect_DisableBuild", () => RadialActions.ToggleBuildingMode(true)), RadialArm.Direction.Middle);
            }

            FinalizeRadial(rArray, ShowMainRadial, true);
        }

        public void ShowPartSelection(Data.BuildMaterial bm)
        {
            RadialArm[] rArray = RadialBuilder.New();
            
            rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_Walls", "ICO_Architect_Walls", () => ShowPartVariantSelection(bm, Data.BuildPart.Wall)), RadialArm.Direction.East);
            rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_Floors", "ICO_Architect_Floors", () => ShowPartVariantSelection(bm, Data.BuildPart.Floor)), RadialArm.Direction.South);
            rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_Roofs", "ICO_Architect_Roofs", () => ShowPartVariantSelection(bm, Data.BuildPart.Roof)), RadialArm.Direction.North);
            rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_Pillars", "ICO_Architect_Pillars", () => ShowPartVariantSelection(bm, Data.BuildPart.Pillar)), RadialArm.Direction.West);
            rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_Stairs", "ICO_Architect_Stairs", () => ShowPartVariantSelection(bm, Data.BuildPart.Stairs)), RadialArm.Direction.NorthEast);
            rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_Interactive", "ICO_Architect_Interactive", () => ShowPartVariantSelection(bm, Data.BuildPart.Door)), RadialArm.Direction.NorthWest);
            rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_Misc", "ICO_Architect_Misc", () => ShowPartVariantSelection(bm, Data.BuildPart.Misc)), RadialArm.Direction.SouthWest);
            rArray.SetSpecific(RadialBuilder.CreateRadialArm("ARC_Deco", "ICO_Architect_Deco", () => ShowPartVariantSelection(bm, Data.BuildPart.Deco)), RadialArm.Direction.SouthEast);

            
            switch (bm)
            {
                case Data.BuildMaterial.WoodPlank:
                    //rArray.SetSpecific(RadialBuilder.CreateEmptyRadialArm(), RadialArm.Direction.West);
                    rArray.SetSpecific(RadialBuilder.CreateEmptyRadialArm(), RadialArm.Direction.SouthEast);
                    break;

                case Data.BuildMaterial.WoodLog:
                    rArray.SetSpecific(RadialBuilder.CreateEmptyRadialArm(), RadialArm.Direction.NorthWest);
                    rArray.SetSpecific(RadialBuilder.CreateEmptyRadialArm(), RadialArm.Direction.SouthEast);
                    break;

                case Data.BuildMaterial.Stone:
                    //rArray.SetSpecific(RadialBuilder.CreateEmptyRadialArm(), RadialArm.Direction.North);
                    //rArray.SetSpecific(RadialBuilder.CreateEmptyRadialArm(), RadialArm.Direction.NorthWest);
                    break;
            }
            

            FinalizeRadial(rArray, () => ShowPartSelection(bm));
        }


        public void ShowPartVariantSelection(Data.BuildMaterial bm, Data.BuildPart bp)
        {
            RadialArm[] rArray = RadialBuilder.New();

            switch (bm)
            {
                case Data.BuildMaterial.WoodPlank:
                    switch (bp)
                    {
                        case Data.BuildPart.Wall:
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_halfWall"), RadialArm.Direction.North);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_narrowWall"), RadialArm.Direction.NorthEast);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_regularWall"), RadialArm.Direction.East);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_doorWall"), RadialArm.Direction.SouthEast);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_windowWall"), RadialArm.Direction.South);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_halfNarrowWall"), RadialArm.Direction.SouthWest);
                            break;
                        case Data.BuildPart.Floor:
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_regularFloor"), RadialArm.Direction.North);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_quarterFloor"), RadialArm.Direction.West);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_stairsFloor"), RadialArm.Direction.SouthWest);
                            break;
                        case Data.BuildPart.Roof:
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_regularRoof"), RadialArm.Direction.North);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_triangleWallLeft"), RadialArm.Direction.SouthEast);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_triangleWallRight"), RadialArm.Direction.South);
                            break;
                        case Data.BuildPart.Pillar:
                            //rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_pillar"), RadialArm.Direction.North);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_railingEnd"), RadialArm.Direction.North);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_railingShort"), RadialArm.Direction.SouthEast);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_railingLong"), RadialArm.Direction.East);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_railingSlanted"), RadialArm.Direction.NorthEast);
                            break;
                        case Data.BuildPart.Stairs:
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_regularStairs"), RadialArm.Direction.East);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_longStairs"), RadialArm.Direction.NorthEast);
                            break;
                        case Data.BuildPart.Door: // interactive
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_doorFancy"), RadialArm.Direction.North);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_doorSimple"), RadialArm.Direction.NorthEast);
                            break;
                        case Data.BuildPart.Deco:
                            break;
                        case Data.BuildPart.Misc: // patch-ups, scaffolding
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_scaffolding"), RadialArm.Direction.South);
                            //rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_plank_beam"), RadialArm.Direction.East);
                            break;
                    }
                    
                    break;

                case Data.BuildMaterial.WoodLog:
                    switch (bp)
                    {
                        case Data.BuildPart.Wall:
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_halfWall"), RadialArm.Direction.North);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_narrowWall"), RadialArm.Direction.NorthEast);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_regularWall"), RadialArm.Direction.East);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_doorWall"), RadialArm.Direction.SouthEast);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_windowWall"), RadialArm.Direction.South);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_halfNarrowWall"), RadialArm.Direction.SouthWest);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_largeWall"), RadialArm.Direction.West);
                            break;
                        case Data.BuildPart.Floor:
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_regularFloor"), RadialArm.Direction.North);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_elevatedFloor"), RadialArm.Direction.SouthEast);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_foundation"), RadialArm.Direction.South);
                            break;
                        case Data.BuildPart.Roof:
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_triangleWallLeft"), RadialArm.Direction.SouthEast);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_triangleWallRight"), RadialArm.Direction.South);
                            break;
                        case Data.BuildPart.Pillar:
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_pillar"), RadialArm.Direction.North);
                            break;
                        case Data.BuildPart.Stairs:
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_regularStairs"), RadialArm.Direction.East);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_longStairs"), RadialArm.Direction.NorthEast);
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_ramp"), RadialArm.Direction.NorthWest);
                            break;
                        case Data.BuildPart.Door: // interactive
                            break;
                        case Data.BuildPart.Deco:
                            break;
                        case Data.BuildPart.Misc: // patch-ups, scaffolding
                            rArray.SetSpecific(RadialBuilder.CreateRadialArmToPlaceWall("ARC_log_beam"), RadialArm.Direction.East);
                            break;
                    }
                    break;

                case Data.BuildMaterial.Stone:

                    break;
            }

            FinalizeRadial(rArray, () => ShowPartVariantSelection(bm, bp));
        }
    }


    internal class RadialMenuManager
    {
        public static List<CustomRadialMenu> radialMenuList = new List<CustomRadialMenu>();

        internal static void AddToList(CustomRadialMenu customRadialMenu)
        {
            radialMenuList.Add(customRadialMenu);
        }

        internal static void MaybeShowMenu()
        {
            foreach (CustomRadialMenu radialMenu in radialMenuList)
            {
                if (radialMenu.enabled)
                {
                    if (Utility.GetKeyDown(radialMenu.keycode) && !InterfaceManager.GetPanel<Panel_ActionsRadial>().IsEnabled())
                    {
                        InputManager.OpenRadialMenu();
                        if (InterfaceManager.GetPanel<Panel_ActionsRadial>().IsEnabled()) radialMenu.ShowMainRadial();

                    }
                }
            }
        }
    }
}