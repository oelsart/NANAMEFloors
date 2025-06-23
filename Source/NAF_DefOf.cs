using RimWorld;
using Verse;

namespace NanameFloors
{
    [DefOf]
    public static class NAF_DefOf
    {
        static NAF_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(NAF_DefOf));
        }

        public static ShaderTypeDef TerrainHardBlend;

        public static ShaderTypeDef TerrainFadeBlend;

        public static ShaderTypeDef TerrainFadeRoughBlend;

        public static ShaderTypeDef TerrainWaterBlend;

        public static ShaderTypeDef TerrainWaterPollutedBlend;

        public static ShaderTypeDef TerrainHardLinearBurnBlend;

        public static ShaderTypeDef TerrainFadeLinearBurnBlend;

        public static ShaderTypeDef TerrainFadeRoughSoftLightBlend;

        public static ShaderTypeDef TerrainFadeRoughLinearAddBlend;

        public static ShaderTypeDef TerrainFadeRoughLinearBurnBlend;

        public static ShaderTypeDef WaterDepthBlend;
    }
}
