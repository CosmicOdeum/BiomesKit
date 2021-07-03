using BiomesKit.ExtensionMethod;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace BiomesKit
{
	public sealed class HarmonyStarter : Mod
	{
		public const String HarmonyId = "net.biomes.terrainkit";

		public HarmonyStarter(ModContentPack content) : base(content)
		{
			Assembly terrainAssembly = Assembly.GetExecutingAssembly();
			string DLLName = terrainAssembly.GetName().Name;
			Version loadedVersion = terrainAssembly.GetName().Version;
			Version laterVersion = loadedVersion;

			List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
			foreach (ModContentPack mod in runningModsListForReading)
			{
				foreach (FileInfo item in from f in ModContentPack.GetAllFilesForMod(mod, "Assemblies/", (string e) => e.ToLower() == ".dll") select f.Value)
				{
					var newAssemblyName = AssemblyName.GetAssemblyName(item.FullName);
					if (newAssemblyName.Name == DLLName && newAssemblyName.Version > laterVersion)
					{
						laterVersion = newAssemblyName.Version;
						Log.Error(String.Format("BiomesKit load order error detected. {0} is loading an older version {1} before {2} loads version {3}. Please put the BiomesKit, or BiomesCore modes above this one if they are active.",
							content.Name, loadedVersion, mod.Name, laterVersion));
					}
				}
			}

			var harmony = new Harmony(HarmonyId);
			harmony.PatchAll(terrainAssembly);
		}
	}

	public class BiomesKitControls : DefModExtension
	{
		public List<BiomeDef> spawnOnBiomes = new List<BiomeDef>();
		public int? biomePriority;
		public string materialPath = "World/MapGraphics/Default";
		public bool forested = false;
		public bool uniqueHills = false;
		public float forestSnowyBelow = -9999;
		public float forestSparseBelow = -9999;
		public float forestDenseAbove = 9999;
		public int materialLayer = 3515;
		public float smallHillSizeMultiplier = 1.5f;
		public float largeHillSizeMultiplier = 2f;
		public float mountainSizeMultiplier = 1.4f;
		public float impassableSizeMultiplier = 1.3f;
		public float materialSizeMultiplier = 1;
		public float materialOffset = 1f;
		public bool materialRandomRotation = true;
		public Hilliness materialMinHilliness;
		public Hilliness materialMaxHilliness;
		public bool allowOnWater = false;
		public bool allowOnLand = true;
		public bool setNotWaterCovered = false;
		public int minimumWaterNeighbors = 0;
		public int minimumLandNeighbors = 0;
		public bool needRiver = false;
		public bool randomizeHilliness = false;
		public float minTemperature = -999;
		public float maxTemperature = 999;
		public float minElevation = -9999;
		public float maxElevation = 9999;
		public float? setElevation;
		public float minNorthLatitude = -9999;
		public float maxNorthLatitude = -9999;
		public float minSouthLatitude = -9999;
		public float maxSouthLatitude = -9999;
		public Hilliness minHilliness = Hilliness.Flat;
		public Hilliness maxHilliness = Hilliness.Impassable;
		public BiomesKitHilliness? spawnHills = null;
		public BiomesKitHilliness? setHills = null;
		public float minRainfall = -9999;
		public float maxRainfall = 9999;
		public int frequency = 100;
		public bool useAlternativePerlinSeedPreset = false;
		public bool usePerlin = false;
		public int? perlinCustomSeed = null;
		public float perlinCulling = 0.99f;
		public double perlinFrequency;
		public double perlinLacunarity;
		public double perlinPersistence;
		public int perlinOctaves;
	}


	public class UniversalBiomeWorker : BiomeWorker
	{
		public override float GetScore(Tile tile, int tileID)
		{
			return 0f;
		}
	}

	public static class Dictionaries
	{
		public static Dictionary<Tile, Hilliness> backupHilliness = new Dictionary<Tile, Hilliness>();
		public static Dictionary<Tile, Hilliness> tileRestored = new Dictionary<Tile, Hilliness>();
	}

	public enum BiomesKitHilliness
	{
		Undefined,
		Flat,
		SmallHills,
		LargeHills,
		Mountainous,
		Impassable,
		Random,
		Valley
	}

	[StaticConstructorOnStartup]
	public static class ErrorLogs
	{
		static ErrorLogs()
		{
			List<BiomeDef> allDefsListForReading = DefDatabase<BiomeDef>.AllDefsListForReading;
			foreach (BiomeDef biomeDef2 in allDefsListForReading.Where(x => x.HasModExtension<BiomesKitControls>()))
			{
				BiomesKitControls biomesKit = biomeDef2.GetModExtension<BiomesKitControls>();
				HashSet<BiomeDef> defs = new HashSet<BiomeDef>();
				foreach (BiomeDef targetBiome in biomesKit.spawnOnBiomes)
				{
					if (!defs.Add(targetBiome))
					{
						Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": spawnOnBiomes includes " + targetBiome + " twice.");
					}
				}
				Material testMaterial;
				if (biomesKit.materialPath != "World/MapGraphics/Default")
					testMaterial = MaterialPool.MatFrom(biomesKit.materialPath, ShaderDatabase.WorldOverlayTransparentLit, biomesKit.materialLayer);
				if (biomesKit.materialLayer >= 3560)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": The materialLayer is set to 3560 or higher, making the material display on top of the selection indicator.");
				}
				if (!biomesKit.allowOnLand && !biomesKit.allowOnWater)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": Biome is disallowed on both land and water and will never spawn.");
				}
				if (biomesKit.minTemperature > biomesKit.maxTemperature)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": minTemperature set above maxTemperature.");
				}
				if (biomesKit.minNorthLatitude > biomesKit.maxNorthLatitude)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": minNorthLatitude set above maxNorthLatitude.");
				}
				if (biomesKit.minSouthLatitude > biomesKit.maxSouthLatitude)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": minSouthLatitude set above maxSouthLatitude.");
				}
				if (biomesKit.minHilliness > biomesKit.maxHilliness)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": minHilliness set above maxHilliness.");
				}
				if (biomesKit.minElevation > biomesKit.maxElevation)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": minElevation set above maxElevation.");
				}
				if (biomesKit.minRainfall > biomesKit.maxRainfall)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": minRainfall set above maxRainfall.");
				}
				if (biomesKit.frequency > 100)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": frequency set above 100. Frequency accepts values 1-100. Setting Frequency higher than that is not supported.");
				}
				if (biomesKit.usePerlin == false && biomesKit.useAlternativePerlinSeedPreset == true)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": usePerlin is false but useAlternativePerlinSeedPreset is true. useAlternativePerlinSeedPreset should be false if usePerlin is set to false.");
				}
				if (biomesKit.usePerlin == false && biomesKit.perlinCustomSeed != null)
				{
					Log.Warning("[BiomesKit] XML Config Error: " + biomeDef2 + ": usePerlin is false but perlinCustomSeed is assigned. perlinCustomSeed will not be read if usePerlin is set to false.");
				}

			}
		}

	}

	public class LateBiomeWorker : WorldGenStep // Technically not a biomeworker, but whatever. Thanks Garthor!
	{
		public static ModuleBase PerlinNoise = null;
		public bool validForPrinting = true;
		List<BiomeDef> firstOrderBiomes = new List<BiomeDef>();
		List<BiomeDef> secondOrderBiomes = new List<BiomeDef>();
		List<BiomeDef> thirdOrderBiomes = new List<BiomeDef>();
		public override int SeedPart
		{
			get
			{
				return 123456789;
			}
		}
		public override void GenerateFresh(string seed) // This part doesn't do anything except call BiomesKit.
		{
			BiomesKit();
		}
		private void BiomesKit()
		{
			foreach (BiomeDef biomeDef in DefDatabase<BiomeDef>.AllDefsListForReading.Where(x => x.HasModExtension<BiomesKitControls>()))
			{
				// We then get the modextension from the specific BiomeDef we are iterating through right now.
				BiomesKitControls biomesKit = biomeDef.GetModExtension<BiomesKitControls>();
				switch (biomesKit.biomePriority)
				{
					case 1:
						Log.Message("adding " + biomeDef.defName + "to primary biomes");
						firstOrderBiomes.Add(biomeDef);
						break;
					case 2:
						Log.Message("adding " + biomeDef.defName + "to secondary biomes");
						secondOrderBiomes.Add(biomeDef);
						break;
					case 3:
						Log.Message("adding " + biomeDef.defName + "to tertiary biomes");
						thirdOrderBiomes.Add(biomeDef);
						break;
					default:
						goto case 1;
				}
			}
			foreach (BiomeDef biomeDef in firstOrderBiomes)
			{
				Log.Error("running primary biome " + biomeDef);
				BiomesKitCalcs(biomeDef);
			}
			foreach (BiomeDef biomeDef in secondOrderBiomes)
			{
				Log.Error("running secondary biome " + biomeDef);
				BiomesKitCalcs(biomeDef);
			}
			foreach (BiomeDef biomeDef in thirdOrderBiomes)
			{
				Log.Error("running tertiary biome " + biomeDef);
				BiomesKitCalcs(biomeDef);
			}
		}
		private void BiomesKitCalcs(BiomeDef biomeDef)
		{
			BiomesKitControls biomesKit = biomeDef.GetModExtension<BiomesKitControls>();
			// South Latitudes need to be negative values. Here we convert them for the user's convenience.
			float minSouthLatitude = biomesKit.minSouthLatitude * -1;
			float maxSouthLatitude = biomesKit.maxSouthLatitude * -1;
			// Now we start iterating through tiles on the world map. This is inside the loop iterating through BiomeDefs.
			// so this is done for each BiomeDef with the ModExtension.
			WorldGrid worldGrid = Find.WorldGrid;
			for (int tileID = 0; tileID < worldGrid.TilesCount; tileID++)
			{
				Tile tile = worldGrid[tileID];
				// Next we find out what latitude the tile is on.
				float latitude = worldGrid.LongLatOf(tileID).y;
				// We set up some perlin values.
				int perlinSeed = Find.World.info.Seed;
				var coords = worldGrid.GetTileCenter(tileID);
				// We give ourselves a way to reference the tile that's being checked.
				// Now we start actually doing something. First up, we respond to the spawnOnBiomes tag.
				bool validTarget = true; // The tile is a valid target by default.
				// We iterate through another list. This time of biomes specified by the tag.
				foreach (BiomeDef targetBiome in biomesKit.spawnOnBiomes)
				{
					// If the BiomeDef matches the tile, we declare the tile a valid target and stop iterating.
					if (tile.biome == targetBiome)
					{
						validTarget = true;
						break;
					}
					// If the BiomeDef doesn't match the tile, we declare the tile an invalid target and move on to the next BiomeDef on the list.
					else
					{
						validTarget = false;
					}
				}
				// After that, if the tile is no longer a valid target, we skip to the next tile.
				if (validTarget == false)
				{
					continue;
				}
				// Next up is the latitude tags.
				bool validSouthLatitude = true; // Southern latitude is valid by default.
												// If the tile's southern latitude is lesser than the minimum and greater than the maximum, declare the tile's south latitude valid. 
												// Since southern altitude uses negative numbers, we want it lower than the minimum and higher than the maximum.
				if (latitude < minSouthLatitude && latitude > maxSouthLatitude)
				{
					validSouthLatitude = true;
				}
				// If the tile's southern latitude is greater than the ninimum and lesser than the maximum, we declare the tile's south latitude invalid.
				else
				{
					validSouthLatitude = false;
				}
				// Now for the northern latitude.
				bool validNorthLatitude = true; // Northern latitude is also valid by default.
												// If the tile's northern latitude is greater than the minimum and lesser than the maximum, declare the north latitude valid.
				if (latitude > biomesKit.minNorthLatitude && latitude < biomesKit.maxNorthLatitude)
				{
					validNorthLatitude = true;
				}
				// If the tile's northern latitude is lesser than the minimum and greater than the maximum, declare the northern latitude invalid.
				else
				{
					validNorthLatitude = false;
				}
				// We check if both the north and the south latitudes are invalid.
				if (validNorthLatitude == false && validSouthLatitude == false)
				{
					// If they are both invalid, we check if every latitude tag has been set by the user.
					if (biomesKit.minSouthLatitude != -9999 && biomesKit.minNorthLatitude != -9999 && biomesKit.maxSouthLatitude != -9999 && biomesKit.maxNorthLatitude != 9999)
					{
						continue; // If not a single latitude tag has been set, we skip to the next tile.
					}
				}
				// If the tile is a water tile and the biome is not allowed on water, we skip to the next tile.
				if (tile.WaterCovered && biomesKit.allowOnWater == false)
				{
					continue;
				}
				// If the tile is a land tile and the biome is not allowed on land, we skip to the next tile.
				if (!tile.WaterCovered && biomesKit.allowOnLand == false)
				{
					continue;
				}
				// Does the biome need a river?
				if (biomesKit.needRiver == true)
				{
					// If it does, and the tile doesn't have a river, we skip to the next tile.
					if (tile.Rivers == null || tile.Rivers.Count == 0)
					{
						continue;
					}
				}
				// Now we define the Perlin Noise seed.
				// If the user has assigned a custom seed, we use that.
				if (biomesKit.perlinCustomSeed != null)
				{
					perlinSeed = biomesKit.perlinCustomSeed.Value;
				}
				// If not, we check if they've asked to use the alternative preset seed.
				else if (biomesKit.useAlternativePerlinSeedPreset) // If they have, we use that.
				{
					perlinSeed = tileID;
				}
				// Are we using perlin for this biome?
				if (biomesKit.usePerlin == true)
				{
					// If we are, it's time to generate our perlin noise.
					PerlinNoise = new Perlin(0.1, 10, 0.6, 12, perlinSeed, QualityMode.Low);
					float perlinNoiseValue = PerlinNoise.GetValue(coords);
					// And after that we cull the lower perlin values.
					if (perlinNoiseValue <= (biomesKit.perlinCulling))
					{
						continue;
					}
				}
				// Compare a random number between 0 and 1 to the userdefined frequency to the power of two divided by ten thousand. I promise it makes sense to do it this way.
				if (Rand.Value > (Math.Pow(biomesKit.frequency, 2) / 10000f))
				{
					continue;
				}
				// If the tile's elevation is higher is lower than the minimum or higher than the maximum, we skip to the next tile.
				if (tile.elevation < biomesKit.minElevation || tile.elevation > biomesKit.maxElevation)
				{
					continue;
				}
				// If the tile's temperature is higher is lower than the minimum or higher than the maximum, we skip to the next tile.
				if (tile.temperature < biomesKit.minTemperature || tile.temperature > biomesKit.maxTemperature)
				{
					continue;
				}
				// If the tile's rainfall is higher is lower than the minimum or higher than the maximum, we skip to the next tile.
				if (tile.rainfall < biomesKit.minRainfall || tile.rainfall > biomesKit.maxRainfall)
				{
					continue;
				}
				// If the tile's hilliness is higher is lower than the minimum or higher than the maximum, we skip to the next tile.
				if (tile.hilliness < biomesKit.minHilliness || tile.hilliness > biomesKit.maxHilliness)
				{
					continue;
				}
				List<int> tmpNeighbors = new List<int>();
				worldGrid.GetTileNeighbors(tileID, tmpNeighbors);
				int neighbor = 0;
				int waterNeighbors = 0;
				bool enoughWaterNeighbors = true;
				if (biomesKit.minimumWaterNeighbors > 0)
				{
					for (int count = tmpNeighbors.Count; neighbor < count; neighbor++)
					{
						if (worldGrid[tmpNeighbors[neighbor]].biome == BiomeDefOf.Ocean)
						{
							waterNeighbors += 1;
							Log.Message("Water Neighbors =" + waterNeighbors);
						}
						if (waterNeighbors < biomesKit.minimumWaterNeighbors)
						{
							enoughWaterNeighbors = false;
						}
						if (waterNeighbors == biomesKit.minimumWaterNeighbors)
						{
							enoughWaterNeighbors = true;
						}
					}
				}
				if (enoughWaterNeighbors == false)
				{
					continue;
				}
				int landNeighbors = 0;
				bool enoughLandNeighbors = true;
				if (biomesKit.minimumWaterNeighbors > 0)
				{
					for (int count = tmpNeighbors.Count; neighbor < count; neighbor++)
					{
						if (worldGrid[tmpNeighbors[neighbor]].biome != BiomeDefOf.Ocean)
						{
							landNeighbors += 1;
						}
						if (waterNeighbors < biomesKit.minimumLandNeighbors)
						{
							enoughLandNeighbors = false;
						}
						if (waterNeighbors == biomesKit.minimumLandNeighbors)
						{
							enoughLandNeighbors = true;
						}
					}
				}
				if (enoughLandNeighbors == false)
				{
					continue;
				}
				// If we get this far, we can spawn the biome!
				if (biomeDef.workerClass.Name == "UniversalBiomeWorker")
					tile.biome = biomeDef;
				// If the user wants to actually change the hilliness of a tile, we handle that here.
				if (biomesKit.setHills != null)
				{
					int hilliness = (int)biomesKit.setHills;
					if (biomesKit.spawnHills != null && biomesKit.setHills == null) // convert deprecated tag if modern tag is not used
					{
						hilliness = (int)biomesKit.spawnHills;
						biomesKit.setHills = biomesKit.spawnHills;
					}
					if (biomesKit.setHills == BiomesKitHilliness.Random)
					{
						// random number from 0 to 3.
						switch (Rand.Range(0, 3))
						{
							case 0: // 0 means flat.
								tile.hilliness = Hilliness.Flat;
								break;
							case 1: // 1 means small hills.
								tile.hilliness = Hilliness.SmallHills;
								break;
							case 2: // 2 means large hills.
								tile.hilliness = Hilliness.LargeHills;
								break;
							case 3: // 3 means mountainous.
								tile.hilliness = Hilliness.Mountainous;
								break;
						}
					}
					else if (biomesKit.setHills < BiomesKitHilliness.Random)
					{
						tile.hilliness = (Hilliness)biomesKit.setHills;
					}
				}
				if (biomesKit.setElevation != null)
				{
					tile.elevation = (float)biomesKit.setElevation;
				}
			}
			Log.Message("finished biome cycle");
		}
	}

	public class BiomesKitWorldLayer : WorldLayer // Let's paint some worldmaterials.
	{
		private static readonly IntVec2 TexturesInAtlas = new IntVec2(2, 2); // two by two, meaning four variants for each worldmaterial.
		public override IEnumerable Regenerate()
		{
			foreach (object obj in base.Regenerate())
			{
				yield return obj;
			}
			Rand.PushState();
			Rand.Seed = Find.World.info.Seed;
			WorldGrid worldGrid = Find.WorldGrid;
			for (int tileID = 0; tileID < Find.WorldGrid.TilesCount; tileID++)
			{
				Tile tile = Find.WorldGrid[tileID];
				if (tile.biome.HasModExtension<BiomesKitControls>())
				{
					bool noRoad = tile.Roads.NullOrEmpty();
					bool noRiver = tile.Rivers.NullOrEmpty();
					BiomesKitControls biomesKit = tile.biome.GetModExtension<BiomesKitControls>();
					Vector3 vector = worldGrid.GetTileCenter(tileID);
					if (biomesKit.uniqueHills)
					{
						Dictionary<Tile, Hilliness> backupHilliness = Dictionaries.backupHilliness;
						Dictionary<Tile, Hilliness> tileRestored = Dictionaries.tileRestored;
						if (noRiver && noRoad)
						{
							Material hillMaterial;
							string hillPath = "WorldMaterials/BiomesKit/" + tile.biome.defName + "/Hills/";
							bool canBeSnowy = false;
							switch (tile.hilliness)
							{
								case Hilliness.Flat:
									hillPath = null;
									break;
								case Hilliness.SmallHills:
									hillPath += "SmallHills";
									break;
								case Hilliness.LargeHills:
									hillPath += "LargeHills";
									break;
								case Hilliness.Mountainous:
									hillPath += "Mountains";
									canBeSnowy = true;
									break;
								case Hilliness.Impassable:
									hillPath += "Impassable";
									canBeSnowy = true;
									break;

							}
							if (hillPath != null)
							{
								if (canBeSnowy == true)
								{
									switch (tile.temperature)
									{
										case float temp when temp < biomesKit.forestSnowyBelow:
											hillPath += "_Snowy";
											break;
									}
								}
								hillMaterial = MaterialPool.MatFrom(hillPath, ShaderDatabase.WorldOverlayTransparentLit, biomesKit.materialLayer);
								LayerSubMesh subMeshHill = GetSubMesh(hillMaterial);
								WorldRendererUtility.PrintQuadTangentialToPlanet(vector, vector, (worldGrid.averageTileSize * biomesKit.impassableSizeMultiplier), 0.01f, subMeshHill, false, biomesKit.materialRandomRotation, false);
								WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, TexturesInAtlas.x), Rand.Range(0, TexturesInAtlas.z), TexturesInAtlas.x, TexturesInAtlas.z, subMeshHill);
							}
						}
					}
					if (biomesKit.forested && tile.hilliness == Hilliness.Flat && noRiver && noRoad)
					{
						string forestPath = "WorldMaterials/BiomesKit/" + tile.biome.defName + "/Forest/Forest_";
						bool pathHasChanged = false;
						switch (tile.temperature)
                        {
							case float temp when temp < biomesKit.forestSnowyBelow:
								forestPath += "Snowy";
								pathHasChanged = true;
								break;
						}
						switch (tile.rainfall)
						{
							case float rain when rain < biomesKit.forestSparseBelow:
								forestPath += "Sparse";
								pathHasChanged = true;
								break;
							case float rain when rain > biomesKit.forestDenseAbove:
								forestPath += "Dense";
								pathHasChanged = true;
								break;
						}
						if (!pathHasChanged)
							forestPath = forestPath.Remove(forestPath.Length - 1, 1);
						Material forestMaterial = MaterialPool.MatFrom(forestPath, ShaderDatabase.WorldOverlayTransparentLit, biomesKit.materialLayer);
						LayerSubMesh subMeshForest = GetSubMesh(forestMaterial);
						WorldRendererUtility.PrintQuadTangentialToPlanet(vector, vector, (worldGrid.averageTileSize * biomesKit.materialSizeMultiplier), 0.01f, subMeshForest, false, biomesKit.materialRandomRotation, false);
						WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, TexturesInAtlas.x), Rand.Range(0, TexturesInAtlas.z), TexturesInAtlas.x, TexturesInAtlas.z, subMeshForest);
					}
					if (biomesKit.materialPath != "World/MapGraphics/Default")
					{
						Material material = MaterialPool.MatFrom(biomesKit.materialPath, ShaderDatabase.WorldOverlayTransparentLit, biomesKit.materialLayer);
						LayerSubMesh subMesh = GetSubMesh(material);
						WorldRendererUtility.PrintQuadTangentialToPlanet(vector, vector, (worldGrid.averageTileSize * biomesKit.materialSizeMultiplier), 0.01f, subMesh, false, biomesKit.materialRandomRotation, false);
						WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, TexturesInAtlas.x), Rand.Range(0, TexturesInAtlas.z), TexturesInAtlas.x, TexturesInAtlas.z, subMesh);
					}
				}
			}
			Rand.PopState();
			base.FinalizeMesh(MeshParts.All);
			yield break;
		}
	}
}