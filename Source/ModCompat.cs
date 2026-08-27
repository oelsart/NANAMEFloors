global using static NanameFloors.ModCompat;
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace NanameFloors;

public static class ModCompat
{
  public static class ReplaceStuff
  {
    public static readonly bool Active = ModsConfig.IsActive("Memegoddess.ReplaceStuff");
    public static readonly List<TerrainDef> allBridgeTerrains;

    static ReplaceStuff()
    {
      if (!Active) return;
      allBridgeTerrains =
        AccessTools.StaticFieldRefAccess<List<TerrainDef>>(
          "Replace_Stuff.PlaceBridges.BridgelikeTerrain:allBridgeTerrains");
    }
  }
  
  public static class AsAboveSoBelow
  {
    public static readonly bool Active = ModsConfig.IsActive("astryl.AsAboveSoBelow2");
    public const string PatchCategory = "NanameFloors.AsAboveSoBelow";
    public static readonly Type SectionLayer_ABBelowV2;
    public static readonly Type SectionLayer_ABBelowWatergen;
    public static readonly Func<Material, int, int, Material> DepthQueue;

    static AsAboveSoBelow()
    {
	    if (Active)
	    {
		    SectionLayer_ABBelowV2 = GenTypes.GetTypeInAnyAssembly("AsAboveSoBelow.SectionLayer_ABBelowV2", "AsAboveSoBelow");
		    SectionLayer_ABBelowWatergen = GenTypes.GetTypeInAnyAssembly("AsAboveSoBelow.SectionLayer_ABBelowWatergen", "AsAboveSoBelow");
		    DepthQueue = AccessTools.MethodDelegate<Func<Material, int, int, Material>>("AsAboveSoBelow.SectionLayer_ABBelowV2:DepthQueue");
	    }
    }
  }
    
  extension (SectionLayer layer)
  {
	  public bool IsAASBLayer => AsAboveSoBelow.Active && layer.GetType() == AsAboveSoBelow.SectionLayer_ABBelowV2;
  }
}