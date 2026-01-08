namespace Architect
{
    internal static class Interfaces
    {

        public static Color red = new Color(0.64f, 0.2f, 0.23f, 1f);
        public static Color white = new Color(0.98f, 0.98f, 0.98f, 1f);

        public static BreakDown InitializeForBreakdown(this BreakDown bd)
        {
            Structure sc = bd.GetComponent<Structure>();
            StructureBuildInfo sr = Data.GetStructureResources(sc.resources, sc.buildMaterial);

            bd.m_UsableTools = sr.tools;
            bd.m_YieldObject = sr.yields;//Settings.options.noRequirements ? [] : sr.yields;
            bd.m_YieldObjectUnits = sr.yieldsNum;
            bd.m_RequiresTool = true;//!Settings.options.noRequirements;
            bd.m_TimeCostHours = Settings.options.buildingTimeMult * sr.breakTime;
            bd.m_BreakDownAudio = sr.breakAudio;
            bd.m_LocalizedDisplayName = new LocalizedString() { m_LocalizationID = sc.localizationKey };
            return bd;
        }

        public static BreakDown InitializeForBuilding(this BreakDown bd)
        {
            Structure sc = bd.GetComponent<Structure>();
            StructureBuildInfo sr = Data.GetStructureResources(sc.resources, sc.buildMaterial);

            bd.m_UsableTools = sr.tools;
            bd.m_YieldObject = sr.requirements;//Settings.options.noRequirements ? [] : sr.requirements;
            bd.m_YieldObjectUnits = sr.requirementsNum;
            bd.m_RequiresTool = true;// !Settings.options.noRequirements;
            bd.m_TimeCostHours = Settings.options.buildingTimeMult * sr.buildTime;
            bd.m_BreakDownAudio = sr.buildAudio;
            bd.m_LocalizedDisplayName = new LocalizedString() { m_LocalizationID = sc.localizationKey };
            return bd;
        }

        public static void ChangeBreakDownLabelsToBuild(bool change)
        {
            Panel_BreakDown bd = InterfaceManager.GetPanel<Panel_BreakDown>();
            Transform parent = bd.m_PanelElements.transform;

            string[] labelsToChange = ["TopNav/Label_Title", "InfoPanel/Header_Label", "Yield/Label_ForagingWillTake", "Mouse_Breakdown/Mouse_Button_BreakDown/ButtonActions/Mouse_Button_Label"];

            foreach (string label in labelsToChange)
            {
                UILocalize loc = parent.Find(label).GetComponent<UILocalize>();
                string key = loc.key;


                loc.key = key.Replace(change ? "GAMEPLAY" : "ARC_Interface", change ? "ARC_Interface" : "GAMEPLAY");
                loc.OnLocalize();
                /*
                if (key == "ARC_Interface_BreakDown")
                {
                    if (Utils.RollChance(50))
                    {
                        loc.key = key.Replace("BreakDown", "BreakDownButFunny");
                        loc.OnLocalize();
                        loc.key = key.Replace("BreakDownButFunny", "BreakDown");
                    }
                }
                */
            }
        }


        public static void UpdateRequirementLabels(BreakDown bd)
        {
            Panel_BreakDown bdp = InterfaceManager.GetPanel<Panel_BreakDown>();
            //Transform pe = InterfaceManager.GetPanel<Panel_BreakDown>().m_PanelElements.transform;
            HarvestRepairMaterial[] labels = bdp.m_YieldObjects.ToArray();

            for (int i = 0; i < bd.m_YieldObject.Count; i++) 
            {

                if (PlayerHasEnoughItems(bd.m_YieldObject[i].GetComponent<GearItem>(), bd.m_YieldObjectUnits[i]))
                {
                    labels[i].m_GearSprite.color = white;
                    labels[i].m_StackLabel.color = white;
                    labels[i].m_StackLabel.gameObject.SetActive(true);
                    labels[i].m_GearLabel.color = white;
                }
                else // not enough items
                {
                    labels[i].m_GearSprite.color = red;
                    labels[i].m_GearSprite.alpha = 0.25f;
                    labels[i].m_StackLabel.text = GameManager.GetInventoryComponent().NumGearInInventory(bd.m_YieldObject[i].name) + Localization.Get("ARC_Interface_OutOf") + bd.m_YieldObjectUnits[i];
                    labels[i].m_StackLabel.color = red;
                    labels[i].m_StackLabel.gameObject.SetActive(true);
                    labels[i].m_GearLabel.color = red;
                }
            }
        }
        public static void ResetRequirementLabels(BreakDown bd)
        {
            Panel_BreakDown bdp = InterfaceManager.GetPanel<Panel_BreakDown>();
            HarvestRepairMaterial[] labels = bdp.m_YieldObjects.ToArray();

            for (int i = 0; i < labels.Length; i++) 
            {
                labels[i].m_GearSprite.color = white;
                labels[i].m_StackLabel.color = white;
                labels[i].m_GearLabel.color = white;
            }

            for (int i = 0; i < bd.m_YieldObject.Count; i++)
            {
                labels[i].m_StackLabel.gameObject.SetActive(true);
                labels[i].m_StackLabel.text = "x " + bd.m_YieldObjectUnits[i].ToString();
            }
        }
    }
}
