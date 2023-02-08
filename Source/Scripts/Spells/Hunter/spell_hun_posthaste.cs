using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 781 - Disengage
internal class spell_hun_posthaste : SpellScript, ISpellAfterCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.PosthasteTalent, HunterSpells.PosthasteIncreaseSpeed);
	}

	public void AfterCast()
	{
		if (GetCaster().HasAura(HunterSpells.PosthasteTalent))
		{
			GetCaster().RemoveMovementImpairingAuras(true);
			GetCaster().CastSpell(GetCaster(), HunterSpells.PosthasteIncreaseSpeed, GetSpell());
		}
	}
}