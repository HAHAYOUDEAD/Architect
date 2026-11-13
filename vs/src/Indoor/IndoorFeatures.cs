namespace Architect
{
    // Entry point, called from ArcMain
    internal static class IndoorFeatures
    {
        public static void OnInitialize(HarmonyLib.Harmony harmony) => DynamicTemperatureBridge.TryEnable(harmony);

        public static void OnSceneWasInitialized()
        {
            try
            {
                BuildingTracker.ResetScene();
            }
            catch (Exception e)
            {
                LogOnce.Error("IndoorFeatures.OnSceneWasInitialized", e);
            }
        }

        public static void OnUpdate()
        {
            try
            {
                BuildingTracker.Tick(false);
            }
            catch (Exception e)
            {
                LogOnce.Error("IndoorFeatures.OnUpdate", e);
            }
        }
    }
}
