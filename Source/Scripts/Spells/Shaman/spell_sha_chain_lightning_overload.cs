// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 45297 - Chain Lightning Overload
[SpellScript(45297)]
internal class spell_sha_chain_lightning_overload : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.ChainLightningOverloadEnergize, ShamanSpells.MaelstromController) && Global.SpellMgr.GetSpellInfo(ShamanSpells.MaelstromController, Difficulty.None).GetEffects().Count > 5;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
	}

	private void HandleScript(uint effIndex)
	{
		var energizeAmount = GetCaster().GetAuraEffect(ShamanSpells.MaelstromController, 5);

		if (energizeAmount != null)
			GetCaster()
				.CastSpell(GetCaster(),
				           ShamanSpells.ChainLightningOverloadEnergize,
				           new CastSpellExtraArgs(energizeAmount)
					           .AddSpellMod(SpellValueMod.BasePoint0, (int)(energizeAmount.GetAmount() * GetUnitTargetCountForEffect(0))));
	}
}