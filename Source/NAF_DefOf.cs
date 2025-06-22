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

        public static ShaderTypeDef TerrainHardLinearBurnBlend;

        public static ShaderTypeDef TerrainFadeRoughLinearAddBlend;
    }
}
