using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_death_choice : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.DeathChoiceNormalStrength, ItemSpellIds.DeathChoiceNormalAgility, ItemSpellIds.DeathChoiceHeroicStrength, ItemSpellIds.DeathChoiceHeroicAgility);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		Unit  caster = eventInfo.GetActor();
		float str    = caster.GetStat(Stats.Strength);
		float agi    = caster.GetStat(Stats.Agility);

		switch (aurEff.GetId())
		{
			case ItemSpellIds.DeathChoiceNormalAura:
			{
				if (str > agi)
					caster.CastSpell(caster, ItemSpellIds.DeathChoiceNormalStrength, new CastSpellExtraArgs(aurEff));
				else
					caster.CastSpell(caster, ItemSpellIds.DeathChoiceNormalAgility, new CastSpellExtraArgs(aurEff));

				break;
			}
			case ItemSpellIds.DeathChoiceHeroicAura:
			{
				if (str > agi)
					caster.CastSpell(caster, ItemSpellIds.DeathChoiceHeroicStrength, new CastSpellExtraArgs(aurEff));
				else
					caster.CastSpell(caster, ItemSpellIds.DeathChoiceHeroicAgility, new CastSpellExtraArgs(aurEff));

				break;
			}
			default:
				break;
		}
	}
}