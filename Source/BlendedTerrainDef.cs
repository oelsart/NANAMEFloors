using UnityEngine;
using Verse;

namespace NanameFloors;

public class BlendedTerrainDef : TerrainDef
{
    private static readonly int AlphaAddTex = Shader.PropertyToID("_AlphaAddTex");
    private static readonly int BurnTex = Shader.PropertyToID("_BurnTex");
    private static readonly int ScrollSpeed = Shader.PropertyToID("_ScrollSpeed");
    private static readonly int BurnScale = Shader.PropertyToID("_BurnScale");
    private static readonly int PollutionTintColor = Shader.PropertyToID("_PollutionTintColor");

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
                if (shader == AddedShaders.TerrainFadeRoughBlend || shader == AddedShaders.TerrainWaterBlend)
                {
                    CoverGraphic.MatSingle.SetTexture(AlphaAddTex, TexGame.AlphaAddTex);
                }
                CoverGraphic.MatSingle.SetTexture(ShaderPropertyIDs.MaskTex, MaskTex);
                CoverGraphic.MatSingle.renderQueue = 2000;
            }
            if (!CoverTerrain.waterDepthShader.NullOrEmpty())
            {
                CoverWaterDepthMaterial = new Material(ShaderDatabase.LoadShader(CoverTerrain.waterDepthShader).GetBlendShader());
                CoverWaterDepthMaterial.SetTexture(AlphaAddTex, TexGame.AlphaAddTex);
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
                    matSingle.SetTexture(BurnTex, texture2D);
                }
                //matSingle.SetColor("_BurnColor", CoverTerrain.pollutionColor);
                matSingle.SetVector(ScrollSpeed, CoverTerrain.pollutionOverlayScrollSpeed);
                matSingle.SetVector(BurnScale, CoverTerrain.pollutionOverlayScale);
                matSingle.SetColor(PollutionTintColor, CoverTerrain.pollutionTintColor);
                if (shader == AddedShaders.TerrainFadeRoughLinearBurnBlend)
                {
                    matSingle.SetTexture(AlphaAddTex, TexGame.AlphaAddTex);
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