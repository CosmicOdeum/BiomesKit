﻿using BiomesCore.GenSteps;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Verse;
using Verse.Noise;

namespace BiomesKit
{

	[StaticConstructorOnStartup]
	public class BiomesKitControls : DefModExtension
	{
		public List<BiomeDef> spawnOnBiomes = new List<BiomeDef>();
		public string materialPath = "World/MapGraphics/Default";
		public int materialLayer = 3515;
		public bool allowOnWater = false;
		public bool allowOnLand = false;
		public bool needRiver = false;
		public float minTemperature = -999;
		public float maxTemperature = 999;
		public float minElevation = -9999;
		public float maxElevation = 9999;
		public float minNorthLatitude = -9999;
		public float maxNorthLatitude = -9999;
		public float minSouthLatitude = -9999;
		public float maxSouthLatitude = -9999;
		public Hilliness minHilliness = Hilliness.Flat;
		public Hilliness maxHilliness = Hilliness.Impassable;
		public Hilliness? spawnHills = null;
		public float minRainfall = -9999;
		public float maxRainfall = 9999;
		public float frequency = 100;
		public bool useAlternativePerlinSeedPreset = false;
		public bool usePerlin = false;
		public int? perlinCustomSeed = null;
		public float perlinCulling = 0;
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

	[HarmonyPatch(typeof(Tile))]
	[HarmonyPatch("Hilliness")]
	static class Tile_Hilliness
	{
		public enum Hilliness : byte
		{
			Valley
		}

	}



	public class LateBiomeWorker : WorldGenStep // Technically not a biomeworker, but whatever. Thanks Garthor!
	{
		public static ModuleBase PerlinNoise = null;
		public bool validForPrinting = true;
		private static readonly IntVec2 TexturesInAtlas = new IntVec2(2, 2);
		public override int SeedPart
		{
			get
			{
				return 123456789;
			}
		}
		public override void GenerateFresh(string seed)
		{

			List<BiomeDef> allDefsListForReading = DefDatabase<BiomeDef>.AllDefsListForReading;
				foreach (BiomeDef biomeDef2 in allDefsListForReading.Where(x => x.HasModExtension<BiomesKitControls>()))
				{
					BiomesKitControls biomesKit = biomeDef2.GetModExtension<BiomesKitControls>();
					float minSouthLatitude = biomesKit.minSouthLatitude * -1;
					float maxSouthLatitude = biomesKit.maxSouthLatitude * -1;
					for (int tileID = 0; tileID < Find.WorldGrid.TilesCount; tileID++)
					{

						float latitude = Find.WorldGrid.LongLatOf(tileID).y;
						int perlinSeed = Find.World.info.Seed;
						PerlinNoise = new Perlin(0.1, 10, 0.6, 12, perlinSeed, QualityMode.Low);
						var coords = Find.WorldGrid.GetTileCenter(tileID);
						float perlinNoiseValue = PerlinNoise.GetValue(coords);
						Tile tile = Find.WorldGrid[tileID];
						bool validTarget = true;
						foreach (BiomeDef targetBiome in biomesKit.spawnOnBiomes)
						{
							if (tile.biome == targetBiome)
							{
								validTarget = true;
								break;
							}
							else
							{
								validTarget = false;
							}
						}
						if (validTarget == false)
						{
							continue;
						}
						bool validSouthLatitude = true;
						if (latitude < minSouthLatitude && latitude > maxSouthLatitude)
						{
							validSouthLatitude = true;
						}
						else
						{
							validSouthLatitude = false;
						}
						bool validNorthLatitude = true;
						if (latitude > biomesKit.minNorthLatitude && latitude < biomesKit.maxNorthLatitude)
						{
							validNorthLatitude = true;
						}
						else
						{
							validNorthLatitude = false;
						}
						if (validNorthLatitude == false && validSouthLatitude == false)
						{
							if (biomesKit.minSouthLatitude != -9999 && biomesKit.minNorthLatitude != -9999 && biomesKit.maxSouthLatitude != -9999 && biomesKit.maxNorthLatitude != 9999)
							{
								continue;
							}
						}
						if (biomesKit.perlinCustomSeed != null)
						{
							perlinSeed = biomesKit.perlinCustomSeed.Value;
						}
						else if (biomesKit.useAlternativePerlinSeedPreset)
						{
							perlinSeed = tileID;
						}
						if (tile.WaterCovered && biomesKit.allowOnWater == false)
						{
							continue;
						}
						if (!tile.WaterCovered && biomesKit.allowOnLand == false)
						{
							continue;
						}
						if (biomesKit.needRiver == true)
						{
							if (tile.Rivers == null || tile.Rivers.Count == 0)
							{
								continue;
							}
						}
						if (biomesKit.usePerlin == true)
						{
							if (perlinNoiseValue > (biomesKit.perlinCulling / 100f))
							{
								continue;
							}
						}
						if (Rand.Value > (Math.Pow(biomesKit.frequency, 2) / 10000f))
						{
							continue;
						}
						if (tile.elevation < biomesKit.minElevation || tile.elevation > biomesKit.maxElevation)
						{
							continue;
						}
						if (tile.temperature < biomesKit.minTemperature || tile.temperature > biomesKit.maxTemperature)
						{
							continue;
						}
						if (tile.rainfall < biomesKit.minRainfall || tile.rainfall > biomesKit.maxRainfall)
						{
							continue;
						}
						if (tile.hilliness < biomesKit.minHilliness || tile.hilliness > biomesKit.maxHilliness)
						{
							continue;
						}
						tile.biome = biomeDef2;
						if (biomesKit.spawnHills != null)
						{
							tile.hilliness = biomesKit.spawnHills.Value;
						}

					}
				}
		}

	}

	public class BiomesKitWorldLayer : WorldLayer
	{
		private static readonly IntVec2 TexturesInAtlas = new IntVec2(2, 2);
		public override IEnumerable Regenerate()
		{
			foreach (object obj in base.Regenerate())
			{
				yield return obj;
			}
			Rand.PushState();
			Rand.Seed = Find.World.info.Seed;
			WorldGrid worldGrid = Find.WorldGrid;
			List<BiomeDef> allDefsListForReading = DefDatabase<BiomeDef>.AllDefsListForReading;
				foreach (BiomeDef biomeDef2 in allDefsListForReading.Where(x => x.HasModExtension<BiomesKitControls>()))
				{
					for (int tileID = 0; tileID < Find.WorldGrid.TilesCount; tileID++)
					{
						Tile t = Find.WorldGrid[tileID];
						BiomesKitControls biomesKit = biomeDef2.GetModExtension<BiomesKitControls>();
						if (t.biome != biomeDef2)
							continue;
						if (biomesKit.materialPath == "World/MapGraphics/Default")
							continue;
						Material material = MaterialPool.MatFrom(biomesKit.materialPath, ShaderDatabase.WorldOverlayTransparentLit, biomesKit.materialLayer);
						LayerSubMesh subMesh = base.GetSubMesh(material);
						Vector3 vector = worldGrid.GetTileCenter(tileID);
						WorldRendererUtility.PrintQuadTangentialToPlanet(vector, vector, worldGrid.averageTileSize, 0.01f, subMesh, false, false, false);
						WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, TexturesInAtlas.x), Rand.Range(0, TexturesInAtlas.z), TexturesInAtlas.x, TexturesInAtlas.z, subMesh);
					}
				}
			Rand.PopState();
			base.FinalizeMesh(MeshParts.All);
			yield break;
		}
	}
}