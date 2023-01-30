// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Game.Spells
{
    [StructLayout(LayoutKind.Explicit)]
    public struct SpellMisc
    {
        // Alternate names for this value 
        [FieldOffset(0)] public uint TalentId;

        [FieldOffset(0)] public uint SpellId;

        [FieldOffset(0)] public uint SpecializationId;

        // SPELL_EFFECT_SET_FOLLOWER_QUALITY
        // SPELL_EFFECT_INCREASE_FOLLOWER_ITEM_LEVEL
        // SPELL_EFFECT_INCREASE_FOLLOWER_EXPERIENCE
        // SPELL_EFFECT_RANDOMIZE_FOLLOWER_ABILITIES
        // SPELL_EFFECT_LEARN_FOLLOWER_ABILITY
        [FieldOffset(0)] public uint FollowerId;

        [FieldOffset(4)] public uint FollowerAbilityId; // only SPELL_EFFECT_LEARN_FOLLOWER_ABILITY

        // SPELL_EFFECT_FINISH_GARRISON_MISSION
        [FieldOffset(0)] public uint GarrMissionId;

        // SPELL_EFFECT_UPGRADE_HEIRLOOM
        [FieldOffset(0)] public uint ItemId;

        [FieldOffset(0)] public uint Data0;

        [FieldOffset(4)] public uint Data1;

        public uint[] GetRawData()
        {
            return new uint[]
                   {
                       Data0, Data1
                   };
        }
    }
}