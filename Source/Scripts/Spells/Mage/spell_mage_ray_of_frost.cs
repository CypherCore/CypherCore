using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 205021 - Ray of Frost
internal class spell_mage_ray_of_frost : SpellScript, ISpellOnHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.RayOfFrostFingersOfFrost);
	}

	public void OnHit()
	{
		var caster = GetCaster();

		caster?.CastSpell(caster, MageSpells.RayOfFrostFingersOfFrost, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
	}
}