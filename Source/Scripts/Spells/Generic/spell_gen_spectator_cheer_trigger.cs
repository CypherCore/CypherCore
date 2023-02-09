using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_spectator_cheer_trigger : SpellScript, IHasSpellEffects
{
	private static readonly Emote[] EmoteArray =
	{
		Emote.OneshotCheer, Emote.OneshotExclamation, Emote.OneshotApplaud
	};

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		GetCaster().HandleEmoteCommand(EmoteArray.SelectRandom());
	}
}