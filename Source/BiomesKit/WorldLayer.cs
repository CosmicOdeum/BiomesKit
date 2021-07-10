using HarmonyLib;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BiomesKit
{

    public class BiomesKitWorldLayer : RimWorld.Planet.WorldLayer // Let's paint some worldmaterials.
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
							bool canBeSemiSnowy = false;
							bool canBeSnowy = false;
							bool canBeExtraSnowy = false;
                            switch (tile.hilliness)
                            {
                                case Hilliness.Flat:
                                    hillPath = null;
                                    break;
                                case Hilliness.SmallHills:
                                    hillPath += "SmallHills";
                                    canBeExtraSnowy = true;
                                    break;
                                case Hilliness.LargeHills:
                                    hillPath += "LargeHills";
                                    canBeExtraSnowy = true;
                                    break;
                                case Hilliness.Mountainous:
                                    hillPath += "Mountains";
                                    canBeSemiSnowy = true;
                                    canBeSnowy = true;
                                    canBeExtraSnowy = true;
                                    break;
                                case Hilliness.Impassable:
                                    hillPath += "Impassable";
                                    canBeSemiSnowy = true;
                                    canBeSnowy = true;
                                    canBeExtraSnowy = true;
                                    break;

                            }
                            if (hillPath != null)
							{
                                if (canBeExtraSnowy == true)
                                {
                                    switch (tile.temperature)
                                    {
                                        case float temp when temp < biomesKit.hillExtraSnowyBelow:
                                            hillPath += "_ExtraSnowy";
                                            canBeSemiSnowy = false;
                                            canBeSnowy = false;
                                            break;
                                    }
                                }
                                if (canBeSnowy == true)
                                {
                                    switch (tile.temperature)
                                    {
                                        case float temp when temp < biomesKit.hillSnowyBelow:
                                            hillPath += "_Snowy";
                                            canBeSemiSnowy = false;
                                            break;
                                    }
                                }
                                if (canBeSemiSnowy == true)
                                {
                                    switch (tile.temperature)
                                    {
                                        case float temp when temp < biomesKit.hillSemiSnowyBelow:
                                            hillPath += "_SemiSnowy";
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
