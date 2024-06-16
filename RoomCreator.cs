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
		public static RoomAsset GetAssetFromPath(string path, int spawnWeight, Transform lightPre, int minItemValue, int maxItemValue, bool isOffLimits, RoomFunctionContainer existingContainer, bool isAHallway = false, bool isASecretRoom = false)
		{
			if (!File.Exists(path) || Path.GetExtension(path) != ".cbld")
				throw new System.ArgumentException($"Path ({path}) is invalid! It must be a .cbld file!");
			int idx = isAHallway ? 0 : 1;

			RoomAsset rAsset;
			using (BinaryReader reader = new(File.OpenRead(path)))
			{
				var lvlAsset = CustomLevelLoader.LoadLevelAsset(LevelExtensions.ReadLevel(reader));
				rAsset = ScriptableObject.CreateInstance<RoomAsset>();



				// **************** Load Room Asset Data **************
				// Note, everything must be stored as value, not reference; LevelAssets will be destroyed afterwards
				// There should be only a single room per level asset (only uses rooms[idx])

				rAsset.activity = lvlAsset.rooms[idx].activity.GetNew();
				rAsset.basicObjects = new List<BasicObjectData>(lvlAsset.rooms[idx].basicObjects);
				rAsset.blockedWallCells = new List<IntVector2>(lvlAsset.rooms[idx].blockedWallCells);
				rAsset.category = lvlAsset.rooms[idx].category;

				foreach (var cell in lvlAsset.tile)
				{
					if (cell.type < 16 && cell.roomId == 1)
						rAsset.cells.Add(cell);
				}

				rAsset.color = lvlAsset.rooms[idx].color;
				rAsset.doorMats = lvlAsset.rooms[idx].doorMats;
				rAsset.entitySafeCells = new List<IntVector2>(lvlAsset.rooms[idx].entitySafeCells);
				rAsset.eventSafeCells = new List<IntVector2>(lvlAsset.rooms[idx].eventSafeCells);
				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var obj = rAsset.basicObjects[i];
					if (obj.prefab.name == "nonSafeCellMarker")
					{
						rAsset.entitySafeCells.Remove(IntVector2.GetGridPosition(obj.position));
						rAsset.eventSafeCells.Remove(IntVector2.GetGridPosition(obj.position));
						rAsset.basicObjects.RemoveAt(i--);
					}
				}

				rAsset.forcedDoorPositions = new List<IntVector2>(lvlAsset.rooms[idx].forcedDoorPositions);
				rAsset.hasActivity = lvlAsset.rooms[idx].hasActivity;
				rAsset.itemList = new List<WeightedItemObject>(lvlAsset.rooms[idx].itemList);
				rAsset.items = new List<ItemData>(lvlAsset.rooms[idx].items);
				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var obj = rAsset.basicObjects[i];
					if (obj.prefab.name == "itemSpawnMarker")
					{
						rAsset.basicObjects.RemoveAt(i--);
						rAsset.itemSpawnPoints.Add(new ItemSpawnPoint() { weight = 50, position = new Vector2(obj.position.x, obj.position.z) });
					}
				}

				//rAsset.itemSpawnPoints = new(lvlAsset.rooms[idx].itemSpawnPoints);
				rAsset.keepTextures = false;
				rAsset.lightPre = lightPre;
				rAsset.mapMaterial = lvlAsset.rooms[idx].mapMaterial;
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

				//rAsset.potentialDoorPositions = new(lvlAsset.rooms[idx].potentialDoorPositions); // Check if this works
				rAsset.requiredDoorPositions = new List<IntVector2>(lvlAsset.rooms[idx].requiredDoorPositions);
				if (isASecretRoom) // secret room :O
					rAsset.secretCells.AddRange(rAsset.cells.Select(x => x.pos));
				else
					rAsset.secretCells = new List<IntVector2>(lvlAsset.rooms[idx].secretCells);
				
				rAsset.spawnWeight = spawnWeight;

				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var obj = rAsset.basicObjects[i];
					if (obj.prefab.name == "lightSpotMarker")
					{
						rAsset.basicObjects.RemoveAt(i--);
						rAsset.standardLightCells.Add(IntVector2.GetGridPosition(obj.position));
					}
				}

				rAsset.type = lvlAsset.rooms[idx].type;

				rAsset.name = $"Room_{rAsset.category}_{Path.GetFileNameWithoutExtension(path)}";

				if (existingContainer)
					rAsset.roomFunctionContainer = existingContainer;
				else
				{
					var roomFunctionContainer = new GameObject(rAsset.name + "FunctionContainer").AddComponent<RoomFunctionContainer>();
					roomFunctionContainer.functions = []; // For some freaking reason, this is not initialized by default
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
