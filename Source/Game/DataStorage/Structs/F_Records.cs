// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class FactionRecord
    {
        public string Description;
        public byte Expansion;
        public int Flags;
        public uint FriendshipRepID;
        public uint Id;
        public LocalizedString Name;
        public ushort ParagonFactionID;
        public byte[] ParentFactionCap = new byte[2]; // The highest rank the faction will profit from incoming spillover
        public ushort ParentFactionID;
        public float[] ParentFactionMod = new float[2]; // Faction outputs rep * ParentFactionModOut as spillover reputation
        public int RenownCurrencyID;
        public int RenownFactionID;
        public int[] ReputationBase = new int[4];
        public short[] ReputationClassMask = new short[4];
        public ushort[] ReputationFlags = new ushort[4];
        public short ReputationIndex;
        public int[] ReputationMax = new int[4];
        public long[] ReputationRaceMask = new long[4];

        // helpers
        public bool CanHaveReputation()
        {
            return ReputationIndex >= 0;
        }
    }

    public sealed class FactionTemplateRecord
    {
        public ushort[] Enemies = new ushort[MAX_FACTION_RELATIONS];
        public byte EnemyGroup;
        public ushort Faction;
        public byte FactionGroup;
        public ushort Flags;
        public ushort[] Friend = new ushort[MAX_FACTION_RELATIONS];
        public byte FriendGroup;

        public uint Id;
        private static readonly int MAX_FACTION_RELATIONS = 8;

        // helpers
        public bool IsFriendlyTo(FactionTemplateRecord entry)
        {
            if (this == entry)
                return true;

            if (entry.Faction != 0)
            {
                for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
                    if (Enemies[i] == entry.Faction)
                        return false;

                for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
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
                for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
                    if (Enemies[i] == entry.Faction)
                        return true;

                for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
                    if (Friend[i] == entry.Faction)
                        return false;
            }

            return (EnemyGroup & entry.FactionGroup) != 0;
        }

        public bool IsHostileToPlayers()
        {
            return (EnemyGroup & (byte)FactionMasks.Player) != 0;
        }

        public bool IsNeutralToAll()
        {
            for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Enemies[i] != 0)
                    return false;

            return EnemyGroup == 0 && FriendGroup == 0;
        }

        public bool IsContestedGuardFaction()
        {
            return (Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0;
        }
    }

    public sealed class FriendshipRepReactionRecord
    {
        public uint FriendshipRepID;
        public uint Id;
        public int OverrideColor;
        public LocalizedString Reaction;
        public ushort ReactionThreshold;
    }

    public sealed class FriendshipReputationRecord
    {
        public LocalizedString Description;
        public int FactionID;
        public FriendshipReputationFlags Flags;
        public uint Id;
        public LocalizedString StandingChanged;
        public LocalizedString StandingModified;
        public int TextureFileID;
    }
}