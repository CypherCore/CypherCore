// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Incinerate - 29722
	[SpellScript(29722)]
	public class spell_warl_incinerate : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();
        double _brimstoneDamage = 0;
		private void HandleOnHitMainTarget(int UnnamedParameter)
		{
			GetCaster().CastSpell(WarlockSpells.INCINERATE_ENERGIZE, true);

            if (IsHitCrit())
                GetCaster().ModifyPower(PowerType.SoulShards, 10);
        }

		private void HandleOnHitTarget(int UnnamedParameter)
        {
            var target = GetHitUnit();
            var caster = GetCaster();

            if (target == null || caster == null)
                return;

            DiabolicEmbers(target);
            FireAndBrimstone(target, caster);
            RoaringBlaze(target, caster);
        }

        private void DiabolicEmbers(Unit caster)
        {
            if (caster.HasAura(WarlockSpells.DIABOLIC_EMBERS))
                caster.CastSpell(WarlockSpells.INCINERATE_ENERGIZE, true);
        }

        private void FireAndBrimstone(Unit target, Unit caster)
        {
            if (!caster.TryGetAura(WarlockSpells.FIRE_AND_BRIMSTONE, out var fab))
            {
                if (target != GetExplTargetUnit())
                {
                    PreventHitDamage();
                    return;
                }
            }
            else
            {
                if (target != GetExplTargetUnit())
                {
                    if (_brimstoneDamage == 0)
                        _brimstoneDamage = MathFunctions.CalculatePct(GetHitDamage(), fab.GetEffect(0).GetBaseAmount());

                    SetHitDamage(_brimstoneDamage);
                }
            }
        }

        private void RoaringBlaze(Unit target, Unit caster)
        {
            if (caster.HasAura(WarlockSpells.ROARING_BLAZE) && GetExplTargetUnit() == target)
            {
                var aur = target.GetAura(WarlockSpells.IMMOLATE_DOT, caster.GetGUID());
                var dmgEff = Global.SpellMgr.GetSpellInfo(WarlockSpells.ROARING_BLASE_DMG_PCT, Difficulty.None)?.GetEffect(0);

                if (aur != null && dmgEff != null)
                {
                    var dmg = GetHitDamage();
                    SetHitDamage(MathFunctions.AddPct(ref dmg, dmgEff.BasePoints));
                }
            }
        }

        public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHitMainTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}
}