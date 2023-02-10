using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 45284 - Lightning Bolt Overload
internal class spell_sha_lightning_bolt_overload : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.LightningBoltOverloadEnergize, ShamanSpells.MaelstromController) && Global.SpellMgr.GetSpellInfo(ShamanSpells.MaelstromController, Difficulty.None).GetEffects().Count > 1;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
	}

	private void HandleScript(uint effIndex)
	{
		var energizeAmount = GetCaster().GetAuraEffect(ShamanSpells.MaelstromController, 1);

		if (energizeAmount != null)
			GetCaster()
				.CastSpell(GetCaster(),
				           ShamanSpells.LightningBoltOverloadEnergize,
				           new CastSpellExtraArgs(energizeAmount)
					           .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.GetAmount()));
	}
}