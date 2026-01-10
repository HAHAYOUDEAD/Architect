
using Il2CppNodeCanvas.Tasks.Conditions;
using static Architect.StructureData;
using static Il2CppDigitalOpus.MB.Core.MB3_AgglomerativeClustering;

namespace Architect
{

    [RegisterTypeInIl2Cpp]
    public class Structure : MonoBehaviour
    {
        public Structure(IntPtr intPtr) : base(intPtr) { }

        public string localizedName;
        public string localizationKey;
        public bool isBuilt = false;
        public BuildMaterial buildMaterial;
        public BuildPart buildPart;
        public ResourcesPreset resources;
        public DoorVariant doorType;
        //public bool isDoor = false;
        //public bool isWindow = false;
        public bool hasSnow = false;
        public Color insidePaintColor = Color.black;
        public Color outsidePaintColor = Color.black;
        public int isPaintable;
        public bool isAltMaterial = false; // for reclaimed wood or fir logs
        public bool doorState; // true = opened, false = closed

        //public Snap.SnapPoint currentSnapPoint;

        public Renderer renderer;
        public BreakDown breakdown;

        public bool isBeingPlaced;
        public bool initialized;

        public float constructionDate = 0f;
        //public bool isBeingConstructed;

        public object ghostCoroutine;

        public Quaternion lastPlacedRotation = Quaternion.identity;

        public List<Structure> adjacentFloors = new List<Structure>();
        public int adjacentFlatFloors;
        public List<Structure> adjacentRoofs = new List<Structure>();
        public List<Structure> adjacentWalls = new List<Structure>();
        public List<Structure> elevatedFloors = new List<Structure>();

        public bool isEnclosedFloorTile;
        public bool isSeeingTheSky;

        public bool isStatic;

        public int coroutineRunning = 0;

        public IndoorSpaceTrigger interiorTrigger;
        public string interiorClusterID;

        public void Start()
        {
            if (GetComponentsInChildren<ParticleKiller>().Length == 0)
            {
                GameObject pk = new GameObject() { name = "ParticleKiller", layer = vp_Layer.ParticleKiller };
                BoxCollider bc = pk.AddComponent<BoxCollider>();
                bc.center = this.GetComponent<MeshFilter>().sharedMesh.bounds.center;
                bc.size = this.GetComponent<MeshFilter>().sharedMesh.bounds.size * 1.2f; // increase size by 20% to prevent snow sneaking in from the sides
                if (this.buildPart == Data.BuildPart.Wall) bc.size += Vector3.forward * 0.5f; // increase depth for walls
                pk.AddComponent<ParticleKiller>();
                pk.transform.SetParent(transform);
                pk.transform.ZeroLocal();
            }

            renderer = GetComponent<MeshRenderer>();
            localizedName = Localization.Get(localizationKey);

            SetupBreakDown();
            Finalize(true);

            StructureManager.Add(this);
        }

