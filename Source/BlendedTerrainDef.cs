using UnityEngine;
using Verse;

namespace NanameFloors;

public class BlendedTerrainDef : TerrainDef
{
    public TerrainDef BaseTerrain { get; private set; }

    public TerrainDef CoverTerrain { get; private set; }
        
    public Rot4 Rotation { get; private set; }

    public Texture2D MaskTex { get; private set; }

    public Graphic CoverGraphic { get; private set; }

    public Graphic CoverGraphicPolluted { get; private set; }

    public Material CoverWaterDepthMaterial { get; private set; }

    private static Shader ShaderPolluted(TerrainDef def)
    {
        if (def.pollutionShaderType != null)
        {
            return def.pollutionShaderType.Shader;
        }

        return def.edgeType switch
        {
            TerrainEdgeType.Hard => ShaderDatabase.TerrainHardPolluted,
            TerrainEdgeType.Fade => ShaderDatabase.TerrainFadePolluted,
            TerrainEdgeType.FadeRough => ShaderDatabase.TerrainFadeRoughPolluted,
            _ => null
        };
    }

    public override void PostLoad()
    {
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            var terrainMask = GetModExtension<TerrainMask>();
            if (terrainMask == null) return;

            BaseTerrain = terrainMask.baseTerrain;
            CoverTerrain = terrainMask.coverTerrain;
            Rotation = terrainMask.rotation ?? Rot4.North;

            graphic = graphic.GetColoredVersion(ShaderDatabase.TerrainHard, BaseTerrain.DrawColor, Color.white);
            graphicPolluted = BaseTerrain.graphicPolluted == BaseContent.BadGraphic
                ? BaseContent.BadGraphic
                : BaseTerrain.graphicPolluted.GetColoredVersion(ShaderDatabase.TerrainHardPolluted, BaseTerrain.DrawColor, Color.white);
            
            var maskPath = "NanameFloors/TerrainMasks/" + terrainMask.maskTextureName;
            MaskTex = ContentFinder<Texture2D>.Get("NanameFloors/TerrainMasks/" + terrainMask.maskTextureName, false);
            MaskTex.wrapMode = TextureWrapMode.Clamp;
            MaskTex.mipMapBias = -2.5f;
            if (CoverGraphic == null)
            {
                var shader = CoverTerrain.Shader.GetBlendShader();
                CoverGraphic = GraphicDatabase.Get<Graphic_Terrain>(CoverTerrain.texturePath, shader, Vector2.one, CoverTerrain.DrawColor, Color.white, null, maskPath);
                if (shader == NAF_DefOf.TerrainFadeRoughBlend.Shader || shader == NAF_DefOf.TerrainWaterBlend.Shader)
                {
                    CoverGraphic.MatSingle.SetTexture(ShaderPropertyIDs.AlphaAddTex, TexGame.AlphaAddTex);
                }
                CoverGraphic.MatSingle.SetTexture(ShaderPropertyIDs.MaskTex, MaskTex);
                CoverGraphic.MatSingle.renderQueue = 2000;
            }
            if (!CoverTerrain.waterDepthShader.NullOrEmpty())
            {
                CoverWaterDepthMaterial = new Material(ShaderDatabase.LoadShader(CoverTerrain.waterDepthShader).GetBlendShader());
                CoverWaterDepthMaterial.SetTexture(ShaderPropertyIDs.AlphaAddTex, TexGame.AlphaAddTex);
                if (CoverTerrain.waterDepthShaderParameters != null)
                {
                    for (var j = 0; j < CoverTerrain.waterDepthShaderParameters.Count; j++)
                    {
                        CoverTerrain.waterDepthShaderParameters[j].Apply(CoverWaterDepthMaterial);
                    }
                }
                CoverWaterDepthMaterial.SetTexture(ShaderPropertyIDs.MaskTex, MaskTex);
                CoverWaterDepthMaterial.renderQueue = 2000 + CoverTerrain.renderPrecedence;
            }
            if (ModsConfig.BiotechActive && CoverGraphicPolluted == null && (!CoverTerrain.pollutionOverlayTexturePath.NullOrEmpty() || !CoverTerrain.pollutedTexturePath.NullOrEmpty()))
            {
                Texture2D texture2D = null;
                if (!CoverTerrain.pollutionOverlayTexturePath.NullOrEmpty())
                {
                    texture2D = ContentFinder<Texture2D>.Get(CoverTerrain.pollutionOverlayTexturePath);
                }
                var shader = ShaderPolluted(CoverTerrain).GetBlendShader();
                CoverGraphicPolluted = GraphicDatabase.Get<Graphic_Terrain>(CoverTerrain.pollutedTexturePath ?? CoverTerrain.texturePath, shader, Vector2.one, CoverTerrain.DrawColor, Color.white, null, maskPath);
                var matSingle = CoverGraphicPolluted.MatSingle;
                if (texture2D != null)
                {
                    matSingle.SetTexture(ShaderPropertyIDs.BurnTex, texture2D);
                }
                //matSingle.SetColor("_BurnColor", CoverTerrain.pollutionColor);
                matSingle.SetVector(ShaderPropertyIDs.ScrollSpeed, CoverTerrain.pollutionOverlayScrollSpeed);
                matSingle.SetVector(ShaderPropertyIDs.BurnScale, CoverTerrain.pollutionOverlayScale);
                matSingle.SetColor(ShaderPropertyIDs.PollutionTintColor, CoverTerrain.pollutionTintColor);
                if (shader == NAF_DefOf.TerrainFadeRoughLinearBurnBlend.Shader)
                {
                    matSingle.SetTexture(ShaderPropertyIDs.AlphaAddTex, TexGame.AlphaAddTex);
                }
                matSingle.SetTexture(ShaderPropertyIDs.MaskTex, MaskTex);
                matSingle.renderQueue = 2000;
            }
        });
        base.PostLoad();
    }

    public BlendedTerrainDef Rotated(RotationDirection direction)
    {
        var terrainMask = GetModExtension<TerrainMask>();
        if (terrainMask?.rotation is null) return this;

        var rot = terrainMask.rotation.Value.Rotated(direction);
        if (DefDatabase<BlendedTerrainDef>.GetNamedSilentFail(
                TerrainMask.GetDefName(
                    terrainMask.maskTextureName,
                    terrainMask.baseTerrain,
                    terrainMask.coverTerrain,
                    rot)) is { } rotated)
            return rotated;
        
        var terrainMask2 = new TerrainMask(terrainMask.maskTextureName, terrainMask.baseTerrain, terrainMask.coverTerrain, rot);
        BlendedTerrainUtil.MakeBlendedTerrain(terrainMask2);
        return DefDatabase<BlendedTerrainDef>.GetNamedSilentFail(terrainMask2.DefName) ?? this;
    }
}