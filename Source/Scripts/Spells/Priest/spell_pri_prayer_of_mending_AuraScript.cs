using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 41635 - Prayer of Mending (Aura) - SPELL_PRIEST_PRAYER_OF_MENDING_AURA
internal class spell_pri_prayer_of_mending_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.PrayerOfMendingHeal, PriestSpells.PrayerOfMendingJump);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleHeal, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleHeal(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		// Caster: player (priest) that cast the Prayer of Mending
		// Target: player that currently has Prayer of Mending aura on him
		var target = GetTarget();
		var caster = GetCaster();

		if (caster != null)
		{
			// Cast the spell to heal the owner
			caster.CastSpell(target, PriestSpells.PrayerOfMendingHeal, new CastSpellExtraArgs(aurEff));

			// Only cast Jump if stack is higher than 0
			int stackAmount = GetStackAmount();

			if (stackAmount > 1)
			{
				CastSpellExtraArgs args = new(aurEff);
				args.OriginalCaster = caster.GetGUID();
				args.AddSpellMod(SpellValueMod.BasePoint0, stackAmount - 1);
				target.CastSpell(target, PriestSpells.PrayerOfMendingJump, args);
			}

			Remove();
		}
	}
}