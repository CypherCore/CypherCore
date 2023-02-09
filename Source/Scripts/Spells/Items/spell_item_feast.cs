using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_great_feast", TextIds.GreatFeast)]
[Script("spell_item_fish_feast", TextIds.TextFishFeast)]
[Script("spell_item_gigantic_feast", TextIds.TextGiganticFeast)]
[Script("spell_item_small_feast", TextIds.SmallFeast)]
[Script("spell_item_bountiful_feast", TextIds.BountifulFeast)]
internal class spell_item_feast : SpellScript, IHasSpellEffects
{
	private readonly uint _text;

	public spell_item_feast(uint text)
	{
		_text = text;
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return CliDB.BroadcastTextStorage.ContainsKey(_text);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
	}

	private void HandleScript(uint effIndex)
	{
		Unit caster = GetCaster();
		caster.TextEmote(_text, caster, false);
	}
}