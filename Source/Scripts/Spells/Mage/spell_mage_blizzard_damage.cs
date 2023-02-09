using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 190357 - Blizzard (Damage)
internal class spell_mage_blizzard_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.BlizzardSlow);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleSlow, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleSlow(int effIndex)
	{
		GetCaster().CastSpell(GetHitUnit(), MageSpells.BlizzardSlow, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
	}
}