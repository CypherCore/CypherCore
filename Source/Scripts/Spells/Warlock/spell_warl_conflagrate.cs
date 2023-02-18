// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 17962 - Conflagrate
	[SpellScript(17962)]
	public class spell_warl_conflagrate : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(WarlockSpells.IMMOLATE);
		}

		private void HandleHit(uint UnnamedParameter)
        {
            var caster = GetCaster();
            var target = GetHitUnit();

            if (caster == null || target == null)
                return;

            caster.ModifyPower(PowerType.SoulShards, 75);

            Backdraft(caster);
            RoaringBlaze(caster, target);
            Decimation(caster, target);
        }

        private void Decimation(Unit caster, Unit target)
        {
            if (caster.TryGetAura(WarlockSpells.DECIMATION, out var dec) && target.HealthBelowPct(dec.GetEffect(1).GetBaseAmount()))
                caster.GetSpellHistory().ModifyCooldown(WarlockSpells.SOUL_FIRE, TimeSpan.FromMilliseconds(dec.GetEffect(0).GetBaseAmount()));
        }

        private void Backdraft(Unit caster)
        {
            if (caster.HasAura(WarlockSpells.BACKDRAFT_AURA))
                caster.CastSpell(caster, WarlockSpells.BACKDRAFT, true);
        }

        private void RoaringBlaze(Unit caster, Unit target)
        {
            if (caster.HasAura(WarlockSpells.ROARING_BLAZE))
            {
                var aur = target.GetAura(WarlockSpells.IMMOLATE_DOT, caster.GetGUID());

                if (aur != null)
                {
                    var aurEff = aur.GetEffect(0);
                    var dmgEff = Global.SpellMgr.GetSpellInfo(WarlockSpells.ROARING_BLASE_DMG_PCT, Difficulty.None)?.GetEffect(0);

                    if (aurEff != null && dmgEff != null)
                    {
                        var damage = aurEff.GetAmount();
                        aurEff.SetAmount(MathFunctions.AddPct(ref damage, dmgEff.BasePoints));
                        aur.SetNeedClientUpdateForTargets();
                    }
                }
            }
        }

        public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}
}