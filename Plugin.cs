using BepInEx;
using HarmonyLib;
using System.IO;
using System.Collections.Generic;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using UnityEngine;
using System.Linq;
using MTM101BaldAPI;
using BaldiLevelEditor;
using PlusLevelFormat;
using BepInEx.Bootstrap;
using PlusLevelLoader;

namespace EditorCustomRooms
{
    [BepInPlugin("pixelguy.pixelmodding.baldiplus.editorcustomrooms", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("mtm101.rulerp.baldiplus.leveleditor", BepInDependency.DependencyFlags.SoftDependency)] // soft!!
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("mtm101.rulerp.baldiplus.levelloader", BepInDependency.DependencyFlags.HardDependency)]
	public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
			new Harmony("pixelguy.pixelmodding.baldiplus.editorcustomrooms").PatchAllConditionals();
			ModPath = AssetLoader.GetModPath(this);

			LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
			{
				if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.leveleditor"))
					AddEditorfeatures();

				var lightPre = Resources.FindObjectsOfTypeAll<RoomAsset>().First(x => x.category == RoomCategory.Class).lightPre;
				var asset = CustomRoomExtensions.GetAssetFromPath(Path.Combine(ModPath, "classroomDemo1.cbld"), 65, true, lightPre, 35, 75, false, null);
				asset.MarkAsNeverUnload();
				assetsToAdd.Add(new() { selection = asset, weight = 999 });

			}, false);

			GeneratorManagement.Register(this, GenerationModType.Addend, (floorName, floorNum, lvlObj) =>
			{
				lvlObj.potentialClassRooms = lvlObj.potentialClassRooms.AddRangeToArray([.. assetsToAdd]);
			});
        }

		void AddEditorfeatures()
		{
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
				markersToAdd.Add(new(name));

				BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(name, cube, Vector3.up * offset));
				PlusLevelLoaderPlugin.Instance.prefabAliases.Add(name, cube);
			}
				
			
			
		}

		internal static string ModPath = string.Empty;

		readonly List<WeightedRoomAsset> assetsToAdd = [];

		readonly internal static List<RotateAndPlacePrefab> markersToAdd = [];
    }

	[ConditionalPatchMod("mtm101.rulerp.baldiplus.leveleditor")]
	[HarmonyPatch]
	internal static class PrivateCallsInEditor
	{
		
		[HarmonyPatch(typeof(BaldiLevelEditorPlugin), "SetUpUIPrefabs")] // this is called once I guess
		[HarmonyPrefix]
		static void AddMySprites(BaldiLevelEditorPlugin __instance)
		{
			AddSpriteFolderToAssetMan("UI/", 40f, Plugin.ModPath, "EditorUI");


			void AddSpriteFolderToAssetMan(string prefix = "", float pixelsPerUnit = 40f, params string[] path) // Copypaste lmao
			{
				string[] paths = Directory.GetFiles(Path.Combine(path));
				for (int i = 0; i < paths.Length; i++)
					__instance.assetMan.Add(prefix + Path.GetFileNameWithoutExtension(paths[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(paths[i]), pixelsPerUnit));
				
			}
		}

		[HarmonyPatch(typeof(PlusLevelEditor), "SpawnUI")]
		[HarmonyPrefix]
		static void AddMyCats(PlusLevelEditor __instance) =>
			__instance.toolCats.Find(x => x.name == "utilities").tools.AddRange(Plugin.markersToAdd);
		
	}
}
