using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(19434)]
public class spell_hun_aimed_shot : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleDamage(uint effIndex)
	{
		float      distance       = 30.0f;
		int        damagePct      = 50;
		List<Unit> targetList     = new List<Unit>();
		List<Unit> victimList     = new List<Unit>();
		bool       canApplyDamage = true;

		Player modOwner = GetCaster().GetSpellModOwner();
		if (modOwner != null)
		{
			if (modOwner.HasAura(199522))
			{
				Unit mainTarget = GetHitUnit();
				if (mainTarget != null)
				{
					mainTarget.GetAnyUnitListInRange(targetList, distance);

					if (targetList.Count > 0)
					{
						foreach (var target in targetList)
						{
							if (!modOwner.IsFriendlyTo(target))
							{
								if (target == mainTarget)
								{
									continue;
								}

								if (target.HasAura(187131))
								{
									canApplyDamage = false;
								}

								victimList.Add(target);
							}
						}
						if (canApplyDamage)
						{
							damagePct += 15;
						}

						foreach (var victim in victimList)
						{
							CastSpellExtraArgs args     = new CastSpellExtraArgs();
							int                castTime = 0;
							mainTarget.ModSpellCastTime(GetSpellInfo(), ref castTime);
							args.AddSpellMod(SpellValueMod.BasePoint0, (int)damagePct);
							args.SetOriginalCaster(modOwner.GetGUID());
							args.SetTriggerFlags(TriggerCastFlags.FullMask);

							mainTarget.CastSpell(victim, 164340, args);
						}
					}
				}
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}