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
        public short ReputationIndex;
        public ushort ParentFactionID;
        public byte Expansion;
        public byte FriendshipRepID;
        public byte Flags;
        public ushort ParagonFactionID;
        public short[] ReputationClassMask = new short[4];
        public ushort[] ReputationFlags = new ushort[4];
        public int[] ReputationBase = new int[4];
        public int[] ReputationMax = new int[4];
        public float[] ParentFactionMod = new float[2];                        // Faction outputs rep * ParentFactionModOut as spillover reputation
        public byte[] ParentFactionCap = new byte[2];                        // The highest rank the faction will profit from incoming spillover

        // helpers
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
        public byte FactionGroup;
        public byte FriendGroup;
        public byte EnemyGroup;
        public ushort[] Enemies = new ushort[4];
        public ushort[] Friend = new ushort[4];

        // helpers
        public bool IsFriendlyTo(FactionTemplateRecord entry)
        {
            if (this == entry)
                return true;

            if (entry.Faction != 0)
            {
                for (int i = 0; i < 4; ++i)
                    if (Enemies[i] == entry.Faction)
                        return false;
                for (int i = 0; i < 4; ++i)
                    if (Friend[i] == entry.Faction)
                        return true;
            }
            return (FriendGroup & entry.FactionGroup) != 0 || (FactionGroup & entry.FriendGroup) != 0;
        }
        public bool IsHostileTo(FactionTemplateRecord entry)
        {
            if (this == entry)
                return false;

            if (entry.Faction != 0)
            {
                for (int i = 0; i < 4; ++i)
                    if (Enemies[i] == entry.Faction)
                        return true;
                for (int i = 0; i < 4; ++i)
                    if (Friend[i] == entry.Faction)
                        return false;
            }
            return (EnemyGroup & entry.FactionGroup) != 0;
        }
        public bool IsHostileToPlayers() { return (EnemyGroup & (byte)FactionMasks.Player) != 0; }
        public bool IsNeutralToAll()
        {
            for (int i = 0; i < 4; ++i)
                if (Enemies[i] != 0)
                    return false;
            return EnemyGroup == 0 && FriendGroup == 0;
        }
        public bool IsContestedGuardFaction() { return (Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0; }
        public bool ShouldSparAttack() { return (Flags & (ushort)FactionTemplateFlags.EnemySpar) != 0; }
    }
}