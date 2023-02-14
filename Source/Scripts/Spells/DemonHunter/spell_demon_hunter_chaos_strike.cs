// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(new uint[]
             {
	             162794, 201427
             })]
public class spell_demon_hunter_chaos_strike : SpellScript, ISpellBeforeCast
{
	public void BeforeCast()
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (caster == null || target == null)
			return;

		// Chaos Strike and Annihilation have a mainhand and an offhand spell, but the crit chance should be the same.
		var criticalChances = caster.GetUnitCriticalChanceAgainst(WeaponAttackType.BaseAttack, target);
		caster.VariableStorage.Set("Spells.ChaosStrikeCrit", RandomHelper.randChance(criticalChances));
		caster.CastSpell(DemonHunterSpells.SPELL_DH_CHAOS_STRIKE_PROC, true);
	}
}