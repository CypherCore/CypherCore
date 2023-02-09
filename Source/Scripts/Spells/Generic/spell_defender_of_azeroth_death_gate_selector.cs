using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_defender_of_azeroth_death_gate_selector : SpellScript, IHasSpellEffects
{
	private (WorldLocation, uint) OrgrimmarInnLoc = (new WorldLocation(1, 1573.18f, -4441.62f, 16.06f, 1.818284034729003906f), 8618);
	private (WorldLocation, uint) StormwindInnLoc = (new WorldLocation(0, -8868.1f, 675.82f, 97.9f, 5.164778709411621093f), 5148);
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(GenericSpellIds.DeathGateTeleportStormwind, GenericSpellIds.DeathGateTeleportOrgrimmar);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		Player player = GetHitUnit().ToPlayer();

		if (player == null)
			return;

		if (player.GetQuestStatus(QuestIds.DefenderOfAzerothAlliance) == QuestStatus.None &&
		    player.GetQuestStatus(QuestIds.DefenderOfAzerothHorde) == QuestStatus.None)
			return;

		(WorldLocation Loc, uint AreaId) bindLoc = player.GetTeam() == Team.Alliance ? StormwindInnLoc : OrgrimmarInnLoc;
		player.SetHomebind(bindLoc.Loc, bindLoc.AreaId);
		player.SendBindPointUpdate();
		player.SendPlayerBound(player.GetGUID(), bindLoc.AreaId);

		player.CastSpell(player, player.GetTeam() == Team.Alliance ? GenericSpellIds.DeathGateTeleportStormwind : GenericSpellIds.DeathGateTeleportOrgrimmar);
	}
}