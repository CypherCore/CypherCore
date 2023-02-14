// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(new uint[]
             {
	             237680, 49184
             })]
public class spell_dk_howling_blast : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public const uint SPELL_VISUAL_ID_HOWLING_BLAST = 66812;

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(DeathKnightSpells.SPELL_DK_HOWLING_BLAST_AREA_DAMAGE, DeathKnightSpells.SPELL_DK_FROST_FEVER);
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.RemoveIf((WorldObject target) =>
		                 {
			                 if (GetSpellInfo().Id == DeathKnightSpells.SPELL_DK_HOWLING_BLAST_AREA_DAMAGE)
			                 {
				                 if (GetSpell().m_customArg.has_value())
					                 return target.GetGUID() == (ObjectGuid)GetSpell().m_customArg;
			                 }
			                 else
			                 {
				                 return GetExplTargetUnit() != target;
			                 }

			                 return false;
		                 });
	}

	private void HandleFrostFever(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
			caster.CastSpell(GetHitUnit(), DeathKnightSpells.SPELL_DK_FROST_FEVER);
	}

	private void HandleAreaDamage(uint UnnamedParameter)
	{
		GetCaster().CastSpell(GetExplTargetUnit(), DeathKnightSpells.SPELL_DK_HOWLING_BLAST_AREA_DAMAGE, new CastSpellExtraArgs().SetCustomArg(GetExplTargetUnit().GetGUID()));
	}

	private void HandleSpellVisual(uint UnnamedParameter)
	{
		if (!GetSpell().m_customArg.has_value())
			return;

		var caster = GetCaster();

		if (caster != null)
		{
			var primaryTarget = ObjectAccessor.Instance.GetUnit(caster, (ObjectGuid)GetSpell().m_customArg);

			if (primaryTarget != null)
				primaryTarget.SendPlaySpellVisual(GetHitUnit(), SPELL_VISUAL_ID_HOWLING_BLAST, 0, 0, 0.0f);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new EffectHandler(HandleFrostFever, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));

		if (ScriptSpellId == DeathKnightSpells.SPELL_DK_HOWLING_BLAST_AREA_DAMAGE)
			SpellEffects.Add(new EffectHandler(HandleSpellVisual, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		else
			SpellEffects.Add(new EffectHandler(HandleAreaDamage, 1, SpellEffectName.Dummy, SpellScriptHookType.LaunchTarget));
	}
}