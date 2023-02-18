// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(774)]
public class spell_dru_rejuvenation : SpellScript, ISpellBeforeHit, ISpellAfterHit
{
	public struct Spells
	{
		public static uint CULTIVATION = 200390;
		public static uint CULTIVATION_HOT = 200389;
		public static uint GERMINATION = 155675;
		public static uint GERMINATION_HOT = 155777;
		public static uint ABUNDANCE = 207383;
		public static uint ABUNDANCE_BUFF = 207640;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.GERMINATION, Spells.GERMINATION_HOT, Spells.ABUNDANCE, Spells.ABUNDANCE_BUFF);
	}

	private int m_RejuvenationAura = 0;
	private int m_RejuvenationAuraAmount = 0;

	public void AfterHit()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var target = GetHitUnit();

		if (target == null)
			return;

		var RejuvenationAura = target.GetAura(DruidSpells.REJUVENATION, caster.GetGUID());

		if (RejuvenationAura != null && m_RejuvenationAura > 0)
			RejuvenationAura.SetDuration(m_RejuvenationAura);

		var NewRejuvenationAuraEffect = target.GetAuraEffect(DruidSpells.REJUVENATION, 0);

		if (NewRejuvenationAuraEffect != null)
			if (caster.HasAura(SoulOfTheForestSpells.SOUL_OF_THE_FOREST_RESTO))
			{
				NewRejuvenationAuraEffect.SetAmount(NewRejuvenationAuraEffect.GetAmount() * 2);
				caster.RemoveAura(SoulOfTheForestSpells.SOUL_OF_THE_FOREST_RESTO);
			}

		if (caster.HasAura(207383))
			caster.CastSpell(caster, Spells.ABUNDANCE, true);
	}

	public void BeforeHit(SpellMissInfo missInfo)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var target = GetHitUnit();

		if (target == null)
			return;

		if (caster.HasAura(SoulOfTheForestSpells.SOUL_OF_THE_FOREST_RESTO))
			//      NewRejuvenationAuraEffect->SetAmount(NewRejuvenationAuraEffect->GetAmount() * 2);
			SetHitHeal(GetHitHeal() * 2);

		//      caster->RemoveAura(SOUL_OF_THE_FOREST_RESTO);
		///Germination
		if (caster.HasAura(155675) && target.HasAura(DruidSpells.REJUVENATION, caster.GetGUID()))
		{
			var RejuvenationAura = target.GetAura(DruidSpells.REJUVENATION, caster.GetGUID());

			if (RejuvenationAura == null)
				return;

			if (!target.HasAura(155777, caster.GetGUID()))
			{
				caster.CastSpell(target, 155777, true);
				m_RejuvenationAura = RejuvenationAura.GetDuration();
			}
			else
			{
				var GerminationAura = target.GetAura(155777, caster.GetGUID());
				;

				if (GerminationAura != null && RejuvenationAura != null)
				{
					var GerminationDuration  = GerminationAura.GetDuration();
					var RejuvenationDuration = RejuvenationAura.GetDuration();

					if (GerminationDuration > RejuvenationDuration)
					{
						caster.AddAura(DruidSpells.REJUVENATION, target);
					}
					else
					{
						caster.CastSpell(target, 155777, true);
						m_RejuvenationAura = RejuvenationDuration;
					}
				}
			}
		}
	}
}