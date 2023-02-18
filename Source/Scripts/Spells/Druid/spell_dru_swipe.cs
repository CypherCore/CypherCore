// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(106785)]
public class spell_dru_swipe : SpellScript, IHasSpellEffects
{
	private bool _awardComboPoint = true;

	public List<ISpellEffect> SpellEffects { get; } = new();


	private void HandleOnHit(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		var damage      = GetHitDamage();
		var casterLevel = caster.GetLevelForTarget(caster);

		// This prevent awarding multiple Combo Points when multiple targets hit with Swipe AoE
		if (_awardComboPoint)
			// Awards the caster 1 Combo Point (get value from the spell data)
			caster.ModifyPower(PowerType.ComboPoints, Global.SpellMgr.GetSpellInfo(DruidSpells.SWIPE_CAT, Difficulty.None).GetEffect(0).BasePoints);

		// If caster is level >= 44 and the target is bleeding, deals 20% increased damage (get value from the spell data)
		if ((casterLevel >= 44) && target.HasAuraState(AuraStateType.Bleed))
			MathFunctions.AddPct(ref damage, Global.SpellMgr.GetSpellInfo(DruidSpells.SWIPE_CAT, Difficulty.None).GetEffect(1).BasePoints);

		SetHitDamage(damage);
		_awardComboPoint = false;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}