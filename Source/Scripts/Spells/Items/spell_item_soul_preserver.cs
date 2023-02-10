using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_soul_preserver : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.SoulPreserverDruid, ItemSpellIds.SoulPreserverPaladin, ItemSpellIds.SoulPreserverPriest, ItemSpellIds.SoulPreserverShaman);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var caster = eventInfo.GetActor();

		switch (caster.GetClass())
		{
			case Class.Druid:
				caster.CastSpell(caster, ItemSpellIds.SoulPreserverDruid, new CastSpellExtraArgs(aurEff));

				break;
			case Class.Paladin:
				caster.CastSpell(caster, ItemSpellIds.SoulPreserverPaladin, new CastSpellExtraArgs(aurEff));

				break;
			case Class.Priest:
				caster.CastSpell(caster, ItemSpellIds.SoulPreserverPriest, new CastSpellExtraArgs(aurEff));

				break;
			case Class.Shaman:
				caster.CastSpell(caster, ItemSpellIds.SoulPreserverShaman, new CastSpellExtraArgs(aurEff));

				break;
			default:
				break;
		}
	}
}