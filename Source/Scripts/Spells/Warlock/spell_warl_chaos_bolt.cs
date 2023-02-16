// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	[SpellScript(116858)] // 116858 - Chaos Bolt
	internal class spell_warl_chaos_bolt : SpellScript, IHasSpellEffects, ISpellCalcCritChance, ISpellOnHit
	{
		public override bool Load()
		{
			return GetCaster().IsPlayer();
		}

		public void CalcCritChance(Unit victim, ref float critChance)
		{
			critChance = 100.0f;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}

		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleDummy(uint effIndex)
		{
			SetHitDamage(GetHitDamage() + MathFunctions.CalculatePct(GetHitDamage(), GetCaster().ToPlayer().m_activePlayerData.SpellCritPercentage));
		}

		public void OnHit()
        {
            var p = GetCaster().ToPlayer();

            if (p == null)
                return;

            var target = GetExplTargetUnit();

            if (target == null)
                return;

            InternalCombustion(p, target);
            CryHavoc(p, target);
        }

        private static void CryHavoc(Player p, Unit target)
        {
            var cryHavoc = p.GetAura(WarlockSpells.CRY_HAVOC);
            if (cryHavoc == null)
                return;

            var havoc = target.GetAura(WarlockSpells.HAVOC);

            if (havoc == null) 
                return;

            var dmg = (uint)(cryHavoc.GetEffect(0).GetAmount() * .5); // 50%

            var spellInfo = cryHavoc.GetSpellInfo();

            if (spellInfo != null)
            {
                var spell = new SpellNonMeleeDamage(p, target, spellInfo, new SpellCastVisual(spellInfo.GetSpellVisual(p), 0), SpellSchoolMask.Shadow);
                spell.damage = dmg;
                spell.cleanDamage = spell.damage;
                p.DealSpellDamage(spell, false);
                p.SendSpellNonMeleeDamageLog(spell);
            }
        }

        private static void InternalCombustion(Player p, Unit target)
        {
            var internalCombustion = p.GetAura(WarlockSpells.INTERNAL_COMBUSTION_TALENT_AURA);

            if (internalCombustion == null)
                return;

            var immolationAura = target.GetAura(WarlockSpells.IMMOLATE_DOT);

            if (immolationAura == null)
                return;

            var estAmount = immolationAura.GetEffect(0).GetEstimatedAmount();

            if (!estAmount.HasValue)
                return;

            var dmgPerTick = (int)estAmount.Value;

            var duration = immolationAura.GetDuration();
            var modDur = internalCombustion.GetEffect(0).m_baseAmount * Time.InMilliseconds;

            if (modDur <= 0)
                modDur = Time.InMilliseconds;

            if (duration <= 0)
                duration = Time.InMilliseconds;

            var diff = duration - modDur;

            if (diff > 0)
            {
                immolationAura.ModDuration(-modDur);
                p.CastSpell(target, WarlockSpells.INTERNAL_COMBUSTION_DMG, Math.Max(modDur / Time.InMilliseconds, 1) * dmgPerTick, true);
            }
            else
            {
                immolationAura.ModDuration(-duration);
                p.CastSpell(target, WarlockSpells.INTERNAL_COMBUSTION_DMG, Math.Max(duration / Time.InMilliseconds, 1) * dmgPerTick, true);
            }
        }
    }
}
            
        