// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206491)]
public class spell_dh_nemesis : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void HandleAfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (GetTargetApplication() == null)
			return;

		if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
			return;

		var target = GetTargetApplication().GetTarget();
		var type   = target.GetCreatureType();
		var dur    = GetTargetApplication().GetBase().GetDuration();
		var caster = GetAura().GetCaster();

		if (caster == null || target == null)
			return;

		uint spellId = 0;

		switch (type)
		{
			case CreatureType.Aberration:
				spellId = NemesisSpells.NEMESIS_ABERRATION;

				break;
			case CreatureType.Beast:
				spellId = NemesisSpells.NEMESIS_BEASTS;

				break;
			case CreatureType.Critter:
				spellId = NemesisSpells.NEMESIS_CRITTERS;

				break;
			case CreatureType.Demon:
				spellId = NemesisSpells.NEMESIS_DEMONS;

				break;
			case CreatureType.Dragonkin:
				spellId = NemesisSpells.NEMESIS_DRAGONKIN;

				break;
			case CreatureType.Elemental:
				spellId = NemesisSpells.NEMESIS_ELEMENTAL;

				break;
			case CreatureType.Giant:
				spellId = NemesisSpells.NEMESIS_GIANTS;

				break;
			case CreatureType.Humanoid:
				spellId = NemesisSpells.NEMESIS_HUMANOID;

				break;
			case CreatureType.Mechanical:
				spellId = NemesisSpells.NEMESIS_MECHANICAL;

				break;
			case CreatureType.Undead:
				spellId = NemesisSpells.NEMESIS_UNDEAD;

				break;
			default:
				break;
		}

		if (spellId != 0)
		{
			var aur = caster.AddAura(spellId, caster);

			if (aur != null)
				aur.SetDuration(dur);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 0, AuraType.ModSchoolMaskDamageFromCaster, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}