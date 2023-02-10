using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 27539 - Obsidian Armor
internal class spell_gen_obsidian_armor : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.Holy, GenericSpellIds.Fire, GenericSpellIds.Nature, GenericSpellIds.Frost, GenericSpellIds.Shadow, GenericSpellIds.Arcane);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo() == null)
			return false;

		if (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()) == SpellSchools.Normal)
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProcEffect, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void OnProcEffect(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		uint spellId;

		switch (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()))
		{
			case SpellSchools.Holy:
				spellId = GenericSpellIds.Holy;

				break;
			case SpellSchools.Fire:
				spellId = GenericSpellIds.Fire;

				break;
			case SpellSchools.Nature:
				spellId = GenericSpellIds.Nature;

				break;
			case SpellSchools.Frost:
				spellId = GenericSpellIds.Frost;

				break;
			case SpellSchools.Shadow:
				spellId = GenericSpellIds.Shadow;

				break;
			case SpellSchools.Arcane:
				spellId = GenericSpellIds.Arcane;

				break;
			default:
				return;
		}

		GetTarget().CastSpell(GetTarget(), spellId, new CastSpellExtraArgs(aurEff));
	}
}