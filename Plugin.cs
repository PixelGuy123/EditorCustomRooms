using BepInEx;
using BepInEx.Bootstrap;
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


		static void Postfix()
		{
			if (++assetLoadCalls != 3) return;

			PlusLevelLoaderPlugin.Instance.textureAliases.Add("SaloonWall", PlusLevelLoaderPlugin.Instance.assetMan.Get<Texture2D>("SaloonWall")); // why not include this ;-;

			CreateCube("potentialDoorMarker", Color.blue, 5f, 1f);
			CreateCube("forcedDoorMarker", new(0f, 0.5f, 1f), 5f, 1f);
			CreateCube("itemSpawnMarker", Color.red, 2f, 5f);
			CreateCube("nonSafeCellMarker", Color.green, 3f, 1f);
			CreateCube("lightSpotMarker", Color.yellow, 3f, 10f);


			static void CreateCube(string name, Color color, float scale, float offset)
			{

				var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
				Destroy(cube.GetComponent<Collider>()); // No collision required
				var renderer = cube.GetComponent<MeshRenderer>();
				renderer.material.shader = Shader.Find("Unlit/Color");
				renderer.material.color = color;

				cube.transform.localScale = Vector3.one * scale;
				cube.name = name;
				cube.ConvertToPrefab(true);

				PlusLevelLoaderPlugin.Instance.prefabAliases.Add(name, cube);

				if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.leveleditor"))
					EditorUsage.AddEditorfeatures(cube, name, offset);

			}
		}

		static int assetLoadCalls = 0;
	}


}
