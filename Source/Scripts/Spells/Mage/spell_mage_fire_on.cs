// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(205029)]
public class spell_mage_fire_on : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MageSpells.SPELL_MAGE_FIRE_ON, MageSpells.SPELL_MAGE_FIRE_BLAST);
	}

	private void HandleDummy(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null || caster.GetTypeId() != TypeId.Player)
			return;

		caster.ToPlayer().GetSpellHistory().ResetCharges(Global.SpellMgr.GetSpellInfo(MageSpells.FireBlast, Difficulty.None).ChargeCategoryId);
		// caster->ToPlayer()->GetSpellHistory()->ResetCharges(Global.SpellMgr->GetSpellInfo(SPELL_MAGE_FIRE_BLAST, Difficulty.None)->ChargeCategoryId);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}