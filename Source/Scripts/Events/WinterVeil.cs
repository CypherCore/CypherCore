/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Events.WinterVeil
{
    struct SpellIds
    {
        //Mistletoe
        public const uint CreateMistletoe = 26206;
        public const uint CreateHolly = 26207;
        public const uint CreateSnowflakes = 45036;

        //Winter Wondervolt
        public const uint Px238WinterWondervoltTransform1 = 26157;
        public const uint Px238WinterWondervoltTransform2 = 26272;
        public const uint Px238WinterWondervoltTransform3 = 26273;
        public const uint Px238WinterWondervoltTransform4 = 26274;
    }

    [Script] // 26218 - Mistletoe
    class spell_winter_veil_mistletoe : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.CreateMistletoe, SpellIds.CreateHolly, SpellIds.CreateSnowflakes);
        }

        void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target)
            {
                uint spellId = RandomHelper.RAND(SpellIds.CreateHolly, SpellIds.CreateMistletoe, SpellIds.CreateSnowflakes);
                GetCaster().CastSpell(target, spellId, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 26275 - PX-238 Winter Wondervolt TRAP
    class spell_winter_veil_px_238_winter_wondervolt : SpellScript
    {     
        static uint[] spells =
        {
            SpellIds.Px238WinterWondervoltTransform1,
            SpellIds.Px238WinterWondervoltTransform2,
            SpellIds.Px238WinterWondervoltTransform3,
            SpellIds.Px238WinterWondervoltTransform4
        };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Px238WinterWondervoltTransform1, SpellIds.Px238WinterWondervoltTransform2,
                SpellIds.Px238WinterWondervoltTransform3, SpellIds.Px238WinterWondervoltTransform4);
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Unit target = GetHitUnit();
            if (target)
            {
                for (byte i = 0; i < 4; ++i)
                    if (target.HasAura(spells[i]))
                        return;

                target.CastSpell(target, spells[RandomHelper.URand(0, 3)], true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }
































}
