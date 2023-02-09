using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_defender_of_azeroth_speak_with_mograine : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		if (!GetCaster())
			return;

		Player player = GetCaster().ToPlayer();

		if (player == null)
			return;

		Creature nazgrim = GetHitUnit().FindNearestCreature(CreatureIds.Nazgrim, 10.0f);

		nazgrim?.HandleEmoteCommand(Emote.OneshotPoint, player);

		Creature trollbane = GetHitUnit().FindNearestCreature(CreatureIds.Trollbane, 10.0f);

		trollbane?.HandleEmoteCommand(Emote.OneshotPoint, player);

		Creature whitemane = GetHitUnit().FindNearestCreature(CreatureIds.Whitemane, 10.0f);

		whitemane?.HandleEmoteCommand(Emote.OneshotPoint, player);

		// @TODO: spawntracking - show death gate for casting player
	}
}