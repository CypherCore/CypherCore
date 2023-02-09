using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_demon_broiled_surprise : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public override bool Validate(SpellInfo spell)
	{
		return Global.ObjectMgr.GetCreatureTemplate(CreatureIds.AbyssalFlamebringer) != null && Global.ObjectMgr.GetQuestTemplate(QuestIds.SuperHotStew) != null && ValidateSpellInfo(ItemSpellIds.CreateDemonBroiledSurprise);
	}

	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public SpellCastResult CheckCast()
	{
		var player = GetCaster().ToPlayer();

		if (player.GetQuestStatus(QuestIds.SuperHotStew) != QuestStatus.Incomplete)
			return SpellCastResult.CantDoThatRightNow;

		var creature = player.FindNearestCreature(CreatureIds.AbyssalFlamebringer, 10, false);

		if (creature)
			if (creature.IsDead())
				return SpellCastResult.SpellCastOk;

		return SpellCastResult.NotHere;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(uint effIndex)
	{
		var player = GetCaster();
		player.CastSpell(player, ItemSpellIds.CreateDemonBroiledSurprise, false);
	}
}