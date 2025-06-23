using UnityEngine;
using Verse;

namespace NanameFloors
{
    public class BlendedTerrainDef : TerrainDef
    {
        public TerrainDef BaseTerrain { get; private set; }

        public TerrainDef CoverTerrain { get; private set; }

        public Texture2D MaskTex { get; private set; }

        public Graphic CoverGraphic { get; private set; }

        public Graphic CoverGraphicPolluted { get; private set; }

        public Material CoverWaterDepthMaterial { get; private set; }

        private Shader ShaderPolluted(TerrainDef def)
        {
            if (def.pollutionShaderType != null)
            {
                return def.pollutionShaderType.Shader;
            }
            Shader result = null;
            switch (def.edgeType)
            {
                case TerrainEdgeType.Hard:
                    result = ShaderDatabase.TerrainHardPolluted;
                    break;
                case TerrainEdgeType.Fade:
                    result = ShaderDatabase.TerrainFadePolluted;
                    break;
                case TerrainEdgeType.FadeRough:
                    result = ShaderDatabase.TerrainFadeRoughPolluted;
                    break;
            }
            return result;
        }

        public override void PostLoad()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                TerrainMask terrainmask = GetModExtension<TerrainMask>();
                if (terrainmask == null) return;

                BaseTerrain = terrainmask.baseTerrain;
                CoverTerrain = terrainmask.coverTerrain;

                graphic = graphic.GetColoredVersion(ShaderDatabase.TerrainHard, BaseTerrain.DrawColor, Color.white);
                if (BaseTerrain.graphicPolluted == BaseContent.BadGraphic)
                {
                    graphicPolluted = BaseContent.BadGraphic;
                }
                else
                {
                    graphicPolluted = BaseTerrain.graphicPolluted.GetColoredVersion(ShaderDatabase.TerrainHardPolluted, BaseTerrain.DrawColor, Color.white);
                }
                var maskPath = "NanameFloors/TerrainMasks/" + terrainmask.maskTextureName;
                MaskTex = ContentFinder<Texture2D>.Get("NanameFloors/TerrainMasks/" + terrainmask.maskTextureName, false);
                MaskTex.wrapMode = TextureWrapMode.Clamp;
                MaskTex.mipMapBias = -0.35f;
                if (CoverGraphic == null)
                {
                    Shader shader = BlendedTerrainUtil.GetBlendShader(CoverTerrain.Shader);
                    CoverGraphic = GraphicDatabase.Get<Graphic_Terrain>(CoverTerrain.texturePath, shader, Vector2.one, CoverTerrain.DrawColor, Color.white, null, maskPath);
                    if (shader == NAF_DefOf.TerrainFadeRoughBlend.Shader || shader == NAF_DefOf.TerrainWaterBlend.Shader)
                    {
                        CoverGraphic.MatSingle.SetTexture("_AlphaAddTex", TexGame.AlphaAddTex);
                    }
                    CoverGraphic.MatSingle.SetTexture("_MaskTex", MaskTex);
                    CoverGraphic.MatSingle.renderQueue = 2000;
                }
                if (!CoverTerrain.waterDepthShader.NullOrEmpty())
                {
                    CoverWaterDepthMaterial = new Material(ShaderDatabase.LoadShader(CoverTerrain.waterDepthShader));
                    CoverWaterDepthMaterial.SetTexture("_AlphaAddTex", TexGame.AlphaAddTex);
                    if (CoverTerrain.waterDepthShaderParameters != null)
                    {
                        for (int j = 0; j < CoverTerrain.waterDepthShaderParameters.Count; j++)
                        {
                            CoverTerrain.waterDepthShaderParameters[j].Apply(CoverWaterDepthMaterial);
                        }
                    }
                    CoverWaterDepthMaterial.SetTexture("_MaskTex", MaskTex);
                    CoverWaterDepthMaterial.renderQueue = 2000 + CoverTerrain.renderPrecedence;
                }
                if (ModsConfig.BiotechActive && CoverGraphicPolluted == null && (!CoverTerrain.pollutionOverlayTexturePath.NullOrEmpty() || !CoverTerrain.pollutedTexturePath.NullOrEmpty()))
                {
                    Texture2D texture2D = null;
                    if (!CoverTerrain.pollutionOverlayTexturePath.NullOrEmpty())
                    {
                        texture2D = ContentFinder<Texture2D>.Get(CoverTerrain.pollutionOverlayTexturePath, true);
                    }
                    Shader shader = BlendedTerrainUtil.GetBlendShader(ShaderPolluted(CoverTerrain));
                    CoverGraphicPolluted = GraphicDatabase.Get<Graphic_Terrain>(CoverTerrain.pollutedTexturePath ?? CoverTerrain.texturePath, shader, Vector2.one, CoverTerrain.DrawColor, Color.white, null, maskPath);
                    Material matSingle = CoverGraphicPolluted.MatSingle;
                    if (texture2D != null)
                    {
                        matSingle.SetTexture("_BurnTex", texture2D);
                    }
                    matSingle.SetColor("_BurnColor", CoverTerrain.pollutionColor);
                    matSingle.SetVector("_ScrollSpeed", CoverTerrain.pollutionOverlayScrollSpeed);
                    matSingle.SetVector("_BurnScale", CoverTerrain.pollutionOverlayScale);
                    matSingle.SetColor("_PollutionTintColor", CoverTerrain.pollutionTintColor);
                    if (shader == NAF_DefOf.TerrainFadeRoughLinearBurnBlend.Shader)
                    {
                        matSingle.SetTexture("_AlphaAddTex", TexGame.AlphaAddTex);
                    }
                    matSingle.SetTexture("_MaskTex", MaskTex);
                    matSingle.renderQueue = 2000;
                }
            });
            base.PostLoad();
        }
    }
}
