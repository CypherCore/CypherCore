using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(108968)]
public class spell_pri_void_shift : SpellScript, IHasSpellEffects, ISpellCheckCast
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(PriestSpells.SPELL_PRIEST_VOID_SHIFT, Difficulty.None) != null)
			return false;

		return true;
	}

	public SpellCastResult CheckCast()
	{
		if (GetExplTargetUnit())
			if (GetExplTargetUnit().GetTypeId() != TypeId.Player)
				return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}

	private void HandleDummy(uint UnnamedParameter)
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
		{
			var target = GetHitUnit();

			if (target != null)
			{
				var playerPct = _player.GetHealthPct();
				var targetPct = target.GetHealthPct();

				if (playerPct < 25)
					playerPct = 25;

				if (targetPct < 25)
					targetPct = 25;

				playerPct /= 100;
				targetPct /= 100;

				_player.SetHealth(_player.GetMaxHealth() * targetPct);
				target.SetHealth(target.GetMaxHealth() * playerPct);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}