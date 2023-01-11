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

namespace Scripts.DragonIsles.RubyLifePools
{
    struct SpellIds
    {
        // Flashfrost Chillweaver
        public const uint IceShield = 372749;

        // Primal Juggernaut
        public const uint Excavate = 373497;
    };

    // 371652 - Executed
    class spell_ruby_life_pools_executed : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag3(UnitFlags3.FakeDead);
            target.SetUnitFlag2(UnitFlags2.FeignDeath);
            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        }
    }

    // 384933 - Ice Shield
    class spell_ruby_life_pools_ice_shield : AuraScript
    {
        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Aura iceShield = GetTarget()?.GetAura(SpellIds.IceShield);
            iceShield?.RefreshDuration();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    // 372793 - Excavate
    class spell_ruby_life_pools_excavate : AuraScript
    {
        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            GetCaster()?.CastSpell(GetTarget(), SpellIds.Excavate, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    // 395029 - Storm Infusion
    class spell_ruby_life_pools_storm_infusion : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            dest.RelocateOffset(new Position(9.0f, 0.0f, 4.0f, 0.0f));
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(SetDest, 1, Targets.DestDest));
        }
    }
}
