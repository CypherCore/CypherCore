// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(190925)]
public class spell_hun_harpoon : SpellScript, IHasSpellEffects, ISpellAfterCast, ISpellOnCast
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (!Global.SpellMgr.HasSpellInfo(HunterSpells.HARPOON, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(HunterSpells.HARPOON_ROOT, Difficulty.None))
			return false;

		return true;
	}

	public void OnCast()
	{
		var player = GetCaster().ToPlayer();
		var target = GetExplTargetUnit();

		if (player == null || target == null)
			return;

		player.CastSpell(target, HunterSpells.HARPOON_ROOT, true);
	}

	private void HandleDummy(int effIndex)
	{
		var player = GetCaster().ToPlayer();
		var target = GetExplTargetUnit();

		if (player == null || target == null)
			return;

		var pTarget = target.GetWorldLocation();

		float speedXY;
        float speedZ;
		speedZ  = 1.8f;
		speedXY = player.GetExactDist2d(pTarget) * 10.0f / speedZ;
		player.GetMotionMaster().MoveJump(pTarget, speedXY, speedZ, EventId.Jump);
	}

	public void AfterCast()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
			if (player.HasSpell(HunterSpells.POSTHAST))
				player.CastSpell(player, HunterSpells.POSTHAST_SPEED, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHitTarget));
	}
}