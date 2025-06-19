using RimWorld;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    public class BlendedTerrainDef : TerrainDef
    {
        public Graphic CoverGraphic {  get; private set; }
        public Graphic CoverGraphicPolluted { get; private set; }

        public override void PostLoad()
        {
            placingDraggableDimensions = 2;
            var terrainMask = GetModExtension<TerrainMask>();
            if (terrainMask == null) return;

            (Graphic, Graphic) GetGraphic(TerrainDef terrain, bool invert = false)
            {
                var shader = AddedShaders.TerrainHardBlend;
                if (invert) shader = AddedShaders.Invert(shader);
                var graphic = GraphicDatabase.Get(typeof(Graphic_Terrain), terrain.texturePath, shader, Vector2.one, terrain.DrawColor, Color.white, "NanameFloors/TerrainMasks/" + terrainMask.maskTextureName);
                graphic.MatSingle.renderQueue = 2000 + terrain.renderPrecedence;

                if (!ModsConfig.BiotechActive) return (graphic, null);
                Shader shader2 = terrain.pollutionShaderType == ShaderTypeDefOf.TerrainFadeRoughLinearAdd ? AddedShaders.TerrainFadeRoughLinearAddBlend : AddedShaders.TerrainHardPollutedBlend;
                if (invert) shader2 = AddedShaders.Invert(shader2);
                string path = terrain.pollutedTexturePath.NullOrEmpty() ? terrain.texturePath : terrain.pollutedTexturePath;
                var graphicPolluted = GraphicDatabase.Get(typeof(Graphic_Terrain), path, shader2, Vector2.one, terrain.DrawColor, Color.white, "NanameFloors/TerrainMasks/" + terrainMask.maskTextureName);

                var matSingle = graphicPolluted.MatSingle;
                if (!terrain.pollutionOverlayTexturePath.NullOrEmpty()) matSingle.SetTexture("_BurnTex", ContentFinder<Texture2D>.Get(terrain.pollutionOverlayTexturePath));
                matSingle.SetColor("_BurnColor", terrain.pollutionColor);
                matSingle.SetColor("_PollutionTintColor", terrain.pollutionTintColor);
                if (shader == AddedShaders.TerrainFadeRoughLinearAddBlend)
                {
                    matSingle.SetVector("_BurnScale", terrain.pollutionOverlayScale);
                    matSingle.SetTexture("_AlphaAddTex", TexGame.AlphaAddTex);
                }
                return (graphic, graphicPolluted);
            }

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                (graphic, graphicPolluted) = GetGraphic(terrainMask.baseTerrain, false);
                (CoverGraphic, CoverGraphicPolluted) = GetGraphic(terrainMask.coverTerrain, true);
            });
            if (tools != null)
            {
                for (int i = 0; i < tools.Count; i++)
                {
                    tools[i].id = i.ToString();
                }
            }
        }
    }
}
