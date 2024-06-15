using MTM101BaldAPI;
using PlusLevelFormat;
using PlusLevelLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EditorCustomRooms
{
	public static class CustomRoomExtensions
	{
		public static RoomAsset GetAssetFromPath(string path, int spawnWeight, bool allCellsAreLightCells, Transform lightPre, int minItemValue, int maxItemValue, bool isOffLimits, RoomFunctionContainer existingContainer)
		{
			if (!File.Exists(path) || Path.GetExtension(path) != ".cbld")
				throw new System.ArgumentException($"Path ({path}) is invalid! It must be a .cbld file!");

			RoomAsset rAsset;
			using (BinaryReader reader = new(File.OpenRead(path)))
			{
				var lvlAsset = CustomLevelLoader.LoadLevelAsset(LevelExtensions.ReadLevel(reader));
				rAsset = ScriptableObject.CreateInstance<RoomAsset>();



				// **************** Load Room Asset Data **************
				// Note, everything must be stored as value, not reference; LevelAssets will be destroyed afterwards
				// There should be only a single room per level asset (only uses rooms[1])

				rAsset.activity = lvlAsset.rooms[1].activity.GetNew();
				rAsset.basicObjects = new List<BasicObjectData>(lvlAsset.rooms[1].basicObjects);
				rAsset.blockedWallCells = new List<IntVector2>(lvlAsset.rooms[1].blockedWallCells);
				rAsset.category = lvlAsset.rooms[1].category;

				foreach (var cell in lvlAsset.tile)
				{
					if (cell.type < 16 && cell.roomId == 1)
						rAsset.cells.Add(cell);
				}

				rAsset.color = lvlAsset.rooms[1].color;
				rAsset.doorMats = lvlAsset.rooms[1].doorMats;
				rAsset.entitySafeCells = new List<IntVector2>(lvlAsset.rooms[1].entitySafeCells);
				rAsset.eventSafeCells = new List<IntVector2>(lvlAsset.rooms[1].eventSafeCells);
				rAsset.forcedDoorPositions = new List<IntVector2>(lvlAsset.rooms[1].forcedDoorPositions);
				rAsset.hasActivity = lvlAsset.rooms[1].hasActivity;
				rAsset.itemList = new List<WeightedItemObject>(lvlAsset.rooms[1].itemList);
				rAsset.items = new List<ItemData>(lvlAsset.rooms[1].items);
				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var obj = rAsset.basicObjects[i];
					if (obj.prefab.name == "itemSpawnMarker")
					{
						rAsset.basicObjects.RemoveAt(i--);
						rAsset.itemSpawnPoints.Add(new ItemSpawnPoint() { weight = 50, position = new Vector2(obj.position.x, obj.position.z) });
					}
				}

				//rAsset.itemSpawnPoints = new(lvlAsset.rooms[1].itemSpawnPoints);
				rAsset.keepTextures = false;
				rAsset.lightPre = lightPre;
				rAsset.mapMaterial = lvlAsset.rooms[1].mapMaterial;
				rAsset.maxItemValue = maxItemValue;
				rAsset.minItemValue = minItemValue;
				rAsset.offLimits = isOffLimits;

				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var obj = rAsset.basicObjects[i];
					if (obj.prefab.name == "potentialDoorMarker")
					{
						rAsset.basicObjects.RemoveAt(i--);
						rAsset.potentialDoorPositions.Add(IntVector2.GetGridPosition(obj.position));
					}
				}

				//rAsset.potentialDoorPositions = new(lvlAsset.rooms[1].potentialDoorPositions); // Check if this works
				rAsset.requiredDoorPositions = new List<IntVector2>(lvlAsset.rooms[1].requiredDoorPositions);
				rAsset.secretCells = new List<IntVector2>(lvlAsset.rooms[1].secretCells); // don't know how should this be set up
				rAsset.spawnWeight = spawnWeight;

				if (allCellsAreLightCells)
					rAsset.standardLightCells.AddRange(rAsset.cells.Select(x => x.pos));

				rAsset.type = lvlAsset.rooms[1].type;

				rAsset.name = $"Room_{rAsset.category}_{Path.GetFileNameWithoutExtension(path)}";

				if (existingContainer)
					rAsset.roomFunctionContainer = existingContainer;
				else
				{
					var roomFunctionContainer = new GameObject(rAsset.name + "FunctionContainer").AddComponent<RoomFunctionContainer>();
					roomFunctionContainer.gameObject.ConvertToPrefab(true);

					rAsset.roomFunctionContainer = roomFunctionContainer;
				}


				Object.Destroy(lvlAsset); // Remove the created level asset from memory
			}
			return rAsset;
		}

		public static RoomAsset SetPotentialPosters(this RoomAsset asset, float posterChance, params WeightedPosterObject[] posters)
		{
			asset.posterChance = posterChance;
			asset.posters = new List<WeightedPosterObject>(posters);
			return asset;
		}

		public static RoomAsset SetPotentialWindows(this RoomAsset asset, float windowChance, WindowObject window)
		{
			asset.windowChance = windowChance;
			asset.windowObject = window;
			return asset;
		}

		public static RoomAsset SetMats(this RoomAsset asset, Material ceil, Material wall, Material floor)
		{
			if (ceil)
			{
				asset.ceilMat = new Material(ceil);
				asset.ceilTex = Object.Instantiate((Texture2D)ceil.mainTexture);
			}
			if (wall)
			{
				asset.wallMat = new Material(wall);
				asset.wallTex = Object.Instantiate((Texture2D)wall.mainTexture);
			}
			if (floor)
			{
				asset.florMat = new Material(floor);
				asset.florTex = Object.Instantiate((Texture2D)floor.mainTexture);
			}
			asset.keepTextures = asset.florMat && asset.wallMat && asset.ceilMat;
			return asset;
		}

	}
}
