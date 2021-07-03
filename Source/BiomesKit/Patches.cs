using System;
using RimWorld;
using HarmonyLib;
using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using BiomesKit;
using Verse.Noise;

namespace BiomesKitPatches
{
    //[HarmonyPatch(typeof(WorldLayer_Hills), nameof(WorldLayer_Hills.Regenerate))]
    //internal static class WorldLayer
    //{
    //    internal static void Prefix()
    //    {
    //        for (int tileID = 0; tileID < Find.WorldGrid.TilesCount; tileID++)
    //        {
    //            Tile tile = Find.WorldGrid[tileID];
    //            if (tile.biome.HasModExtension<BiomesKitControls>())
    //            {
    //                tile.hilliness = Hilliness.Flat;
    //            }
    //        }
    //    }
    //}
    //[HarmonyPatch(typeof(WorldLayer_Hills), nameof(WorldLayer_Hills.Regenerate))]
    //internal static class WorldLayer2
    //{
    //    internal static void Postfix()
    //    {
    //        for (int tileID = 0; tileID < Find.WorldGrid.TilesCount; tileID++)
    //        {
    //            Tile tile = Find.WorldGrid[tileID];
    //            if (tile.biome.HasModExtension<BiomesKitControls>())
    //            {
    //                tile.hilliness = Hilliness.SmallHills;
    //            }
    //        }
    //    }
    //}



}
