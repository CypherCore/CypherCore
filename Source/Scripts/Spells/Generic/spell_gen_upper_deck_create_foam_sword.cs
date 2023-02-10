using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_upper_deck_create_foam_sword : SpellScript, IHasSpellEffects
{
	//                       green  pink   blue   red    yellow
	private static readonly uint[] itemId =
	{
		45061, 45176, 45177, 45178, 45179
	};

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		var player = GetHitPlayer();

		if (player)
		{
			// player can only have one of these items
			for (byte i = 0; i < 5; ++i)
				if (player.HasItemCount(itemId[i], 1, true))
					return;

			CreateItem(itemId[RandomHelper.URand(0, 4)], ItemContext.None);
		}
	}
}