using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(108199)]
public class spell_dk_gorefiends_grasp : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


	private void HandleScript(uint UnnamedParameter)
	{
		Player _player = GetCaster().ToPlayer();
		if (_player != null)
		{
			Unit target = GetHitUnit();
			if (target != null)
			{
				List<Unit> tempList = new List<Unit>();
				List<Unit> gripList = new List<Unit>();

				_player.GetAttackableUnitListInRange(tempList, 20.0f);

				foreach (var itr in tempList)
				{
					if (itr.GetGUID() == _player.GetGUID())
					{
						continue;
					}

					if (!_player.IsValidAttackTarget(itr))
					{
						continue;
					}

					if (itr.IsImmunedToSpell(GetSpellInfo(), GetCaster()))
					{
						continue;
					}

					if (!itr.IsWithinLOSInMap(_player))
					{
						continue;
					}

					gripList.Add(itr);
				}

				foreach (var itr in gripList)
				{
					itr.CastSpell(target, DeathKnightSpells.SPELL_DK_DEATH_GRIP_ONLY_JUMP, true);
					itr.CastSpell(target, DeathKnightSpells.SPELL_DK_GOREFIENDS_GRASP_GRIP_VISUAL, true);
				}
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}
}