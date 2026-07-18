using System.Collections.Generic;
using HarmonyLib;
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
}