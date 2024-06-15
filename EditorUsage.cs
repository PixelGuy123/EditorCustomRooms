using BaldiLevelEditor;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using PlusLevelFormat;
using System.IO;
using UnityEngine;
using EditorCustomRooms.BasePlugin;
using System.Collections.Generic;
using System.Linq;

namespace EditorCustomRooms.Patches
{
	[HarmonyPatch]
	internal static class PrivateCallsInEditor
	{
		[HarmonyPatch(typeof(PlusLevelEditor), "SpawnUI")]
		[HarmonyPrefix]
		static void AddMyUI() =>
			PlusLevelEditor.Instance.toolCats.Find(x => x.name == "utilities").tools.AddRange(EditorUsage.cubes.Select(x => new RotateAndPlacePrefab(x)));
		

	}

	internal static class EditorUsage // workaround for C# to not touch this piece of code with LevelEditor
	{
		internal static void AddEditorfeatures(GameObject cube, string name, float offset)
		{
			cubes.Add(name);
			BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(name, cube, Vector3.up * offset));

			if (addSpritefolders) return;
			addSpritefolders = true;
			AddSpriteFolderToAssetMan("UI/", 40f, Plugin.ModPath, "EditorUI");


			static void AddSpriteFolderToAssetMan(string prefix = "", float pixelsPerUnit = 40f, params string[] path) // Copypaste lmao
			{
				string[] paths = Directory.GetFiles(Path.Combine(path));
				for (int i = 0; i < paths.Length; i++)
					BaldiLevelEditorPlugin.Instance.assetMan.Add(prefix + Path.GetFileNameWithoutExtension(paths[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(paths[i]), pixelsPerUnit));

			}

		}

		static bool addSpritefolders = false;

		internal readonly static List<string> cubes = [];
	}
}
