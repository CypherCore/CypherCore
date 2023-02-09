using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_flareAI : AreaTriggerAI
{
	public at_hun_flareAI(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnCreate()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (caster.GetTypeId() != TypeId.Player)
			return;

		var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromSeconds(200));

		if (tempSumm == null)
		{
			tempSumm.SetFaction(caster.GetFaction());
			tempSumm.SetSummonerGUID(caster.GetGUID());
			PhasingHandler.InheritPhaseShift(tempSumm, caster);
			caster.CastSpell(tempSumm, HunterSpells.SPELL_HUNTER_FLARE_EFFECT, true);
		}
	}
}