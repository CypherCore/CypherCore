using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 50842 - Blood Boil
internal class spell_dk_blood_boil : SpellScript, ISpellOnHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.BloodPlague);
	}

	public void OnHit()
	{
		GetCaster().CastSpell(GetHitUnit(), DeathKnightSpells.BloodPlague, true);
	}
}