using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 53099, 57896, 58418, 58420, 59064, 59065, 59439, 60900, 60940
internal class spell_quest_portal_with_condition : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 1 && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue()) && Global.ObjectMgr.GetQuestTemplate((uint)spellInfo.GetEffect(1).CalcValue()) != null;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(int effIndex)
	{
		var target = GetHitPlayer();

		if (target == null)
			return;

		var spellId = (uint)GetEffectInfo().CalcValue();
		var questId = (uint)GetEffectInfo(1).CalcValue();

		// This probably should be a way to throw error in SpellCastResult
		if (target.IsActiveQuest(questId))
			target.CastSpell(target, spellId, true);
	}
}