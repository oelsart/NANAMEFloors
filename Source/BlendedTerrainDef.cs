using UnityEngine;
using Verse;

namespace NanameFloors;

public class BlendedTerrainDef : TerrainDef
{
    private static readonly int RippleTex = Shader.PropertyToID("_RippleTex");
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
        if (def.customShader != null)
        {
            return def.customShader.Shader;
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
            
            CoverTerrain = terrainMask.coverTerrain;
            Rotation = terrainMask.rotation ?? Rot4.North;
            
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
                if (CoverTerrain.customShader != null && CoverTerrain.customShaderParameters != null)
                {
                    for (var i = 0; i < CoverTerrain.customShaderParameters.Count; i++)
                    {
                        CoverTerrain.customShaderParameters[i].Apply(CoverGraphic.MatSingle);
                    }
                }
                CoverGraphic.MatSingle.SetTexture(ShaderPropertyIDs.MaskTex, MaskTex);
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
                if (texture2D)
                {
                    matSingle.SetTexture(ShaderPropertyIDs.BurnTex, texture2D);
                }
                matSingle.SetColor(ShaderPropertyIDs.BurnColor, CoverTerrain.pollutionColor);
                matSingle.SetVector(ShaderPropertyIDs.ScrollSpeed, CoverTerrain.pollutionOverlayScrollSpeed);
                matSingle.SetVector(ShaderPropertyIDs.BurnScale, CoverTerrain.pollutionOverlayScale);
                matSingle.SetColor(ShaderPropertyIDs.PollutionTintColor, CoverTerrain.pollutionTintColor);
                if (shader == NAF_DefOf.TerrainFadeRoughLinearBurnBlend.Shader)
                {
                    matSingle.SetTexture(ShaderPropertyIDs.AlphaAddTex, TexGame.AlphaAddTex);
                }
                if (matSingle != CoverGraphic.MatSingle)
                {
                    matSingle.SetFloat(ShaderPropertyIDs.IsPolluted, 1f);
                }
                if ((CoverTerrain.pollutionShaderType != null || CoverTerrain.customShader != null) && CoverTerrain.customShaderParameters != null)
                {
                    for (var k = 0; k < CoverTerrain.customShaderParameters.Count; k++)
                    {
                        CoverTerrain.customShaderParameters[k].Apply(matSingle);
                    }
                }
                matSingle.SetTexture(ShaderPropertyIDs.MaskTex, MaskTex);
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