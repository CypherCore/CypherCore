using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 40056 Knockdown Fel Cannon: Choose Loc
internal class spell_q11010_q11102_q11023_choose_loc : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(uint effIndex)
	{
		var caster = GetCaster();
		// Check for player that is in 65 y range
		List<Unit>                  playerList = new();
		AnyPlayerInObjectRangeCheck checker    = new(caster, 65.0f);
		PlayerListSearcher          searcher   = new(caster, playerList, checker);
		Cell.VisitWorldObjects(caster, searcher, 65.0f);

		foreach (Player player in playerList)
			// Check if found player Target is on fly Mount or using flying form
			if (player.HasAuraType(AuraType.Fly) ||
			    player.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
				// Summom Fel Cannon (bunny version) at found player
				caster.SummonCreature(CreatureIds.FelCannon2, player.GetPositionX(), player.GetPositionY(), player.GetPositionZ());
	}
}