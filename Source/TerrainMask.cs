using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace NanameFloors;

[StaticConstructorOnStartup]
public class TerrainMask : DefModExtension, IExposable
{
    private string oldMaskName;
    public string maskTextureName;
    public TerrainDef baseTerrain;
    public TerrainDef coverTerrain;
    public Rot4? rotation;
    
    public static List<Texture2D> cachedTerrainMasks;

    public string DefName => 
        !oldMaskName.NullOrEmpty()
            ? $"{baseTerrain.defName}_{oldMaskName}_{coverTerrain.defName}"
            : GetDefName(maskTextureName, baseTerrain, coverTerrain, rotation ?? Rot4.North);

    public static string GetDefName(string maskTextureName, TerrainDef baseTerrain, TerrainDef coverTerrain, Rot4 rotation)
    {
        // For back compatibility
        var resolvedDirection = rotation.AsInt switch
        {
            Rot4.EastInt => "SouthEast",
            Rot4.SouthInt => "SouthWest",
            Rot4.WestInt => "NorthWest",
            _ => "NorthEast"
        };
        return $"{baseTerrain.defName}_{maskTextureName}_{resolvedDirection}_{coverTerrain.defName}";
    }
        
    static TerrainMask()
    {
        if (UnityData.IsInMainThread)
        {
            Init();
            return;
        }
        LongEventHandler.ExecuteWhenFinished(Init);
        return;

        static void Init()
        {
            cachedTerrainMasks = ContentFinder<Texture2D>.GetAllInFolder("NanameFloors/TerrainMasks").ToList();
            NanameFloors.UI.terrainMasks = cachedTerrainMasks.Where(m => !NanameFloors.settings.exceptMaskList.Contains(m.name)).ToList();
        }
    }

    [UsedImplicitly]
    public TerrainMask() { }

    public TerrainMask(string name, TerrainDef baseTerr, TerrainDef coverTerr)
    {
        maskTextureName = name;
        oldMaskName = name;
        baseTerrain = baseTerr;
        coverTerrain = coverTerr;
        BackCompatibilityConvert(this);
    }

    public TerrainMask(string name, TerrainDef baseTerr, TerrainDef coverTerr, Rot4 rot)
    {
        maskTextureName = name;
        baseTerrain = baseTerr;
        coverTerrain = coverTerr;
        rotation = rot;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref maskTextureName, nameof(maskTextureName));
        Scribe_Defs.Look(ref baseTerrain, nameof(baseTerrain));
        Scribe_Defs.Look(ref coverTerrain, nameof(coverTerrain));
        Scribe_Values.Look(ref rotation, nameof(rotation));
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            BackCompatibilityConvert(this);
        }
    }

    public static readonly Dictionary<string, string> OldNameToNewName = new()
    {
        {"Half", "HalfTriangle"}
    };

    private enum OldSuffix
    {
        _NorthEast, _SouthEast, _SouthWest, _NorthWest, _North, _East, _South, _West
    }

    private static void BackCompatibilityConvert(TerrainMask terrainMask)
    {
        var name = terrainMask.maskTextureName;
        if (!cachedTerrainMasks.Select(m => m.name).Contains(name))
        {
            terrainMask.oldMaskName = name;
            foreach (var suffix in Enum.GetValues(typeof(OldSuffix)))
            {
                var suffixName = Enum.GetName(typeof(OldSuffix), suffix);
                if (suffixName is null) continue;
                if (name.EndsWith(suffixName))
                {
                    terrainMask.rotation ??= suffix switch
                    {
                        OldSuffix._East => Rot4.East,
                        OldSuffix._SouthEast => Rot4.South,
                        OldSuffix._South => Rot4.South,
                        OldSuffix._SouthWest => Rot4.South,
                        OldSuffix._West => Rot4.West,
                        OldSuffix._NorthWest => Rot4.West,
                        _ => Rot4.North
                    };
                    name = name[..^suffixName.Length];
                }
            }
        
            if (OldNameToNewName.TryGetValue(name, out var newName))
                name = newName;
            
            terrainMask.maskTextureName = name;
        }
    }
}