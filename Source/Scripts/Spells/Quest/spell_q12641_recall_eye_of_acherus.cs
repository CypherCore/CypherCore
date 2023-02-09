using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 52694 - Recall Eye of Acherus
internal class spell_q12641_recall_eye_of_acherus : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		var player = GetCaster().GetCharmerOrOwner().ToPlayer();

		if (player)
		{
			player.StopCastingCharm();
			player.StopCastingBindSight();
			player.RemoveAura(QuestSpellIds.TheEyeOfAcherus);
		}
	}
}