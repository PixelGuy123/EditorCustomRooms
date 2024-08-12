using BepInEx;
using BepInEx.Bootstrap;
using EditorCustomRooms.Patches;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
#if DEBUG
using MTM101BaldAPI.Registers;
using System;
using System.Linq;
#endif
using UnityEngine;
using PlusLevelLoader;

namespace EditorCustomRooms.BasePlugin
{
	[BepInPlugin("pixelguy.pixelmodding.baldiplus.editorcustomrooms", PluginInfo.PLUGIN_NAME, "1.0.4.3")]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("mtm101.rulerp.baldiplus.levelloader", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("mtm101.rulerp.baldiplus.leveleditor", BepInDependency.DependencyFlags.SoftDependency)]
	internal class Plugin : BaseUnityPlugin
	{
		private void Awake()
		{
			Harmony h = new("pixelguy.pixelmodding.baldiplus.editorcustomrooms");

			// For debug purposes
#if DEBUG
			LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
			{
				try
				{
					var r = RoomFactory.CreateAssetsFromPath(System.IO.Path.Combine(AssetLoader.GetModPath(this), "test.cbld"), 200, false, null, false, false, null, false, true);
					Resources.FindObjectsOfTypeAll<LevelObject>().Do(x =>
					{

						var ld = x.roomGroup.FirstOrDefault(x => x.name == "Class");
						if (ld != null)
						{
							ld.potentialRooms = ld.potentialRooms.AddRangeToArray([.. r.ConvertAll(x => new WeightedRoomAsset() { selection = x, weight = 9999999 })]);
						}
					});
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					Debug.LogWarning("Editor custom rooms has thrown a crash log for failing to load the test room");
				}
			}, false);
#endif

			h.Patch(AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(PlusLevelLoaderPlugin), "OnAssetsLoaded")),
				null, new(typeof(Plugin).GetMethod("Postfix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic))); // Manual patch that HAS to work without the editor
			try
			{
				h.PatchAll();
			}
			catch
			{
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
				DestroyImmediate(cube.GetComponentInChildren<Collider>()); // No collision required
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
