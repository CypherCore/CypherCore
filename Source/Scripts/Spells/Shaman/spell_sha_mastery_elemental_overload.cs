// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 168534 - Mastery: Elemental Overload (passive)
[SpellScript(168534)]
internal class spell_sha_mastery_elemental_overload : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.LightningBolt,
		                         ShamanSpells.LightningBoltOverload,
		                         ShamanSpells.ElementalBlast,
		                         ShamanSpells.ElementalBlastOverload,
		                         ShamanSpells.Icefury,
		                         ShamanSpells.IcefuryOverload,
		                         ShamanSpells.LavaBurst,
		                         ShamanSpells.LavaBurstOverload,
		                         ShamanSpells.ChainLightning,
		                         ShamanSpells.ChainLightningOverload,
		                         ShamanSpells.LavaBeam,
		                         ShamanSpells.LavaBeamOverload,
		                         ShamanSpells.Stormkeeper);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var spellInfo = eventInfo.GetSpellInfo();

		if (spellInfo == null ||
		    !eventInfo.GetProcSpell())
			return false;

		if (GetTriggeredSpellId(spellInfo.Id) == 0)
			return false;

		double chance = aurEff.GetAmount(); // Mastery % amount

		if (spellInfo.Id == ShamanSpells.ChainLightning)
			chance /= 3.0f;

		var stormkeeper = eventInfo.GetActor().GetAura(ShamanSpells.Stormkeeper);

		if (stormkeeper != null)
			if (eventInfo.GetProcSpell().m_appliedMods.Contains(stormkeeper))
				chance = 100.0f;

		return RandomHelper.randChance(chance);
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
	{
		PreventDefaultAction();

		var caster = procInfo.GetActor();

		var targets         = new CastSpellTargetArg(procInfo.GetProcTarget());
		var overloadSpellId = GetTriggeredSpellId(procInfo.GetSpellInfo().Id);
		var originalCastId  = procInfo.GetProcSpell().m_castId;

		caster.m_Events.AddEventAtOffset(() =>
		                                 {
			                                 if (targets.Targets == null)
				                                 return;

			                                 targets.Targets.Update(caster);

			                                 CastSpellExtraArgs args = new();
			                                 args.OriginalCastId = originalCastId;
			                                 caster.CastSpell(targets, overloadSpellId, args);
		                                 },
		                                 TimeSpan.FromMilliseconds(400));
	}

	private uint GetTriggeredSpellId(uint triggeringSpellId)
	{
		switch (triggeringSpellId)
		{
			case ShamanSpells.LightningBolt:
				return ShamanSpells.LightningBoltOverload;
			case ShamanSpells.ElementalBlast:
				return ShamanSpells.ElementalBlastOverload;
			case ShamanSpells.Icefury:
				return ShamanSpells.IcefuryOverload;
			case ShamanSpells.LavaBurst:
				return ShamanSpells.LavaBurstOverload;
			case ShamanSpells.ChainLightning:
				return ShamanSpells.ChainLightningOverload;
			case ShamanSpells.LavaBeam:
				return ShamanSpells.LavaBeamOverload;
			default:
				break;
		}

		return 0;
	}
}