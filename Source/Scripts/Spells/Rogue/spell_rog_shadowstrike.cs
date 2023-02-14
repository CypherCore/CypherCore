// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 185438 - Shadowstrike
internal class spell_rog_shadowstrike : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	private bool _hasPremeditationAura = false;

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(RogueSpells.PremeditationAura, RogueSpells.SliceAndDice, RogueSpells.PremeditationPassive) && Global.SpellMgr.GetSpellInfo(RogueSpells.PremeditationPassive, Difficulty.None).GetEffects().Count > 0;
	}

	public SpellCastResult CheckCast()
	{
		// Because the premeditation aura is removed when we're out of stealth,
		// when we reach HandleEnergize the aura won't be there, even if it was when player launched the spell
		_hasPremeditationAura = GetCaster().HasAura(RogueSpells.PremeditationAura);

		return SpellCastResult.Success;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEnergize, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleEnergize(uint effIndex)
	{
		var caster = GetCaster();

		if (_hasPremeditationAura)
		{
			if (caster.HasAura(RogueSpells.SliceAndDice))
			{
				var premeditationPassive = caster.GetAura(RogueSpells.PremeditationPassive);

				if (premeditationPassive != null)
				{
					var auraEff = premeditationPassive.GetEffect(1);

					if (auraEff != null)
						SetHitDamage(GetHitDamage() + auraEff.GetAmount());
				}
			}

			// Grant 10 seconds of slice and dice
			var duration = Global.SpellMgr.GetSpellInfo(RogueSpells.PremeditationPassive, Difficulty.None).GetEffect(0).CalcValue(GetCaster());

			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.Duration, duration * Time.InMilliseconds);
			caster.CastSpell(caster, RogueSpells.SliceAndDice, args);
		}
	}
}