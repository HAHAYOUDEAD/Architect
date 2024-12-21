namespace Architect
{
    public class ArcMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Settings.OnLoad();

            CustomRadialMenu radialMenu = new CustomRadialMenu(Settings.options.menuKey);

            // Load embedded
            LocalizationManager.LoadJsonLocalization(Utility.LoadEmbeddedJSON("Localization.json"));
            meshBundle = Utility.LoadEmbeddedAssetBundle("architect");

            //Materials.vanillaSkinnedShader = Shader.Find("Shader Forge/TLD_StandardSkinned");
            //Materials.vanillaDefaultShader = Shader.Find("Shader Forge/TLD_StandardDiffuse");
            //Materials.SetupGhostMaterials();

            //ClassInjector.RegisterTypeInIl2Cpp(typeof(StructureData.BuildMaterial));


        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {

        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (Utility.IsScenePlayable(sceneName))
            {
                gameStarted = true;
            }
            else
            {
                gameStarted = false;
            }
        }

        public override void OnUpdate()
        {
            // MMB interacion
            if (Utility.GetKeyDown(KeyCode.Mouse2) && gameStarted)
            {
                Structure? sc = Utility.GetInteractiveGameObjectUnderCrosshair()?.GetComponent<Structure>();
                if (sc != null)
                {
                    if (InputManager.GetSprintDown(InputManager.m_CurrentContext))
                    {
                        if (!sc.isBuilt)
                        {
                            sc.ProcessDeleteInteraction();
                        }
                    }
                    else
                    {
                        sc.ProcessCloneInteraction();
                    }
                }
            }            

            // temp
            /*
            if (Utility.GetKeyDown(KeyCode.N))
            {
                int layerMask = 0;

                layerMask |= (1 << vp_Layer.InteractiveProp);

                if (Physics.Raycast(GameManager.GetPlayerObject().transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 5, layerMask))
                {
                    MelonLogger.Msg(System.ConsoleColor.Yellow, "Hit " + hit.transform.name);
                    Structure str = hit.transform.GetComponent<Structure>();

                    if (str) MelonCoroutines.Start(Interior.DetectInterior(str));
                }
            }
            */
            // end of temp
        }
    }
}
