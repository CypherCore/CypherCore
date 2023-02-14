// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_heartpierce", ItemSpellIds.InvigorationEnergy, ItemSpellIds.InvigorationMana, ItemSpellIds.InvigorationRage, ItemSpellIds.InvigorationRp)]
[Script("spell_item_heartpierce_hero", ItemSpellIds.InvigorationEnergyHero, ItemSpellIds.InvigorationManaHero, ItemSpellIds.InvigorationRageHero, ItemSpellIds.InvigorationRpHero)]
internal class spell_item_heartpierce : AuraScript, IHasAuraEffects
{
	private readonly uint _energySpellId;
	private readonly uint _manaSpellId;
	private readonly uint _rageSpellId;
	private readonly uint _rpSpellId;

	public spell_item_heartpierce(uint energySpellId, uint manaSpellId, uint rageSpellId, uint rpSpellId)
	{
		_energySpellId = energySpellId;
		_manaSpellId   = manaSpellId;
		_rageSpellId   = rageSpellId;
		_rpSpellId     = rpSpellId;
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(_energySpellId, _manaSpellId, _rageSpellId, _rpSpellId);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = eventInfo.GetActor();

		uint spellId;

		switch (caster.GetPowerType())
		{
			case PowerType.Mana:
				spellId = _manaSpellId;

				break;
			case PowerType.Energy:
				spellId = _energySpellId;

				break;
			case PowerType.Rage:
				spellId = _rageSpellId;

				break;
			// Death Knights can't use daggers, but oh well
			case PowerType.RunicPower:
				spellId = _rpSpellId;

				break;
			default:
				return;
		}

		caster.CastSpell((Unit)null, spellId, new CastSpellExtraArgs(aurEff));
	}
}