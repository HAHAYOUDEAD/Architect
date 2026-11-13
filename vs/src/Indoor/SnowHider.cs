using Il2CppTLD.WeatherParticle;

namespace Architect
{
    // In strong wind a few flakes still get past SnowKillers. While the player is inside with all doors and
    // shutters closed, the weather snow isn't rendered at all.
    // Particles must not be read here: GetParticles on these systems crashes the game (they run in a job).
    internal static class SnowHider
    {
        private static readonly List<ParticleSystemRenderer> hidden = new List<ParticleSystemRenderer>();
        private static bool warned;

        public static void Reset()
        {
            Show();
            warned = false;
        }

        private static List<ParticleSystemRenderer> FindSnowRenderers()
        {
            List<ParticleSystemRenderer> renderers = new List<ParticleSystemRenderer>();
            WeatherParticleManager? manager = GameManager.GetUniStorm()?.m_WeatherParticleManager;
            if (manager == null) return renderers;

            foreach (ParticleSystem? ps in new[] { manager.m_FallingSnowSystem, manager.m_BlowingSnowSystem })
            {
                if (ps == null) continue;
                ParticleSystemRenderer? r = ps.GetComponent<ParticleSystemRenderer>();
                if (r != null && !renderers.Contains(r)) renderers.Add(r);
            }
            return renderers;
        }

        private static void Show()
        {
            try
            {
                foreach (ParticleSystemRenderer r in hidden)
                {
                    if (r) r.forceRenderingOff = false;
                }
            }
            catch (Exception e)
            {
                LogOnce.Error("SnowHider.Show", e);
            }
            hidden.Clear();
        }

        public static void Update(bool hide)
        {
            if (!hide)
            {
                if (hidden.Count > 0) Show();
                return;
            }
            try
            {
                List<ParticleSystemRenderer> renderers = FindSnowRenderers();
                if (renderers.Count == 0)
                {
                    if (!warned) MelonLogger.Warning("Weather snow not found, it can't be hidden inside buildings");
                    warned = true;
                    return;
                }
                foreach (ParticleSystemRenderer r in renderers)
                {
                    if (r.forceRenderingOff) continue;
                    r.forceRenderingOff = true;
                    if (!hidden.Contains(r)) hidden.Add(r);
                }
            }
            catch (Exception e)
            {
                LogOnce.Error("SnowHider.Update", e);
            }
        }
    }
}
