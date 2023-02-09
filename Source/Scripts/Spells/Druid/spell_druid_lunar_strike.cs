using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(194153)]
public class spell_druid_lunar_strike : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private struct Spells
	{
		public static readonly uint SPELL_DRUID_LUNAR_STRIKE = 194153;
		public static readonly uint SPELL_DRUID_WARRIOR_OF_ELUNE = 202425;
		public static readonly uint SPELL_DRUID_NATURES_BALANCE = 202430;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MoonfireSpells.SPELL_DRUID_MOONFIRE_DAMAGE, Spells.SPELL_DRUID_WARRIOR_OF_ELUNE, Spells.SPELL_DRUID_LUNAR_STRIKE, Spells.SPELL_DRUID_NATURES_BALANCE);
	}

	private void HandleHitTarget(uint UnnamedParameter)
	{
		var explTarget    = GetExplTargetUnit();
		var currentTarget = GetHitUnit();

		if (explTarget == null || currentTarget == null)
			return;

		if (currentTarget != explTarget)
			SetHitDamage(GetHitDamage() * GetSpellInfo().GetEffect(2).BasePoints / 100);

		if (GetCaster().HasAura(Spells.SPELL_DRUID_NATURES_BALANCE))
		{
			var moonfireDOT = currentTarget.GetAura(MoonfireSpells.SPELL_DRUID_MOONFIRE_DAMAGE, GetCaster().GetGUID());

			if (moonfireDOT != null)
			{
				var duration    = moonfireDOT.GetDuration();
				var newDuration = duration + 6 * Time.InMilliseconds;

				if (newDuration > moonfireDOT.GetMaxDuration())
					moonfireDOT.SetMaxDuration(newDuration);

				moonfireDOT.SetDuration(newDuration);
			}
		}

		if (GetCaster() && RandomHelper.randChance(20) && GetCaster().HasAura(DruidSpells.SPELL_DRU_ECLIPSE))
			GetCaster().CastSpell(null, DruidSpells.SPELL_DRU_SOLAR_EMPOWEREMENT, true);
	}

	private void HandleHit(uint UnnamedParameter)
	{
		var WarriorOfElune = GetCaster().GetAura(Spells.SPELL_DRUID_WARRIOR_OF_ELUNE);

		if (WarriorOfElune != null)
		{
			var amount = WarriorOfElune.GetEffect(0).GetAmount();
			WarriorOfElune.GetEffect(0).SetAmount(amount - 1);

			if (amount == -102)
				GetCaster().RemoveAurasDueToSpell(Spells.SPELL_DRUID_WARRIOR_OF_ELUNE);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHit));
	}
}