// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants
{
    public struct BattlefieldSounds
    {
        public const uint HordeWins = 8454;
        public const uint AllianceWins = 8455;
        public const uint Start = 3439;
    }

    public enum BattleFieldObjectiveStates
    {
        Neutral = 0,
        Alliance,
        Horde,
        NeutralAllianceChallenge,
        NeutralHordeChallenge,
        AllianceHordeChallenge,
        HordeAllianceChallenge
    }

    public enum BattlefieldState
    {
        Inactive = 0,
        Warnup = 1,
        InProgress = 2
    }

    public struct BattlefieldIds
    {
        public const uint WG = 1;        // Wintergrasp battle
        public const uint TB = 21;      // Tol Barad
        public const uint Ashran = 24;       // Ashran
    }
}
