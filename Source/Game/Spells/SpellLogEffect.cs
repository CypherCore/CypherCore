// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Networking.Packets;

namespace Game.Spells
{
    public class SpellLogEffect
    {
        public List<SpellLogEffectDurabilityDamageParams> DurabilityDamageTargets { get; set; } = new();
        public int Effect { get; set; }
        public List<SpellLogEffectExtraAttacksParams> ExtraAttacksTargets { get; set; } = new();
        public List<SpellLogEffectFeedPetParams> FeedPetTargets { get; set; } = new();
        public List<SpellLogEffectGenericVictimParams> GenericVictimTargets { get; set; } = new();

        public List<SpellLogEffectPowerDrainParams> PowerDrainTargets { get; set; } = new();
        public List<SpellLogEffectTradeSkillItemParams> TradeSkillTargets { get; set; } = new();
    }
}