        public void SphereCastAdjacentStructures()
        {
            if (buildPart != BuildPart.Floor) return;

            adjacentFloors.Clear();
            adjacentRoofs.Clear();
            adjacentWalls.Clear();
            adjacentFlatFloors = 0;
            isEnclosedFloorTile = false;

            int layerMask = 0;
            layerMask |= (1 << vp_Layer.InteractiveProp);
            layerMask |= (1 << vp_Layer.Buildings);
            RaycastHit[] hits = Physics.SphereCastAll(renderer.bounds.center, Interior.floorDetectionRadius, Vector3.up, 1000f, layerMask, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                Structure str = hit.transform.GetComponent<Structure>();
                if (str && str != this && !adjacentFloors.Contains(str))
                {
                    bool isRoof = (str.buildPart == BuildPart.Roof || (str.buildPart == BuildPart.Stairs && str.name.ToLower().Contains("ramp"))) && (str.transform.position.y - this.transform.position.y) > 0 || 
                        (str.buildPart == BuildPart.Floor && (str.transform.position.y - this.transform.position.y) > Interior.storeyHeight);
                    bool isFloor = str.buildPart == BuildPart.Floor && !str.name.ToLower().Contains("foundation");// || str.buildPart == BuildPart.Stairs;
                    bool isFlatFloor = isFloor && (str.transform.position.y - this.transform.position.y) < Interior.storeyHeight;
                    bool isWall = str.buildPart == BuildPart.Wall;

                    if (isRoof)
                    {
                        RaycastHit hit2 = Physics.RaycastAll(str.renderer.bounds.center + Vector3.up * 0.1f, Vector3.up, Interior.storeyHeight * 5f, layerMask, QueryTriggerInteraction.Ignore)
                            .FirstOrDefault(
                                h => h.transform.GetComponent<Structure>() &&
                                h.transform.GetComponent<Structure>() != str &&
                                (h.transform.GetComponent<Structure>().buildPart == BuildPart.Roof || h.transform.GetComponent<Structure>().buildPart == BuildPart.Floor) &&
                                Mathf.Abs(h.transform.position.y - str.transform.position.y) > Interior.storeyHeight / 6f
                                );
                        if (hit2.transform) // if this structure is roof and found another roof above - move from roofs to elevated floors
                        {
                            elevatedFloors.Add(str);
                            if (isFlatFloor) adjacentFlatFloors++;
                        }
                        else if (!str.name.ToLower().Contains("edgeroof"))
                        {
                            adjacentRoofs.Add(str);
                        }
                        continue;
                    }

                    if (isFloor)
                    {
                        adjacentFloors.Add(str);
                    }

                    if (isWall) adjacentWalls.Add(str);

                    if (isFlatFloor) adjacentFlatFloors++;
                }
            }

            if (adjacentFlatFloors > 6) isEnclosedFloorTile = true;
        }

        public void Update()
        {

            if (isStatic == isInBuildingMode)
            {
                isStatic = !isInBuildingMode;
                if (this.buildPart == BuildPart.Door)
                {
                    Undoor(!isStatic);
                }
                AutoSwitchMode();
            }


            if (!hasSnow)
            {
                // check if should add snow
                // if outdoors
                // if had something above - raycast?
                // if player is near and looking
                // 
            }
            else
            {
                // check if should remove snow (something built above)
            }



        }

        public void ProcessInteraction()
        {
            // unused, handled by vanilla BreakDown component interaction
        }

        public void ProcessAltInteraction() // interrupted by BetterBases
        {
            if (!isBuilt)
            {
                if (InputManager.GetSprintDown(InputManager.m_CurrentContext)) // precise place
                {
                    //enter placing with detached camera
                    HUDMessage.AddMessage(Localization.Get("ARC_NudgeKeysReminder"), 5f, false, false);
                    
                    nudgePlacementMode = true;
                }

                GameManager.GetPlayerManagerComponent().StartPlaceMesh(gameObject, PlaceMeshFlags.None, placeRules);
            }
            else
            {
                HUDMessage.AddMessage("Painting :)", false, false);
            }
        }

        public void ProcessCloneInteraction()
        {
            string nameTruncated = this.name[0..^2];
            StructurePreset sp = Data.allStructures[nameTruncated];
            sp.prefabName = nameTruncated;

            MelonCoroutines.Start(RadialActions.StartPlacing(sp));
        }


        public void ProcessDeleteInteraction()
        {
            StructureManager.Remove(this);
            if (!String.IsNullOrEmpty(this.interiorClusterID))
            {
                if (Interior.currentRegionClusterSets.TryGetValue(this.interiorClusterID, out var bop))
                {
                    bop.Remove(this);
                }
            }
            Destroy(this.gameObject);
        }

        private void ToggleHelperArrow(bool enable)
        {
            this.transform.FindInactive("HelperArrow")?.gameObject.SetActive(enable);
        }


