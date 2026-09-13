using HarmonyLib;
using MjolnirMenu.Core;

namespace MjolnirMenu.Patches
{
    [HarmonyPatch(typeof(EnvMan))]
    internal static class EnvManPatches
    {
        /// <summary>
        /// The game clears m_debugEnv when the console "env" command is used or a new EnvMan spawns;
        /// re-apply our forced weather so the toggle stays truthful.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(nameof(EnvMan.SetForceEnvironment))]
        private static void SetForceEnvironment_Postfix(string env)
        {
            if (env != State.ForcedWeather)
                State.ForcedWeather = env ?? "";
        }
    }
}
