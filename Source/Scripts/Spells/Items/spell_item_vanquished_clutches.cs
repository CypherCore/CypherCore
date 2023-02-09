using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_vanquished_clutches : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.Crusher, ItemSpellIds.Constrictor, ItemSpellIds.Corruptor);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(int effIndex)
	{
		var spellId = RandomHelper.RAND(ItemSpellIds.Crusher, ItemSpellIds.Constrictor, ItemSpellIds.Corruptor);
		var caster  = GetCaster();
		caster.CastSpell(caster, spellId, true);
	}
}