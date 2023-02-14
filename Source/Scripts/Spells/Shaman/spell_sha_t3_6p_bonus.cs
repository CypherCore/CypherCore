// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 28823 - Totemic Power
[SpellScript(28823)]
internal class spell_sha_t3_6p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.TotemicPowerArmor, ShamanSpells.TotemicPowerAttackPower, ShamanSpells.TotemicPowerSpellPower, ShamanSpells.TotemicPowerMp5);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		uint spellId;
		var  caster = eventInfo.GetActor();
		var  target = eventInfo.GetProcTarget();

		switch (target.GetClass())
		{
			case Class.Paladin:
			case Class.Priest:
			case Class.Shaman:
			case Class.Druid:
				spellId = ShamanSpells.TotemicPowerMp5;

				break;
			case Class.Mage:
			case Class.Warlock:
				spellId = ShamanSpells.TotemicPowerSpellPower;

				break;
			case Class.Hunter:
			case Class.Rogue:
				spellId = ShamanSpells.TotemicPowerAttackPower;

				break;
			case Class.Warrior:
				spellId = ShamanSpells.TotemicPowerArmor;

				break;
			default:
				return;
		}

		caster.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
	}
}