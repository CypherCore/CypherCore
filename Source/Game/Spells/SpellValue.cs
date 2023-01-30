// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Spells
{
    public class SpellValue
    {
        public int AuraStackAmount;
        public float CriticalChance { get; set; }
        public uint CustomBasePointsMask { get; set; }
        public int? Duration { get; set; }
        public float DurationMul { get; set; }

        public int[] EffectBasePoints { get; set; } = new int[SpellConst.MaxEffects];
        public uint MaxAffectedTargets { get; set; }
        public float RadiusMod { get; set; }

        public SpellValue(SpellInfo proto, WorldObject caster)
        {
            foreach (var spellEffectInfo in proto.GetEffects())
                EffectBasePoints[spellEffectInfo.EffectIndex] = spellEffectInfo.CalcBaseValue(caster, null, 0, -1);

            CustomBasePointsMask = 0;
            MaxAffectedTargets = proto.MaxAffectedTargets;
            RadiusMod = 1.0f;
            AuraStackAmount = 1;
            CriticalChance = 0.0f;
            DurationMul = 1;
        }
    }
}