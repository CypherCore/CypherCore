using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// Intercept (As of Legion) - 198304
	[SpellScript(198304)]
	public class spell_warr_intercept : SpellScript, ISpellCheckCast, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return Global.SpellMgr.GetSpellInfo(WarriorSpells.INTERVENE_TRIGGER, Difficulty.None) != null;
		}

		private void HandleDummy(uint UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (target == null)
				return;

			if (target.IsFriendlyTo(caster))
			{
				caster.CastSpell(target, WarriorSpells.INTERVENE_TRIGGER, true);
			}
			else
			{
				caster.CastSpell(target, WarriorSpells.CHARGE_EFFECT, true);

				if (caster.HasAura(WarriorSpells.WARBRINGER))
					caster.CastSpell(target, WarriorSpells.WARBRINGER_ROOT, true);
				else
					caster.CastSpell(target, WarriorSpells.INTERCEPT_STUN, true);
			}
		}

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();
			var target = GetExplTargetUnit();
			var pos    = target.GetPosition();

			if (caster.GetDistance(pos) < 8.0f && !caster.IsFriendlyTo(target))
				return SpellCastResult.TooClose;

			return SpellCastResult.SpellCastOk;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}