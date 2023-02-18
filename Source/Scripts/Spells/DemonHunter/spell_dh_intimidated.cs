// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206891)]
public class spell_dh_intimidated : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();


	private readonly List<ObjectGuid> _uniqueTargets = new();

	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var attacker  = eventInfo.GetActor();
		var auraOwner = GetAura().GetOwner();

		if (attacker == null || auraOwner == null)
			return;

		if (attacker == GetCaster())
		{
			RefreshDuration();

			return;
		}

		if (_uniqueTargets.Count >= 4 || !auraOwner.ToUnit())
			return;

		if (_uniqueTargets.Contains(attacker.GetGUID()))
		{
			attacker.CastSpell(auraOwner.ToUnit(), GetSpellInfo().Id, true);
			_uniqueTargets.Add(attacker.GetGUID());
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ModDamagePercentTaken, AuraScriptHookType.EffectProc));
	}
}