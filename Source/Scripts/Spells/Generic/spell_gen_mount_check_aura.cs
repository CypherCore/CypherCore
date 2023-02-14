// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 147066 - (Serverside/Non-DB2) Generic - Mount Check Aura
internal class spell_gen_mount_check_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void OnPeriodic(AuraEffect aurEff)
	{
		var  target         = GetTarget();
		uint mountDisplayId = 0;

		var tempSummon = target.ToTempSummon();

		if (tempSummon == null)
			return;

		var summoner = tempSummon.GetSummoner()?.ToPlayer();

		if (summoner == null)
			return;

		if (summoner.IsMounted() &&
		    (!summoner.IsInCombat() || summoner.IsFlying()))
		{
			var summonedData = Global.ObjectMgr.GetCreatureSummonedData(tempSummon.GetEntry());

			if (summonedData != null)
			{
				if (summoner.IsFlying() &&
				    summonedData.FlyingMountDisplayID.HasValue)
					mountDisplayId = summonedData.FlyingMountDisplayID.Value;
				else if (summonedData.GroundMountDisplayID.HasValue)
					mountDisplayId = summonedData.GroundMountDisplayID.Value;
			}
		}

		if (mountDisplayId != target.GetMountDisplayId())
			target.SetMountDisplayId(mountDisplayId);
	}
}