        public void Finalize(bool updateLayer = false)
        {
            // setup NavMeshObstacle
            NavMeshObstacle nvmo = gameObject.GetComponent<NavMeshObstacle>();

            if (isBuilt)
            {
                if (!nvmo) nvmo = gameObject.AddComponent<NavMeshObstacle>();
                nvmo.Initialize();
            }
            else
            {
                if (nvmo) Destroy(nvmo);
            }

            // make door
            if (buildPart == BuildPart.Door && isBuilt)
            {
                breakdown.enabled = false;

                if (this.GetComponent<OpenClose>() == null)
                {
                    // setting up iTween animator
                    int angle;

                    switch (this.doorType)
                    {
                        default:
                            angle = doorRotationDegree;
                            break;
                        case DoorVariant.Window:
                            angle = windowRotationDegree;
                            break;
                        case DoorVariant.RoofWindow:
                            angle = roofWindowRotationDegree;
                            break;
                    }
                    iTweenEvent itOpen = gameObject.AddComponent<iTweenEvent>().Initialize(this.doorType, "open", angle / 360f, doorOpenTime);
                    iTweenEvent itClose = gameObject.AddComponent<iTweenEvent>().Initialize(this.doorType, "close", -angle / 360f, doorCloseTime);

                    // bumping iTween to process the values
                    itOpen.DeserializeValues();
                    itClose.DeserializeValues();

                    // setting up open/close animation components
                    ObjectAnim oa = gameObject.AddComponent<ObjectAnim>().Initialize(gameObject);
                    OpenClose oc = gameObject.AddComponent<OpenClose>().Initialize(this.doorType, oa, this);

                    // closing/opening audio
                    WwiseEventReference eventOpen = ScriptableObject.CreateInstance<WwiseEventReference>();
                    WwiseEventReference eventClose = ScriptableObject.CreateInstance<WwiseEventReference>();
                    eventOpen.id = AkSoundEngine.GetIDFromString("Play_SndMechDoorWoodOpen1"); 
                    eventClose.id = AkSoundEngine.GetIDFromString("Play_SndMechDoorWoodClose1"); 
                    oc.m_OpenAudioEvent.ObjectReference = eventOpen;
                    oc.m_CloseAudioEvent.ObjectReference = eventClose;
                }

                if (doorState) this.GetComponent<OpenClose>().m_IsOpen = true;

                if (!isStatic) Undoor(true);
            }


            // set material tag
            if (!isBuilt)
            {
                tag = "Ice";
            }
            else
            {
                if (buildMaterial == BuildMaterial.WoodPlank || buildMaterial == BuildMaterial.WoodLog)
                {
                    tag = "Wood";
                }
                else if (buildMaterial == BuildMaterial.Stone)
                {
                    tag = "Stone";
                }
                else if (buildMaterial == BuildMaterial.MetalSheet)
                {
                    tag = "Metal";
                }
            }

            // manage materials
            if (!isBuilt)
            {
                Materials.AssignMaterial(this, BuildPartSide.Both, true);
                renderer.castShadows = false;
            }
            else
            {
                Materials.AssignMaterial(this, BuildPartSide.Both);
                if (insidePaintColor != Color.black) Paint(BuildPartSide.Inside, insidePaintColor);
                if (outsidePaintColor != Color.black) Paint(BuildPartSide.Outside, outsidePaintColor);

                foreach (Renderer r in this.GetComponentsInChildren<Renderer>()) // assign vanilla shader to any nested objects except helpers
                {
                    if (r != renderer && !r.name.ToLower().Contains("helper")) r.material.shader = Materials.vanillaDefaultShader;
                }

                renderer.castShadows = true;
            }

            // create interior
            if (this.buildPart == Data.BuildPart.Floor && !String.IsNullOrEmpty(this.interiorClusterID) && this.interiorTrigger == null)
            {
                if (!Interior.currentRegionClusterSets.TryGetValue(this.interiorClusterID, out _))
                {
                    Interior.currentRegionClusterSets[this.interiorClusterID] = new ();
                }
                Interior.CreateNewInteriorTrigger(this, this.interiorClusterID);
            }

            this.ToggleSnapTriggers(true);

            this.HideNestedMeshes(!isBuilt);

            if (updateLayer) AutoSwitchMode();
            this.initialized = true;
        }

