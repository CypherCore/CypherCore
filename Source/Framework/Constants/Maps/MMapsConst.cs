// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public enum Spot
    {
        Top = 1,
        Right = 2,
        Left = 3,
        Bottom = 4,
        Entire = 5
    }

    public enum GridNumber
    {
        V8,
        V9
    }

    public enum NavArea
    {
        Empty = 0,
        MagmaSlime = 8, // don't need to differentiate between them
        Water = 9,
        GroundSteep = 10,
        Ground = 11,
        MaxValue = Ground,
        MinValue = MagmaSlime,
        AllMask = 0x3F // max allowed value
        // areas 1-60 will be used for destructible areas (currently skipped in vmaps, WMO with flag 1)
        // ground is the highest value to make recast choose ground over water when merging surfaces very close to each other (shallow water would be walkable) 
    }

    public enum NavTerrainFlag : ushort
    {
        Empty = 0x00,
        Ground = 1 << (NavArea.MaxValue - NavArea.Ground),
        GroundSteep = 1 << (NavArea.MaxValue - NavArea.GroundSteep),
        Water = 1 << (NavArea.MaxValue - NavArea.Water),
        MagmaSlime = 1 << (NavArea.MaxValue - NavArea.MagmaSlime)
    }

    public enum OffMeshConnectionFlag : byte
    {
        BiDirectional = 0x01
    }
}
