using HarmonyLib;
using PluginAPI.Core.Attributes;
using System;

namespace BrightPlugin
{
    public class Plugin
    {
        public static Plugin Singleton { get; private set; }
        public static Harmony Harmony { get; private set; }
        
        [PluginEntryPoint("Mirror Patch", "1.0.0", "Repairs a potential exploit in Mirror that breaks the server", "Mitzey")]
        void LoadPlugin()
        {
            Singleton = this;
            Harmony = new Harmony($"mirrorPatch-{DateTime.Now.Ticks}");
            Harmony.PatchAll();
        }
        
        [PluginUnload]
        void UnloadPlugin()
        {
            Harmony.UnpatchAll(Harmony.Id);
            Harmony = null;
        }
    }
}
