using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 33395 Water Elemental's Freeze
internal class spell_mage_water_elemental_freeze : SpellScript, ISpellAfterHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.FingersOfFrost);
	}

	public void AfterHit()
	{
		Unit owner = GetCaster().GetOwner();

		if (!owner)
			return;

		owner.CastSpell(owner, MageSpells.FingersOfFrost, true);
	}
}