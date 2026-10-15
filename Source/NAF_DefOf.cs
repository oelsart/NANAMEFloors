using RimWorld;
using Verse;

namespace NanameFloors;

[DefOf]
public static class NAF_DefOf
{
  static NAF_DefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(NAF_DefOf));
  }

  public static ShaderDef TerrainHardBlend;

  public static ShaderDef TerrainFadeBlend;
  
  public static ShaderDef TerrainFadeRoughBlend;
  
  public static ShaderDef TerrainWaterBlend;
  
  public static ShaderDef TerrainWaterPollutedBlend;
  
  public static ShaderDef TerrainHardLinearBurnBlend;
  
  public static ShaderDef TerrainFadeLinearBurnBlend;
  
  public static ShaderDef TerrainFadeRoughSoftLightBlend;
  
  public static ShaderDef TerrainFadeRoughLinearAddBlend;
  
  public static ShaderDef TerrainFadeRoughLinearBurnBlend;
  
  public static ShaderDef WaterDepthBlend;

  public static KeyBindingDef NAF_Designator_RotateLeft;

  public static KeyBindingDef NAF_Designator_RotateRight;
}