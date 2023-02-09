using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 57345 - Darkmoon Card: Greatness
internal class spell_item_darkmoon_card_greatness : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.DarkmoonCardStrenght, ItemSpellIds.DarkmoonCardAgility, ItemSpellIds.DarkmoonCardIntellect, ItemSpellIds.DarkmoonCardVersatility);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var caster = eventInfo.GetActor();
		var str    = caster.GetStat(Stats.Strength);
		var agi    = caster.GetStat(Stats.Agility);
		var intl   = caster.GetStat(Stats.Intellect);
		var vers   = 0.0f; // caster.GetStat(STAT_VERSATILITY);
		var stat   = 0.0f;

		var spellTrigger = ItemSpellIds.DarkmoonCardStrenght;

		if (str > stat)
		{
			spellTrigger = ItemSpellIds.DarkmoonCardStrenght;
			stat         = str;
		}

		if (agi > stat)
		{
			spellTrigger = ItemSpellIds.DarkmoonCardAgility;
			stat         = agi;
		}

		if (intl > stat)
		{
			spellTrigger = ItemSpellIds.DarkmoonCardIntellect;
			stat         = intl;
		}

		if (vers > stat)
		{
			spellTrigger = ItemSpellIds.DarkmoonCardVersatility;
			stat         = vers;
		}

		caster.CastSpell(caster, spellTrigger, new CastSpellExtraArgs(aurEff));
	}
}