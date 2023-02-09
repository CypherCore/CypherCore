using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(80313)]
public class spell_druid_pulverize : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private struct Spells
	{
		public static readonly uint SPELL_DRUID_PULVERIZE = 80313;
		public static readonly uint SPELL_DRUID_TRASH_DOT_TWO_STACKS_MARKER = 158790;
		public static readonly uint SPELL_DRUID_PULVERIZE_DAMAGE_REDUCTION_BUFF = 158792;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.SPELL_DRUID_PULVERIZE, Spells.SPELL_DRUID_TRASH_DOT_TWO_STACKS_MARKER);
	}

	private void HandleHitTarget(uint UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
		{
			target.RemoveAurasDueToSpell(Spells.SPELL_DRUID_TRASH_DOT_TWO_STACKS_MARKER);
			GetCaster().CastSpell(target, Spells.SPELL_DRUID_PULVERIZE_DAMAGE_REDUCTION_BUFF, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHitTarget, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}