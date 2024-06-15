using BepInEx;
using EditorCustomRooms.Patches;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using PlusLevelLoader;
using UnityEngine;

namespace EditorCustomRooms.BasePlugin
{
    [BepInPlugin("pixelguy.pixelmodding.baldiplus.editorcustomrooms", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("mtm101.rulerp.baldiplus.levelloader", BepInDependency.DependencyFlags.HardDependency)]
	//[BepInDependency("mtm101.rulerp.baldiplus.leveleditor", BepInDependency.DependencyFlags.HardDependency)]
	public class Plugin : BaseUnityPlugin
    {
		private void Awake()
		{
			Harmony h = new("pixelguy.pixelmodding.baldiplus.editorcustomrooms");
			h.Patch(AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(PlusLevelLoaderPlugin), "OnAssetsLoaded")), 
				null, new(typeof(Plugin).GetMethod("Postfix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic))); // Manual patch that HAS to work without the editor
			try
			{
				h.PatchAll();
			}
			catch
			{
				Debug.LogWarning("Mod has crashed due to the editor missing");
			}
			finally
			{
				ModPath = AssetLoader.GetModPath(this);
			}
		}

		internal static string ModPath = string.Empty;

		internal static bool hasEditor = false;


		static void Postfix()
		{
			if (++assetLoadCalls != 3) return;

			CreateCube("potentialDoorMarker", Color.blue, 5f, 0);
			CreateCube("itemSpawnMarker", Color.red, 2f, 5f);


			static void CreateCube(string name, Color color, float scale, float offset)
			{

				var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
				var renderer = cube.GetComponent<MeshRenderer>();
				renderer.material.shader = Shader.Find("Unlit/Color");
				renderer.material.color = color;

				cube.transform.localScale = Vector3.one * scale;
				cube.name = name;
				cube.ConvertToPrefab(true);

				PlusLevelLoaderPlugin.Instance.prefabAliases.Add(name, cube);

				if (hasEditor)
					EditorUsage.AddEditorfeatures(cube, name, offset);

			}
		}
		
		static int assetLoadCalls = 0;
	}


}
