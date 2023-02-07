using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 116 - Frostbolt
internal class spell_mage_frostbolt : SpellScript, ISpellOnHit
{
	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(MageSpells.Chilled);
	}

	public void OnHit()
	{
		Unit target = GetHitUnit();

		if (target != null)
			GetCaster().CastSpell(target, MageSpells.Chilled, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
	}
}