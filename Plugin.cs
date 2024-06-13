using BepInEx;
using HarmonyLib;

namespace EditorCustomRooms
{
    [BepInPlugin("pixelguy.pixelmodding.baldiplus.editorcustomrooms", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
			new Harmony("pixelguy.pixelmodding.baldiplus.editorcustomrooms").PatchAll();


        }
    }
}
