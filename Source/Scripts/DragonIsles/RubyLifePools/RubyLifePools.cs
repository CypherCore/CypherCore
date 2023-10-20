// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
    }

    [Script] // 371652 - Executed
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

    [Script] // 384933 - Ice Shield
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

    [Script] // 372793 - Excavate
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

    [Script] // 395029 - Storm Infusion
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
