using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(190925)]
public class spell_hun_harpoon : SpellScript, IHasSpellEffects, ISpellAfterCast, ISpellOnCast
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (!Global.SpellMgr.HasSpellInfo(HunterSpells.SPELL_HUNTER_HARPOON, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(HunterSpells.SPELL_HUNTER_HARPOON_ROOT, Difficulty.None))
		{
			return false;
		}
		return true;
	}

	public void OnCast()
	{
		Player player = GetCaster().ToPlayer();
		Unit   target = GetExplTargetUnit();

		if (player == null || target == null)
		{
			return;
		}

		player.CastSpell(target, HunterSpells.SPELL_HUNTER_HARPOON_ROOT, true);
	}

	private void HandleDummy(uint UnnamedParameter)
	{
		Player player = GetCaster().ToPlayer();
		Unit   target = GetExplTargetUnit();

		if (player == null || target == null)
		{
			return;
		}

		WorldLocation pTarget = target.GetWorldLocation();

		float speedXY;
		float speedZ;
		speedZ  = 1.8f;
		speedXY = player.GetExactDist2d(pTarget) * 10.0f / speedZ;
		player.GetMotionMaster().MoveJump(pTarget, speedXY, speedZ, EventId.Jump);
	}

	public void AfterCast()
	{
		Player player = GetCaster().ToPlayer();
		if (player != null)
		{
			if (player.HasSpell(HunterSpells.SPELL_HUNTER_POSTHAST))
			{
				player.CastSpell(player, HunterSpells.SPELL_HUNTER_POSTHAST_SPEED, true);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHitTarget));
	}
}