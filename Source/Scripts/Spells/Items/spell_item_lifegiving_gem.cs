using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 23725 - Gift of Life
internal class spell_item_lifegiving_gem : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.GiftOfLife1, ItemSpellIds.GiftOfLife2);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(uint effIndex)
	{
		var caster = GetCaster();
		caster.CastSpell(caster, ItemSpellIds.GiftOfLife1, true);
		caster.CastSpell(caster, ItemSpellIds.GiftOfLife2, true);
	}
}