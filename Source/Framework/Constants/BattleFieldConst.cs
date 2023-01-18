// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
