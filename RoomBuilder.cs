using MTM101BaldAPI;
using PlusLevelFormat;
using PlusLevelLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EditorCustomRooms
{
	/// <summary>
	/// A builder class to take care of the <see cref="RoomAsset"/> creation.
	/// </summary>
	public static class RoomFactory
	{
		/// <summary>
		/// Creates a <see cref="RoomAsset"/> object based on the provided .cbld file.
		/// </summary>
		/// <param name="path">The path to the required .cbld file.</param>
		/// <param name="maxItemValue">The maximum starting value of the room to "afford" an item to appear inside the room.</param>
		/// <param name="isOffLimits">If the room is off limits (for example, an elevator).</param>
		/// <param name="existingContainer">The <see cref="RoomFunctionContainer"/> of a RoomAsset. Generally, every BB+ room points to a single Container of their collection, for example, Faculty has a single container shared to every asset that is supposed to be a faculty. Leaving this null will result in the creation of a unique container for the room.
		/// <para>If the asset shouldn't have a container, it can be manually destroyed after the creation of the RoomAsset.</para>
		/// </param>
		/// <param name="isAHallway">If True, the asset will follow specific parameters to match a hallway format.</param>
		/// <param name="isASecretRoom">If True, every tile in the room will be marked as secret. Like the Mystery Room.</param>
		/// <param name="mapBg">The background image that appears over the room in the Advanced Map. Leaving null will make the asset use the default map material, with no background animation.</param>
		/// <param name="keepTextures">If True, when passed to the generator, it won't change its textures if it's a potential classroom/faculty/office type.</param>
		/// <param name="squaredShape">If True, the room will (internally) turn into a square shape. This is highly recommended for loading Special Rooms.</param>
		/// <returns>A new instance of a <see cref="RoomAsset"/></returns>
		/// <exception cref="ArgumentException"></exception>
		public static RoomAsset CreateAssetFromPath(string path, int maxItemValue, bool isOffLimits, RoomFunctionContainer existingContainer = null, bool isAHallway = false, bool isASecretRoom = false, Texture2D mapBg = null, bool keepTextures = true, bool squaredShape = false)
		{

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

				IntVector2 biggestSize = default;

				foreach (var cell in lvlAsset.tile)
				{
					if (cell.roomId == idx && cell.type != 16)
					{
						if (isAHallway)
							cell.type = 0;

						rAsset.cells.Add(cell);
						if (biggestSize.x < cell.pos.x) // Separated each axis, to actually give a square shape
							biggestSize.x = cell.pos.x;

						if (biggestSize.z < cell.pos.z)
							biggestSize.z = cell.pos.z;
					}
				}
				var posList = rAsset.cells.ConvertAll(x => x.pos);



				rAsset.color = lvlAsset.rooms[idx].color;
				rAsset.doorMats = lvlAsset.rooms[idx].doorMats;

				rAsset.entitySafeCells = new List<IntVector2>(posList);
				rAsset.eventSafeCells = new List<IntVector2>(posList); // Ignore editor's implementation of this, it's horrible and the green marker should work better
				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var obj = rAsset.basicObjects[i];
					if (obj.prefab.name == "nonSafeCellMarker")
					{
						var pos = IntVector2.GetGridPosition(obj.position);
						if (!isAHallway)
						{
							rAsset.entitySafeCells.Remove(pos);
							rAsset.eventSafeCells.Remove(pos);
						}
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

				rAsset.keepTextures = keepTextures;
				rAsset.ceilTex = lvlAsset.rooms[idx].ceilTex;
				rAsset.florTex = lvlAsset.rooms[idx].florTex;
				rAsset.wallTex = lvlAsset.rooms[idx].wallTex;
				rAsset.mapMaterial = lvlAsset.rooms[idx].mapMaterial;
				rAsset.maxItemValue = maxItemValue;
				rAsset.offLimits = isOffLimits;


				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var obj = rAsset.basicObjects[i];
					var pos = IntVector2.GetGridPosition(obj.position);
					if (obj.prefab.name == "potentialDoorMarker")
					{
						rAsset.basicObjects.RemoveAt(i--);
						if (!isAHallway)
						{
							rAsset.potentialDoorPositions.Add(pos);
							rAsset.blockedWallCells.Remove(pos);
						}
					}
					else if (obj.prefab.name == "forcedDoorMarker")
					{
						rAsset.basicObjects.RemoveAt(i--);
						if (!isAHallway)
						{
							rAsset.forcedDoorPositions.Add(pos);
							rAsset.blockedWallCells.Remove(pos);
						}
					}
				}

				rAsset.requiredDoorPositions = new List<IntVector2>(lvlAsset.rooms[idx].requiredDoorPositions); // It seems required has a higher priority than forced, but has no apparent difference
				if (isASecretRoom) // secret room :O
					rAsset.secretCells.AddRange(rAsset.cells.Select(x => x.pos));
				else
					rAsset.secretCells = new List<IntVector2>(lvlAsset.rooms[idx].secretCells);

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
				((UnityEngine.Object)rAsset).name = rAsset.name;

				if (existingContainer)
				{
					rAsset.roomFunctionContainer = existingContainer;
				}
				else if (!isAHallway) // No container for hallway
				{
					var roomFunctionContainer = new GameObject(rAsset.name + "FunctionContainer").AddComponent<RoomFunctionContainer>();
					roomFunctionContainer.functions = []; // For some freaking reason, this is not initialized by default
					roomFunctionContainer.gameObject.ConvertToPrefab(true);

					rAsset.roomFunctionContainer = roomFunctionContainer;
				}

				if (mapBg != null)
				{
					rAsset.mapMaterial = new(rAsset.mapMaterial);
					rAsset.mapMaterial.SetTexture("_MapBackground", mapBg);
					rAsset.mapMaterial.SetShaderKeywords(["_KEYMAPSHOWBACKGROUND_ON"]);
					rAsset.mapMaterial.name = rAsset.name;
				}
				else if (isAHallway)
					rAsset.mapMaterial = null; // hallways have no material

				if (squaredShape && biggestSize.z > 0 && biggestSize.x > 0 && !isAHallway) // Fillup empty spots
				{
					for (int x = 0; x <= biggestSize.x; x++)
					{
						for (int z = 0; z <= biggestSize.z; z++)
						{
							IntVector2 pos = new(x, z);
							if (!rAsset.cells.Any(x => x.pos == pos))
							{
								rAsset.cells.Add(new() { pos = pos });
								rAsset.secretCells.Add(pos);
							}
						}
					}
				}

				UnityEngine.Object.Destroy(lvlAsset); // Remove the created level asset from memory
			}
			return rAsset;
		}

	}
}
