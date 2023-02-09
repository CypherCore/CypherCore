using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter
{
	internal struct DemonHunterSpells
	{
		public const uint AbyssalStrike = 207550;
		public const uint Annihilation = 201427;
		public const uint AnnihilationMh = 227518;
		public const uint AnnihilationOh = 201428;
		public const uint AwakenTheDemonWithinCd = 207128;
		public const uint Blur = 212800;
		public const uint BlurTrigger = 198589;
		public const uint BurningAlive = 207739;
		public const uint BurningAliveTargetSelector = 207760;
		public const uint ChaosNova = 179057;
		public const uint ChaosStrike = 162794;
		public const uint ChaosStrikeEnergize = 193840;
		public const uint ChaosStrikeMh = 222031;
		public const uint ChaosStrikeOh = 199547;
		public const uint ConsumeSoulHavoc = 228542;
		public const uint ConsumeSoulHavocDemon = 228556;
		public const uint ConsumeSoulHavocShattered = 228540;
		public const uint ConsumeSoulHeal = 203794;
		public const uint ConsumeSoulVengeance = 208014;
		public const uint ConsumeSoulVengeanceDemon = 210050;
		public const uint ConsumeSoulVengeanceShattered = 210047;
		public const uint DarknessAbsorb = 209426;
		public const uint DemonBladesDmg = 203796;
		public const uint DemonSpikes = 203819;
		public const uint DemonSpikesTrigger = 203720;
		public const uint Demonic = 213410;
		public const uint DemonicOrigins = 235893;
		public const uint DemonicOriginsBuff = 235894;
		public const uint DemonicTrampleDmg = 208645;
		public const uint DemonicTrampleStun = 213491;
		public const uint DemonsBite = 162243;
		public const uint EyeBeam = 198013;
		public const uint EyeBeamDmg = 198030;
		public const uint EyeOfLeotherasDmg = 206650;
		public const uint FeastOfSouls = 207697;
		public const uint FeastOfSoulsPeriodicHeal = 207693;
		public const uint FeedTheDemon = 218612;
		public const uint FelBarrage = 211053;
		public const uint FelBarrageDmg = 211052;
		public const uint FelBarrageProc = 222703;
		public const uint FelDevastation = 212084;
		public const uint FelDevastationDmg = 212105;
		public const uint FelDevastationHeal = 212106;
		public const uint FelRush = 195072;
		public const uint FelRushDmg = 192611;
		public const uint FelRushGround = 197922;
		public const uint FelRushWaterAir = 197923;
		public const uint Felblade = 232893;
		public const uint FelbladeCharge = 213241;
		public const uint FelbladeDmg = 213243;
		public const uint FelbladeProc = 203557;
		public const uint FelbladeProcVisual = 204497;
		public const uint FelbladeProc1 = 236167;
		public const uint FieryBrand = 204021;
		public const uint FieryBrandDmgReductionDebuff = 207744;
		public const uint FieryBrandDot = 207771;
		public const uint FirstBlood = 206416;
		public const uint FlameCrash = 227322;
		public const uint Frailty = 224509;
		public const uint Glide = 131347;
		public const uint GlideDuration = 197154;
		public const uint GlideKnockback = 196353;
		public const uint HavocMastery = 185164;
		public const uint IllidansGrasp = 205630;
		public const uint IllidansGraspDamage = 208618;
		public const uint IllidansGraspJumpDest = 208175;
		public const uint InfernalStrikeCast = 189110;
		public const uint InfernalStrikeImpactDamage = 189112;
		public const uint InfernalStrikeJump = 189111;
		public const uint JaggedSpikes = 205627;
		public const uint JaggedSpikesDmg = 208790;
		public const uint JaggedSpikesProc = 208796;
		public const uint ManaRiftDmgPowerBurn = 235904;
		public const uint Metamorphosis = 191428;
		public const uint MetamorphosisDummy = 191427;
		public const uint MetamorphosisImpactDamage = 200166;
		public const uint MetamorphosisReset = 320645;
		public const uint MetamorphosisTransform = 162264;
		public const uint MetamorphosisVengeanceTransform = 187827;
		public const uint Momentum = 208628;
		public const uint NemesisAberrations = 208607;
		public const uint NemesisBeasts = 208608;
		public const uint NemesisCritters = 208609;
		public const uint NemesisDemons = 208608;
		public const uint NemesisDragonkin = 208610;
		public const uint NemesisElementals = 208611;
		public const uint NemesisGiants = 208612;
		public const uint NemesisHumanoids = 208605;
		public const uint NemesisMechanicals = 208613;
		public const uint NemesisUndead = 208614;
		public const uint RainFromAbove = 206803;
		public const uint RainOfChaos = 205628;
		public const uint RainOfChaosImpact = 232538;
		public const uint RazorSpikes = 210003;
		public const uint Sever = 235964;
		public const uint ShatterSoul = 209980;
		public const uint ShatterSoul1 = 209981;
		public const uint ShatterSoul2 = 210038;
		public const uint ShatteredSoul = 226258;
		public const uint ShatteredSoulLesserSoulFragment1 = 228533;
		public const uint ShatteredSoulLesserSoulFragment2 = 237867;
		public const uint Shear = 203782;
		public const uint SigilOfChainsAreaSelector = 204834;
		public const uint SigilOfChainsGrip = 208674;
		public const uint SigilOfChainsJump = 208674;
		public const uint SigilOfChainsSlow = 204843;
		public const uint SigilOfChainsSnare = 204843;
		public const uint SigilOfChainsTargetSelect = 204834;
		public const uint SigilOfChainsVisual = 208673;
		public const uint SigilOfFlameAoe = 204598;
		public const uint SigilOfFlameDamage = 204598;
		public const uint SigilOfFlameFlameCrash = 228973;
		public const uint SigilOfMisery = 207685;
		public const uint SigilOfMiseryAoe = 207685;
		public const uint SigilOfSilence = 204490;
		public const uint SigilOfSilenceAoe = 204490;
		public const uint SoulBarrier = 227225;
		public const uint SoulCleave = 228477;
		public const uint SoulCleaveDmg = 228478;
		public const uint SoulFragmentCounter = 203981;
		public const uint SoulRending = 204909;
		public const uint SpiritBombDamage = 218677;
		public const uint SpiritBombHeal = 227255;
		public const uint SpiritBombVisual = 218678;
		public const uint ThrowGlaive = 185123;
		public const uint UncontainedFel = 209261;
		public const uint VengefulRetreat = 198813;
		public const uint VengefulRetreatTrigger = 198793;
		public const uint SPELL_DH_FEL_RUSH_DASH = 197922;
		public const uint SPELL_DH_FEL_RUSH_AIR = 197923;
		public const uint SPELL_DH_FEL_RUSH = 195072;
		public const uint SPELL_DH_FEL_RUSH_DAMAGE = 223107;
		public const uint SPELL_DH_EYE_BEAM = 198013;
		public const uint SPELL_DH_EYE_BEAM_VISUAL = 194326;
		public const uint SPELL_DH_EYE_BEAM_DAMAGE = 198030;
		public const uint SPELL_DH_CHAOS_STRIKE_PROC = 193840;
		public const uint SPELL_DH_CHAOS_STRIKE_MAIN_HAND = 199547;
		public const uint SPELL_DH_CHAOS_STRIKE_OFF_HAND = 222031;
		public const uint SPELL_DH_CHARRED_FLESH = 336639;
		public const uint SPELL_DH_ANNIHILIATION_MAIN_HAND = 201428;
		public const uint SPELL_DH_ANNIHILIATION_OFF_HAND = 227518;
		public const uint SPELL_DH_FEL_ERUPTION = 211881;
		public const uint SPELL_DH_FEL_ERUPTION_DAMAGE = 225102;
		public const uint SPELL_DH_THROW_GLAIVE = 185123;
		public const uint SPELL_DH_LAST_RESORT_DEBUFF = 209261;
		public const uint SPELL_DH_METAMORPHOSIS_VENGEANCE = 187827;
		public const uint SPELL_DH_METAMORPHOSIS_HAVOC = 162264;
		public const uint SPELL_DH_METAMORPHOSIS_JUMP = 191428;
		public const uint SPELL_DH_METAMORPHOSIS_STUN = 200166;
		public const uint SPELL_DH_GLIDE = 131347;
		public const uint SPELL_DH_GLIDE_DURATION = 197154;
		public const uint SPELL_DH_GLIDE_KNOCKBACK = 196353;
		public const uint SPELL_DH_FEL_MASTERY_FURY = 234244;
		public const uint SPELL_DH_FEL_MASTERY = 192939;
		public const uint SPELL_DH_PREPARED = 203551;
		public const uint SPELL_DH_PREPARED_FURY = 203650;
		public const uint SPELL_DH_DEMON_BLADES = 203796;
		public const uint SPELL_DH_FELBLADE = 232893;
		public const uint SPELL_DH_DEMONS_BITE = 162243;
		public const uint SPELL_DH_SHEAR = 203782;
		public const uint SPELL_DH_SHEAR_PROC = 203783;
		public const uint SPELL_DH_GLUTTONY_BUFF = 227330;
		public const uint SPELL_DH_FIRST_BLOOD = 206416;
		public const uint SPELL_DH_BLOODLET_DOT = 207690;
		public const uint SPELL_DH_SOUL_RENDING_HAVOC = 204909;
		public const uint SPELL_DH_SOUL_RENDING_VENGEANCE = 217996;
		public const uint SPELL_DH_DEMONIC = 213410;
		public const uint SPELL_DH_DEMON_REBORN = 193897;
		public const uint SPELL_DH_BLUR = 198589;
		public const uint SPELL_DH_CHAOS_NOVA = 179057;
		public const uint SPELL_DH_FEL_BARRAGE = 211053;
		public const uint SPELL_DH_FEL_BARRAGE_TRIGGER = 211052;
		public const uint SPELL_DH_INFERNAL_STRIKE_JUMP = 189111;
		public const uint SPELL_DH_INFERNAL_STRIKE_DAMAGE = 189112;
		public const uint SPELL_DH_INFERNAL_STRIKE_VISUAL = 208461;
		public const uint SPELL_DH_INFERNAL_STRIKE = 189110;
		public const uint SPELL_DH_SOUL_CLEAVE_DAMAGE = 228478;
		public const uint SPELL_DH_FIERY_BRAND_DOT = 207771;
		public const uint SPELL_DH_FIERY_BRAND_MARKER = 207744;
		public const uint SPELL_DH_BURNING_ALIVE = 207739;
		public const uint SPELL_DH_RAZOR_SPIKES_SLOW = 210003;
		public const uint SPELL_DH_DEMON_SPIKES_BUFF = 203819;
		public const uint SPELL_DH_FEAST_OF_SOULS = 207697;
		public const uint SPELL_DH_FEAST_OF_SOULS_HEAL = 207693;
		public const uint SPELL_DH_SOUL_BARRIER = 263648;
		public const uint SPELL_DH_NETHER_BOND = 207810;
		public const uint SPELL_DH_NETHER_BOND_PERIODIC = 207811;
		public const uint SPELL_DH_NETHER_BOND_DAMAGE = 207812;
		public const uint SPELL_DH_SOLITUDE_BUFF = 211510;
		public const uint SPELL_DH_AWAKEN_THE_DEMON_CD = 207128;
		public const uint SPELL_DH_GLIMPSE = 203468;
		public const uint SPELL_DH_BLUR_BUFF = 212800;
		public const uint SPELL_DH_RUSHING_VAULT = 205601;
		public const uint SPELL_DH_EYE_OF_LEOTHERAS = 206649;
		public const uint SPELL_DH_EYE_OF_LEOTHERAS_DAMAGE = 206650;
		public const uint SPELL_DH_CLEANSED_BY_FLAME = 205625;
		public const uint SPELL_DH_CLEANSED_BY_FLAME_DISPEL = 208770;
		public const uint SPELL_DH_RAIN_OF_CHAOS = 205628;
		public const uint SPELL_DH_RAIN_OF_CHAOS_SLOW = 232538;
		public const uint SPELL_DH_JAGGED_SPIKES = 205627;
		public const uint SPELL_DH_JAGGED_SPIKES_DAMAGE = 208790;
		public const uint SPELL_DH_INTIMIDATED = 206891;
		public const uint SPELL_DH_TORMENTOR = 207029;
		public const uint SPELL_DH_DEMON_SPEED = 201469;
		public const uint SPELL_DH_VENGEFUL_RETREAT_TRIGGER = 198793;
		public const uint SPELL_DH_DECEIVERS_FURY_ENERGIZE = 202120;
		public const uint SPELL_DH_ANGUISH_DAMAGE = 202446;
		public const uint SPELL_DH_SIGIL_OF_MISERY_TRIGGER = 207685;
		public const uint SPELL_DH_SIGIL_OF_MISERY_EXPLOSION = 208714;
		public const uint SPELL_DH_SIGIL_OF_FLAME_TRIGGER = 204598;
		public const uint SPELL_DH_SIGIL_OF_FLAME_EXPLOSION = 208710;
		public const uint SPELL_DH_SIGIL_OF_SILENCE_TRIGGER = 204490;
		public const uint SPELL_DH_SIGIL_OF_SILENCE_EXPLOSION = 208709;
		public const uint SPELL_DH_DARKNESS_ABSORB = 209426;
		public const uint SPELL_DH_BLIND_FURY = 203550;
		public const uint SPELL_DH_DEMONIC_APPETITE = 206478;
		public const uint SPELL_DH_CHAOS_CLEAVE_PROC = 236237;
		public const uint SPELL_DH_FALLOUT = 227174;
		public const uint SPELL_DH_FELBLADE_CHARGE = 213241;
		public const uint SPELL_DH_FELBLADE_DAMAGE = 213243;
		public const uint SPELL_DH_ABYSSAL_STRIKE = 207550;
		public const uint SPELL_DH_SIGIL_OF_FLAME_NO_DEST = 228973;
		public const uint SPELL_DH_FEED_THE_DEMON = 218612;
		public const uint SPELL_DH_DEMON_SPIKES = 203720;
		public const uint SPELL_DH_SIGIL_OF_CHAINS_EXPLOSION = 208673;
		public const uint SPELL_DH_SIGIL_OF_CHAINS_TRIGGER = 208674;
		public const uint SPELL_DH_SIGIL_OF_CHAINS_SLOW = 204843;
		public const uint SPELL_DH_FEL_DEVASTATION_DAMAGE = 212105;
		public const uint SPELL_DH_FEL_DEVASTATION_HEAL = 212106;
		public const uint SPELL_DH_FRAILTY = 224509;
		public const uint SPELL_DH_FRAILTY_HEAL = 227255;
		public const uint SPELL_DH_SPIRIT_BOMB_DAMAGE = 218677;
		public const uint SPELL_DH_FURY_OF_THE_ILLIDARI_MAINHAND = 201628;
		public const uint SPELL_DH_FURY_OF_THE_ILLIDARI_OFFHAND = 201789;
		public const uint SPELL_DH_INNER_DEMONS_DAMAGE = 202388;
		public const uint SPELL_DH_BALANCED_BLADES = 201470;
		public const uint SPELL_DH_RAGE_OF_THE_ILLIDARI_VISUAL = 226948;
		public const uint SPELL_DH_RAGE_OF_THE_ILLIDARI_DAMAGE = 217070;
		public const uint SPELL_DH_OVERWHELMING_POWER = 201464;
		public const uint SPELL_DK_RAIN_FROM_ABOVE_SLOWFALL = 206804;
		public const uint SPELL_DH_DEMONIC_ORIGINS_BUFF = 235894;
		public const uint SPELL_DH_DEMONIC_ORIGINS = 235893;
		public const uint SPELL_DH_MANA_RIFT_SPELL = 235903;
		public const uint SPELL_DH_MANA_RIFT_DAMAGE = 235904;
		public const uint SPELL_DH_DEMONIC_TRAMPLE_DAMAGE = 208645;
		public const uint SPELL_DH_DEMONIC_TRAMPLE_STUN = 213491;
		public const uint SPELL_DH_DEMON_BLADES_AURA = 203555;
		public const uint SPELL_DH_REWARD_PAIN = 187724;
		public const uint SPELL_DH_FEAST_ON_THE_SOULS = 201468;
		public const uint SPELL_DH_MANA_BREAK = 203704;
		public const uint SPELL_DH_BLADE_DANCE = 188499;
		public const uint SPELL_DH_TRAIL_OF_RUIN_TALENT = 258881;
		public const uint SPELL_DH_CHAOS_STRIKE = 162794;
		public const uint SPELL_DH_SOUL_CARVER_DAMAGE = 214743;
		public const uint SPELL_DH_DOUBLE_JUMP = 196055;

    }

	public struct ShatteredSoulsSpells
	{
		public const uint SPELL_DH_SHATTERED_SOULS = 204255;
		public const uint SPELL_DH_SHATTERED_SOULS_DEMON = 204256;
		public const uint SPELL_DH_LESSER_SOUL_SHARD = 203795;
		public const uint SPELL_DH_LESSER_SOUL_SHARD_HEAL = 203794;
		public const uint SPELL_DH_SHATTERED_SOULS_MISSILE = 209651;
		public const uint SPELL_DH_SOUL_FRAGMENT_HEAL_25_HAVOC = 178963;
		public const uint SPELL_DH_SOUL_FRAGMENT_DEMON_BONUS = 163073;
		public const uint SPELL_DH_SOUL_FRAGMENT_HEAL_VENGEANCE = 210042;
		public const uint SPELL_DH_CONSUME_SOUL_MISSILE = 210047;
		public const uint SPELL_DH_LESSER_SOUL_FRAGMENT_HAVOC = 228532;
		public const uint SPELL_DH_PAINBRINGER = 207387;
		public const uint SPELL_DH_PAINBRINGER_BUFF = 212988;
		public const uint SPELL_DH_DEVOUR_SOULS = 212821;
		public const uint SPELL_DH_CHARRED_WARBLADES_HEAL = 213011;
		public const uint SPELL_DH_SHATTER_THE_SOULS = 212827;
		public const uint SPELL_DH_FIERY_DEMISE_DEBUFF = 212818;
		public const uint SPELL_DH_COVER_OF_DARKNESS = 227635;
		public const uint SPELL_DH_SHATTERED_SOULS_HAVOC = 178940;
		public const uint SPELL_DH_SHATTERED_SOULS_VENGEANCE = 204254;
		public const uint SPELL_DH_MOMENTUM = 206476;
		public const uint SPELL_DH_MOMENTUM_BUFF = 208628;
		public const uint SPELL_DH_SHATTERED_SOULS_SHEAR_1 = 226258;
		public const uint SPELL_DH_SHATTERED_SOULS_SHEAR_2 = 226259;
	}

	public struct NemesisSpells
	{
		public const uint NEMESIS_ABERRATION = 208607;
		public const uint NEMESIS_ELEMENTAL = 208611;
		public const uint NEMESIS_MECHANICAL = 208616;
		public const uint NEMESIS_DRAGONKIN = 208610;
		public const uint NEMESIS_HUMANOID = 208605;
		public const uint NEMESIS_CRITTERS = 208609;
		public const uint NEMESIS_DEMONS = 208579;
		public const uint NEMESIS_GIANTS = 208612;
		public const uint NEMESIS_UNDEAD = 208614;
		public const uint NEMESIS_BEASTS = 208608;
	}

	internal struct AreaTriggerIds
	{
		public const uint ShatteredSoulsHavoc = 8352;
		public const uint ShatteredSoulsHavocDemon = 11231;
		public const uint ShatteredSoulsVengeance = 11266;
		public const uint ShatteredSoulsVengeanceDemon = 10693;
		public const uint SoulFragmentHavoc = 12929;
		public const uint SoulFragmentVengeance = 10665;
	}

	// Bloodlet - 206473
	[SpellScript(206473)]
	public class spell_dh_bloodlet : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			if (eventInfo.GetSpellInfo().Id == DemonHunterSpells.SPELL_DH_THROW_GLAIVE)
				return true;

			return false;
		}

		private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
		{
			var caster = GetCaster();
			var target = eventInfo.GetActionTarget();

			if (caster == null || target == null || eventInfo.GetDamageInfo() != null || !GetSpellInfo().GetEffect(0).IsEffect())
				return;

			var basePoints = GetSpellInfo().GetEffect(0).BasePoints;
			var dmg = (eventInfo.GetDamageInfo().GetDamage() * (float)basePoints) / 100.0f;
			var dmgPerTick = (float)dmg / 5.0f;

			// Any remaining damage must be added
			var dot = target.GetAuraEffect(DemonHunterSpells.SPELL_DH_BLOODLET_DOT, 0, caster.GetGUID());

			if (dot != null)
				dmgPerTick += (dot.GetAmount() * (dot.GetTotalTicks() - dot.GetTickNumber())) / 5;

			var args = new CastSpellExtraArgs();
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)dmgPerTick);
			args.SetTriggerFlags(TriggerCastFlags.FullMask);
			caster.CastSpell(target, DemonHunterSpells.SPELL_DH_BLOODLET_DOT, args);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}


	// Vengeful Retread Damage - 198813
	[SpellScript(198813)]
	public class spell_dh_vengeful_retreat_damage : SpellScript, IHasSpellEffects, ISpellOnCast
	{
		public List<ISpellEffect> SpellEffects => new();

		private bool _targetHit;

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_PREPARED_FURY, Difficulty.None) != null)
				return false;

			return true;
		}

		private void CountTargets(List<WorldObject> targets)
		{
			_targetHit = targets.Count > 0;
		}

		public void OnCast()
		{
			var caster = GetCaster();

			if (caster != null)
			{
				if (caster.HasAura(DemonHunterSpells.SPELL_DH_PREPARED) && _targetHit)
					caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_PREPARED_FURY, true);

				var aur = caster.GetAura(DemonHunterSpells.SPELL_DH_GLIMPSE);

				if (aur != null)
				{
					var aurEff = aur.GetEffect(0);

					if (aurEff != null)
					{
						var blur = caster.AddAura(DemonHunterSpells.SPELL_DH_BLUR_BUFF, caster);

						if (blur != null)
							blur.SetDuration(aurEff.GetBaseAmount());
					}
				}

				if (caster.HasAura(DemonHunterSpells.SPELL_DH_RUSHING_VAULT))
				{
					var chargeCatId = Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_FEL_RUSH, Difficulty.None).ChargeCategoryId;
					caster.GetSpellHistory().RestoreCharge(chargeCatId);
				}
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitSrcAreaEnemy)); // 33
		}
	}


	// Fel Rush Damage - 223107
	[SpellScript(223107)]
	public class spell_dh_fel_rush_damage : SpellScript, IHasSpellEffects, ISpellOnHit, ISpellOnCast
	{
		public List<ISpellEffect> SpellEffects => new();

		private bool _targetHit;

		private void FilterTargets(List<WorldObject> targets)
		{
			targets.Remove(GetCaster());
		}

		private void CountTargets(List<WorldObject> targets)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			targets.Clear();
			var units = new List<Unit>();
			caster.GetAttackableUnitListInRange(units, 25.0f);


			units.RemoveIf((Unit unit) => { return !caster.HasInLine(unit, 6.0f, caster.GetObjectScale()); });

			foreach (var unit in units)
				targets.Add(unit);

			_targetHit = targets.Count > 0;
		}

		public void OnCast()
		{
			var caster = GetCaster();

			if (caster != null)
				if (caster.HasAura(DemonHunterSpells.SPELL_DH_FEL_MASTERY) && _targetHit)
					caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FEL_MASTERY_FURY, true);
		}

		public void OnHit()
		{
			if (GetCaster() && GetHitUnit())
			{
				var attackPower = GetCaster().m_unitData.AttackPower / 100 * 25.3f;
				SetHitDamage(attackPower);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitRectCasterEnemy));
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitRectCasterEnemy));
		}
	}

	// Metamorphosis (Havoc) - 191427
	[SpellScript(191427)]
	public class spell_dh_metamorphosis : SpellScript, ISpellBeforeCast
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (!Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_JUMP, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_STUN, Difficulty.None))
				return false;

			return true;
		}

		public void BeforeCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var player = caster.ToPlayer();

			if (player == null)
				return;

			WorldLocation dest = GetExplTargetDest();
			if (dest != null)
			{
				player.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), DemonHunterSpells.SPELL_DH_METAMORPHOSIS_JUMP, true);
			}

			if (player.HasAura(DemonHunterSpells.SPELL_DH_DEMON_REBORN)) // Remove CD of Eye Beam, Chaos Nova and Blur
			{
				player.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_CHAOS_NOVA, true);
				player.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_BLUR, true);
				player.GetSpellHistory().AddCooldown(DemonHunterSpells.SPELL_DH_BLUR_BUFF, 0, TimeSpan.FromMinutes(1));
				player.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_BLUR_BUFF, true);
				player.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_EYE_BEAM, true);
			}
		}
	}


	// Metamorphosis temporary immunity - 201453
	[SpellScript(201453)]
	public class spell_dh_metamorphosis_immunity : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_METAMORPHOSIS_STUN, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.AbilityIgnoreAurastate, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}
	}


	// Fel Rush air - 197923
	[SpellScript(197923)]
	public class spell_dh_fel_rush_dash_AuraScript : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				caster.m_Events
					  .AddEventAtOffset(() =>
								{
									if (!caster.HasAura(DemonHunterSpells.SPELL_DH_FEL_RUSH_AIR))
										caster.SetDisableGravity(false);
								}, TimeSpan.FromMilliseconds(100));
		}

		private void CalcSpeed(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
		{
			amount = 1250;
			RefreshDuration();
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 1, AuraType.ModSpeedNoControl));
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 3, AuraType.ModMinimumSpeed));
			AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 9, AuraType.ModMinimumSpeedRate, AuraEffectHandleModes.SendForClientMask, AuraScriptHookType.EffectAfterRemove));
		}
	}

	[SpellScript(197923)]
	public class spell_dh_fel_rush_dash_SpellScript : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void PreventTrigger(uint effIndex)
		{
			PreventHitEffect(effIndex);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(PreventTrigger, 6, SpellEffectName.TriggerSpell, SpellScriptHookType.Launch));
			SpellEffects.Add(new EffectHandler(PreventTrigger, 6, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHit));
		}
	}


	// Fel Rush air - 197922
	[SpellScript(197922)]
	public class spell_dh_fel_rush_dash_ground : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				caster.SetDisableGravity(false);
		}

		private void CalcSpeed(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
		{
			amount = 1250;
			RefreshDuration();
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 1, AuraType.ModSpeedNoControl));
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 3, AuraType.ModMinimumSpeed));
			AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 6, AuraType.ModMinimumSpeedRate, AuraEffectHandleModes.SendForClientMask, AuraScriptHookType.EffectAfterRemove));
		}
	}


	// Fel Rush - 195072
	[SpellScript(195072)]
	public class spell_dh_fel_rush : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_FEL_RUSH_DASH, Difficulty.None) != null)
				return false;

			if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_FEL_RUSH_AIR, Difficulty.None) != null)
				return false;

			return true;
		}

		private void HandleDashGround(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				if (!caster.IsFalling() || caster.IsInWater())
				{
					caster.RemoveAurasDueToSpell(DemonHunterSpells.SPELL_DH_GLIDE);
					caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FEL_RUSH_DASH, true);

					if (GetHitUnit())
						caster.CastSpell(GetHitUnit(), DemonHunterSpells.SPELL_DH_FEL_RUSH_DAMAGE, true);

					if (caster.HasAura(ShatteredSoulsSpells.SPELL_DH_MOMENTUM))
						caster.CastSpell(ShatteredSoulsSpells.SPELL_DH_MOMENTUM_BUFF, true);
				}

				caster.GetSpellHistory().AddCooldown(GetSpellInfo().Id, 0, TimeSpan.FromMicroseconds(750));
			}
		}

		private void HandleDashAir(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster != null)
				if (caster.IsFalling())
				{
					caster.RemoveAurasDueToSpell(DemonHunterSpells.SPELL_DH_GLIDE);
					caster.SetDisableGravity(true);
					caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FEL_RUSH_AIR, true);

					if (GetHitUnit())
						caster.CastSpell(GetHitUnit(), DemonHunterSpells.SPELL_DH_FEL_RUSH_DAMAGE, true);

					if (caster.HasAura(ShatteredSoulsSpells.SPELL_DH_MOMENTUM))
						caster.CastSpell(ShatteredSoulsSpells.SPELL_DH_MOMENTUM_BUFF, true);

					caster.GetSpellHistory().AddCooldown(GetSpellInfo().Id, 0, TimeSpan.FromMicroseconds(750));
				}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDashGround, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new EffectHandler(HandleDashAir, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}


	// Eye Beam - 198013
	[SpellScript(198013)]
	public class spell_dh_eye_beam : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private bool _firstTick = true;

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (!Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_EYE_BEAM, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_EYE_BEAM_DAMAGE, Difficulty.None))
				return false;

			return true;
		}

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster != null)
				if (!_firstTick)
				{
					caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_EYE_BEAM_DAMAGE, true);
					var energize = caster.GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_BLIND_FURY, 2);

					if (energize != 0)
						caster.ModifyPower(PowerType.Fury, energize * 2.0f / 50.0f);
				}

			_firstTick = false;
		}

		private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				caster.RemoveAurasDueToSpell(DemonHunterSpells.SPELL_DH_EYE_BEAM_VISUAL);
		}

		private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				if (!caster.HasAura(DemonHunterSpells.SPELL_DH_DEMONIC))
					caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_EYE_BEAM_VISUAL, true);

				if (caster.HasAura(DemonHunterSpells.SPELL_DH_DEMONIC))
				{
					var aur = caster.GetAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC);

					if (aur != null)
						aur.ModDuration(8 * Time.InMilliseconds);
					else
						aur = caster.AddAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC, caster);

					if (aur != null)
						aur.SetDuration(10 * Time.InMilliseconds);
				}
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicTriggerSpell));
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 2, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
			AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 2, AuraType.Dummy, AuraEffectHandleModes.Real));
		}
	}


	// 198030 - Eye Beam damage
	[SpellScript(198030)]
	public class spell_demon_hunter_eye_beam_damage : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void FilterTargets(List<WorldObject> unitList)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			unitList.Clear();
			var units = new List<Unit>();
			caster.GetAttackableUnitListInRange(units, 25.0f);


			units.RemoveIf((Unit unit) => { return !caster.HasInLine(unit, 5.0f, caster.GetObjectScale()); });

			foreach (var unit in units)
				unitList.Add(unit);
		}

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitRectCasterEnemy));
		}
	}


	// Master of the Glaive - 203556
	[SpellScript(203556)]
	public class spell_dh_master_of_the_glaive : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			if (eventInfo.GetSpellInfo().Id == DemonHunterSpells.SPELL_DH_THROW_GLAIVE)
				return true;

			return false;
		}
	}


	// Fel Eruption - 211881
	[SpellScript(211881)]
	public class spell_dh_fel_eruption : SpellScript, ISpellBeforeHit, ISpellOnHit
	{
		public void OnHit()
		{
			var caster = GetCaster();
			var target = GetExplTargetUnit();

			if (caster == null || target == null)
				return;

			caster.CastSpell(target, DemonHunterSpells.SPELL_DH_FEL_ERUPTION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, 1));
		}

		public void BeforeHit(SpellMissInfo missInfo)
		{
			var caster = GetCaster();
			var target = GetExplTargetUnit();

			if (caster == null || target == null)
				return;

			if (missInfo == SpellMissInfo.Immune || missInfo == SpellMissInfo.Immune2)
				caster.CastSpell(target, DemonHunterSpells.SPELL_DH_FEL_ERUPTION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, 2));
		}
	}


	// Last resort - 209258
	[SpellScript(209258)]
	public class spell_dh_last_resort : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_LAST_RESORT_DEBUFF, Difficulty.None) != null)
				return false;

			return true;
		}

		private void CalcAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
		{
			amount = -1;
		}

		private void HandleAbsorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref uint absorbAmount)
		{
			var target = GetTarget();

			if (target == null)
				return;

			if (dmgInfo.GetDamage() < target.GetHealth())
				return;

			if (target.HasAura(DemonHunterSpells.SPELL_DH_LAST_RESORT_DEBUFF))
				return;

			var healthPct = GetSpellInfo().GetEffect(1).IsEffect() ? GetSpellInfo().GetEffect(1).BasePoints : 0;
			target.SetHealth(1);
			var healInfo = new HealInfo(target, target, target.CountPctFromMaxHealth(healthPct), GetSpellInfo(), (SpellSchoolMask)GetSpellInfo().SchoolMask);
			target.HealBySpell(healInfo);
			// We use AddAura instead of CastSpell, since if the spell is on cooldown, it will not be casted
			target.AddAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_VENGEANCE, target);
			target.CastSpell(target, DemonHunterSpells.SPELL_DH_LAST_RESORT_DEBUFF, true);

			absorbAmount = dmgInfo.GetDamage();
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.SchoolAbsorb));
			AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
		}
	}


	// Fel Barrage - 211053
	[SpellScript(211053)]
	public class spell_dh_fel_barrage : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private int _charges = 1;

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (!Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_FEL_BARRAGE, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_FEL_BARRAGE_TRIGGER, Difficulty.None))
				return false;

			return true;
		}

		public override bool Load()
		{
			var caster = GetCaster();

			if (caster == null || GetSpellInfo() == null)
				return false;

			var chargeCategoryId = GetSpellInfo().ChargeCategoryId;

			while (caster.GetSpellHistory().HasCharge(chargeCategoryId))
			{
				caster.GetSpellHistory().ConsumeCharge(chargeCategoryId);
				_charges++;
			}

			return true;
		}

		private void HandleTrigger(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetTarget();

			if (caster == null || target == null)
				return;

			var args = new CastSpellExtraArgs();
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)_charges);
			args.SetTriggerFlags(TriggerCastFlags.FullMask);
			caster.CastSpell(target, DemonHunterSpells.SPELL_DH_FEL_BARRAGE_TRIGGER, args);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandleTrigger, 0, AuraType.PeriodicDummy));
		}
	}

	// Fel Barrage Damage - 211052
	[SpellScript(211052)]
	public class spell_dh_fel_barrage_damage : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var chargesUsed = GetSpellValue().EffectBasePoints[0];
			var dmg = GetHitDamage();
			SetHitDamage((float)(dmg * chargesUsed) / 5.0f);
		}
	}


	// Fel Barrage aura - 222703
	[SpellScript(222703)]
	public class spell_dh_fel_barrage_aura : AuraScript, IHasAuraEffects, IAuraCheckProc
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		//Blade Dance    //Chaos Strike   //Fel Barrage
		List<uint> _removeSpellIds = new List<uint>()
								 {
									 199552,
									 210153,
									 222031,
									 227518,
									 211052
								 };

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			// Blade Dance, Chaos Strike and Annihilation have many damagers,
			// so we accept only 1 of those, and we remove the others
			// Also we remove fel barrage itself too.
			if (eventInfo.GetSpellInfo() != null)
				return false;

			return !_removeSpellIds.Contains(eventInfo.GetSpellInfo().Id);
		}

		private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
		{
			PreventDefaultAction();

			var caster = GetCaster();

			if (caster == null)
				return;

			var chargeCatId = Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_FEL_BARRAGE, Difficulty.None).ChargeCategoryId;

			if (chargeCatId != 0)
				caster.GetSpellHistory().RestoreCharge(chargeCatId);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}


	// Nemesis - 206491
	[SpellScript(206491)]
	public class spell_dh_nemesis : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandleAfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			if (GetTargetApplication() == null)
				return;

			if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
				return;

			var target = GetTargetApplication().GetTarget();
			var type = target.GetCreatureType();
			var dur = GetTargetApplication().GetBase().GetDuration();
			var caster = GetAura().GetCaster();

			if (caster == null || target == null)
				return;

			uint spellId = 0;

			switch (type)
			{
				case CreatureType.Aberration:
					spellId = NemesisSpells.NEMESIS_ABERRATION;

					break;
				case CreatureType.Beast:
					spellId = NemesisSpells.NEMESIS_BEASTS;

					break;
				case CreatureType.Critter:
					spellId = NemesisSpells.NEMESIS_CRITTERS;

					break;
				case CreatureType.Demon:
					spellId = NemesisSpells.NEMESIS_DEMONS;

					break;
				case CreatureType.Dragonkin:
					spellId = NemesisSpells.NEMESIS_DRAGONKIN;

					break;
				case CreatureType.Elemental:
					spellId = NemesisSpells.NEMESIS_ELEMENTAL;

					break;
				case CreatureType.Giant:
					spellId = NemesisSpells.NEMESIS_GIANTS;

					break;
				case CreatureType.Humanoid:
					spellId = NemesisSpells.NEMESIS_HUMANOID;

					break;
				case CreatureType.Mechanical:
					spellId = NemesisSpells.NEMESIS_MECHANICAL;

					break;
				case CreatureType.Undead:
					spellId = NemesisSpells.NEMESIS_UNDEAD;

					break;
				default:
					break;
			}

			if (spellId != 0)
			{
				var aur = caster.AddAura(spellId, caster);

				if (aur != null)
					aur.SetDuration(dur);
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 0, AuraType.ModSchoolMaskDamageFromCaster, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}
	}


	public class event_dh_infernal_strike : BasicEvent
	{
		public event_dh_infernal_strike(Unit caster)
		{
			this._caster = caster;
		}

		public override bool Execute(ulong UnnamedParameter, uint UnnamedParameter2)
		{
			if (_caster != null)
			{
				_caster.CastSpell(_caster, DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_DAMAGE, true);

				if (_caster.HasAura(DemonHunterSpells.SPELL_DH_RAIN_OF_CHAOS))
					_caster.CastSpell(_caster, DemonHunterSpells.SPELL_DH_RAIN_OF_CHAOS_SLOW, true);

				if (_caster.HasAura(DemonHunterSpells.SPELL_DH_ABYSSAL_STRIKE))
					_caster.CastSpell(_caster, DemonHunterSpells.SPELL_DH_SIGIL_OF_FLAME_NO_DEST, true);
			}

			return true;
		}

		private readonly Unit _caster;
	}

	// Infernal Strike - 189110
	// 189110 - Infernal Strike
	[SpellScript(189110)]
	public class spell_dh_infernal_strike : SpellScript, ISpellOnCast, ISpellOnHit
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_JUMP, DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_DAMAGE);
		}

		public void OnHit()
		{
			var caster = GetCaster();
			var dest = GetHitDest();
			var target = GetHitUnit();

			if (caster == null || dest == null || target == null)
				return;

			if (target.IsHostileTo(caster))
			{
				caster.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_JUMP, true);
				caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_VISUAL, true);
			}
		}

		public void OnCast()
		{
			var caster = GetCaster();

			if (caster != null)
				caster.m_Events.AddEventAtOffset(new event_dh_infernal_strike(caster), TimeSpan.FromMilliseconds(750));
		}

	}

	// Soul Cleave - 228477
	[SpellScript(228477)]
	public class spell_dh_soul_cleave : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHeal(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (caster.GetTypeId() != TypeId.Player)
				return;

			if (caster.HasAura(DemonHunterSpells.SPELL_DH_FEAST_OF_SOULS))
				caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FEAST_OF_SOULS_HEAL, true);
		}

		private void HandleDummy(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			// Consume all soul fragments in 25 yards;
			var fragments = new List<List<AreaTrigger>>();
			fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS));
			fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_DEMON));
			fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD));
			var range = GetEffectInfo().BasePoints;

			foreach (var vec in fragments)
			{
				foreach (var at in vec)
				{
					if (!caster.IsWithinDist(at, range))
						continue;

					TempSummon tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPositionX(), at.GetPositionY(), at.GetPositionZ(), 0, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(100));
					if (tempSumm != null)
					{
						tempSumm.SetFaction(caster.GetFaction());
						tempSumm.SetSummonerGUID(caster.GetGUID());
						var bp = 0;

						switch (at.GetTemplate().Id.Id)
						{
							case 6007:
							case 5997:
								bp = (int)ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_HEAL_VENGEANCE;

								break;
							case 6710:
								bp = (int)ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD_HEAL;

								break;
						}

						caster.CastSpell(tempSumm, ShatteredSoulsSpells.SPELL_DH_CONSUME_SOUL_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp));

						if (at.GetTemplate().Id.Id == 6007)
							caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_DEMON_BONUS, true);

						if (caster.HasAura(DemonHunterSpells.SPELL_DH_FEED_THE_DEMON))
							caster.GetSpellHistory().ModifyCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_DEMON_SPIKES, Difficulty.None).ChargeCategoryId, TimeSpan.FromMilliseconds(-1000));

						if (caster.HasAura(ShatteredSoulsSpells.SPELL_DH_PAINBRINGER))
							caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_PAINBRINGER_BUFF, true);

						var soulBarrier = caster.GetAuraEffect(DemonHunterSpells.SPELL_DH_SOUL_BARRIER, 0);

						if (soulBarrier != null)
						{
							var amount = soulBarrier.GetAmount() + ((float)(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_SOUL_BARRIER, Difficulty.None).GetEffect(1).BasePoints) / 100.0f) * caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
							soulBarrier.SetAmount(amount);
						}

						at.SetDuration(0);
					}
				}
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new EffectHandler(HandleHeal, 3, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
		}
	}


	// Soul cleave damage - 228478
	[SpellScript(228478)]
	public class spell_dh_soul_cleave_damage : SpellScript, IHasSpellEffects, ISpellOnHit
	{
		public List<ISpellEffect> SpellEffects => new();

		private readonly int m_ExtraSpellCost = 0;

		public void OnHit()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			float dmg = GetHitDamage() * 2;
			dmg *= caster.VariableStorage.GetValue<float>("lastSoulCleaveMod", 0);
			SetHitDamage(dmg);
		}

		private void HandleDamage(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var dmg = GetHitDamage() * 2;
			dmg = (int)((float)dmg * (((float)m_ExtraSpellCost + 300.0f) / 600.0f));
			SetHitDamage(dmg);

			caster.SetPower(PowerType.Pain, caster.GetPower(PowerType.Pain) - m_ExtraSpellCost);
			caster.ToPlayer().SetPower(PowerType.Pain, caster.GetPower(PowerType.Pain) - m_ExtraSpellCost);

			if (caster.HasAura(DemonHunterSpells.SPELL_DH_GLUTTONY_BUFF))
				caster.RemoveAurasDueToSpell(DemonHunterSpells.SPELL_DH_GLUTTONY_BUFF);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.WeaponPercentDamage, SpellScriptHookType.EffectHitTarget));
		}
	}

	// Fiery Brand - 204021
	[SpellScript(204021)]
	public class spell_dh_fiery_brand : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_FIERY_BRAND_DOT, DemonHunterSpells.SPELL_DH_FIERY_BRAND_MARKER);
		}

		private void HandleDamage(uint UnnamedParameter)
		{
			var target = GetHitUnit();

			if (target != null)
				GetCaster().CastSpell(target, DemonHunterSpells.SPELL_DH_FIERY_BRAND_DOT, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}

	// Fiery Brand Dot - 207771
	[SpellScript(207771)]
	public class spell_dh_fiery_brand_dot : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void PeriodicTick(AuraEffect aurEff)
		{
			var caster = GetCaster();

			if (caster == null || !caster.HasAura(DemonHunterSpells.SPELL_DH_BURNING_ALIVE))
				return;

			var unitList = new List<Unit>();
			GetTarget().GetAnyUnitListInRange(unitList, 8.0f);

			foreach (var target in unitList)
				if (!target.HasAura(DemonHunterSpells.SPELL_DH_FIERY_BRAND_DOT) && !target.HasAura(DemonHunterSpells.SPELL_DH_FIERY_BRAND_MARKER) && !caster.IsFriendlyTo(target))
				{
					caster.CastSpell(target, DemonHunterSpells.SPELL_DH_FIERY_BRAND_MARKER, true);

					break;
				}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 2, AuraType.PeriodicDamage));
		}
	}

	// Razor Spikes - 209400
	[SpellScript(209400)]
	public class spell_dh_razor_spikes : AuraScript, IAuraCheckProc
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_RAZOR_SPIKES_SLOW);
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			var damageInfo = eventInfo.GetDamageInfo();

			if (damageInfo == null)
				return false;

			if (damageInfo.GetAttackType() == WeaponAttackType.BaseAttack || damageInfo.GetAttackType() == WeaponAttackType.OffAttack)
			{
				var caster = damageInfo.GetAttacker();
				var target = damageInfo.GetVictim();

				if (caster == null || target == null || !caster.ToPlayer())
					return false;

				if (!caster.IsValidAttackTarget(target))
					return false;

				if (caster.HasAura(DemonHunterSpells.SPELL_DH_DEMON_SPIKES_BUFF))
					caster.m_Events.AddEventAtOffset(() => { caster.CastSpell(target, DemonHunterSpells.SPELL_DH_RAZOR_SPIKES_SLOW, true); }, TimeSpan.FromMilliseconds(750));

				return true;
			}

			return false;
		}
	}


	// Soul Barrier - 263648
	[SpellScript(263648)]
	public class spell_dh_soul_barrier : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void CalcAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var player = caster.ToPlayer();

			if (player != null)
			{
				var coeff = amount / 100.0f;
				var soulShardCoeff = GetSpellInfo().GetEffect(1).BasePoints / 100.0f;
				var ap = player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);

				amount = (int)(coeff * ap);

				// Consume all soul fragments in 25 yards;
				var fragments = new List<List<AreaTrigger>>();
				fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS));
				fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_DEMON));
				fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD));
				var range = 25.0f;

				foreach (var vec in fragments)
				{
					foreach (var at in vec)
					{
						if (!caster.IsWithinDist(at, range))
							continue;

						TempSummon tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPositionX(), at.GetPositionY(), at.GetPositionZ(), 0, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(100));
						if (tempSumm != null)
						{
							tempSumm.SetFaction(caster.GetFaction());
							tempSumm.SetSummonerGUID(caster.GetGUID());
							var bp = 0;

							switch (at.GetTemplate().Id.Id)
							{
								case 6007:
								case 5997:
									bp = (int)ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_HEAL_VENGEANCE;

									break;
								case 6710:
									bp = (int)ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD_HEAL;

									break;
							}

							caster.CastSpell(tempSumm, ShatteredSoulsSpells.SPELL_DH_CONSUME_SOUL_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp));

							if (at.GetTemplate().Id.Id == 6007)
								caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_DEMON_BONUS, true);

							if (caster.HasAura(DemonHunterSpells.SPELL_DH_FEED_THE_DEMON))
								caster.GetSpellHistory().ModifyCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_DEMON_SPIKES, Difficulty.None).ChargeCategoryId, TimeSpan.FromMilliseconds(-1000));

							if (caster.HasAura(ShatteredSoulsSpells.SPELL_DH_PAINBRINGER))
								caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_PAINBRINGER_BUFF, true);

							amount += (int)(soulShardCoeff * ap);

							at.SetDuration(0);
						}
					}
				}
			}

			var appList = caster.GetAuraApplication(DemonHunterSpells.SPELL_DH_SOUL_BARRIER);

			if (appList != null)
				foreach (var app in appList)
					app.ClientUpdate();
		}

		private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var threshold = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.4914f;

			if (absorbAmount < dmgInfo.GetDamage())
				aurEff.SetAmount(absorbAmount + threshold);

			var appList = caster.GetAuraApplication(DemonHunterSpells.SPELL_DH_SOUL_BARRIER);

			if (appList != null)
				foreach (var app in appList)
					app.ClientUpdate();
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.SchoolAbsorb));
			AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
		}
	}


	// Nether Bond - 207810
	[SpellScript(207810)]
	public class spell_dh_nether_bond : SpellScript, ISpellOnCast
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (!Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_NETHER_BOND_DAMAGE, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_NETHER_BOND_PERIODIC, Difficulty.None))
				return false;

			return true;
		}

		public void OnCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_NETHER_BOND_PERIODIC, true);
		}
	}


	// Nether Bond periodic - 207811
	[SpellScript(207811)]
	public class spell_dh_nether_bond_periodic : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private Unit m_BondUnit;

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			// Try to get the bonded Unit
			if (m_BondUnit == null)
				m_BondUnit = GetBondUnit();

			// If still not found, return
			if (m_BondUnit == null)
				return;

			ulong casterHealBp = 0;
			ulong casterDamageBp = 0;
			ulong targetHealBp = 0;
			ulong targetDamageBp = 0;

			var casterHp = caster.GetHealthPct();
			var targetHp = m_BondUnit.GetHealthPct();
			var healthPct = (casterHp + targetHp) / 2.0f;

			if (casterHp < targetHp)
			{
				casterHealBp = caster.CountPctFromMaxHealth(healthPct) - caster.GetHealth();
				targetDamageBp = m_BondUnit.GetHealth() - m_BondUnit.CountPctFromMaxHealth(healthPct);
			}
			else
			{
				casterDamageBp = caster.GetHealth() - caster.CountPctFromMaxHealth(healthPct);
				targetHealBp = m_BondUnit.CountPctFromMaxHealth(healthPct) - m_BondUnit.GetHealth();
			}

			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_NETHER_BOND_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, casterDamageBp).AddSpellMod(SpellValueMod.BasePoint1, casterHealBp));
			caster.CastSpell(m_BondUnit, DemonHunterSpells.SPELL_DH_NETHER_BOND_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, targetDamageBp).AddSpellMod(SpellValueMod.BasePoint1, targetHealBp));
		}

		private Unit GetBondUnit()
		{
			var caster = GetCaster();

			if (caster == null)
				return null;

			var units = new List<Unit>();
			AnyUnitInObjectRangeCheck check = new AnyUnitInObjectRangeCheck(caster, 100.0f);
			UnitListSearcher search = new UnitListSearcher(caster, units, check);
			Cell.VisitAllObjects(caster, search, 100.0f);

			foreach (var u in units)
				if (u.HasAura(DemonHunterSpells.SPELL_DH_NETHER_BOND, caster.GetGUID()))
					return u;

			return null;
		}

		private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			m_BondUnit = GetBondUnit();
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
		}
	}


	/// Honor Talents

	// Solitude - 211509
	[SpellScript(211509)]
	public class spell_dh_solitude : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_SOLITUDE_BUFF, Difficulty.None) != null)
				return false;

			return true;
		}

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			PreventDefaultAction();

			var caster = GetCaster();

			if (caster == null || !GetSpellInfo().GetEffect(1).IsEffect())
				return;

			var range = GetSpellInfo().GetEffect(1).BasePoints;
			var allies = new List<Unit>();
			AnyFriendlyUnitInObjectRangeCheck check = new AnyFriendlyUnitInObjectRangeCheck(caster, caster, range, true);
			UnitListSearcher searcher = new UnitListSearcher(caster, allies, check);
			Cell.VisitAllObjects(caster, searcher, range);
			allies.Remove(caster);

			if (allies.Count == 0 && !caster.HasAura(DemonHunterSpells.SPELL_DH_SOLITUDE_BUFF))
				caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_SOLITUDE_BUFF, true);
			else if (allies.Count > 0)
				caster.RemoveAurasDueToSpell(DemonHunterSpells.SPELL_DH_SOLITUDE_BUFF);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
		}
	}


	// Awaken the demon - 205598
	[SpellScript(205598)]
	public class spell_dh_awaken_the_demon : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			var caster = GetCaster();

			if (caster == null || eventInfo.GetDamageInfo() != null)
				return;

			if (!GetSpellInfo().GetEffect(1).IsEffect() || !GetSpellInfo().GetEffect(2).IsEffect())
				return;

			var threshold1 = caster.CountPctFromMaxHealth(aurEff.GetBaseAmount());
			var threshold2 = caster.CountPctFromMaxHealth(GetSpellInfo().GetEffect(1).BasePoints);
			var duration = GetSpellInfo().GetEffect(2).BasePoints;

			if (caster.GetHealth() - eventInfo.GetDamageInfo().GetDamage() < threshold1)
			{
				if (caster.HasAura(DemonHunterSpells.SPELL_DH_AWAKEN_THE_DEMON_CD))
					return;

				caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_AWAKEN_THE_DEMON_CD, true);
				var aur = caster.GetAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC);

				if (aur != null)
				{
					aur.SetDuration(Math.Min(duration * Time.InMilliseconds + aur.GetDuration(), aur.GetMaxDuration()));

					return;
				}

				aur = caster.AddAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC, caster);

				if (aur != null)
					aur.SetDuration(duration * Time.InMilliseconds);
			}

			// Check only if we are above the second threshold and we are falling under it just now
			if (caster.GetHealth() > threshold2 && caster.GetHealth() - eventInfo.GetDamageInfo().GetDamage() < threshold2)
			{
				var aur = caster.GetAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_HAVOC);

				if (aur != null)
				{
					aur.SetDuration(Math.Min(duration * Time.InMilliseconds + aur.GetDuration(), aur.GetMaxDuration()));

					return;
				}
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}


	public class auraData
	{
		public auraData(uint id, ObjectGuid casterGUID)
		{
			m_id = id;
			m_casterGuid = casterGUID;
		}

		public uint m_id;
		public ObjectGuid m_casterGuid = new();
	}

	// Reverse Magic - 205604
	[SpellScript(205604)]
	public class spell_dh_reverse_magic : SpellScript, ISpellOnCast
	{
		public void OnCast()
		{
			var player = GetCaster();

			if (player == null || !player.ToPlayer())
				return;

			Unit _player = player.ToPlayer();

			var allies = new List<Unit>();
			AnyFriendlyUnitInObjectRangeCheck check = new AnyFriendlyUnitInObjectRangeCheck(_player, _player, 10.0f, true);
			UnitListSearcher searcher = new UnitListSearcher(_player, allies, check);
			Cell.VisitAllObjects(_player, searcher, 10.0f);

			foreach (var unit in allies)
			{
				var auraListToRemove = new SortedSet<auraData>();
				var AuraList = unit.GetAppliedAuras();

				foreach (var iter in AuraList.KeyValueList)
				{
					Aura aura = iter.Value.GetBase();

					if (aura == null)
						continue;

					var caster = aura.GetCaster();

					if (caster == null || caster.GetGUID() == unit.GetGUID())
						continue;

					if (!caster.IsWithinDist(unit, 40.0f))
						continue;

					if (aura.GetSpellInfo().IsPositive())
						continue;

					if (aura.GetSpellInfo().Dispel != DispelType.Magic)
						continue;

					var creature = caster.ToCreature();

					if (creature != null)
					{
						if (creature.GetCreatureTemplate().Rank == CreatureEliteType.WorldBoss)
							continue;

						if (creature.GetCreatureTemplate().Rank == CreatureEliteType.Elite && creature.GetMap().IsDungeon())
							continue;
					}

					var targetAura = unit.AddAura(aura.GetSpellInfo().Id, caster);

					if (targetAura != null)
					{
						for (uint i = 0; i < SpellConst.MaxEffects; ++i)
						{
							targetAura.SetMaxDuration(aura.GetMaxDuration());
							targetAura.SetDuration(aura.GetDuration());

							if (targetAura.GetEffect(i) != null && aura.GetEffect(i) != null)
							{
								var auraEffect = unit.GetAuraEffect(aura.GetSpellInfo().Id, i);

								if (auraEffect == null)
									continue;

								var amount = auraEffect.GetAmount();

								if (auraEffect.GetAuraType() == AuraType.PeriodicDamage || auraEffect.GetAuraType() == AuraType.PeriodicDamagePercent)
									amount = (int)caster.SpellDamageBonusDone(unit, aura.GetSpellInfo(), amount, DamageEffectType.DOT, aura.GetSpellInfo().GetEffects()[(int)i], auraEffect.GetBase().GetStackAmount());

								//targetAura->GetEffect(i)->VariableStorage.Set("DontRecalculatePerodics", true);
								targetAura.GetEffect(i).SetAmount(amount);
								targetAura.GetEffect(i).SetPeriodicTimer(auraEffect.GetPeriodicTimer());
							}
						}

						targetAura.SetNeedClientUpdateForTargets();
					}

					auraListToRemove.Add(new auraData(aura.GetSpellInfo().Id, caster.GetGUID()));
				}

				foreach (var aura in auraListToRemove)
				{
					unit.RemoveAura(aura.m_id, aura.m_casterGuid);
				}

				auraListToRemove.Clear();
			}
		}
	}


	// Eye of leotheras - 206649
	[SpellScript(206649)]
	public class spell_dh_eye_of_leotheras : AuraScript, IAuraCheckProc
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_EYE_OF_LEOTHERAS_DAMAGE, Difficulty.None) != null)
				return false;

			return true;
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			var caster = GetCaster();
			var target = GetAura().GetOwner();

			if (caster == null || target == null || eventInfo.GetSpellInfo() != null || !caster.ToPlayer())
				return false;

			var unitTarget = target.ToUnit();

			if (unitTarget == null || eventInfo.GetSpellInfo().IsPositive())
				return false;

			var aurEff = GetAura().GetEffect(0);

			if (aurEff != null)
			{
				var bp = aurEff.GetAmount();
				GetAura().RefreshDuration();


				caster.m_Events.AddEventAtOffset(() => { caster.CastSpell(unitTarget, DemonHunterSpells.SPELL_DH_EYE_OF_LEOTHERAS_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp)); }, TimeSpan.FromMilliseconds(100));

				return true;
			}

			return false;
		}
	}


	// Immolation Aura - 258920
	[SpellScript(258920)]
	public class spell_dh_immolation_aura : SpellScript, ISpellOnCast
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_CLEANSED_BY_FLAME, DemonHunterSpells.SPELL_DH_CLEANSED_BY_FLAME_DISPEL, DemonHunterSpells.SPELL_DH_FALLOUT, ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE);
		}

		public void OnCast()
		{
			var caster = GetCaster();

			if (caster.HasAura(DemonHunterSpells.SPELL_DH_CLEANSED_BY_FLAME))
				caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_CLEANSED_BY_FLAME_DISPEL, true);

			/*
			if (RandomHelper.randChance(40) && caster->HasAura(SPELL_DH_FALLOUT))
			    caster->CastSpell(caster, SPELL_DH_SHATTERED_SOULS_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)SPELL_DH_LESSER_SOUL_SHARD));
			*/
		}
	}

	// Immolation Aura damage - 258922
	[SpellScript(258922)]
	public class spell_dh_immolation_aura_damage : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		uint[] _hit = new uint[]
					{
						DemonHunterSpells.SPELL_DH_FIERY_BRAND_DOT,
						DemonHunterSpells.SPELL_DH_FIERY_BRAND_MARKER
					};


		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_CHARRED_FLESH, DemonHunterSpells.SPELL_DH_FIERY_BRAND_DOT, DemonHunterSpells.SPELL_DH_FIERY_BRAND_MARKER);
		}

		private void HandleHit(uint UnnamedParameter)
		{
			var target = GetHitUnit();

			if (target != null)
				if (GetCaster().HasAura(DemonHunterSpells.SPELL_DH_CHARRED_FLESH))
				{
					foreach (uint spellId in _hit)
					{
						var fieryBrand = target.GetAura(spellId);

						if (fieryBrand != null)
						{
							var durationMod = GetCaster().GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_CHARRED_FLESH, 0);
							fieryBrand.ModDuration(durationMod);
						}
					}
				}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}

	// Jagged Spikes - 208796
	[SpellScript(208796)]
	public class spell_dh_jagged_spikes : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
		{
			var caster = GetCaster();
			var target = eventInfo.GetActor();

			if (caster == null || eventInfo.GetDamageInfo() != null)
				return;

			if (caster.IsFriendlyTo(target))
				return;

			var pct = caster.GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_JAGGED_SPIKES, 0);
			var damage = eventInfo.GetDamageInfo().GetDamage();
			MathFunctions.ApplyPct(ref damage, pct);

			caster.CastSpell(target, DemonHunterSpells.SPELL_DH_JAGGED_SPIKES_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}


	// 206966 - Fel Lance
	[SpellScript(206966)]
	public class spell_dh_fel_lance : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			var pct = GetSpellInfo().GetEffect(0).BasePoints;
			SetHitDamage(GetHitDamage() + (int)target.CountPctFromMaxHealth(pct));
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}


	// 206891 - Intimidated
	[SpellScript(206891)]
	public class spell_dh_intimidated : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private readonly List<ObjectGuid> _uniqueTargets = new();

		private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
		{
			var attacker = eventInfo.GetActor();
			var auraOwner = GetAura().GetOwner();

			if (attacker == null || auraOwner == null)
				return;

			if (attacker == GetCaster())
			{
				RefreshDuration();

				return;
			}

			if (_uniqueTargets.Count >= 4 || !auraOwner.ToUnit())
				return;

			if (_uniqueTargets.Contains(attacker.GetGUID()))
			{
				attacker.CastSpell(auraOwner.ToUnit(), GetSpellInfo().Id, true);
				_uniqueTargets.Add(attacker.GetGUID());
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ModDamagePercentTaken, AuraScriptHookType.EffectProc));
		}

	}


	// 203753 - Blade Turning
	[SpellScript(203753)]
	public class spell_dh_blade_turning : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			if ((eventInfo.GetHitMask() & ProcFlagsHit.Parry) != 0)
				return true;

			return false;
		}
	}


	// 201469 - Demon Speed
	// Called by Blur (212800) and Netherwalk (196555)
	[SpellScript(201469)]
	public class spell_dh_artifact_demon_speed : SpellScript, ISpellOnCast
	{
		public void OnCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var aurEff = caster.GetAuraEffect(DemonHunterSpells.SPELL_DH_DEMON_SPEED, 0);

			if (aurEff != null)
				for (byte i = 0; i < aurEff.GetAmount(); ++i)
					caster.GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_FEL_RUSH, Difficulty.None).ChargeCategoryId);
		}
	}

	// 201463 - Deceiver's Fury
	[SpellScript(201463)]
	public class spell_dh_artifact_deceivers_fury : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private void OnProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_DECEIVERS_FURY_ENERGIZE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)aurEff.GetAmount()));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}


	// 202443 - Anguish
	[SpellScript(202443)]
	public class spell_dh_artifact_anguish : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();
			var target = GetUnitOwner();

			if (caster == null || target == null)
				return;

			caster.CastSpell(target, DemonHunterSpells.SPELL_DH_ANGUISH_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, GetStackAmount()));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}
	}


	// 202446 - Anguish damage
	[SpellScript(202446)]
	public class spell_dh_artifact_anguish_damage : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(uint UnnamedParameter)
		{
			var stacks = GetSpellValue().AuraStackAmount;
			SetHitDamage(GetHitDamage() * stacks);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}


	// 209651 - Shattered Souls missile
	[SpellScript(209651)]
	public class spell_dh_shattered_souls_missile : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(uint effIndex)
		{
			PreventHitDefaultEffect(effIndex);
			var caster = GetCaster();

			if (caster == null)
				return;

			var spellToCast = GetSpellValue().EffectBasePoints[0];

			WorldLocation dest = GetHitDest();

			if (dest != null)
				caster.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), (uint)spellToCast, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHit));
		}
	}


	// 203783 - Shear proc
	[SpellScript(203783)]
	public class spell_dh_shear_proc : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var caster = GetCaster();

			if (caster == null || eventInfo.GetSpellInfo() != null)
				return;

			var procChance = 100;

			if (eventInfo.GetSpellInfo().Id == DemonHunterSpells.SPELL_DH_SHEAR)
			{
				procChance = 15;
				procChance += caster.GetAuraEffectAmount(ShatteredSoulsSpells.SPELL_DH_SHATTER_THE_SOULS, 0);
			}

			/*
			if (RandomHelper.randChance(procChance))
			    caster->CastSpell(caster, SPELL_DH_SHATTERED_SOULS_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)SPELL_DH_LESSER_SOUL_SHARD));
			*/

			if (caster.GetSpellHistory().HasCooldown(DemonHunterSpells.SPELL_DH_FELBLADE))
				if (RandomHelper.randChance(caster.GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_SHEAR_PROC, 3)))
					caster.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_FELBLADE);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}
	}


	// 210047 - Consume Soul missile
	[SpellScript(210047)]
	public class spell_dh_consume_soul_missile : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(uint effIndex)
		{
			PreventHitDefaultEffect(effIndex);
			var caster = GetCaster();

			if (caster == null)
				return;

			var spellToCast = GetSpellValue().EffectBasePoints[0];
			caster.CastSpell(caster, (uint)spellToCast, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHit));
		}
	}


	// 209426 - Darkness absorb
	[SpellScript(209426)]
	public class spell_dh_darkness_absorb : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
		{
			amount = -1;
		}

		private void OnAbsorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref uint absorbAmount)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var chance = GetSpellInfo().GetEffect(1).BasePoints + caster.GetAuraEffectAmount(ShatteredSoulsSpells.SPELL_DH_COVER_OF_DARKNESS, 0);

			if (RandomHelper.randChance(chance))
				absorbAmount = dmgInfo.GetDamage();
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 0));
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		}
	}


	// 196718 - Darkness
	// MiscId - 6615
	[Script]
	public class at_dh_darkness : AreaTriggerAI
	{
		public at_dh_darkness(AreaTrigger areatrigger) : base(areatrigger)
		{
		}

		private bool entered;

		public override void OnInitialize()
		{
			at.SetDuration(8000);
		}

		public override void OnUnitEnter(Unit unit)
		{
			var caster = at.GetCaster();

			if (caster == null || unit == null)
				return;

			if (caster.IsFriendlyTo(unit) && !unit.HasAura(DemonHunterSpells.SPELL_DH_DARKNESS_ABSORB))
			{
				entered = true;

				if (entered)
				{
					caster.CastSpell(unit, DemonHunterSpells.SPELL_DH_DARKNESS_ABSORB, true);
					entered = false;
				}
			}
		}

		public override void OnUnitExit(Unit unit)
		{
			var caster = at.GetCaster();

			if (caster == null || unit == null)
				return;

			if (unit.HasAura(DemonHunterSpells.SPELL_DH_DARKNESS_ABSORB))
				unit.RemoveAurasDueToSpell(DemonHunterSpells.SPELL_DH_DARKNESS_ABSORB, caster.GetGUID());
		}
	}

	// 203650 - Prepared
	[SpellScript(203650)]
	public class spell_dh_prepared : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void PeriodicTick(AuraEffect aurEff)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.ModifyPower(PowerType.Fury, aurEff.GetAmount() / 10.0f);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.ModPowerRegen));
		}
	}


	// 206478 - Demonic Appetite
	[SpellScript(206478)]
	public class spell_dh_demonic_appetite : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
		{
			PreventDefaultAction();
			var caster = GetCaster();

			if (caster == null)
				return;

			//caster->CastSpell(caster, SPELL_DH_SHATTERED_SOULS_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)SPELL_DH_LESSER_SOUL_SHARD));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}
	}


	// Soul Fragment healing effects - 178963, 203794, 228532
	[SpellScript(new uint[] { 178963, 203794, 228532 })]
	public class spell_dh_soul_fragment_heals : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(uint effIndex)
		{
			if (!GetCaster().HasAura(DemonHunterSpells.SPELL_DH_DEMONIC_APPETITE))
				PreventHitDefaultEffect(effIndex);
		}

		private void HandleHeal(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var reductionTime = caster.GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_FEAST_ON_THE_SOULS, 0);

			if (reductionTime != 0)
			{
				caster.GetSpellHistory().ModifyCooldown(DemonHunterSpells.SPELL_DH_CHAOS_NOVA, TimeSpan.FromSeconds(-reductionTime));
				caster.GetSpellHistory().ModifyCooldown(DemonHunterSpells.SPELL_DH_EYE_BEAM, TimeSpan.FromSeconds(-reductionTime));
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.LaunchTarget));
			SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.Launch));
		}
	}


	// 206475 - Chaos Cleave
	[SpellScript(206475)]
	public class spell_dh_chaos_cleave : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var caster = GetCaster();

			if (caster == null || eventInfo.GetDamageInfo() != null)
				return;

			var damage = MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_CHAOS_CLEAVE_PROC, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}
	}


	// 205411 - Desperate Instints
	[SpellScript(205411)]
	public class spell_dh_desperate_instincts : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var caster = GetCaster();

			if (caster == null || eventInfo.GetDamageInfo() != null)
				return;

			if (caster.GetSpellHistory().HasCooldown(DemonHunterSpells.SPELL_DH_BLUR_BUFF))
				return;

			var triggerOnHealth = caster.CountPctFromMaxHealth(aurEff.GetAmount());
			var currentHealth = caster.GetHealth();

			// Just falling below threshold
			if (currentHealth > triggerOnHealth && (currentHealth - eventInfo.GetDamageInfo().GetDamage()) <= triggerOnHealth)
				caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_BLUR_BUFF, false);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.TriggerSpellOnHealthPct, AuraScriptHookType.EffectProc));
		}
	}


	// 209795 - Fracture
	[SpellScript(209795)]
	public class spell_dh_fracture : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			//  for (uint8 i = 0; i < 2; ++i)
			//caster->CastCustomSpell(SPELL_DH_SHATTERED_SOULS_MISSILE, SpellValueMod.BasePoint0, SPELL_DH_LESSER_SOUL_SHARD, caster, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}


	// 212084 - Fel Devastation
	[SpellScript(212084)]
	public class spell_dh_fel_devastation : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void PeriodicTick(AuraEffect aurEff)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (aurEff.GetTickNumber() == 1)
				return;

			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FEL_DEVASTATION_DAMAGE, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
		}
	}


	// 212105 - Fel Devastation damage
	[SpellScript(212105)]
	public class spell_dh_fel_devastation_damage : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private bool firstHit = true;

		private void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (firstHit)
			{
				firstHit = false;
				caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FEL_DEVASTATION_HEAL, true);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}


	// 218679 - Spirit Bomb
	[SpellScript(218679)]
	public class spell_dh_spirit_bomb : SpellScript, ISpellOnHit, ISpellCheckCast
	{
		uint[] _ids = new uint[]
			{
				ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD,
				ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS,
				ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_DEMON
			};

		private bool TryCastDamage(Unit caster, Unit target, uint spellId)
		{
			AreaTrigger at = caster.GetAreaTrigger(spellId);

			if (at != null)
			{
				caster.CastSpell(target, DemonHunterSpells.SPELL_DH_SPIRIT_BOMB_DAMAGE, true);
				at.Remove();

				return true;
			}

			return false;
		}

		public void OnHit()
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			foreach (uint spellId in _ids)

			{
				if (TryCastDamage(caster, target, spellId))
					break;
			}
		}

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return SpellCastResult.CantDoThatRightNow;

			if (!caster.GetAreaTrigger(ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD) && !caster.GetAreaTrigger(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS) && !caster.GetAreaTrigger(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_DEMON))
				return SpellCastResult.CantDoThatRightNow;

			return SpellCastResult.SpellCastOk;
		}
	}

	// 224509 - Frailty
	[SpellScript(224509)]
	public class spell_dh_frailty : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();
		uint _damage = 0;
		private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var caster = GetCaster();

			if (caster == null || caster != eventInfo.GetActor() || eventInfo.GetDamageInfo() != null)
				return;

			_damage += MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
		}

		private void PeriodicTick(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (_damage != 0)
			{
				caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FRAILTY_HEAL, (int)(_damage * .1), true);
				_damage = 0;
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
		}
	}

	// 236189 - Demonic Infusion
	[SpellScript(236189)]
	public class spell_dh_demonic_infusion : SpellScript, ISpellOnCast
	{
		public void OnCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.GetSpellHistory().ResetCharges(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_DEMON_SPIKES, Difficulty.None).ChargeCategoryId);
			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_DEMON_SPIKES, true);
			caster.GetSpellHistory().ResetCharges(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_DEMON_SPIKES, Difficulty.None).ChargeCategoryId);
		}
	}

	// 201467 - Fury of the Illidari
	// MiscId - 5758
	[Script]
	public class at_dh_artifact_fury_of_the_illidari : AreaTriggerAI
	{
		public at_dh_artifact_fury_of_the_illidari(AreaTrigger areatrigger) : base(areatrigger)
		{
		}

		//   void OnInitialize() override
		//  {
		//     at->VariableStorage.Set<int32>("_timer", 500);
		//  }

		public override void OnUpdate(uint diff)
		{
			var caster = at.GetCaster();

			if (caster == null)
				return;

			//  int32 timer = at->VariableStorage.GetValue<int32>("_timer") + diff;
			/* if (timer >= 490)
			 {
			     at->VariableStorage.Set<int32>("_timer", timer - 490);
			     caster->CastSpell(at, SPELL_DH_FURY_OF_THE_ILLIDARI_MAINHAND, true);
			     caster->CastSpell(at, SPELL_DH_FURY_OF_THE_ILLIDARI_OFFHAND, true);
			 }
			 else
			     at->VariableStorage.Set("_timer", timer);*/
		}

		public override void OnRemove()
		{
			var caster = at.GetCaster();

			if (caster == null || !caster.ToPlayer())
				return;

			//   int32 rageOfTheIllidari = caster->VariableStorage.GetValue<int32>("Spells.RageOfTheIllidariDamage");
			// if (!rageOfTheIllidari)
			//     return;

			// caster->VariableStorage.Set<int32>("Spells.RageOfTheIllidariDamage", 0);

			// Cannot cast custom spell on position...
			TempSummon target = caster.SummonCreature(SharedConst.WorldTrigger, at, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(1));

			if (target != null)
				caster.CastSpell(at, DemonHunterSpells.SPELL_DH_RAGE_OF_THE_ILLIDARI_VISUAL, true);
			//  caster->m_Events.AddEventAtOffset(() =>
			// {
			//caster->CastCustomSpell(SPELL_DH_RAGE_OF_THE_ILLIDARI_DAMAGE, SpellValueMod.BasePoint0, rageOfTheIllidari, target, TriggerCastFlags.FullMask);
			//}, TimeSpan.FromMilliseconds(750), [caster, target);
		}
	}

	// 201471 - Inner Demons
	[SpellScript(201471)]
	public class spell_dh_artifact_inner_demons : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
		{
			var caster = GetCaster();
			var target = eventInfo.GetActionTarget();

			if (caster == null || target == null)
				return;

			caster.VariableStorage.Set("Spells.InnerDemonsTarget", target.GetGUID());
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}
	}

	// 202387 - Inner Demons
	// MiscId - 5823
	[Script]
	public class at_dh_artifact_inner_demons : AreaTriggerAI
	{
		public at_dh_artifact_inner_demons(AreaTrigger areatrigger) : base(areatrigger)
		{
		}

		public override void OnInitialize()
		{
			var caster = at.GetCaster();

			if (caster == null)
				return;

			var guid = caster.VariableStorage.GetValue<ObjectGuid>("Spells.InnerDemonsTarget", ObjectGuid.Empty);
			var target = ObjectAccessor.Instance.GetUnit(caster, guid);

			if (target != null)
			{
				List<Vector3> splinePoints = new();
				var orientation = caster.GetOrientation();
				float posX = caster.GetPositionX() - 7 * (float)Math.Cos(orientation);
				float posY = caster.GetPositionY() - 7 * (float)Math.Sin(orientation); // Start from behind the caster
				splinePoints.Add(new Vector3(posX, posY, caster.GetPositionZ()));
				splinePoints.Add(new Vector3(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ()));

				at.InitSplines(splinePoints, 1000);
			}
			else
				caster.VariableStorage.Remove("Spells.InnerDemonsTarget");

		}

		public override void OnRemove()
		{
			var caster = at.GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(at, DemonHunterSpells.SPELL_DH_INNER_DEMONS_DAMAGE, true);
		}
	}

	// 201472 - Rage of the Illidari
	[SpellScript(201472)]
	public class spell_dh_artifact_rage_of_the_illidari : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			var caster = GetCaster();

			if (caster == null || eventInfo.GetDamageInfo() != null)
				return;

			var damage = MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetSpellEffectInfo().BasePoints);

			if (damage == 0)
				return;

			// damage += caster->VariableStorage.GetValue<int32>("Spells.RageOfTheIllidariDamage");

			//  caster->VariableStorage.Set("Spells.RageOfTheIllidariDamage", damage);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}

	// 201464 - Overwhelming Power
	// Called by 179057 - Chaos Nova
	[SpellScript(201464)]
	public class spell_dh_artifact_overwhelming_power : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (RandomHelper.randChance(caster.GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_OVERWHELMING_POWER, 0)))
				caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE, SpellValueMod.BasePoint0, (int)ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
		}
	}

	// 207407 - Soul Carver
	[SpellScript(207407)]
	public class spell_dh_artifact_soul_carver_SpellScript : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleOnHit(uint UnnamedParameter)
		{
			var target = GetHitUnit();

			if (target != null)
			{
				var attackPower = GetCaster().GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
				var damage = (165.0f / 100.0f) * attackPower + (165.0f / 100.0f) * attackPower;
				var damageOverTime = (107.415f / 100.0f) * attackPower;
				GetCaster().CastSpell(target, DemonHunterSpells.SPELL_DH_SOUL_CARVER_DAMAGE, (int)damage);
				GetCaster().CastSpell(target, DemonHunterSpells.SPELL_DH_SOUL_CARVER_DAMAGE, (int)damageOverTime);
				// Code for shattering the soul fragments
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 2, SpellEffectName.WeaponPercentDamage, SpellScriptHookType.EffectHitTarget));
		}
	}

	[SpellScript(207407)]
	public class spell_dh_artifact_soul_carver_AuraScript : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private void PeriodicTick(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE, SpellValueMod.BasePoint0, (int)ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
		}
	}


	// 213010 - Charred Warblades
	[SpellScript(213010)]
	public class spell_dh_artifact_charred_warblades : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			var caster = GetCaster();

			if (caster == null || eventInfo.GetDamageInfo() != null)
				return;

			if (eventInfo.GetDamageInfo() != null || (eventInfo.GetDamageInfo().GetSchoolMask() & SpellSchoolMask.Fire) == 0)
				return;

			var heal = MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
			caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_CHARRED_WARBLADES_HEAL, (int)heal);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}


	// 213017 - Fueled by Pain
	[SpellScript(213017)]
	public class spell_dh_artifact_fueled_by_pain : AuraScript, IHasAuraEffects, IAuraCheckProc
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var duration = aurEff.GetAmount() * Time.InMilliseconds;
			var aur = caster.AddAura(DemonHunterSpells.SPELL_DH_METAMORPHOSIS_VENGEANCE, caster);

			if (aur != null)
			{
				aur.SetMaxDuration(duration);
				aur.RefreshDuration();
			}
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			return eventInfo.GetSpellInfo() != null && (eventInfo.GetSpellInfo().Id == ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_HEAL_VENGEANCE || eventInfo.GetSpellInfo().Id == ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD_HEAL);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}


	// 212817 - Fiery Demise
	[SpellScript(212817)]
	public class spell_dh_artifact_fiery_demise : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			var caster = GetCaster();
			var target = eventInfo.GetActionTarget();

			if (caster == null || target == null || !caster.IsValidAttackTarget(target))
				return;

			caster.CastSpell(target, ShatteredSoulsSpells.SPELL_DH_FIERY_DEMISE_DEBUFF, aurEff.GetAmount());
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}


	// 206803 - Rain from Above
	[SpellScript(206803)]
	public class spell_demon_hunter_rain_from_above : SpellScript, ISpellAfterCast
	{
		public void AfterCast()
		{
			var caster = GetCaster();

			if (caster == null || !caster.ToPlayer())
				return;

			caster.m_Events.AddEventAtOffset(() => { caster.CastSpell(caster, DemonHunterSpells.SPELL_DK_RAIN_FROM_ABOVE_SLOWFALL); }, TimeSpan.FromMilliseconds(1750));
		}
	}


	// 162264 - Metamorphosis buffs @ Vengeance
	// SpellId - SpellName
	[SpellScript(162264)]
	public class spell_demon_hunter_metamorphosis_buffs : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.RemoveAura(DemonHunterSpells.SPELL_DH_DEMONIC_ORIGINS_BUFF);
		}

		private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (caster.HasAura(DemonHunterSpells.SPELL_DH_DEMONIC_ORIGINS))
				caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_DEMONIC_ORIGINS_BUFF, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Transform, AuraEffectHandleModes.Real));
		}
	}


	// 235903 - Mana Rift
	// MiscId - 9695
	[Script]
	public class at_demon_hunter_mana_rift : AreaTriggerAI
	{
		public at_demon_hunter_mana_rift(AreaTrigger areatrigger) : base(areatrigger)
		{
		}

		public override void OnUnitExit(Unit unit)
		{
			var caster = at.GetCaster();

			if (caster == null || unit == null)
				return;

			var spellProto = Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_MANA_RIFT_SPELL, Difficulty.None);

			if (spellProto == null)
				return;

			if (at.IsRemoved())
				if (caster.IsValidAttackTarget(unit))
				{
					var hpBp = unit.CountPctFromMaxHealth(spellProto.GetEffect(1).BasePoints);
					var manaBp = unit.CountPctFromMaxPower(PowerType.Mana, spellProto.GetEffect(2).BasePoints);
					var args = new CastSpellExtraArgs();
					args.AddSpellMod(SpellValueMod.BasePoint0, hpBp);
					args.AddSpellMod(SpellValueMod.BasePoint0, manaBp);
					args.SetTriggerFlags(TriggerCastFlags.FullMask);
					caster.CastSpell(unit, DemonHunterSpells.SPELL_DH_MANA_RIFT_DAMAGE, args);
				}
		}
	}

	// 205629 - Demonic Trample
	// MiscId - 6482
	[Script]
	public class at_demon_hunter_demonic_trample : AreaTriggerAI
	{
		public at_demon_hunter_demonic_trample(AreaTrigger areatrigger) : base(areatrigger)
		{
		}

		public override void OnUnitEnter(Unit unit)
		{
			var caster = at.GetCaster();

			if (caster == null || unit == null)
				return;

			if (caster.IsValidAttackTarget(unit))
			{
				caster.CastSpell(unit, DemonHunterSpells.SPELL_DH_DEMONIC_TRAMPLE_STUN, true);
				caster.CastSpell(unit, DemonHunterSpells.SPELL_DH_DEMONIC_TRAMPLE_DAMAGE, true);
			}
		}
	}

	// 162794 - Chaos Strike
	// 201427 - Annihilation
	[SpellScript(new uint[] { 162794, 201427 })]
	public class spell_demon_hunter_chaos_strike : SpellScript, ISpellBeforeCast
	{
		public void BeforeCast()
		{
			var caster = GetCaster();
			var target = GetExplTargetUnit();

			if (caster == null || target == null)
				return;

			// Chaos Strike and Annihilation have a mainhand and an offhand spell, but the crit chance should be the same.
			var criticalChances = caster.GetUnitCriticalChanceAgainst(WeaponAttackType.BaseAttack, target);
			caster.VariableStorage.Set("Spells.ChaosStrikeCrit", RandomHelper.randChance(criticalChances));
			caster.CastSpell(DemonHunterSpells.SPELL_DH_CHAOS_STRIKE_PROC, true);
		}
	}

	// 185244 - Pain
	[SpellScript(185244)]
	public class spell_demon_hunter_pain : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
		{
			var caster = GetCaster();

			if (caster == null || eventInfo.GetDamageInfo() != null)
				return;

			if (eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsPositive())
				return;

			var damageTaken = eventInfo.GetDamageInfo().GetDamage();

			if (damageTaken <= 0)
				return;

			var painAmount = (50.0f * (float)damageTaken) / (float)caster.GetMaxHealth();
			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_REWARD_PAIN, (int)painAmount);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ModPowerDisplay, AuraScriptHookType.EffectProc));
		}
	}


	// 203704 - Mana Break
	[SpellScript(203704)]
	public class spell_demon_hunter_mana_break : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		public void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			var damage = (float)GetSpellInfo().GetEffect(1).BasePoints;
			var powerPct = target.GetPowerPct(PowerType.Mana);

			if (powerPct >= 1.0f)
				damage += (100.0f - powerPct) / 10.0f * GetSpellInfo().GetEffect(2).BasePoints;

			damage = Math.Max((float)GetHitUnit().CountPctFromMaxHealth(GetSpellInfo().GetEffect(1).BasePoints), (float)damage);

			SetHitDamage(damage);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}


	// 258881 - Trail of Ruin
	[SpellScript(258881)]
	public class spell_demon_hunter_trail_of_ruin : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			return eventInfo.GetSpellInfo().Id == Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_BLADE_DANCE, Difficulty.None).GetEffect(0).TriggerSpell;
		}
	}


	// 213480 - Unending Hatred
	[SpellScript(213480)]
	public class spell_demon_hunter_unending_hatred : AuraScript, IAuraCheckProc, IAuraOnProc
    {
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			return eventInfo.GetDamageInfo() != null && (eventInfo.GetDamageInfo().GetSchoolMask() & SpellSchoolMask.Shadow) != 0;
		}

		public void OnProc(ProcEventInfo eventInfo)
		{
			var caster = GetPlayerCaster();

			if (caster == null)
				return;

			var pointsGained = GetPointsGained(caster, eventInfo.GetDamageInfo().GetDamage());

			if (caster.GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
				caster.EnergizeBySpell(caster, GetSpellInfo(), pointsGained, PowerType.Fury);
			else if (caster.GetPrimarySpecialization() == TalentSpecialization.DemonHunterVengeance)
                caster.EnergizeBySpell(caster, GetSpellInfo(), pointsGained, PowerType.Pain);
        }

		public Player GetPlayerCaster()
		{
			var caster = GetCaster();

			if (caster == null)
				return null;

			return caster.ToPlayer();
		}

		public int GetPointsGained(Player caster, uint damage)
		{
			var damagePct = (float)damage / caster.GetMaxHealth() * 100.0f / 2;
			var max = GetSpellInfo().GetEffect(0).BasePoints;

			if (damagePct > max)
				return max;

			if (damagePct < 1F)
				return 1;

			return 0;
		}
	}


	//203720 - Demon Spikes
	[SpellScript(203720)]
	public class spell_dh_demon_spikes : SpellScript, ISpellOnCast
	{
		public void OnCast()
		{
			var caster = GetCaster();
			caster.CastSpell(203819, true);
		}
	}

	[Script]
	public class dh_shattered_souls : ScriptObjectAutoAdd, IPlayerOnCreatureKill
	{
		public dh_shattered_souls() : base("dh_shattered_souls")
		{
		}

		public void OnCreatureKill(Player player, Creature victim)
		{
			if (player.GetClass() != Class.DemonHunter)
				return;

			var fragmentPos = victim.GetRandomNearPosition(5.0f);

			if (victim.GetCreatureType() == CreatureType.Demon && RandomHelper.randChance(30))
			{
				player.CastSpell(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE, true);
				victim.CastSpell(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_DEMON, true);     //at
				player.CastSpell(ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_DEMON_BONUS, true); //buff
			}

			if (victim.GetCreatureType() != CreatureType.Demon && RandomHelper.randChance(30))
			{
				victim.CastSpell(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE, true);
				player.CastSpell(fragmentPos, ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS, true); //10665
			}

			if (player.HasAura(DemonHunterSpells.SPELL_DH_FEED_THE_DEMON))
				player.GetSpellHistory().ModifyCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_DEMON_SPIKES, Difficulty.None).ChargeCategoryId, TimeSpan.FromMilliseconds(-1000));

			if (player.HasAura(ShatteredSoulsSpells.SPELL_DH_PAINBRINGER))
				player.CastSpell(player, ShatteredSoulsSpells.SPELL_DH_PAINBRINGER_BUFF, true);

			var soulBarrier = player.GetAuraEffect(DemonHunterSpells.SPELL_DH_SOUL_BARRIER, 0);

			if (soulBarrier != null)
			{
				var amount = soulBarrier.GetAmount() + ((float)(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_SOUL_BARRIER, Difficulty.None).GetEffect(1).BasePoints) / 100.0f) * player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
				soulBarrier.SetAmount(amount);
			}
		}
	}

	//201427
	[SpellScript(201427)]
	public class spell_dh_annihilation : SpellScript, ISpellBeforeHit
	{
		public void BeforeHit(SpellMissInfo missInfo)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var target = caster.GetVictim();

				if (target == null)
					return;

				var attackPower = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) + 28.7f;
				float damage = GetHitDamage();

				SetHitDamage(damage + attackPower);

				if (RandomHelper.randChance(20))
					caster.ModifyPower(PowerType.Fury, +20);
			}
		}
	}

	//204255 normal, at 10665, 204256 demon, at 10666
	[Script]
	public class at_shattered_soul_fragment : AreaTriggerAI
	{
		public at_shattered_soul_fragment(AreaTrigger areatrigger) : base(areatrigger)
		{
		}

		public override void OnUnitEnter(Unit unit)
		{
			if (unit != at.GetCaster() || !unit.IsPlayer() || unit.ToPlayer().GetClass() != Class.DemonHunter)
				return;

			switch (at.GetEntry())
			{
				case 10665:
					if (at.GetCaster().ToPlayer().GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
						at.GetCaster().CastSpell(at.GetCaster(), ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_HEAL_25_HAVOC, true);

					at.Remove();

					break;

				case 10666:
					if (at.GetCaster().ToPlayer().GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
						at.GetCaster().CastSpell(at.GetCaster(), ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_HEAL_25_HAVOC, true);

					at.Remove();

					break;
			}
		}
	}

	//232893
	[SpellScript(232893)]
	public class spell_dh_felblade : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleOnHit(uint UnnamedParameter)
		{
			if (!GetCaster() || !GetHitUnit())
				return;

			if (GetCaster().GetDistance2d(GetHitUnit()) <= 15.0f)
			{
				GetCaster().CastSpell(GetHitUnit(), DemonHunterSpells.SPELL_DH_FELBLADE_CHARGE, true);
				GetCaster().CastSpell(GetHitUnit(), DemonHunterSpells.SPELL_DH_FELBLADE_DAMAGE, true);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}

	// Fel Rush (initial, without spec in mardum) - 344865
	[SpellScript(344865)]
	public class spell_dh_fel_rush_specless : SpellScript, ISpellOnCast
	{
		public void OnCast()
		{
			var caster = GetCaster();

			caster.CastSpell(DemonHunterSpells.SPELL_DH_FEL_RUSH, true);
		}
	}


	// Chaos Strike (initial, without spec in mardum) - 344862
	[SpellScript(344862)]
	public class spell_dh_chaos_strike_specless : SpellScript, ISpellOnCast
    {
        public void OnCast()
        {
			var caster = GetCaster();

			caster.CastSpell(DemonHunterSpells.SPELL_DH_CHAOS_STRIKE, true);
		}
	}

	[Script]
	public class DH_DisableDoubleJump_OnMount : ScriptObjectAutoAdd, IPlayerOnSpellCast
	{
		public Class PlayerClass => Class.DemonHunter;

		public DH_DisableDoubleJump_OnMount() : base("DH_DisableDoubleJump_OnMount")
		{
		}

		public void OnSpellCast(Player player, Spell spell, bool skipCheck)
		{
			if (player.GetClass() == Class.DemonHunter && player.HasAura(DemonHunterSpells.SPELL_DH_DOUBLE_JUMP) && spell.GetSpellInfo().GetEffect(0).ApplyAuraName == AuraType.Mounted)
				player.SetCanDoubleJump(false);
		}

		public void OnUpdate(Player player, uint diff)
		{
			if (player.GetClass() == Class.DemonHunter && player.HasAura(DemonHunterSpells.SPELL_DH_DOUBLE_JUMP) && !player.IsMounted() && !player.HasExtraUnitMovementFlag(MovementFlag2.CanDoubleJump))
				player.SetCanDoubleJump(true);
		}
	}


	[Script] // 197125 - Chaos Strike
	internal class spell_dh_chaos_strike : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DemonHunterSpells.ChaosStrikeEnergize);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}

		private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
			args.SetTriggeringAura(aurEff);
			GetTarget().CastSpell(GetTarget(), DemonHunterSpells.ChaosStrikeEnergize, args);
		}
	}

	[Script] // 206416 - First Blood
	internal class spell_dh_first_blood : AuraScript
	{
		private ObjectGuid _firstTargetGUID;

		public ObjectGuid GetFirstTarget()
		{
			return _firstTargetGUID;
		}

		public void SetFirstTarget(ObjectGuid targetGuid)
		{
			_firstTargetGUID = targetGuid;
		}

		public override void Register()
		{
		}
	}

	// 188499 - Blade Dance
	[Script] // 210152 - Death Sweep
	internal class spell_dh_blade_dance : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DemonHunterSpells.FirstBlood);
		}

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(DecideFirstTarget, 0, Targets.UnitSrcAreaEnemy));
		}

		private void DecideFirstTarget(List<WorldObject> targetList)
		{
			if (targetList.Empty())
				return;

			var aura = GetCaster().GetAura(DemonHunterSpells.FirstBlood);

			if (aura == null)
				return;

			var firstTargetGUID = ObjectGuid.Empty;
			var selectedTarget = GetCaster().GetTarget();

			// Prefer the selected Target if he is one of the enemies
			if (targetList.Count > 1 &&
				!selectedTarget.IsEmpty())
			{
				var foundObj = targetList.Find(obj => obj.GetGUID() == selectedTarget);

				if (foundObj != null)
					firstTargetGUID = foundObj.GetGUID();
			}

			if (firstTargetGUID.IsEmpty())
				firstTargetGUID = targetList[0].GetGUID();

			var script = aura.GetScript<spell_dh_first_blood>();

			script?.SetFirstTarget(firstTargetGUID);
		}
	}

	// 199552 - Blade Dance
	// 200685 - Blade Dance
	// 210153 - Death Sweep
	[Script] // 210155 - Death Sweep
	internal class spell_dh_blade_dance_damage : SpellScript, ISpellOnHit
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DemonHunterSpells.FirstBlood);
		}

		public void OnHit()
		{
			var damage = GetHitDamage();

			var aurEff = GetCaster().GetAuraEffect(DemonHunterSpells.FirstBlood, 0);

			if (aurEff != null)
			{
				var script = aurEff.GetBase().GetScript<spell_dh_first_blood>();

				if (script != null)
					if (GetHitUnit().GetGUID() == script.GetFirstTarget())
						MathFunctions.AddPct(ref damage, aurEff.GetAmount());
			}

			SetHitDamage(damage);
		}
	}

	// 204596 - Sigil of Flame
	// 207684 - Sigil of Misery
	// 202137 - Sigil of Silence
	[Script("areatrigger_dh_sigil_of_silence", DemonHunterSpells.SigilOfSilenceAoe)]
	[Script("areatrigger_dh_sigil_of_misery", DemonHunterSpells.SigilOfMiseryAoe)]
	[Script("areatrigger_dh_sigil_of_flame", DemonHunterSpells.SigilOfFlameAoe)]
	internal class areatrigger_dh_generic_sigil : AreaTriggerAI
	{
		private readonly uint _trigger;

		public areatrigger_dh_generic_sigil(AreaTrigger at, uint trigger) : base(at)
		{
			_trigger = trigger;
		}

		public override void OnRemove()
		{
			var caster = at.GetCaster();

			caster?.CastSpell(at.GetPosition(), _trigger, new CastSpellExtraArgs());
		}
	}

	[Script] // 208673 - Sigil of Chains
	internal class spell_dh_sigil_of_chains : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DemonHunterSpells.SigilOfChainsSlow, DemonHunterSpells.SigilOfChainsGrip);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleEffectHitTarget(uint effIndex)
		{
			var loc = GetExplTargetDest();

			if (loc != null)
			{
				GetCaster().CastSpell(GetHitUnit(), DemonHunterSpells.SigilOfChainsSlow, new CastSpellExtraArgs(true));
				GetHitUnit().CastSpell(loc.GetPosition(), DemonHunterSpells.SigilOfChainsGrip, new CastSpellExtraArgs(true));
			}
		}
	}

	[Script] // 202138 - Sigil of Chains
	internal class areatrigger_dh_sigil_of_chains : AreaTriggerAI
	{
		public areatrigger_dh_sigil_of_chains(AreaTrigger at) : base(at)
		{
		}

		public override void OnRemove()
		{
			var caster = at.GetCaster();

			if (caster != null)
			{
				caster.CastSpell(at.GetPosition(), DemonHunterSpells.SigilOfChainsVisual, new CastSpellExtraArgs());
				caster.CastSpell(at.GetPosition(), DemonHunterSpells.SigilOfChainsTargetSelect, new CastSpellExtraArgs());
			}
		}
	}

	[Script] // 131347 - Glide
	internal class spell_dh_glide : SpellScript, ISpellCheckCast, ISpellBeforeCast
	{
		public void BeforeCast()
		{
			var caster = GetCaster().ToPlayer();

			if (!caster)
				return;

			caster.CastSpell(caster, DemonHunterSpells.GlideKnockback, true);
			caster.CastSpell(caster, DemonHunterSpells.GlideDuration, true);

			caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.VengefulRetreatTrigger, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
			caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.FelRush, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
		}

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DemonHunterSpells.GlideKnockback, DemonHunterSpells.GlideDuration, DemonHunterSpells.VengefulRetreatTrigger, DemonHunterSpells.FelRush);
		}

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();

			if (caster.IsMounted() ||
				caster.GetVehicleBase())
				return SpellCastResult.DontReport;

			if (!caster.IsFalling())
				return SpellCastResult.NotOnGround;

			return SpellCastResult.SpellCastOk;
		}
	}

	[Script] // 131347 - Glide
	internal class spell_dh_glide_AuraScript : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DemonHunterSpells.GlideDuration);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.FeatherFall, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}

		private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			GetTarget().RemoveAura(DemonHunterSpells.GlideDuration);
		}
	}

	[Script] // 197154 - Glide
	internal class spell_dh_glide_timer : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DemonHunterSpells.Glide);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}

		private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			GetTarget().RemoveAura(DemonHunterSpells.Glide);
		}
	}
}
