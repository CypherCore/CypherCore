using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 46203 - Goblin Weather Machine
internal class spell_item_goblin_weather_machine : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		var target = GetHitUnit();

		var spellId = RandomHelper.RAND(ItemSpellIds.PersonalizedWeather1, ItemSpellIds.PersonalizedWeather2, ItemSpellIds.PersonalizedWeather3, ItemSpellIds.PersonalizedWeather4);
		target.CastSpell(target, spellId, new CastSpellExtraArgs(GetSpell()));
	}
}