        public void ToggleSnapTriggers(bool enable)
        {
            Transform st = this.transform.Find("SnapTriggers");
            if (!st) return;

            foreach (Collider c in st.GetComponentsInChildren<Collider>(true))
            {
                if (enable)
                {
                    c.gameObject.layer = Snap.snapTriggerLayer;
                    c.gameObject.active = true;
                }
                else
                {
                    c.gameObject.layer = vp_Layer.IgnoreRaycast;
                    c.gameObject.active = false;
                }
                
            }
        }

        public void HideNestedMeshes(bool hide)
        {
            foreach (Renderer r in this.GetComponentsInChildren<Renderer>())
            {
                if (r != renderer) r.enabled = !hide;
            }
        }

        public void SetupBreakDown()
        {
            BreakDown bd = gameObject.GetComponent<BreakDown>();
            if (!bd) bd = gameObject.AddComponent<BreakDown>();
            if (isBuilt) bd.InitializeForBreakdown();
            else bd.InitializeForBuilding();
            breakdown = bd;
        }

        [HideFromIl2Cpp]
        public IEnumerator LerpCameraTowardsPivot(Transform t)
        {
            GameObject dummy = new GameObject();
            dummy.transform.position = Utils.GetPlayerEyePosition();
            
            dummy.transform.LookAt(t);
            Vector2 target = new Vector2(dummy.transform.rotation.eulerAngles.y, dummy.transform.rotation.eulerAngles.x);
            Vector2 start = GameManager.GetVpFPSCamera().Angle;
            //float currentYaw = GameManager.GetVpFPSCamera().m_CurrentYaw;
            //float currentPitch = GameManager.GetVpFPSCamera().m_CurrentPitch;

            //GameManager.GetPlayerManagerComponent().DisableCharacterController();
            MelonLogger.Msg(ConsoleColor.Green, start);
            float i = 0f;

            GameManager.GetVpFPSCamera().Angle = target;
            /*
            while (i < 1f)
            {
                i += Time.deltaTime * 5f;
                
 
                GameManager.GetVpFPSCamera().Angle = Vector2.Lerp(start, target, i);
                MelonLogger.Msg(i + " -- " + GameManager.GetVpFPSCamera().m_CurrentYaw + "  " + GameManager.GetVpFPSCamera().m_CurrentPitch);
                //GameManager.GetVpFPSCamera().SetNearPlaneOverride(0.001f);
                //GameManager.GetPlayerManagerComponent().m_NearPlaneOverridden = true;

                yield return new WaitForEndOfFrame();
            }
            */

            //GameManager.GetPlayerManagerComponent().EnableCharacterController();
            GameManager.GetPlayerManagerComponent().StartPlaceMesh(t.gameObject, PlaceMeshFlags.None, placeRules);
            yield break;

        }
        [HideFromIl2Cpp]
        public IEnumerator PlayBuildingSoundsUntilDone()
        {
            GameAudioManager.Play3DSound(AkSoundEngine.GetIDFromString("Play_CraftingWood"), this.gameObject);

            while (!this.isBuilt)
            {
                yield return new WaitForEndOfFrame();
            }

            GameAudioManager.StopAllSoundsFromGameObject(this.gameObject);

            yield break;
        }
        [HideFromIl2Cpp]
        public IEnumerator LerpGhostTransparency(float value)
        {
            float i = 0f;
            float? pv = this.renderer.materials[0]?.GetFloat("_Power");
            float nv = 0f;

            if (pv == null) yield break;

            while (!this.renderer.materials[0].GetFloat("_Power").Equals(value))
            {
                nv = Mathf.Lerp((float)pv, value, i += Time.deltaTime * 2f);

                for (int ii = 0; ii < this.renderer.materials.Length; ii++)
                {
                    this.renderer.materials[ii].SetFloat("_Power", nv);
                }
                
                //if (renderer.materials.Length > 1) this.renderer.materials[1].SetFloat("_Power", nv);

                yield return new WaitForEndOfFrame();
            }
        }

