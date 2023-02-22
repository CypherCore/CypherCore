// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_mixology_bonus : AuraScript, IHasAuraEffects
{
	private double bonus;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo((uint)RequiredMixologySpells.Mixology) && !spellInfo.GetEffects().Empty();
	}

	public override bool Load()
	{
		return GetCaster() && GetCaster().GetTypeId() == TypeId.Player;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, SpellConst.EffectAll, AuraType.Any));
	}

	private void SetBonusValueForEffect(uint effIndex, int value, AuraEffect aurEff)
	{
		if (aurEff.GetEffIndex() == effIndex)
			bonus = value;
	}

	private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		if (GetCaster().HasAura((uint)RequiredMixologySpells.Mixology) &&
		    GetCaster().HasSpell(GetEffectInfo(0).TriggerSpell))
		{
			switch ((RequiredMixologySpells)GetId())
			{
				case RequiredMixologySpells.WeakTrollsBloodElixir:
				case RequiredMixologySpells.MagebloodElixir:
					bonus = amount;

					break;
				case RequiredMixologySpells.ElixirOfFrostPower:
				case RequiredMixologySpells.LesserFlaskOfToughness:
				case RequiredMixologySpells.LesserFlaskOfResistance:
					bonus = MathFunctions.CalculatePct(amount, 80);

					break;
				case RequiredMixologySpells.ElixirOfMinorDefense:
				case RequiredMixologySpells.ElixirOfLionsStrength:
				case RequiredMixologySpells.ElixirOfMinorAgility:
				case RequiredMixologySpells.MajorTrollsBlloodElixir:
				case RequiredMixologySpells.ElixirOfShadowPower:
				case RequiredMixologySpells.ElixirOfBruteForce:
				case RequiredMixologySpells.MightyTrollsBloodElixir:
				case RequiredMixologySpells.ElixirOfGreaterFirepower:
				case RequiredMixologySpells.OnslaughtElixir:
				case RequiredMixologySpells.EarthenElixir:
				case RequiredMixologySpells.ElixirOfMajorAgility:
				case RequiredMixologySpells.FlaskOfTheTitans:
				case RequiredMixologySpells.FlaskOfRelentlessAssault:
				case RequiredMixologySpells.FlaskOfStoneblood:
				case RequiredMixologySpells.ElixirOfMinorAccuracy:
					bonus = MathFunctions.CalculatePct(amount, 50);

					break;
				case RequiredMixologySpells.ElixirOfProtection:
					bonus = 280;

					break;
				case RequiredMixologySpells.ElixirOfMajorDefense:
					bonus = 200;

					break;
				case RequiredMixologySpells.ElixirOfGreaterDefense:
				case RequiredMixologySpells.ElixirOfSuperiorDefense:
					bonus = 140;

					break;
				case RequiredMixologySpells.ElixirOfFortitude:
					bonus = 100;

					break;
				case RequiredMixologySpells.FlaskOfEndlessRage:
					bonus = 82;

					break;
				case RequiredMixologySpells.ElixirOfDefense:
					bonus = 70;

					break;
				case RequiredMixologySpells.ElixirOfDemonslaying:
					bonus = 50;

					break;
				case RequiredMixologySpells.FlaskOfTheFrostWyrm:
					bonus = 47;

					break;
				case RequiredMixologySpells.WrathElixir:
					bonus = 32;

					break;
				case RequiredMixologySpells.ElixirOfMajorFrostPower:
				case RequiredMixologySpells.ElixirOfMajorFirepower:
				case RequiredMixologySpells.ElixirOfMajorShadowPower:
					bonus = 29;

					break;
				case RequiredMixologySpells.ElixirOfMightyToughts:
					bonus = 27;

					break;
				case RequiredMixologySpells.FlaskOfSupremePower:
				case RequiredMixologySpells.FlaskOfBlindingLight:
				case RequiredMixologySpells.FlaskOfPureDeath:
				case RequiredMixologySpells.ShadowpowerElixir:
					bonus = 23;

					break;
				case RequiredMixologySpells.ElixirOfMightyAgility:
				case RequiredMixologySpells.FlaskOfDistilledWisdom:
				case RequiredMixologySpells.ElixirOfSpirit:
				case RequiredMixologySpells.ElixirOfMightyStrength:
				case RequiredMixologySpells.FlaskOfPureMojo:
				case RequiredMixologySpells.ElixirOfAccuracy:
				case RequiredMixologySpells.ElixirOfDeadlyStrikes:
				case RequiredMixologySpells.ElixirOfMightyDefense:
				case RequiredMixologySpells.ElixirOfExpertise:
				case RequiredMixologySpells.ElixirOfArmorPiercing:
				case RequiredMixologySpells.ElixirOfLightningSpeed:
					bonus = 20;

					break;
				case RequiredMixologySpells.FlaskOfChromaticResistance:
					bonus = 17;

					break;
				case RequiredMixologySpells.ElixirOfMinorFortitude:
				case RequiredMixologySpells.ElixirOfMajorStrength:
					bonus = 15;

					break;
				case RequiredMixologySpells.FlaskOfMightyRestoration:
					bonus = 13;

					break;
				case RequiredMixologySpells.ArcaneElixir:
					bonus = 12;

					break;
				case RequiredMixologySpells.ElixirOfGreaterAgility:
				case RequiredMixologySpells.ElixirOfGiants:
					bonus = 11;

					break;
				case RequiredMixologySpells.ElixirOfAgility:
				case RequiredMixologySpells.ElixirOfGreaterIntellect:
				case RequiredMixologySpells.ElixirOfSages:
				case RequiredMixologySpells.ElixirOfIronskin:
				case RequiredMixologySpells.ElixirOfMightyMageblood:
					bonus = 10;

					break;
				case RequiredMixologySpells.ElixirOfHealingPower:
					bonus = 9;

					break;
				case RequiredMixologySpells.ElixirOfDraenicWisdom:
				case RequiredMixologySpells.GurusElixir:
					bonus = 8;

					break;
				case RequiredMixologySpells.ElixirOfFirepower:
				case RequiredMixologySpells.ElixirOfMajorMageblood:
				case RequiredMixologySpells.ElixirOfMastery:
					bonus = 6;

					break;
				case RequiredMixologySpells.ElixirOfLesserAgility:
				case RequiredMixologySpells.ElixirOfOgresStrength:
				case RequiredMixologySpells.ElixirOfWisdom:
				case RequiredMixologySpells.ElixirOfTheMongoose:
					bonus = 5;

					break;
				case RequiredMixologySpells.StrongTrollsBloodElixir:
				case RequiredMixologySpells.FlaskOfChromaticWonder:
					bonus = 4;

					break;
				case RequiredMixologySpells.ElixirOfEmpowerment:
					bonus = -10;

					break;
				case RequiredMixologySpells.AdeptsElixir:
					SetBonusValueForEffect(0, 13, aurEff);
					SetBonusValueForEffect(1, 13, aurEff);
					SetBonusValueForEffect(2, 8, aurEff);

					break;
				case RequiredMixologySpells.ElixirOfMightyFortitude:
					SetBonusValueForEffect(0, 160, aurEff);

					break;
				case RequiredMixologySpells.ElixirOfMajorFortitude:
					SetBonusValueForEffect(0, 116, aurEff);
					SetBonusValueForEffect(1, 6, aurEff);

					break;
				case RequiredMixologySpells.FelStrengthElixir:
					SetBonusValueForEffect(0, 40, aurEff);
					SetBonusValueForEffect(1, 40, aurEff);

					break;
				case RequiredMixologySpells.FlaskOfFortification:
					SetBonusValueForEffect(0, 210, aurEff);
					SetBonusValueForEffect(1, 5, aurEff);

					break;
				case RequiredMixologySpells.GreaterArcaneElixir:
					SetBonusValueForEffect(0, 19, aurEff);
					SetBonusValueForEffect(1, 19, aurEff);
					SetBonusValueForEffect(2, 5, aurEff);

					break;
				case RequiredMixologySpells.ElixirOfGianthGrowth:
					SetBonusValueForEffect(0, 5, aurEff);

					break;
				default:
					Log.outError(LogFilter.Spells, "SpellId {0} couldn't be processed in spell_gen_mixology_bonus", GetId());

					break;
			}

			amount += bonus;
		}
	}
}