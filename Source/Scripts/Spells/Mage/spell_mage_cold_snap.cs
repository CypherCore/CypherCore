using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 235219 - Cold Snap
internal class spell_mage_cold_snap : SpellScript, IHasSpellEffects
{
	private static readonly uint[] SpellsToReset =
	{
		MageSpells.ConeOfCold, MageSpells.IceBarrier, MageSpells.IceBlock
	};

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(SpellsToReset) && ValidateSpellInfo(MageSpells.FrostNova);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(uint effIndex)
	{
		foreach (uint spellId in SpellsToReset)
			GetCaster().GetSpellHistory().ResetCooldown(spellId, true);

		GetCaster().GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(MageSpells.FrostNova, GetCastDifficulty()).ChargeCategoryId);
	}
}