/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using System;

namespace Game.DataStorage
{
    public sealed class FactionRecord
    {
        public ulong[] ReputationRaceMask = new ulong[4];
        public LocalizedString Name;
        public string Description;
        public uint Id;
        public int[] ReputationBase = new int[4];
        public float ParentFactionModIn;                         // Faction gains incoming rep * ParentFactionModIn
        public float ParentFactionModOut;                        // Faction outputs rep * ParentFactionModOut as spillover reputation
        public uint[] ReputationMax = new uint[4];
        public short ReputationIndex;
        public ushort[] ReputationClassMask = new ushort[4];
        public ushort[] ReputationFlags = new ushort[4];
        public ushort ParentFactionID;
        public ushort ParagonFactionID;
        public byte ParentFactionCapIn;                         // The highest rank the faction will profit from incoming spillover
        public byte ParentFactionCapOut;
        public byte Expansion;
        public byte Flags;
        public byte FriendshipRepID;

        public bool CanHaveReputation()
        {
            return ReputationIndex >= 0;
        }
    }

    public sealed class FactionTemplateRecord
    {
        public uint Id;
        public ushort Faction;
        public ushort Flags;
        public ushort[] Enemies = new ushort[4];
        public ushort[] Friends = new ushort[4];
        public byte Mask;
        public byte FriendMask;
        public byte EnemyMask;

        public bool IsFriendlyTo(FactionTemplateRecord entry)
        {
            if (Id == entry.Id)
                return true;
            if (entry.Faction != 0)
            {
                for (int i = 0; i < 4; ++i)
                    if (Enemies[i] == entry.Faction)
                        return false;
                for (int i = 0; i < 4; ++i)
                    if (Friends[i] == entry.Faction)
                        return true;
            }
            return Convert.ToBoolean(FriendMask & entry.Mask) || Convert.ToBoolean(Mask & entry.FriendMask);
        }
        public bool IsHostileTo(FactionTemplateRecord entry)
        {
            if (Id == entry.Id)
                return false;
            if (entry.Faction != 0)
            {
                for (int i = 0; i < 4; ++i)
                    if (Enemies[i] == entry.Faction)
                        return true;
                for (int i = 0; i < 4; ++i)
                    if (Friends[i] == entry.Faction)
                        return false;
            }
            return (EnemyMask & entry.Mask) != 0;
        }
        public bool IsHostileToPlayers() { return (EnemyMask & (uint)FactionMasks.Player) != 0; }
        public bool IsNeutralToAll()
        {
            for (int i = 0; i < 4; ++i)
                if (Enemies[i] != 0)
                    return false;

            return EnemyMask == 0 && FriendMask == 0;
        }
        public bool IsContestedGuardFaction() { return Flags.HasAnyFlag((ushort)FactionTemplateFlags.ContestedGuard); }
        public bool ShouldSparAttack() { return Flags.HasAnyFlag((ushort)FactionTemplateFlags.EnemySpar); }
    }
}
