using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 186263 - Shadow Mend
internal class spell_pri_shadow_mend : SpellScript, ISpellAfterHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.Atonement, PriestSpells.AtonementTriggered, PriestSpells.Trinity, PriestSpells.MasochismTalent, PriestSpells.MasochismPeriodicHeal, PriestSpells.ShadowMendPeriodicDummy);
	}

	public void AfterHit()
	{
		var target = GetHitUnit();

		if (target != null)
		{
			var caster = GetCaster();

			var periodicAmount            = GetHitHeal() / 20;
			var damageForAuraRemoveAmount = periodicAmount * 10;

			if (caster.HasAura(PriestSpells.Atonement) &&
			    !caster.HasAura(PriestSpells.Trinity))
				caster.CastSpell(target, PriestSpells.AtonementTriggered, new CastSpellExtraArgs(GetSpell()));

			// Handle Masochism talent
			if (caster.HasAura(PriestSpells.MasochismTalent) &&
			    caster.GetGUID() == target.GetGUID())
			{
				caster.CastSpell(caster, PriestSpells.MasochismPeriodicHeal, new CastSpellExtraArgs(GetSpell()).AddSpellMod(SpellValueMod.BasePoint0, periodicAmount));
			}
			else if (target.IsInCombat() &&
			         periodicAmount != 0)
			{
				CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
				args.SetTriggeringSpell(GetSpell());
				args.AddSpellMod(SpellValueMod.BasePoint0, periodicAmount);
				args.AddSpellMod(SpellValueMod.BasePoint1, damageForAuraRemoveAmount);
				caster.CastSpell(target, PriestSpells.ShadowMendPeriodicDummy, args);
			}
		}
	}
}