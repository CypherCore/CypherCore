using System.Collections.Generic;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 274738 - Ancestral Call (Mag'har Orc Racial)
internal class spell_gen_ancestral_call : SpellScript, ISpellOnCast
{
	private static readonly uint[] AncestralCallBuffs =
	{
		GenericSpellIds.RictusOfTheLaughingSkull, GenericSpellIds.ZealOfTheBurningBlade, GenericSpellIds.FerocityOfTheFrostwolf, GenericSpellIds.MightOfTheBlackrock
	};

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(GenericSpellIds.RictusOfTheLaughingSkull, GenericSpellIds.ZealOfTheBurningBlade, GenericSpellIds.FerocityOfTheFrostwolf, GenericSpellIds.MightOfTheBlackrock);
	}

	public void OnCast()
	{
		var caster  = GetCaster();
		var spellId = AncestralCallBuffs.SelectRandom();

		caster.CastSpell(caster, spellId, true);
	}
}