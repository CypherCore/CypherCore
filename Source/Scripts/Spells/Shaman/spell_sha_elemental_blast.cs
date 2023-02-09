using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 120588 - Elemental Blast Overload
internal class spell_sha_elemental_blast : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	private readonly uint[] BuffSpells =
	{
		ShamanSpells.ElementalBlastCrit, ShamanSpells.ElementalBlastHaste, ShamanSpells.ElementalBlastMastery
	};

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.ElementalBlastCrit, ShamanSpells.ElementalBlastHaste, ShamanSpells.ElementalBlastMastery, ShamanSpells.MaelstromController) && Global.SpellMgr.GetSpellInfo(ShamanSpells.MaelstromController, Difficulty.None).GetEffects().Count > 10;
	}

	public void AfterCast()
	{
		var caster  = GetCaster();
		var spellId = BuffSpells.SelectRandomElementByWeight(buffSpellId => { return !caster.HasAura(buffSpellId) ? 1.0f : 0.0f; });

		GetCaster().CastSpell(GetCaster(), spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEnergize, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleEnergize(int effIndex)
	{
		var energizeAmount = GetCaster().GetAuraEffect(ShamanSpells.MaelstromController, GetSpellInfo().Id == ShamanSpells.ElementalBlast ? 9 : 10);

		if (energizeAmount != null)
			GetCaster()
				.CastSpell(GetCaster(),
				           ShamanSpells.ElementalBlastEnergize,
				           new CastSpellExtraArgs(energizeAmount)
					           .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.GetAmount()));
	}
}