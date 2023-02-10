using Framework.Constants;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_running_wild : SpellScript
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.AlteredForm);
	}

	public override bool Load()
	{
		// Definitely not a good thing, but currently the only way to do something at cast start
		// Should be replaced as soon as possible with a new hook: BeforeCastStart
		GetCaster().CastSpell(GetCaster(), GenericSpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

		return false;
	}

	public override void Register()
	{
	}
}