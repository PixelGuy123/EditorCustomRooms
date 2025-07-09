using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MTM101BaldAPI;
using PlusLevelFormat;
using PlusLevelLoader;
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
		[Obsolete("Use RoomFactory.CreateAssetsFromPath() instead. This will be removed in 2.0.0.")]
		public static RoomAsset CreateAssetFromPath(string path, int maxItemValue, bool isOffLimits, RoomFunctionContainer existingContainer = null, bool isAHallway = false, bool isASecretRoom = false, Texture2D mapBg = null, bool keepTextures = true, bool squaredShape = false)
		{
			var a = CreateAssetsFromPath(path, maxItemValue, isOffLimits, existingContainer, isAHallway, isASecretRoom, mapBg, keepTextures, squaredShape);
			return a.Count == 0 ? null : a[0];
		}
		/// <summary>
		/// Creates a collection of <see cref="RoomAsset"/> objects based on the .cbld file provided.
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
		public static List<RoomAsset> CreateAssetsFromPath(string path, int maxItemValue, bool isOffLimits, RoomFunctionContainer existingContainer = null, bool isAHallway = false, bool isASecretRoom = false, Texture2D mapBg = null, bool keepTextures = true, bool squaredShape = false)
		{
			if (Path.GetExtension(path) != ".cbld")
				throw new ArgumentException("Path has invalid extension: " + path);

			List<RoomAsset> assets = [];
			using (BinaryReader reader = new(File.OpenRead(path)))
			{
				var lvlAsset = CustomLevelLoader.LoadLevelAsset(LevelExtensions.ReadLevel(reader));
				string name = Path.GetFileNameWithoutExtension(path);

				try
				{
					assets = GetAssetsFromLevelAsset(lvlAsset, name, isAHallway ? 0 : 1, isAHallway ? 1 : lvlAsset.rooms.Count, maxItemValue, isOffLimits, existingContainer, isASecretRoom, mapBg, keepTextures, squaredShape);
				}
				catch (Exception e)
				{
					Debug.LogWarning("Failed to load a room coming from the cbld: " + path);
					Debug.LogException(e);
				}
				finally
				{
					UnityEngine.Object.Destroy(lvlAsset); // Remove the created level asset from memory
				}

			}
			return assets;
		}


		internal static List<RoomAsset> GetAssetsFromLevelAsset(LevelAsset lvlAsset, string roomname, int minRoomRange, int maxRoomRange, int maxItemValue, bool isOffLimits, RoomFunctionContainer existingContainer = null, bool isASecretRoom = false, Texture2D mapBg = null, bool keepTextures = true, bool squaredShape = false)
		{
			List<RoomAsset> assets = [];
			for (int idx = minRoomRange; idx < maxRoomRange; idx++)
			{
				var rAsset = ScriptableObject.CreateInstance<RoomAsset>();

				// **************** Load Room Asset Data **************
				// Note, everything must be stored as value, not reference; LevelAssets will be destroyed afterwards
				// There should be only a single room per level asset (only uses rooms[idx])

				rAsset.activity = lvlAsset.rooms[idx].activity.GetNew();
				rAsset.basicObjects = [.. lvlAsset.rooms[idx].basicObjects];
				rAsset.blockedWallCells = [.. lvlAsset.rooms[idx].blockedWallCells];
				rAsset.category = lvlAsset.rooms[idx].category;
				rAsset.type = lvlAsset.rooms[idx].type;
				bool isAHallway = rAsset.type == RoomType.Hall;

				IntVector2 biggestSize = default;
				IntVector2 posOffset = new(lvlAsset.levelSize.x, lvlAsset.levelSize.z);

				foreach (var cell in lvlAsset.tile)
				{
					if (cell.roomId == idx && cell.type != 16)
					{
						if (posOffset.x > cell.pos.x)
							posOffset.x = cell.pos.x;

						if (posOffset.z > cell.pos.z)
							posOffset.z = cell.pos.z;

						if (isAHallway)
							cell.type = 0;
					}
				}

				Vector3 worldPosOffset = new(posOffset.x * 10f, 0f, posOffset.z * 10f);

				foreach (var cell in lvlAsset.tile)
				{
					if (cell.roomId == idx && cell.type != 16)
					{
						cell.pos -= posOffset; // Offset that should make a room be in 0,0 regardless
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

				rAsset.entitySafeCells = [.. posList];
				rAsset.eventSafeCells = [.. posList]; // Ignore editor's implementation of this, it's horrible and the green marker should work better


				rAsset.forcedDoorPositions = [.. lvlAsset.rooms[idx].forcedDoorPositions];
				rAsset.hasActivity = lvlAsset.rooms[idx].hasActivity;
				rAsset.itemList = [.. lvlAsset.rooms[idx].itemList];
				rAsset.items = [.. lvlAsset.rooms[idx].items];

				rAsset.keepTextures = keepTextures;
				rAsset.ceilTex = lvlAsset.rooms[idx].ceilTex;
				rAsset.florTex = lvlAsset.rooms[idx].florTex;
				rAsset.wallTex = lvlAsset.rooms[idx].wallTex;
				rAsset.mapMaterial = lvlAsset.rooms[idx].mapMaterial;
				rAsset.maxItemValue = maxItemValue;
				rAsset.offLimits = isOffLimits;

				rAsset.basicObjects.ForEach(x => x.position -= worldPosOffset);
				for (int i = 0; i < rAsset.blockedWallCells.Count; i++)
					rAsset.blockedWallCells[i] -= posOffset;
				//rAsset.blockedWallCells.ForEach(x => x -= posOffset); // ForEach doesn't change the value passed directly? Only fields? Huh
				rAsset.items.ForEach(x => x.position -= new Vector2(worldPosOffset.x, worldPosOffset.z));
				rAsset.activity.position -= worldPosOffset;

				// other markers here
				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var pos = IntVector2.GetGridPosition(rAsset.basicObjects[i].position);
					Vector3 actPos = rAsset.basicObjects[i].position;
					switch (rAsset.basicObjects[i].prefab.name)
					{
						case "lightSpotMarker":
							rAsset.basicObjects.RemoveAt(i--);
							rAsset.standardLightCells.Add(pos);
							break;
						case "itemSpawnMarker":
							rAsset.basicObjects.RemoveAt(i--);
							rAsset.itemSpawnPoints.Add(new ItemSpawnPoint() { weight = 50, position = new Vector2(actPos.x, actPos.z) });
							break;
						case "nonSafeCellMarker":
							if (!isAHallway)
							{
								rAsset.entitySafeCells.Remove(pos);
								rAsset.eventSafeCells.Remove(pos);
							}
							rAsset.basicObjects.RemoveAt(i--);
							break;
						default: break;
					}
				}
				// Potential doors
				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var pos = IntVector2.GetGridPosition(rAsset.basicObjects[i].position);
					if (rAsset.basicObjects[i].prefab.name == "potentialDoorMarker")
					{
						rAsset.basicObjects.RemoveAt(i--);
						if (!isAHallway)
						{
							rAsset.potentialDoorPositions.Add(pos);
							if (!rAsset.basicObjects.Any(x => IntVector2.GetGridPosition(x.position) == pos))
								rAsset.blockedWallCells.Remove(pos);
						}
					}
				}
				// Forced doors
				for (int i = 0; i < rAsset.basicObjects.Count; i++)
				{
					var pos = IntVector2.GetGridPosition(rAsset.basicObjects[i].position);
					if (rAsset.basicObjects[i].prefab.name == "forcedDoorMarker")
					{
						rAsset.basicObjects.RemoveAt(i--);
						if (!isAHallway)
						{
							rAsset.forcedDoorPositions.Add(pos);
							if (!rAsset.basicObjects.Any(x => IntVector2.GetGridPosition(x.position) == pos))
								rAsset.blockedWallCells.Remove(pos);
						}
					}
				}
				rAsset.requiredDoorPositions = [.. lvlAsset.rooms[idx].requiredDoorPositions]; // It seems required has a higher priority than forced, but has no apparent difference
				if (isASecretRoom) // secret room :O
					rAsset.secretCells.AddRange(rAsset.cells.Select(x => x.pos));
				else
				{
					rAsset.secretCells = [.. lvlAsset.rooms[idx].secretCells];
					for (int i = 0; i < rAsset.secretCells.Count; i++)
						rAsset.secretCells[i] -= posOffset;
				}




				rAsset.name = TreatRepeatedName($"Room_{rAsset.category}_{roomname}{(maxRoomRange >= 2 ? string.Empty : idx)}");
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
					existingContainer = roomFunctionContainer;
				}

				if (mapBg != null)
				{
					rAsset.mapMaterial = new(rAsset.mapMaterial);
					rAsset.mapMaterial.SetTexture("_MapBackground", mapBg);
					rAsset.mapMaterial.SetShaderKeywords(["_KEYMAPSHOWBACKGROUND_ON"]);
					rAsset.mapMaterial.name = rAsset.name;
				}
				else if (isAHallway)
				{
					rAsset.mapMaterial = null; // hallways have no material
					if (rAsset.potentialDoorPositions.Count == 0)
					{
						foreach (var cell in rAsset.cells) // Basically reach all the border cells and make them potential spots for the hallways to attach to (as required by 0.9+ standards)
						{
							bool touchesBorder = false;
							for (int i = 0; i < 4; i++)
							{
								var pos = cell.pos + ((Direction)i).ToIntVector2();
								if (!rAsset.cells.Any(x => x.pos == pos))
								{
									touchesBorder = true;
									break;
								}
							}
							if (touchesBorder)
								rAsset.potentialDoorPositions.Add(cell.pos);
						}
					}
				}

				if (!isAHallway && squaredShape && biggestSize.z > 0 && biggestSize.x > 0) // Fillup empty spots
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
				assets.Add(rAsset);
			}
			return assets;
		}

		internal static string TreatRepeatedName(string originalName)
		{
			if (_repeatedNames.ContainsKey(originalName))
			{
				return originalName + $"_{++_repeatedNames[originalName]}"; // Adds the value before concating the string
			}
			_repeatedNames.Add(originalName, 1);
			return originalName;
		}
		readonly static Dictionary<string, int> _repeatedNames = [];
	}

}
