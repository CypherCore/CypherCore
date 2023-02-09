using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_chicken_cover : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public override bool Validate(SpellInfo spell)
	{
		return Global.ObjectMgr.GetQuestTemplate(QuestIds.ChickenParty) != null && Global.ObjectMgr.GetQuestTemplate(QuestIds.FlownTheCoop) != null && ValidateSpellInfo(ItemSpellIds.ChickenNet, ItemSpellIds.CaptureChickenEscape);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = GetCaster().ToPlayer();
		var target = GetHitUnit();

		if (target)
			if (!target.HasAura(ItemSpellIds.ChickenNet) &&
			    (caster.GetQuestStatus(QuestIds.ChickenParty) == QuestStatus.Incomplete || caster.GetQuestStatus(QuestIds.FlownTheCoop) == QuestStatus.Incomplete))
			{
				caster.CastSpell(caster, ItemSpellIds.CaptureChickenEscape, true);
				target.KillSelf();
			}
	}
}