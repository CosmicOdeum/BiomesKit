﻿using System;
using RimWorld;
using HarmonyLib;
using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using BiomesKit;
using Verse.Noise;
using UnityEngine;

namespace BiomesKitPatches
{
    [HarmonyPatch(typeof(WorldLayer_Hills), "Regenerate")]
    internal static class WorldLayer
    {
        internal static void Prefix()
        {
            Material noMaterial = MaterialPool.MatFrom("Transparent", ShaderDatabase.WorldOverlayTransparentLit, 3510);
            AccessTools.Field(typeof(WorldMaterials), nameof(WorldMaterials.SmallHills)).SetValue(null, noMaterial);
            AccessTools.Field(typeof(WorldMaterials), nameof(WorldMaterials.LargeHills)).SetValue(null, noMaterial);
            AccessTools.Field(typeof(WorldMaterials), nameof(WorldMaterials.Mountains)).SetValue(null, noMaterial);
            AccessTools.Field(typeof(WorldMaterials), nameof(WorldMaterials.ImpassableMountains)).SetValue(null, noMaterial);
        }
    }



}
