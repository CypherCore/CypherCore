// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class FactionRecord
    {
        public uint Id;
        public long[] ReputationRaceMask = new long[4];
        public LocalizedString Name;
        public string Description;
        public short ReputationIndex;
        public ushort ParentFactionID;
        public byte Expansion;
        public uint FriendshipRepID;
        public int Flags;
        public ushort ParagonFactionID;
        public int RenownFactionID;
        public int RenownCurrencyID;
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
        static int MAX_FACTION_RELATIONS = 8;

        public uint Id;
        public ushort Faction;
        public int Flags;
        public byte FactionGroup;
        public byte FriendGroup;
        public byte EnemyGroup;
        public ushort[] Enemies = new ushort[MAX_FACTION_RELATIONS];
        public ushort[] Friend = new ushort[MAX_FACTION_RELATIONS];

        // helpers
        public bool HasFlag(FactionTemplateFlags factionTemplateFlags) { return (Flags & (ushort)factionTemplateFlags) != 0; }

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
        public bool IsHostileToPlayers() { return (EnemyGroup & (byte)FactionMasks.Player) != 0; }
        public bool IsNeutralToAll()
        {
            for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Enemies[i] != 0)
                    return false;
            return EnemyGroup == 0 && FriendGroup == 0;
        }
        public bool IsContestedGuardFaction() { return HasFlag(FactionTemplateFlags.ContestedGuard); }
    }

    public sealed class FriendshipRepReactionRecord
    {
        public uint Id;
        public LocalizedString Reaction;
        public uint FriendshipRepID;
        public int ReactionThreshold;
        public int OverrideColor;
    }

    public sealed class FriendshipReputationRecord
    {
        public LocalizedString Description;
        public LocalizedString StandingModified;
        public LocalizedString StandingChanged;
        public uint Id;
        public int FactionID;
        public int TextureFileID;
        public int Flags;

        public bool HasFlag(FriendshipReputationFlags friendshipReputationFlags) { return (Flags & (int)friendshipReputationFlags) != 0; }
    }
}