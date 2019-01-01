/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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

    public enum BFLeaveReason
    {
        Close = 1,
        //BF_LEAVE_REASON_UNK1      = 2, (not used)
        //BF_LEAVE_REASON_UNK2      = 4, (not used)
        Exited = 8,
        LowLevel = 10,
        NotWhileInRaid = 15,
        Deserter = 16
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
