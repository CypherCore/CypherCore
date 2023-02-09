using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 4338 Plant Alliance Battle Standard
internal class spell_q13280_13283_plant_battle_standard : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(int effIndex)
	{
		var caster           = GetCaster();
		var target           = GetHitUnit();
		var triggeredSpellID = QuestSpellIds.AllianceBattleStandardState;

		caster.HandleEmoteCommand(Emote.OneshotRoar);

		if (caster.IsVehicle())
		{
			var player = caster.GetVehicleKit().GetPassenger(0);

			if (player)
				player.ToPlayer().KilledMonsterCredit(CreatureIds.KingOfTheMountaintKc);
		}

		if (GetSpellInfo().Id == QuestSpellIds.PlantHordeBattleStandard)
			triggeredSpellID = QuestSpellIds.HordeBattleStandardState;

		target.RemoveAllAuras();
		target.CastSpell(target, triggeredSpellID, true);
	}
}