        public void Paint(BuildPartSide side, Color color)
        {
            Materials.AssignMaterial(this, side, !this.isBuilt, true); // assign paint material
            if (side == BuildPartSide.Outside)
            {
                if (renderer.materials.Length > 0) renderer.materials[0]?.SetColor("_Color", color);
            }
            if (side == BuildPartSide.Inside)
            {
                if (renderer.materials.Length > 1) renderer.materials[1]?.SetColor("_Color", color);
            }
        }

        public void Undoor(bool doInFactUndoor)
        {
            if (this.buildPart != BuildPart.Door || !this.isBuilt) return;
            this.breakdown.enabled = doInFactUndoor;
            this.GetComponent<OpenClose>().enabled = !doInFactUndoor;
        }

        public void AutoSwitchMode()
        {
            if (isBuilt)
            {
                if (isStatic)
                {
                    if (this.buildPart != BuildPart.Door)
                    {
                        gameObject.layer = vp_Layer.Buildings;
                    }   
                }
                else
                {
                    gameObject.layer = vp_Layer.InteractiveProp;
                }
            }
            else
            {
                if (isStatic)
                {
                    this.renderer.forceRenderingOff = true;
                    gameObject.layer = vp_Layer.IgnoreRaycast;
                }
                else
                {
                    this.renderer.forceRenderingOff = false;
                    gameObject.layer = vp_Layer.InteractivePropNoCollidePlayer;
                }
                
            }

            ToggleHelperArrow(buildPart == BuildPart.Door && (!isBuilt && !isStatic));
            ToggleInteriorTile(isBuilt);
        }

        public void ToggleInteriorTile(bool enable) 
        {
            if (this.interiorTrigger == null) return;
            this.interiorTrigger.m_DontCountAsInterior = !enable;
            this.interiorTrigger.gameObject.SetActive(enable);
        }

        [HideFromIl2Cpp]
        public StructureSaveProxy ToProxy()
        {
            StructureSaveProxy data = new StructureSaveProxy()
            {
                prefabName = this.name,
                //isDoor = this.isDoor,
                //localizationKey = this.localizationKey,
                position = this.transform.position,
                rotation = this.transform.rotation,
                //scale = this.transform.localScale,
                isBuilt = this.isBuilt,
                //material = this.buildMaterial,
                //part = this.buildPart,
                hasSnow = this.hasSnow,
                isAltMaterial = this.isAltMaterial,
                insidePaintColor = this.insidePaintColor,
                outsidePaintColor = this.outsidePaintColor,
                doorState = this.GetComponent<OpenClose>() ? this.GetComponent<OpenClose>().IsOpen() : false,
                interiorCluster = this.interiorClusterID

            };

            //if (this.insidePaintColor != Color.black) data.insidePaintColor = this.insidePaintColor;
            //if (this.outsidePaintColor != Color.black) data.outsidePaintColor = this.outsidePaintColor;
            return data;
        }

        [HideFromIl2Cpp]
        public void FromProxy(StructureSaveProxy proxy)
        {
            allStructures.TryGetValue(proxy.prefabName[0..^2], out StructurePreset? sp); // range operator to trim last 2 characters

            if (sp == null)
            {
                Log(ConsoleColor.Red, $"Structure {proxy.prefabName[0..^2]} could not be loaded: missing in the dictionary");
                return;
            }

            this.localizationKey = sp.loKey;
            this.buildMaterial = sp.bMat;
            this.buildPart = sp.bPart;
            this.resources = sp.res;
            this.doorType = sp.door;
            //isDoor = sp.isDoor;

            //this.localizedName = Localization.Get(this.localizationKey);
            this.transform.position = proxy.position;
            this.transform.rotation = proxy.rotation;
            //this.transform.localScale = proxy.scale;
            this.isBuilt = proxy.isBuilt;
            this.hasSnow = proxy.hasSnow;
            this.isAltMaterial = proxy.isAltMaterial;
            this.insidePaintColor = proxy.insidePaintColor;
            this.outsidePaintColor = proxy.outsidePaintColor;
            this.doorState = proxy.doorState;
            this.interiorClusterID = proxy.interiorCluster;
        }
    }
}

