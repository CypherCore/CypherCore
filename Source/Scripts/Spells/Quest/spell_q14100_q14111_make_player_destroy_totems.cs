using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 66744 - Make Player Destroy Totems
internal class spell_q14100_q14111_make_player_destroy_totems : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(QuestSpellIds.TotemOfTheEarthenRing);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(uint effIndex)
	{
		var player = GetHitPlayer();

		if (player)
			player.CastSpell(player, QuestSpellIds.TotemOfTheEarthenRing, new CastSpellExtraArgs(TriggerCastFlags.FullMask)); // ignore reagent cost, consumed by quest
	}
}