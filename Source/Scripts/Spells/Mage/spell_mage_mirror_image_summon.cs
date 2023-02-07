using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(55342)]
public class spell_mage_mirror_image_summon : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleDummy(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			caster.CastSpell(caster, MageSpells.SPELL_MAGE_MIRROR_IMAGE_LEFT, true);
			caster.CastSpell(caster, MageSpells.SPELL_MAGE_MIRROR_IMAGE_FRONT, true);
			caster.CastSpell(caster, MageSpells.SPELL_MAGE_MIRROR_IMAGE_RIGHT, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}