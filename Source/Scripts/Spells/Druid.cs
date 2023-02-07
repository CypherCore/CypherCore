// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells;
using static Game.Scripting.Interfaces.ISpell.EffectHandler;

namespace Scripts.Spells.Druid
{
    internal struct SpellIds
    {
        public const uint BalanceT10Bonus = 70718;
        public const uint BalanceT10BonusProc = 70721;
        public const uint BearForm = 5487;
        public const uint BlessingOfCenarius = 40452;
        public const uint BlessingOfElune = 40446;
        public const uint BlessingOfRemulos = 40445;
        public const uint BlessingOfTheClaw = 28750;
        public const uint BloodFrenzyAura = 203962;
        public const uint BloodFrenzyRageGain = 203961;
        public const uint BramblesDamageAura = 213709;
        public const uint BramblesPassive = 203953;
        public const uint BramblesRelect = 203958;
        public const uint BristlingFurGainRage = 204031;
        public const uint CatForm = 768;
        public const uint EarthwardenAura = 203975;
        public const uint EclipseDummy = 79577;
        public const uint EclipseLunarAura = 48518;
        public const uint EclipseLunarSpellCnt = 326055;
        public const uint EclipseOoc = 329910;
        public const uint EclipseSolarAura = 48517;
        public const uint EclipseSolarSpellCnt = 326053;
        public const uint Exhilarate = 28742;
        public const uint FormAquaticPassive = 276012;
        public const uint FormAquatic = 1066;
        public const uint FormFlight = 33943;
        public const uint FormStag = 165961;
        public const uint FormSwiftFlight = 40120;
        public const uint FormsTrinketBear = 37340;
        public const uint FormsTrinketCat = 37341;
        public const uint FormsTrinketMoonkin = 37343;
        public const uint FormsTrinketNone = 37344;
        public const uint FormsTrinketTree = 37342;
        public const uint GalacticGuardianAura = 213708;
        public const uint GlyphOfStars = 114301;
        public const uint GlyphOfStarsVisual = 114302;
        public const uint GoreProc = 93622;
        public const uint IdolOfFeralShadows = 34241;
        public const uint IdolOfWorship = 60774;
        public const uint IncarnationKingOfTheJungle = 102543;
        public const uint Innervate = 29166;
        public const uint InnervateRank2 = 326228;
        public const uint Infusion = 37238;
        public const uint Languish = 71023;
        public const uint LifebloomFinalHeal = 33778;
        public const uint LunarInspirationOverride = 155627;
        public const uint Mangle = 33917;
        public const uint MoonfireDamage = 164812;
        public const uint Prowl = 5215;
        public const uint RejuvenationT10Proc = 70691;
        public const uint RestorationT102PBonus = 70658;
        public const uint SavageRoar = 62071;
        public const uint SkullBashCharge = 221514;
        public const uint SkullBashInterrupt = 93985;
        public const uint SunfireDamage = 164815;
        public const uint SurvivalInstincts = 50322;
        public const uint TravelForm = 783;
        public const uint ThrashBear = 77758;
        public const uint ThrashBearAura = 192090;
        public const uint ThrashCat = 106830;
    }

    public struct DruidSpells
    {
        public static uint SPELL_DRUID_THRASH_BEAR_PERIODIC_DAMAGE = 192090;
        public static uint SPELL_DRUID_MAUL = 6807;
        public static uint SPELL_DRUID_BLESSING_OF_THE_ANCIENTS = 202360;
        public static uint SPELL_DRUID_BLESSING_OF_ELUNE = 202737;
        public static uint SPELL_DRUID_BLESSING_OF_ANSHE = 202739;
        public static uint SPELL_DRUID_STARLORD_DUMMY = 202345;
        public static uint SPELL_DRUID_STARLORD_SOLAR = 202416;
        public static uint SPELL_DRUID_STARLORD_LUNAR = 202423;
        public static uint SPELL_DRUID_GLYPH_OF_STARS = 114301;
        public static uint SPELL_DRUID_CHOSEN_OF_ELUNE = 102560;
        public static uint SPELL_DRUID_BLUE_COLOR = 108268;
        public static uint SPELL_DRUID_SHADOWY_GHOST = 165114;
        public static uint SPELL_DRUID_GORE = 210706;
        public static uint SPELL_DRUID_YSERA_GIFT = 145108;
        public static uint SPELL_DRUID_YSERA_GIFT_CASTER_HEAL = 145109;
        public static uint SPELL_DRUID_YSERA_GIFT_RAID_HEAL = 145110;
        public static uint SPELL_DRUID_REJUVENATION = 774;
        public static uint SPELL_DRUID_HEALING_TOUCH = 5185;
        public static uint SPELL_DRUID_SWIFTMEND = 18562;
        public static uint SPELL_DRUID_TRAVEL_FORM = 783;
        public static uint SPELL_DRUID_FELINE_SWIFTNESS = 131768;
        public static uint SPELL_DRUID_SHRED = 5221;
        public static uint SPELL_DRUID_RAKE = 1822;
        public static uint SPELL_DRUID_RIP = 1079;
        public static uint SPELL_DRUID_FEROCIOUS_BITE = 22568;
        public static uint SPELL_DRUID_MOONFIRE_CAT = 155625;
        public static uint SPELL_DRUID_SWIPE_CAT = 106785;
        public static uint SPELL_DRUID_SABERTOOTH = 202031;
        public static uint SPELL_DRUID_FORMS_TRINKET_BEAR = 37340;
        public static uint SPELL_DRUID_FORMS_TRINKET_CAT = 37341;
        public static uint SPELL_DRUID_FORMS_TRINKET_MOONKIN = 37343;
        public static uint SPELL_DRUID_FORMS_TRINKET_NONE = 37344;
        public static uint SPELL_DRUID_FORMS_TRINKET_TREE = 37342;
        public static uint SPELL_DRUID_GLYPH_OF_INNERVATE = 54833;
        public static uint SPELL_DRUID_INCREASED_MOONFIRE_DURATION = 38414;
        public static uint SPELL_DRUID_GLYPH_OF_STARFIRE = 54846;
        public static uint SPELL_DRUID_ITEM_T8_BALANCE_RELIC = 64950;
        public static uint SPELL_DRUID_STAMPEDE = 81022;
        public static uint SPELL_DRUID_STAMPEDE_BAER_RANK_1 = 81016;
        public static uint SPELL_DRUID_STAMPEDE_CAT_RANK_1 = 81021;
        public static uint SPELL_DRUID_STAMPEDE_CAT_STATE = 109881;
        public static uint SPELL_DRUID_FERAL_CHARGE_BEAR = 16979;
        public static uint SPELL_DRUID_FERAL_CHARGE_CAT = 49376;
        public static uint SPELL_DRUID_GLYPH_OF_TYPHOON = 62135;
        public static uint SPELL_DRUID_EXHILARATE = 28742;
        public static uint SPELL_DRUID_INFUSION = 37238;
        public static uint SPELL_DRUID_BLESSING_OF_THE_CLAW = 28750;
        public static uint SPELL_DRUID_BLESSING_OF_REMULOS = 40445;
        public static uint SPELL_DRUID_BLESSING_OF_ELUNE_40446 = 40446;
        public static uint SPELL_DRUID_BLESSING_OF_CENARIUS = 40452;
        public static uint SPELL_DRUID_LANGUISH = 71023;
        public static uint SPELL_DRUID_REJUVENATION_T10_PROC = 70691;
        public static uint SPELL_DRUID_SHOOTING_STARS = 93400;
        public static uint SPELL_DRUID_SOLAR_ECLIPSE = 48517;
        public static uint SPELL_DRUID_SOLAR_ECLIPSE_MARKER = 67483; // Will make the yellow arrow on eclipse bar point to the yellow side (solar)
        public static uint SPELL_DRUID_LUNAR_ECLIPSE = 48518;
        public static uint SPELL_DRUID_LUNAR_ECLIPSE_MARKER = 67484; // Will make the yellow arrow on eclipse bar point to the blue side (lunar)
        public static uint SPELL_DRUID_WRATH = 5176;
        public static uint SPELL_DRUID_EUPHORIA = 152222;
        public static uint SPELL_DRUID_STARFIRE = 2912;
        public static uint SPELL_DRUID_SOLAR_BEAM = 78675;
        public static uint SPELL_DRUID_SOLAR_BEAM_SILENCE = 81261;
        public static uint SPELL_DRU_REGROWTH = 8936;
        public static uint SPELL_DRU_BLOODTALONS = 155672;
        public static uint SPELL_DRU_BLOODTALONS_TRIGGERED = 145152;
        public static uint SPELL_DRU_MOMENT_OF_CLARITY = 155577;
        public static uint SPELL_DRU_CLEARCASTING = 16870;
        public static uint SPELL_DRUID_OMEN_OF_CLARITY = 113043;
        public static uint SPELL_ZANDALARI_TROLL_DRUID_SABERTUSK_CAT_SHAPESHIFT = 287362;
        public static uint SPELL_ZANDALARI_PTERRODAX_FLY_SHAPESHIFT = 265524;
        public static uint SPELL_ZANDALARI_TRAVEL_FORM_RAPTOR_SHAPESHIFT = 271899;
        public static uint SPELL_ZANDALARI_BEAR_FORM_ANKYLODON_SHAPESHIFT = 262551;
        public static uint MODEL_ZANDALARI_BEAR_FORM = 84869;
        //SPELL_ZANDALARI_TROLL_AQUATIC_FORM                    = ?
        public static uint SPELL_FERAL_FRENZY_MAIN = 274837;
        public static uint SPELL_FERAL_FRENZY_BLEED = 274838;
        public static uint SPELL_DRU_ECLIPSE = 279619;
        public static uint SPELL_DRU_SOLAR_EMPOWEREMENT = 164545;
        public static uint SPELL_DRU_LUNAR_EMPOWEREMENT = 164547;
        public static uint SPELL_DRU_STARLORD = 203245;
        public static uint SPELL_DRU_STARLORD_BUFF = 279709;
        public static uint SPELL_DRU_ABUNDANCE = 207383;
        public static uint SPELL_DRU_ABUNDANCE_BUFF = 207640;
        public static uint SPELL_DRU_SOUL_OF_THE_FOREST_RESTO = 158478;
        public static uint SPELL_DRU_SOUL_OF_THE_FOREST_RESTO_BUFF = 114108;
        public static uint SPELL_DRU_CULTIVATION = 200390;
        public static uint SPELL_DRU_CULTIVATION_HEAL = 200389;
        public static uint SPELL_DRU_GERMINATION = 155675;
        public static uint SPELL_DRU_GERMINATION_HOT = 155777;
        public static uint SPELL_DRU_GLYPH_OF_REJUVENATION = 17076;
        public static uint SPELL_DRU_GLYPH_OF_REJUVENATION_EFFECT = 96206;
        public static uint SPELL_DRU_SPRING_BLOSSOMS = 207385;
        public static uint SPELL_DRU_SPRING_BLOSSOMS_HEAL = 207386;
        public static uint SPELL_DRU_PHOTOSYNTHESIS = 274902;
        public static uint SPELL_DRU_PHOTOSYNTHESIS_MOD_HEAL_TICKS = 274906;
        public static uint SPELL_DRU_LUNAR_BEAM_DAMAGE_HEAL = 204069;
        public static uint SPELL_DRU_URSOL_VORTEX_PULL = 118283;
        public static uint SPELL_DRU_URSOL_VORTEX_DEBUFF = 127797;
        public static uint SPELL_DRU_MASS_ENTANGLEMENT = 102359;
        public static uint SPELL_DRU_GALACTIC_GUARDIAN = 203964;
        public static uint SPELL_DRU_GALACTIC_GAURDIAN_MOD_MOONFIRE = 213708;
        public static uint SPELL_DRU_PREDATOR = 202021;
        public static uint SPELL_DRU_TIGER_FURY = 5217;
    }

    public struct ShapeshiftFormSpells
    {
        public static uint SPELL_DRUID_BEAR_FORM = 5487;
        public static uint SPELL_DRUID_CAT_FORM = 768;
        public static uint SPELL_DRUID_MOONKIN_FORM = 24858;
        public static uint SPELL_DRUID_INCARNATION_TREE_OF_LIFE = 33891;
        public static uint SPELL_DRUID_INCARNATION_KING_OF_JUNGLE = 102543;
    }

    public struct SoulOfTheForestSpells
    {
        public static uint SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO_TALENT = 158478;
        public static uint SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO = 114108;
    }
    public struct MoonfireSpells
    {
        public const uint SPELL_DRUID_MOONFIRE_DAMAGE = 164812;
    }

    public struct EfflorescenceSpells
    {
        public static uint SPELL_DRUID_EFFLORESCENCE_HEAL = 81269;
        public static uint SPELL_DRUID_EFFLORESCENCE_DUMMY = 81262;
    }

    public struct GoreSpells
    {
        public static uint SPELL_DRUID_THRASH = 106832;
        public static uint SPELL_DRUID_MOONFIRE = 8921;
        public static uint SPELL_DRUID_SWIPE = 213764;
    }
    public struct LifebloomSpells
    {
        public static uint SPELL_DRUID_LIFEBLOOM_FINAL_HEAL = 33778;
    }

    public struct SunfireSpells
    {
        public static uint SPELL_DRUID_SUNFIRE_DAMAGE = 164815;
    }

    public struct BalanceAffinitySpells
    {
        public static uint SPELL_DRUID_STARSURGE = 78674;
        public static uint SPELL_DRUID_SUNFIRE = 93402;
        public static uint SPELL_DRUID_LUNAR_STRIKE = 194153;
        public static uint SPELL_DRUID_SOLAR_WRATH = 190984;
    }


    public struct SavageRoarSpells
    {
        public const uint SPELL_DRUID_SAVAGE_ROAR = 62071;
    }

    public struct SurvivalInstinctsSpells
    {
        public const uint SPELL_DRUID_SURVIVAL_INSTINCTS = 50322;
    }

    public struct CatFormSpells
    {
        public static uint SPELL_DRUID_DASH = 1850;
        public static uint SPELL_DRUID_PROWL = 5215;
        public static uint SPELL_DRUID_FORM_CAT_INCREASE_SPEED = 113636;
        public static uint SPELL_DRUID_CATFORM_OVERRIDE = 48629;
        public static uint SPELL_DRUID_LUNAR_INSPIRATION = 155580;
        public static uint SPELL_DRUID_MOONFIRE_CAT_OVERRIDE = 155627;
    }

    public struct DruidFlamesSpells
    {
        public static uint SPELL_DRUID_DRUID_OF_THE_FLAMES = 99245;
        public static uint SPELL_DRUID_BURNING_ESSENCE = 138927;
        public static uint MODEL_DRUID_OF_THE_FLAMES = 38150;
    }

    public struct BearFormSpells
    {
        public static uint SPELL_DRUID_BEARFORM_OVERRIDE = 106829;
        public static uint SPELL_DRUID_STAMPEDING_ROAR = 106898;
        public static uint SPELL_DRUID_STAMPEDING_ROAR_BEAR_OVERRIDE = 106899;
    }

    public struct SkullBashSpells
    {
        public const uint SPELL_DRUID_SKULL_BASH_CHARGE = 221514;
        public const uint SPELL_DRUID_SKULL_BASH_INTERUPT = 93985;
    }

    public struct RakeSpells
    {
        public const uint SPELL_DRUID_RAKE_STUN = 163505;
    }

    public struct MaimSpells
    {
        public const uint SPELL_DRUID_MAIM_STUN = 203123;
    }


    public struct BloodTalonsSpells
    {
        public const uint SPELL_BLOODTALONS = 155672;
        public const uint SPELL_BLOODTALONS_AURA = 145152;
    }

    public struct DruidForms
    {
        public const uint DRUID_AQUATIC_FORM = 1066;
        public const uint DRUID_FLIGHT_FORM = 33943;
        public const uint DRUID_STAG_FORM = 165961;
        public const uint DRUID_SWIFT_FLIGHT_FORM = 40120;
    }


    public struct StarfallSpells
    {
        public const uint SPELL_DRUID_STARFALL_DAMAGE = 191037;
        public const uint SPELL_DRUID_STELLAR_EMPOWERMENT = 197637;
    }

    // Maul (Bear Form) - 6807
    [SpellScript(6807)]
    public class spell_dru_maul_bear : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void OnHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(OnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(OnHit, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    // Blessing of the Ancients - 202360
    [SpellScript(202360)]
    public class spell_dru_blessing_of_the_ancients : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


        private void HandleDummy(uint UnnamedParameter)
        {
            uint removeAura = GetCaster().HasAura(DruidSpells.SPELL_DRUID_BLESSING_OF_ELUNE) ? (uint)DruidSpells.SPELL_DRUID_BLESSING_OF_ELUNE : (uint)DruidSpells.SPELL_DRUID_BLESSING_OF_ANSHE;
            uint addAura = GetCaster().HasAura(DruidSpells.SPELL_DRUID_BLESSING_OF_ELUNE) ? (uint)DruidSpells.SPELL_DRUID_BLESSING_OF_ANSHE : (uint)DruidSpells.SPELL_DRUID_BLESSING_OF_ELUNE;

            GetCaster().RemoveAurasDueToSpell(removeAura);
            GetCaster().CastSpell(null, addAura, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

    // Efflorescence - 145205
    [SpellScript(145205)]
    public class spell_dru_efflorescence : SpellScript, ISpellOnCast
    {


        private struct eCreature
        {
            public static uint NPC_EFFLORESCENCE = 47649;
        }

        public void OnCast()
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Creature efflorescence = caster.GetSummonedCreatureByEntry(eCreature.NPC_EFFLORESCENCE);
                if (efflorescence != null)
                {
                    efflorescence.DespawnOrUnsummon();
                }
            }
        }


    }

    // Efflorescence (Aura) - 81262
    [SpellScript(81262)]
    public class spell_dru_efflorescence_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleHeal(AuraEffect UnnamedParameter)
        {
            if (GetCaster() && GetCaster().GetOwner())
            {
                GetCaster().GetOwner().CastSpell(GetCaster().GetPosition(), EfflorescenceSpells.SPELL_DRUID_EFFLORESCENCE_HEAL);
           
                var playerList = GetCaster().GetPlayerListInGrid(11.2f);
                foreach (var targets in playerList)
                {
                    if (GetCaster().GetOwner().HasAura(DruidSpells.SPELL_DRU_SPRING_BLOSSOMS))
                    {
                        if (!targets.HasAura(DruidSpells.SPELL_DRU_SPRING_BLOSSOMS_HEAL))
                        {
                            GetCaster().GetOwner().CastSpell(targets, DruidSpells.SPELL_DRU_SPRING_BLOSSOMS_HEAL, true);
                        }
                    }
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandleHeal, 0, AuraType.PeriodicDummy));
        }
    }

    // Efflorescence (Heal) - 81269
    [SpellScript(81269)]
    public class spell_dru_efflorescence_heal : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


        private void SortTargets(List<WorldObject> targets)
        {
            targets.Sort(new HealthPctOrderPred());

            if (targets.Count > 3)
            {
                targets.Resize(3);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(SortTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    // 159286 Primal Fury
    [SpellScript(159286)]
    public class spell_dru_primal_fury : AuraScript, IAuraCheckProc
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            bool _spellCanProc = (eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_SHRED || eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_RAKE || eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_SWIPE_CAT || eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_MOONFIRE_CAT);

            if ((eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0 && _spellCanProc)
            {
                return true;
            }

            return false;
        }


    }

    public struct PredatorySwiftnessSpells
    {
        public static uint SPELL_DRUID_PREDATORY_SWIFTNESS = 16974;
        public static uint SPELL_DRUID_PREDATORY_SWIFTNESS_AURA = 69369;
    }

    // Predatory Swiftness - 16974
    // @Called : Maim - 22570, Ferocious Bite - 22568, Rip - 1079, Savage Roar - 1079
    // @Version : 7.1.0.22908
    [SpellScript(16974)]
    public class spell_dru_predatory_swiftness : SpellScript, ISpellCheckCast, ISpellOnHit
    {
        private int _cp;

        public override bool Load()
        {
            _cp = GetCaster().GetPower(PowerType.ComboPoints);
            return true;
        }

        public SpellCastResult CheckCast()
        {
            if (GetCaster())
            {
                if (GetCaster().GetTypeId() != TypeId.Player)
                {
                    return SpellCastResult.DontReport;
                }

                if (GetCaster().ToPlayer().GetPower(PowerType.ComboPoints) != 0)
                {
                    return SpellCastResult.NoComboPoints;
                }
            }
            else
            {
                return SpellCastResult.DontReport;
            }

            return SpellCastResult.SpellCastOk;
        }

        public void OnHit()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                if (player.HasAura(PredatorySwiftnessSpells.SPELL_DRUID_PREDATORY_SWIFTNESS) && RandomHelper.randChance(20 * _cp))
                {
                    player.CastSpell(player, PredatorySwiftnessSpells.SPELL_DRUID_PREDATORY_SWIFTNESS_AURA, true);
                }
            }
        }
    }

    // Predatory Swiftness (Aura) - 69369
    // @Called : Entangling Roots - 339, Rebirth - 20484, Regrowth - 8936
    // @Version : 7.1.0.22908
    [SpellScript(69369)]
    public class spell_dru_predatory_swiftness_aura : SpellScript, ISpellAfterHit
    {


        public void AfterHit()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                if (player.HasAura(PredatorySwiftnessSpells.SPELL_DRUID_PREDATORY_SWIFTNESS_AURA))
                {
                    player.RemoveAurasDueToSpell(PredatorySwiftnessSpells.SPELL_DRUID_PREDATORY_SWIFTNESS_AURA);
                }
            }
        }
    }

    // 197488 Balance Affinity (Feral, Guardian) -
    // @Version : 7.1.0.22908
    [SpellScript(197488)]
    public class spell_dru_balance_affinity_dps : AuraScript, IHasAuraEffects
    {


        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void LearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Player player = caster.ToPlayer();
            if (player != null)
            {
                player.AddTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
                player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
                player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
                player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_SOLAR_WRATH);
                player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_SUNFIRE);
            }
        }

        private void UnlearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Player player = caster.ToPlayer();
            if (player != null)
            {
                player.RemoveTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
                player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
                player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
                player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_SOLAR_WRATH);
                player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_SUNFIRE);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(UnlearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            AuraEffects.Add(new EffectApplyHandler(LearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }


    // 197632 Balance Affinity (Restoration) -
    // @Version : 7.1.0.22908
    [SpellScript(197632)]
    public class spell_dru_balance_affinity_resto : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void LearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Player player = caster.ToPlayer();
            if (player != null)
            {
                player.AddTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
                player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
                player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
            }
        }

        private void UnlearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Player player = caster.ToPlayer();
            if (player != null)
            {
                player.RemoveTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
                player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
                player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(UnlearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            AuraEffects.Add(new EffectApplyHandler(LearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }


    // 102560 Incarnation : Chosen of Elune -
    // @Version : 7.1.0.22908
    [SpellScript(102560)]
    public class spell_dru_incarnation_chosen_of_elune : SpellScript, ISpellOnCast
    {
        public void OnCast()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                if (!player.HasAura(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM))
                {
                    player.CastSpell(player, ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM, true);
                }
            }
        }
    }

    // 102543 Incarnation : King of the Jungle -
    // @Version : 7.1.0.22908
    [SpellScript(102543)]
    public class spell_dru_incarnation_king_of_the_jungle : SpellScript, ISpellOnCast
    {
        public void OnCast()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                if (!player.HasAura(ShapeshiftFormSpells.SPELL_DRUID_CAT_FORM))
                {
                    player.CastSpell(player, ShapeshiftFormSpells.SPELL_DRUID_CAT_FORM, true);
                }
            }
        }
    }

    // 102558 Incarnation : Guardian of Ursoc -
    // @Version : 7.1.0.22908
    [SpellScript(102558)]
    public class spell_dru_incarnation_guardian_of_ursoc : SpellScript, ISpellOnCast
    {
        public void OnCast()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                if (!player.HasAura(ShapeshiftFormSpells.SPELL_DRUID_BEAR_FORM))
                {
                    player.CastSpell(player, ShapeshiftFormSpells.SPELL_DRUID_BEAR_FORM, true);
                }
            }
        }
    }

    // 102383 Wild Charge (Moonkin) -
    // @Version : 7.1.0.22908
    [SpellScript(102383)]
    public class spell_dru_wild_charge_moonkin : SpellScript, ISpellCheckCast
    {
        public SpellCastResult CheckCast()
        {
            if (GetCaster())
            {
                if (!GetCaster().IsInCombat())
                {
                    return SpellCastResult.DontReport;
                }
            }
            else
            {
                return SpellCastResult.DontReport;
            }

            return SpellCastResult.SpellCastOk;
        }
    }


    // Bear form - 5487
    // @Called : Bear Form (Thrash/Swipe) - 106829, Bear Form (Stampeding Roar) - 106899
    // @Version : 7.1.0.22908
    [SpellScript(5487)]
    public class spell_dru_bear_form : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            caster.CastSpell(caster, BearFormSpells.SPELL_DRUID_BEARFORM_OVERRIDE, true);

            if (caster.HasSpell(BearFormSpells.SPELL_DRUID_STAMPEDING_ROAR))
            {
                caster.CastSpell(caster, BearFormSpells.SPELL_DRUID_STAMPEDING_ROAR_BEAR_OVERRIDE, true);
            }
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            caster.RemoveAurasDueToSpell(BearFormSpells.SPELL_DRUID_BEARFORM_OVERRIDE);

            if (caster.HasSpell(BearFormSpells.SPELL_DRUID_STAMPEDING_ROAR))
            {
                caster.RemoveAurasDueToSpell(BearFormSpells.SPELL_DRUID_STAMPEDING_ROAR_BEAR_OVERRIDE);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }

    // Activate Cat Form
    // @Called : Dash - 1850, Prowl - 5215, Displacer Beast - 102280
    // @Version : 7.1.0.22908
    [SpellScript(new uint[] { 1850, 5215, 102280 })]
    public class spell_dru_activate_cat_form : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (!caster.HasAura(ShapeshiftFormSpells.SPELL_DRUID_CAT_FORM))
            {
                caster.CastSpell(caster, ShapeshiftFormSpells.SPELL_DRUID_CAT_FORM, true);
            }
        }
    }

    // Infected wound - 48484
    // @Version : 7.1.0.22908
    [SpellScript(48484)]
    public class spell_dru_infected_wound : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetSpellInfo().Id == DruidSpells.SPELL_DRUID_RAKE)
            {
                return true;
            }

            return false;
        }
    }

    // Ysera's Gift - 145108
    [SpellScript(145108)]
    public class spell_dru_ysera_gift : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster == null || !caster.IsAlive())
            {
                return;
            }

            var amount = MathFunctions.CalculatePct(caster.GetMaxHealth(), aurEff.GetBaseAmount());
            CastSpellExtraArgs values = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
            values.AddSpellMod(SpellValueMod.MaxTargets, 1);
            values.AddSpellMod(SpellValueMod.BasePoint0, (int)amount);

            if (caster.IsFullHealth())
                caster.CastSpell(caster, DruidSpells.SPELL_DRUID_YSERA_GIFT_RAID_HEAL, values);
            else
                caster.CastSpell(caster, DruidSpells.SPELL_DRUID_YSERA_GIFT_CASTER_HEAL, values);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    // Rake - 1822
    [SpellScript(1822)]
    public class spell_dru_rake : SpellScript, IHasSpellEffects
    {
        private bool _stealthed = false;

        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();
        public override bool Load()
        {
            Unit caster = GetCaster();
            if (caster.HasAuraType(AuraType.ModStealth))
            {
                _stealthed = true;
            }

            return true;
        }

        private void HandleOnHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();
            if (caster == null || target == null)
            {
                return;
            }

            // While stealthed or have Incarnation: King of the Jungle aura, deal 100% increased damage
            if (_stealthed || caster.HasAura(ShapeshiftFormSpells.SPELL_DRUID_INCARNATION_KING_OF_JUNGLE))
            {
                SetHitDamage(GetHitDamage() * 2);
            }

            // Only stun if the caster was in stealth
            if (_stealthed)
            {
                caster.CastSpell(target, RakeSpells.SPELL_DRUID_RAKE_STUN, true);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

 
    }

    // Maim - 22570
    [SpellScript(22570)]
    public class spell_dru_maim : SpellScript, ISpellAfterCast, ISpellOnTakePower
    {
        private int _usedComboPoints = 0;

        public void TakePower(SpellPowerCost powerCost)
        {
            if (powerCost.Power == PowerType.ComboPoints)
            {
                _usedComboPoints = powerCost.Amount;
            }
        }

        public void AfterCast()
        {
            Unit target = GetExplTargetUnit();
            if (target == null)
            {
                return;
            }

            GetCaster().CastSpell(target, MaimSpells.SPELL_DRUID_MAIM_STUN, true);

            Aura maimStun = target.GetAura(MaimSpells.SPELL_DRUID_MAIM_STUN, GetCaster().GetGUID());
            if (maimStun != null)
            {
                maimStun.SetDuration(_usedComboPoints * 1000);
            }
        }
    }

    // 24858  Moonkin Form
    // 102560 Chosen of Elune
    // 197625
    [SpellScript(new uint[] { 24858, 102560, 197625 })]
    public class aura_dru_astral_form : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(DruidSpells.SPELL_DRUID_GLYPH_OF_STARS);
        }

        private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit target = GetTarget();
            if (target.HasAura(DruidSpells.SPELL_DRUID_GLYPH_OF_STARS))
            {
                target.SetDisplayId(target.GetNativeDisplayId());
                target.AddAura(DruidSpells.SPELL_DRUID_BLUE_COLOR, target);
                target.AddAura(DruidSpells.SPELL_DRUID_SHADOWY_GHOST, target);
                target.CastSpell(target, (uint)Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_GLYPH_OF_STARS, Difficulty.None).GetEffect(0).BasePoints, true);
            }
        }

        private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit target = GetTarget();
            if (target.HasAura(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM) || target.HasAura(DruidSpells.SPELL_DRUID_CHOSEN_OF_ELUNE))
            {
                return;
            }

            target.RemoveAura((uint)Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_GLYPH_OF_STARS, Difficulty.None).GetEffect(0).BasePoints);
            target.RemoveAura(DruidSpells.SPELL_DRUID_BLUE_COLOR);
            target.RemoveAura(DruidSpells.SPELL_DRUID_SHADOWY_GHOST);
        }

        public override void Register()
        {
            switch (ScriptSpellId)
            {
                case 197625:
                case 24858:
                    AuraEffects.Add(new EffectApplyHandler(AfterApply, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
                    AuraEffects.Add(new EffectApplyHandler(AfterRemove, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
                    break;
                case 102560:
                    AuraEffects.Add(new EffectApplyHandler(AfterApply, 1, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
                    AuraEffects.Add(new EffectApplyHandler(AfterRemove, 1, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
                    break;
            }
        }
    }

    // 197492 - Restoration Affinity
    [SpellScript(197492)]
    public class aura_dru_restoration_affinity : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


        private readonly List<uint> LearnedSpells = new List<uint>() { (uint)DruidSpells.SPELL_DRUID_YSERA_GIFT, (uint)DruidSpells.SPELL_DRUID_REJUVENATION, (uint)DruidSpells.SPELL_DRUID_HEALING_TOUCH, (uint)DruidSpells.SPELL_DRUID_SWIFTMEND };

        private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player target = GetTarget().ToPlayer();
            if (target != null)
            {
                foreach (uint spellId in LearnedSpells)
                {
                    target.LearnSpell(spellId, false);
                }
            }
        }

        private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player target = GetTarget().ToPlayer();
            if (target != null)
            {
                foreach (uint spellId in LearnedSpells)
                {
                    target.RemoveSpell(spellId);
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }
    }

    // 202157 - Feral Affinity
    [SpellScript(202157)]
    public class aura_dru_feral_affinity : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


        private readonly List<uint> LearnedSpells = new List<uint>() { (uint)DruidSpells.SPELL_DRUID_FELINE_SWIFTNESS, (uint)DruidSpells.SPELL_DRUID_SHRED, (uint)DruidSpells.SPELL_DRUID_RAKE, (uint)DruidSpells.SPELL_DRUID_RIP, (uint)DruidSpells.SPELL_DRUID_FEROCIOUS_BITE, (uint)DruidSpells.SPELL_DRUID_SWIPE_CAT };

        private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player target = GetTarget().ToPlayer();
            if (target != null)
            {
                foreach (uint spellId in LearnedSpells)
                {
                    target.LearnSpell(spellId, false);
                }
            }
        }

        private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player target = GetTarget().ToPlayer();
            if (target != null)
            {
                foreach (uint spellId in LearnedSpells)
                {
                    target.RemoveSpell(spellId);
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }
    }

    // 22842 - Frenzied Regeneration
    [SpellScript(22842)]
    public class aura_dru_frenzied_regeneration : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
        {
            Aura frenzied = GetCaster().GetAura(22842);
            if (frenzied != null)
            {
                frenzied.GetMaxDuration();
            }
            ulong healAmount = MathFunctions.CalculatePct(GetCaster().GetDamageOverLastSeconds(5), 50);
            ulong minHealAmount = MathFunctions.CalculatePct(GetCaster().GetMaxHealth(), 5);
            healAmount = Math.Max(healAmount, minHealAmount);
            amount = (int)healAmount;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ObsModHealth));
        }
    }


    // Starfall - 191034
    // AreaTriggerID - 9482
    [Script]
    public class at_dru_starfall : AreaTriggerAI
    {
        public int timeInterval;

        public at_dru_starfall(AreaTrigger areatrigger) : base(areatrigger)
        {
            // How often should the action be executed
            areatrigger.SetPeriodicProcTimer(850);
        }

        public override void OnPeriodicProc()
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                foreach (ObjectGuid objguid in at.GetInsideUnits())
                {
                    Unit unit = ObjectAccessor.Instance.GetUnit(caster, objguid);
                    if (unit != null)
                    {
                        if (caster.IsValidAttackTarget(unit))
                        {
                            if (unit.IsInCombat())
                            {
                                caster.CastSpell(unit, StarfallSpells.SPELL_DRUID_STARFALL_DAMAGE, true);
                                caster.CastSpell(unit, StarfallSpells.SPELL_DRUID_STELLAR_EMPOWERMENT, true);
                            }
                        }
                    }
                }
            }
        }
    }

    public struct UrsolsVortexSpells
    {
        public const uint SPELL_DRUID_URSOLS_VORTEX_SLOW = 127797;
        public const uint SPELL_DRUID_URSOLS_VORTEX_PULL = 118283;
    }

    // Efflorescence
    // NPC Id - 47649
    [CreatureScript(47649)]
    public class npc_dru_efflorescence : ScriptedAI
    {
        public npc_dru_efflorescence(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            me.CastSpell(me, EfflorescenceSpells.SPELL_DRUID_EFFLORESCENCE_DUMMY, true);
            me.SetUnitFlag(UnitFlags.NonAttackable);
            me.SetUnitFlag(UnitFlags.Uninteractible);
            me.SetUnitFlag(UnitFlags.RemoveClientControl);
            me.SetReactState(ReactStates.Passive);
        }
    }

    // Charm Woodland Creature - 127757
    [SpellScript(127757)]
    public class aura_dru_charm_woodland_creature : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            // Make targeted creature follow the player - Using pet's default dist and angle
            //if (Unit* caster = GetCaster())
            //if (Unit* target = GetTarget())
            //target->GetMotionMaster()->MoveFollow(caster, PET_FOLLOW_DIST, PET_FOLLOW_ANGLE);

            var caster = GetCaster();
            var target = GetTarget();

            if (caster != null && target != null)
            {
                target.GetMotionMaster().MoveFollow(caster, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
            }
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            //if (Unit* target = GetTarget())
            //if (target->GetMotionMaster()->GetCurrentMovementGeneratorType() == FOLLOW_MOTION_TYPE)
            //target->GetMotionMaster()->MovementExpired(true); // reset movement
            var target = GetTarget();

            if (target != null)
            {
                if (target.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Follow) 
                {
                    target.GetMotionMaster().Initialize();
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.AoeCharm, AuraEffectHandleModes.Real));
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.AoeCharm, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }

    // Swipe - 106785
    [SpellScript(106785)]
    public class spell_dru_swipe : SpellScript, IHasSpellEffects
    {
        private bool _awardComboPoint = true;

        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


        private void HandleOnHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            int damage = GetHitDamage();
            var casterLevel = caster.GetLevelForTarget(caster);

            // This prevent awarding multiple Combo Points when multiple targets hit with Swipe AoE
            if (_awardComboPoint)
            {
                // Awards the caster 1 Combo Point (get value from the spell data)
                caster.ModifyPower(PowerType.ComboPoints, Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_SWIPE_CAT, Difficulty.None).GetEffect(0).BasePoints);
            }

            // If caster is level >= 44 and the target is bleeding, deals 20% increased damage (get value from the spell data)
            if ((casterLevel >= 44) && target.HasAuraState( AuraStateType.Bleed))
            {
                MathFunctions.AddPct(ref damage, Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_SWIPE_CAT, Difficulty.None).GetEffect(1).BasePoints);
            }

            SetHitDamage(damage);
            _awardComboPoint = false;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

    }

    // Brutal Slash - 202028
    [SpellScript(202028)]
    public class spell_dru_brutal_slash : SpellScript, ISpellOnHit
    {
        private bool _awardComboPoint = true;

        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            // This prevent awarding multiple Combo Points when multiple targets hit with Brutal Slash AoE
            if (_awardComboPoint)
            {
                // Awards the caster 1 Combo Point (get value from the spell data)
                caster.ModifyPower(PowerType.ComboPoints, Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_SWIPE_CAT, Difficulty.None).GetEffect(0).BasePoints);
            }

            _awardComboPoint = false;
        }
    }

    // Thrash (Cat Form) - 106830
    [SpellScript(106830)]
    public class spell_dru_thrash_cat : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void EffectHitTarget(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            // This prevent awarding multiple Combo Points when multiple targets hit with Thrash AoE
            if (m_awardComboPoint)
            {
                // Awards the caster 1 Combo Point
                caster.ModifyPower(PowerType.ComboPoints, 1);
            }
            m_awardComboPoint = false;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(EffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private bool m_awardComboPoint = true;
    }

    // Shred - 5221
    [SpellScript(5221)]
    public class spell_dru_shred : SpellScript, ISpellOnHit, ISpellCalcCritChance
    {


        public override bool Load()
        {
            Unit caster = GetCaster();

            if (caster.HasAuraType(AuraType.ModStealth))
            {
                m_stealthed = true;
            }

            if (caster.HasAura(ShapeshiftFormSpells.SPELL_DRUID_INCARNATION_KING_OF_JUNGLE))
            {
                m_incarnation = true;
            }

            m_casterLevel = caster.GetLevelForTarget(caster);

            return true;
        }

        public void CalcCritChance(Unit victim, ref float chance)
        {
            // If caster is level >= 56, While stealthed or have Incarnation: King of the Jungle aura,
            // Double the chance to critically strike
            if ((m_casterLevel >= 56) && (m_stealthed || m_incarnation))
            {
                chance *= 2.0f;
            }
        }

        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            int damage = GetHitDamage();

            caster.ModifyPower(PowerType.ComboPoints, 1);

            // If caster is level >= 56, While stealthed or have Incarnation: King of the Jungle aura,
            // deals 50% increased damage (get value from the spell data)
            if ((caster.HasAura(231057)) && (m_stealthed || m_incarnation))
            {
                MathFunctions.AddPct(ref damage, Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_SHRED, Difficulty.None).GetEffect(2).BasePoints);
            }

            SetHitDamage(damage);
        }

        private bool m_stealthed = false;
        private bool m_incarnation = false;
        private uint m_casterLevel;
    }

    // 8936 - Regrowth
    [SpellScript(8936)]
    public class spell_dru_regrowth : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(DruidSpells.SPELL_DRU_REGROWTH, DruidSpells.SPELL_DRU_BLOODTALONS, DruidSpells.SPELL_DRU_BLOODTALONS_TRIGGERED, DruidSpells.SPELL_DRU_MOMENT_OF_CLARITY, DruidSpells.SPELL_DRU_CLEARCASTING);
        }

        private void HandleHealEffect(uint UnnamedParameter)
        {
            if (GetCaster().HasAura(DruidSpells.SPELL_DRU_BLOODTALONS))
            {
                GetCaster().AddAura(DruidSpells.SPELL_DRU_BLOODTALONS_TRIGGERED, GetCaster());
            }

            Aura clearcasting = GetCaster().GetAura(DruidSpells.SPELL_DRU_CLEARCASTING);
            if (clearcasting != null)
            {
                if (GetCaster().HasAura(DruidSpells.SPELL_DRU_MOMENT_OF_CLARITY))
                {
                    int amount = clearcasting.GetEffect(0).GetAmount();
                    clearcasting.GetEffect(0).SetAmount(amount - 1);
                    if (amount == -102)
                    {
                        GetCaster().RemoveAurasDueToSpell(DruidSpells.SPELL_DRU_CLEARCASTING);
                    }
                }
                else
                {
                    GetCaster().RemoveAurasDueToSpell(DruidSpells.SPELL_DRU_CLEARCASTING);
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHealEffect, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
        }
    }

    // 197721 - Flourish
    [SpellScript(197721)]
    public class spell_dru_flourish : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint UnnamedParameter)
        {
            if (!GetCaster() || !GetHitUnit())
            {
                return;
            }

            List<AuraEffect> auraEffects = GetHitUnit().GetAuraEffectsByType(AuraType.PeriodicHeal);

            foreach (AuraEffect auraEffect in auraEffects)
            {
                if (auraEffect.GetCasterGUID() == GetCaster().GetGUID())
                {
                    Aura healAura = auraEffect.GetBase();
                    if (healAura != null)
                    {
                        healAura.SetDuration(healAura.GetDuration() + GetEffectValue() * Time.InMilliseconds);
                    }
                }
            }
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            List<WorldObject> tempTargets = new List<WorldObject>();
            foreach (WorldObject target in targets)
            {
                if (target.IsPlayer())
                {
                    if (target.ToUnit().HasAuraTypeWithCaster(AuraType.PeriodicHeal, GetCaster().GetGUID()))
                    {
                        tempTargets.Add(target);
                    }
                }
            }

            if (tempTargets.Count > 0)
            {
                targets.Clear();
                foreach (WorldObject target in tempTargets)
                {
                    targets.Add(target);
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    // 18562 - swiftmend
    [SpellScript(18562)]
    public class spell_dru_swiftmend : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


        private struct Spells
        {
            public static uint SPELL_DRUID_SOUL_OF_THE_FOREST = 158478;
            public static uint SPELL_DRUID_SOUL_OF_THE_FOREST_TRIGGERED = 114108;
        }


        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                if (caster.HasAura(Spells.SPELL_DRUID_SOUL_OF_THE_FOREST))
                {
                    caster.AddAura(Spells.SPELL_DRUID_SOUL_OF_THE_FOREST_TRIGGERED, caster);
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
        }
    }

    // 102793 - Ursol's Vortex
    [SpellScript(102793)]
    public class spell_dru_ursols_vortex : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


        private struct Spells
        {
            public static uint SPELL_DRUID_URSOLS_VORTEX_SLOW = 127797;
        }


        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.AddAura(Spells.SPELL_DRUID_URSOLS_VORTEX_SLOW, GetHitUnit());
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

    // 102351 - Cenarion Ward
    [SpellScript(102351)]
    public class spell_dru_cenarion_ward : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private struct Spells
        {
            public static uint SPELL_DRUID_CENARION_WARD_TRIGGERED = 102352;
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(Spells.SPELL_DRUID_CENARION_WARD_TRIGGERED);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            if (!GetCaster() || !eventInfo.GetActionTarget())
            {
                return;
            }

            GetCaster().CastSpell(eventInfo.GetActionTarget(), Spells.SPELL_DRUID_CENARION_WARD_TRIGGERED, true);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    // 102352 - Cenarion Ward
    [SpellScript(102352)]
    public class spell_dru_cenarion_ward_hot : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


        private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
        {
            if (!GetCaster())
            {
                return;
            }

            amount = (int)MathFunctions.CalculatePct(GetCaster().SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 220) / 4;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicHeal));
        }
    }

    // 54845 - Glyph of Starfire
    [SpellScript(54845)]
    public class spell_dru_glyph_of_starfire_proc : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), DruidSpells.SPELL_DRUID_GLYPH_OF_STARFIRE, true);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    // 61391 - Typhoon
    [SpellScript(61391)]
    public class spell_dru_typhoon : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleKnockBack(uint effIndex)
        {
            // Glyph of Typhoon
            if (GetCaster().HasAura(DruidSpells.SPELL_DRUID_GLYPH_OF_TYPHOON))
            {
                PreventHitDefaultEffect(effIndex);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleKnockBack, 0, SpellEffectName.KnockBack, SpellScriptHookType.EffectHitTarget));
        }
    }

    // 202808 - Primal Vitality
    [SpellScript(202808)]
    public class spell_dru_primal_vitality : AuraScript, IHasAuraEffects
    {


        private const int SPELL_DRUID_PRIMAL_VITALITY_PASSIVE = 202808;
        private const int SPELL_DRUID_PRIMAL_VITALITY_EFFECT = 202812;
        private const int SPELL_DRUID_PROWL = 5215;

        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetSpellInfo() != null)
            {
                return false;
            }

            if (eventInfo.GetDamageInfo() != null)
            {
                return false;
            }

            if (eventInfo.GetSpellInfo().Id != SPELL_DRUID_PROWL)
            {
                return false;
            }

            return true;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit target = eventInfo.GetProcTarget();
            if (target != null)
            {
                if (!target.HasAura(SPELL_DRUID_PRIMAL_VITALITY_EFFECT))
                {
                    target.AddAura(SPELL_DRUID_PRIMAL_VITALITY_EFFECT, target);
                }

            }
        }

        public override void Register()
        {

            AuraEffects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

    }

 
    [SpellScript(50464)]
    public class spell_dru_nourish : SpellScript
    {
        private const int SPELL_DRUID_NOURISH_PASSIVE = 203374;
        private const int SPELL_DRUID_REJUVENATION = 774;

        public void OnHit()
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Unit target = GetHitUnit();
                if (target != null)
                {
                    if (caster.HasAura(SPELL_DRUID_NOURISH_PASSIVE))
                    {
                        caster.CastSpell(target, SPELL_DRUID_REJUVENATION, true);
                    }
                }
            }
        }


    }

    // Called by Ironfur - 192081
    // Den Mother - 201522
    [SpellScript(201522)]
    public class spell_dru_denmother : SpellScript, ISpellOnHit
    {
        private const int SPELL_DRUID_DEN_MOTHER = 201522;
        private const int SPELL_DRUID_DEN_MOTHER_IRONFUR = 201629;

        public void OnHit()
        {
            Player _player = GetCaster().ToPlayer();
            if (_player != null)
            {
                if (_player.HasAura(SPELL_DRUID_DEN_MOTHER))
                {
                    List<Unit> validTargets = new List<Unit>();
                    List<Unit> groupList = new List<Unit>();

                    _player.GetPartyMembers(groupList);

                    if (groupList.Count == 0)
                    {
                        return;
                    }

                    foreach (var itr in groupList)
                    {
                        if ((itr.GetGUID() != _player.GetGUID()) && (itr.IsInRange(_player, 0, 50, true)))
                        {
                            validTargets.Add(itr.ToUnit());
                        }
                    }

                    if (validTargets.Count == 0)
                    {
                        return;
                    }

                    validTargets.Sort(new HealthPctOrderPred());
                    var lowTarget = validTargets.First();

                    _player.CastSpell(lowTarget, 201629, true);
                }
            }
        }
    }
    /*
    // Overgrowth - 203651
    public class spell_dru_overgrowth : SpellScriptLoader
    {
        public spell_dru_overgrowth() : base("spell_dru_overgrowth")
        {
        }

        public class spell_dru_overgrowth : SpellScript
        {


            private const int SPELL_DRUID_REJUVENATION = 774;
            private const int SPELL_DRUID_WILD_GROWTH = 48438;
            private const int SPELL_DRUID_LIFE_BLOOM = 33763;
            private const int SPELL_DRUID_REGROWTH = 8936;

            private void HandleDummy(uint UnnamedParameter)
            {
                Unit caster = GetCaster();
                if (caster != null)
                {
                    Unit target = GetHitUnit();
                    if (target != null)
                    {
                        caster.AddAura(SPELL_DRUID_REJUVENATION, target);
                        caster.AddAura(SPELL_DRUID_WILD_GROWTH, target);
                        caster.AddAura(SPELL_DRUID_LIFE_BLOOM, target);
                        caster.AddAura(SPELL_DRUID_REGROWTH, target);
                    }
                }
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            }
        }



        public override SpellScript GetSpellScript()
        {
            return new spell_dru_overgrowth();
        }
    }

    // 221514 - Skull Bash Charge
    public class spell_dru_skull_bash_charge : SpellScriptLoader
    {
        public spell_dru_skull_bash_charge() : base("spell_dru_skull_bash_charge")
        {
        }

        public class spell_dru_skull_bash_charge : SpellScript
        {


            private void HandleCharge(uint effIndex)
            {
                if (!GetCaster())
                {
                    return;
                }

                if (!GetHitUnit())
                {
                    return;
                }

                GetCaster().CastSpell(GetHitUnit(), 93985, true);
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleCharge, 0, SPELL_EFFECT_CHARGE, SpellScriptHookType.EffectHitTarget));
            }
        }



        public override SpellScript GetSpellScript()
        {
            return new spell_dru_skull_bash_charge();
        }
    }

    // 157982 - Tranquility Heal
    public class spell_dru_tranquility_heal : SpellScriptLoader
    {
        public spell_dru_tranquility_heal() : base("spell_dru_tranquility_heal")
        {
        }

        public class spell_dru_tranquility_heal : SpellScript
        {


            private void HandleHeal(uint UnnamedParameter)
            {
                if (!GetCaster())
                {
                    return;
                }

                Unit caster = GetCaster();
                if (caster != null)
                {
                    uint heal = MathFunctions.CalculatePct(caster.SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 180);
                    SetHitHeal(heal);
                }
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHit));
            }
        }



        public SpellScript GetSpellScript()
        {
            return new spell_dru_tranquility_heal();
        }
    }

    // 200389 - Cultivation
    public class spell_dru_cultivation : SpellScriptLoader
    {
        public spell_dru_cultivation() : base("spell_dru_cultivation")
        {
        }

        public class spell_dru_cultivation : AuraScript
        {


            private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
            {
                if (!GetCaster())
                {
                    return;
                }

                amount = MathFunctions.CalculatePct(GetCaster().SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 60);
            }

            public override void Register()
            {
                AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicHeal));
            }
        }



        public override AuraScript GetAuraScript()
        {
            return new spell_dru_cultivation();
        }
    }

    // 774 - Rejuvenation
    public class spell_dru_rejuvenation : SpellScriptLoader
    {
        public spell_dru_rejuvenation() : base("spell_dru_rejuvenation")
        {
        }

        public enum Spells : uint
        {
            SPELL_DRUID_CULTIVATION = 200390,
            SPELL_DRUID_CULTIVATION_HOT = 200389,
            SPELL_DRUID_GERMINATION = 155675,
            SPELL_DRUID_GERMINATION_HOT = 155777,
            SPELL_DRUID_ABUNDANCE = 207383,
            SPELL_DRUID_ABUNDANCE_BUFF = 207640
        }

        public class spell_dru_rejuvenation : AuraScript
        {


            //Posible Fixed


            // bool Validate(SpellInfo const* spellInfo) override
            // {
            //     return ValidateSpellInfo(
            //         {
            //             SPELL_DRUID_CULTIVATION,
            //             SPELL_DRUID_CULTIVATION_HOT,
            //             SPELL_DRUID_ABUNDANCE,
            //             SPELL_DRUID_ABUNDANCE_BUFF,
            //         });
            // }
            //
            // void AfterRemove(AuraEffect const* aurEff, AuraEffectHandleModes mode)
            // {
            //     if (Unit* caster = GetCaster())
            //         if (caster->HasAura(SPELL_DRUID_ABUNDANCE))
            //             if (Aura* abundanceBuff = caster->GetAura(SPELL_DRUID_ABUNDANCE_BUFF))
            //                 abundanceBuff->ModStackAmount(-1);
            // }
            //
            // void OnPeriodic(AuraEffect const* aurEff)
            // {
            //     if (Unit* target = GetTarget())
            //         if (GetCaster()->HasAura(SPELL_DRUID_CULTIVATION) && !target->HasAura(SPELL_DRUID_CULTIVATION_HOT) && target->HealthBelowPct(Global.SpellMgr->GetSpellInfo//(SPELL_DRUID_CULTIVATION)->GetEffect(0).BasePoints))
            //             GetCaster()->CastSpell(target, SPELL_DRUID_CULTIVATION_HOT, true);
            // }
            //
            // void CalculateAmount(AuraEffect const* aurEff, int32& amount, bool& canBeRecalculated)
            // {
            //     if (!GetCaster())
            //         return;
            //
            //     amount = MathFunctions.CalculatePct(GetCaster()->SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 60);
            // }

            //Posible Fixed
          
            private enum Spells : uint
            {
                GlyphofRejuvenation = 17076,
                GlyphofRejuvenationEffect = 96206
            }
            private void HandleCalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
            {
                Unit l_Caster = GetCaster();
                if (l_Caster != null)
                {
                    ///If soul of the forest is activated we increase the heal by 100%
                    if (l_Caster.HasAura(SoulOfTheForestSpells.SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO) && !l_Caster.HasAura(DruidSpells.SPELL_DRUID_REJUVENATION))
                    {
                        amount *= 2;
                        l_Caster.RemoveAura(SoulOfTheForestSpells.SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO);
                    }
                }
            }

            private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
            {
                Unit caster = GetCaster();

                if (caster == null)
                {
                    return;
                }

                AuraEffect GlyphOfRejuvenation = caster.GetAuraEffect(Spells.GlyphofRejuvenation, 0);
                if (GlyphOfRejuvenation != null)
                {
                    GlyphOfRejuvenation.SetAmount(GlyphOfRejuvenation.GetAmount() + 1);
                    if (GlyphOfRejuvenation.GetAmount() >= 3)
                    {
                        caster.CastSpell(caster, Spells.GlyphofRejuvenationEffect, true);
                    }
                }
            }

            private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
            {
                Unit caster = GetCaster();

                if (caster == null)
                {
                    return;
                }

                AuraEffect l_GlyphOfRejuvenation = caster.GetAuraEffect(Spells.GlyphofRejuvenation, 0);
                if (l_GlyphOfRejuvenation != null)
                {
                    l_GlyphOfRejuvenation.SetAmount(l_GlyphOfRejuvenation.GetAmount() - 1);
                    if (l_GlyphOfRejuvenation.GetAmount() < 3)
                    {
                        caster.RemoveAura(Spells.GlyphofRejuvenationEffect);
                    }
                }
            }

            public override void Register()
            {
                // Posible Fixed
                AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
                AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
                AuraEffects.Add(new EffectCalcAmountHandler(HandleCalculateAmount, 0, AuraType.PeriodicHeal));

                //  OnEffectPeriodic += AuraEffectPeriodicFn(spell_dru_rejuvenation::OnPeriodic, 0, AuraType.PeriodicHeal);
                //  DoEffectCalcAmount += AuraEffectCalcAmountFn(spell_dru_rejuvenation::CalculateAmount, 0, AuraType.PeriodicHeal);
                //  AfterEffectRemove += AuraEffectRemoveFn(spell_dru_rejuvenation::AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real);
            }
        }



        public override AuraScript GetAuraScript()
        {
            return new spell_dru_rejuvenation();
        }

        public class spell_dru_rejuvenation : SpellScript
        {


            public override bool Validate(SpellInfo UnnamedParameter)
            {
                return ValidateSpellInfo(SPELL_DRUID_GERMINATION, SPELL_DRUID_GERMINATION_HOT, SPELL_DRUID_ABUNDANCE, SPELL_DRUID_ABUNDANCE_BUFF);
            }

            private int m_RejuvenationAura = 0;
            private int m_RejuvenationAuraAmount = 0;

            public void AfterHit()
            {
                Unit caster = GetCaster();
                if (caster == null)
                {
                    return;
                }

                Unit target = GetHitUnit();
                if (target == null)
                {
                    return;
                }

                Aura RejuvenationAura = target.GetAura(DruidSpells.SPELL_DRUID_REJUVENATION, caster.GetGUID());

                if (RejuvenationAura != null && m_RejuvenationAura > 0)
                {
                    RejuvenationAura.SetDuration(m_RejuvenationAura);
                }

                AuraEffect NewRejuvenationAuraEffect = target.GetAuraEffect(DruidSpells.SPELL_DRUID_REJUVENATION, 0);
                if (NewRejuvenationAuraEffect != null)
                {
                    if (caster.HasAura(SoulOfTheForestSpells.SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO))
                    {
                        NewRejuvenationAuraEffect.SetAmount(NewRejuvenationAuraEffect.GetAmount() * 2);
                        caster.RemoveAura(SoulOfTheForestSpells.SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO);
                    }
                }
                if (caster.HasAura(207383))
                {
                    caster.CastSpell(caster, SPELL_DRUID_ABUNDANCE, true);
                }
            }

            private void HandleBeforeHit(SpellMissInfo missInfo)
            {
                Unit caster = GetCaster();
                if (caster == null)
                {
                    return;
                }

                Unit target = GetHitUnit();
                if (target == null)
                {
                    return;
                }

                if (caster.HasAura(SoulOfTheForestSpells.SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO))
                {
                    //      NewRejuvenationAuraEffect->SetAmount(NewRejuvenationAuraEffect->GetAmount() * 2);
                    SetHitHeal(GetHitHeal() * 2);
                    //      caster->RemoveAura(SPELL_DRUID_SOUL_OF_THE_FOREST_RESTO);
                }

                ///Germination
                if (caster.HasAura(155675) && target.HasAura(DruidSpells.SPELL_DRUID_REJUVENATION, caster.GetGUID()))
                {
                    Aura RejuvenationAura = target.GetAura(DruidSpells.SPELL_DRUID_REJUVENATION, caster.GetGUID());
                    if (RejuvenationAura == null)
                    {
                        return;
                    }

                    if (!target.HasAura(155777, caster.GetGUID()))
                    {
                        caster.CastSpell(target, 155777, true);
                        m_RejuvenationAura = RejuvenationAura.GetDuration();
                    }
                    else
                    {
                        Aura GerminationAura = target.GetAura(155777, caster.GetGUID());
                        Aura RejuvenationAura = target.GetAura(DruidSpells.SPELL_DRUID_REJUVENATION, caster.GetGUID());
                        if (GerminationAura != null && RejuvenationAura != null)
                        {
                            int GerminationDuration = GerminationAura.GetDuration();
                            int RejuvenationDuration = RejuvenationAura.GetDuration();
                            if (GerminationDuration > RejuvenationDuration)
                            {
                                caster.AddAura(DruidSpells.SPELL_DRUID_REJUVENATION, target);
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

            public override void Register()
            {
                BeforeHit += BeforeSpellHitFn(this.HandleBeforeHit);

            }
        }



        public SpellScript GetSpellScript()
        {
            return new spell_dru_rejuvenation();
        }
    }

    // 58180 - Infected Wounds
    public class spell_dru_infected_wounds : SpellScriptLoader
    {
        public spell_dru_infected_wounds() : base("spell_dru_infected_wounds")
        {
        }


        public class spell_dru_infected_wounds : SpellScript
        {


            private void HandleDummy(uint UnnamedParameter)
            {
                if (!GetCaster())
                {
                    return;
                }

                if (GetCaster().HasAura(GetSpellInfo().Id))
                {
                    GetCaster().RemoveAurasDueToSpell(GetSpellInfo().Id);
                }
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
            }
        }



        public override SpellScript GetSpellScript()
        {
            return new spell_dru_infected_wounds();
        }
    }

    // 190984 - Solar Wrath | 194153 - Lunar Strike
    public class spell_dru_blessing_of_elune : SpellScript
    {


        public void OnHit()
        {
            Unit caster = GetCaster();

            if (caster == null)
            {
                return;
            }

            uint power = GetHitDamage();

            Aura aura = caster.GetAura(202737);
            if (aura != null)
            {
                AuraEffect aurEff = aura.GetEffect(0);
                if (aurEff != null)
                {
                    power += MathFunctions.CalculatePct(power, aurEff.GetAmount());
                }
            }

            SetHitDamage(power);
        }


    }

    // 208253 - Essence of G'Hanir
    public class spell_dru_essence_of_ghanir : SpellScriptLoader
    {
        public spell_dru_essence_of_ghanir() : base("spell_dru_essence_of_ghanir")
        {
        }

        public class spell_dru_essence_of_ghanir : AuraScript
        {


            private void HandleEffectCalcSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
            {
                if (spellMod == null)
                {
                    SpellModifierByClassMask mod = new SpellModifierByClassMask(GetAura());
                    mod.op = SpellModOp.PeriodicHealingAndDamage;
                    mod.type = SPELLMOD_FLAT;
                    mod.spellId = GetId();
                    mod.mask = aurEff.GetSpellEffectInfo().SpellClassMask;
                    spellMod = mod;
                }

                ((SpellModifierByClassMask)spellMod).value = aurEff.GetAmount() / 7;
            }

            public override void Register()
            {
                DoEffectCalcSpellMod += AuraEffectCalcSpellModFn(this.HandleEffectCalcSpellMod, 0, AuraType.AddPctModifier);
            }
        }



        public AuraScript GetAuraScript()
        {
            return new spell_dru_essence_of_ghanir();
        }
    }

    // 200851 - Rage of the Sleeper
    public class spell_dru_rage_of_the_sleeper : SpellScriptLoader
    {
        public spell_dru_rage_of_the_sleeper() : base("spell_dru_rage_of_the_sleeper")
        {
        }

        public class spell_dru_rage_of_the_sleeper : AuraScript
        {


            private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
            {
                amount = -1;
            }

            private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
            {
                Unit caster = GetCaster();
                if (caster != null)
                {
                    Unit target = caster.GetVictim();
                    if (target != null)
                    {
                        caster.CastSpell(target, 219432, true);
                    }
                }
            }

            public override void Register()
            {
                AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
                AuraEffects.Add(new EffectApplyHandler(OnRemove, 1, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
            }
        }



        public AuraScript GetAuraScript()
        {
            return new spell_dru_rage_of_the_sleeper();
        }
    }

    // 194153 Lunar Strike
    public class spell_druid_lunar_strike : SpellScript
    {


        private enum Spells
        {
            SPELL_DRUID_LUNAR_STRIKE = 194153,
            SPELL_DRUID_WARRIOR_OF_ELUNE = 202425,
            SPELL_DRUID_NATURES_BALANCE = 202430
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(MoonfireSpells.SPELL_DRUID_MOONFIRE_DAMAGE, Spells.SPELL_DRUID_WARRIOR_OF_ELUNE, Spells.SPELL_DRUID_LUNAR_STRIKE, Spells.SPELL_DRUID_NATURES_BALANCE);
        }

        private void HandleHitTarget(uint UnnamedParameter)
        {
            Unit explTarget = GetExplTargetUnit();
            Unit currentTarget = GetHitUnit();

            if (explTarget == null || currentTarget == null)
            {
                return;
            }

            if (currentTarget != explTarget)
            {
                SetHitDamage(GetHitDamage() * GetSpellInfo().GetEffect(2).BasePoints / 100);
            }

            if (GetCaster().HasAura(Spells.SPELL_DRUID_NATURES_BALANCE))
            {
                Aura moonfireDOT = currentTarget.GetAura(MoonfireSpells.SPELL_DRUID_MOONFIRE_DAMAGE, GetCaster().GetGUID());
                if (moonfireDOT != null)
                {
                    int duration = moonfireDOT.GetDuration();
                    int newDuration = duration + 6 * Time.InMilliseconds;

                    if (newDuration > moonfireDOT.GetMaxDuration())
                    {
                        moonfireDOT.SetMaxDuration(newDuration);
                    }

                    moonfireDOT.SetDuration(newDuration);
                }
            }

            if (GetCaster() && RandomHelper.randChance(20) && GetCaster().HasAura(DruidSpells.SPELL_DRU_ECLIPSE))
            {
                GetCaster().CastSpell(null, DruidSpells.SPELL_DRU_SOLAR_EMPOWEREMENT, true);
            }
        }

        private void HandleHit(uint UnnamedParameter)
        {
            Aura WarriorOfElune = GetCaster().GetAura(Spells.SPELL_DRUID_WARRIOR_OF_ELUNE);
            if (WarriorOfElune != null)
            {
                int amount = WarriorOfElune.GetEffect(0).GetAmount();
                WarriorOfElune.GetEffect(0).SetAmount(amount - 1);
                if (amount == -102)
                {
                    GetCaster().RemoveAurasDueToSpell(Spells.SPELL_DRUID_WARRIOR_OF_ELUNE);
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHit));
        }
    }

    // 190984 Solar Wrath
    public class spell_druid_solar_wrath : SpellScript
    {


        private enum Spells
        {
            SPELL_DRUID_SOLAR_WRATH = 190984,
            SPELL_DRUID_NATURES_BALANCE = 202430,
            SPELL_DRUID_SUNFIRE_DOT = 164815
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(Spells.SPELL_DRUID_SUNFIRE_DOT, Spells.SPELL_DRUID_SOLAR_WRATH, Spells.SPELL_DRUID_NATURES_BALANCE);
        }

        private void HandleHitTarget(uint UnnamedParameter)
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                if (GetCaster().HasAura(Spells.SPELL_DRUID_NATURES_BALANCE))
                {
                    Aura sunfireDOT = target.GetAura(Spells.SPELL_DRUID_SUNFIRE_DOT, GetCaster().GetGUID());
                    if (sunfireDOT != null)
                    {
                        int duration = sunfireDOT.GetDuration();
                        int newDuration = duration + 4 * Time.InMilliseconds;

                        if (newDuration > sunfireDOT.GetMaxDuration())
                        {
                            sunfireDOT.SetMaxDuration(newDuration);
                        }

                        sunfireDOT.SetDuration(newDuration);
                    }
                }
            }
            if (GetCaster() && RandomHelper.randChance(20) && GetCaster().HasAura(DruidSpells.SPELL_DRU_ECLIPSE))
            {
                GetCaster().CastSpell(null, DruidSpells.SPELL_DRU_LUNAR_EMPOWEREMENT, true);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    // 203975 Earthwarden
    public class spell_druid_earthwarden_triggered : AuraScript
    {


        private enum Spells
        {
            SPELL_DRUID_EARTHWARDEN = 203974,
            SPELL_DRUID_EARTHWARDEN_TRIGGERED = 203975
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(Spells.SPELL_DRUID_EARTHWARDEN, Spells.SPELL_DRUID_EARTHWARDEN_TRIGGERED);
        }

        private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
        {
            amount = -1;
        }

        private void Absorb(AuraEffect auraEffect, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            if (dmgInfo.GetDamageType() == DIRECT_DAMAGE)
            {
                SpellInfo earthwarden = Global.SpellMgr.AssertSpellInfo(Spells.SPELL_DRUID_EARTHWARDEN, Difficulty.None);

                absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), earthwarden.GetEffect(0).BasePoints);
                GetCaster().RemoveAurasDueToSpell(Spells.SPELL_DRUID_EARTHWARDEN_TRIGGERED);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            AuraEffects.Add(new EffectAbsorbHandler(Absorb, 0));
        }
    }

    // 203974 Earthwarden
    public class spell_druid_earthwarden : AuraScript
    {


        private enum Spells
        {
            SPELL_DRUID_EARTHWARDEN = 203974,
            SPELL_DRUID_EARTHWARDEN_TRIGGERED = 203975,
            SPELL_DRUID_TRASH = 77758
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(Spells.SPELL_DRUID_EARTHWARDEN, Spells.SPELL_DRUID_EARTHWARDEN_TRIGGERED, Spells.SPELL_DRUID_TRASH);
        }

        private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            if (!GetCaster().ToPlayer().GetSpellHistory().HasCooldown(Spells.SPELL_DRUID_EARTHWARDEN))
            {
                GetCaster().AddAura(Spells.SPELL_DRUID_EARTHWARDEN_TRIGGERED, GetCaster());
            }
            GetCaster().ToPlayer().GetSpellHistory().AddCooldown(Spells.SPELL_DRUID_EARTHWARDEN, 0, std::chrono.milliseconds(500));
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    // 80313 Pulverize
    public class spell_druid_pulverize : SpellScript
    {


        private enum Spells
        {
            SPELL_DRUID_PULVERIZE = 80313,
            SPELL_DRUID_TRASH_DOT_TWO_STACKS_MARKER = 158790,
            SPELL_DRUID_PULVERIZE_DAMAGE_REDUCTION_BUFF = 158792
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(Spells.SPELL_DRUID_PULVERIZE, Spells.SPELL_DRUID_TRASH_DOT_TWO_STACKS_MARKER);
        }

        private void HandleHitTarget(uint UnnamedParameter)
        {
            Unit target = GetHitUnit();
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

    // 204053 Rend and Tear
    public class spell_druid_rend_and_tear : AuraScript
    {


        private enum Spells
        {
            SPELL_DRUID_REND_AND_TEAR = 204053,
            SPELL_DRUID_TRASH_DOT = 192090
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(Spells.SPELL_DRUID_REND_AND_TEAR, Spells.SPELL_DRUID_TRASH_DOT);
        }

        private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
        {
            amount = -1;
        }

        private void Absorb(AuraEffect auraEffect, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            Unit caster = GetCaster();
            Unit attacker = dmgInfo.GetAttacker();
            absorbAmount = 0;

            if (caster == null || attacker == null || !HasEffect(1))
            {
                return;
            }

            if (caster.GetShapeshiftForm() == FORM_BEAR_FORM)
            {
                Aura trashDOT = attacker.GetAura(Spells.SPELL_DRUID_TRASH_DOT, caster.GetGUID());
                if (trashDOT != null)
                {
                    absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), trashDOT.GetStackAmount() * GetSpellInfo().GetEffect(1).BasePoints);
                }
            }
        }

        private void HandleEffectCalcSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
        {
            if (spellMod == null)
            {
                return;
            }

            ((SpellModifierByClassMask)spellMod).value = GetCaster().GetShapeshiftForm() == FORM_BEAR_FORM ? aurEff.GetAmount() : 0;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            AuraEffects.Add(new EffectAbsorbHandler(Absorb, 0));
            DoEffectCalcSpellMod += AuraEffectCalcSpellModFn(this.HandleEffectCalcSpellMod, 1, AuraType.AddFlatModifier);
            DoEffectCalcSpellMod += AuraEffectCalcSpellModFn(this.HandleEffectCalcSpellMod, 2, AuraType.AddFlatModifier);
        }
    }

    //Incarnation: Tree of Life 33891
    public class incarnation_tree_of_life : SpellScript
    {


        public void AfterCast()
        {
            Unit caster = GetCaster();
            Aura tree = caster.GetAura(33891);
            if (tree != null)
            {
                tree.SetDuration(30000, true);
            }
        }

        public override void Register()
        {
            AfterCast += SpellCastFn(this.HandleAfterCast);
        }
    }

    //274837
    public class spell_feral_frenzy : SpellScript
    {


        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            if (caster == null || target == null)
            {
                return;
            }

            this.strikes = 0;

            int strikeDamage = 100 / 20 + caster.m_unitData.AttackPower;



            caster.GetScheduler().Schedule(TimeSpan.FromMilliseconds(50), (TaskContext context) =>
            {
                if (caster.GetDistance2d(target) <= 5.0f)
                {
                    strikes++;
                    if (this.strikes < 5)
                    {
                        context.Repeat(200ms);
                    }
                    else if (this.strikes == 5)
                    {
                        caster.CastSpell(target, DruidSpells.SPELL_FERAL_FRENZY_BLEED, true);
                        int bleedDamage = 100 / 10 + caster.m_unitData.AttackPower;
                    }
                }
            });
        }



        private byte strikes;
    }

    public class feral_spells : PlayerScript
    {
        public feral_spells() : base("feral_spells")
        {
        }

        public override void OnLogin(Player player, bool UnnamedParameter)
        {
            if (player.GetPrimarySpecialization() != TALENT_SPEC_DRUID_CAT)
            {
                return;
            }

            if (player.GetLevel() >= 5 && !player.HasSpell(DruidSpells.SPELL_DRUID_SHRED))
            {
                player.LearnSpell(DruidSpells.SPELL_DRUID_SHRED, false, true);
            }

            if (player.GetLevel() >= 20 && !player.HasSpell(DruidSpells.SPELL_DRUID_RIP))
            {
                player.LearnSpell(DruidSpells.SPELL_DRUID_RIP, false, true);
            }

            if (player.GetLevel() >= 24 && !player.HasSpell(DruidSpells.SPELL_DRUID_RAKE))
            {
                player.LearnSpell(DruidSpells.SPELL_DRUID_RAKE, false, true);
            }

            if (player.GetLevel() >= 32 && !player.HasSpell(DruidSpells.SPELL_DRUID_FEROCIOUS_BITE))
            {
                player.LearnSpell(DruidSpells.SPELL_DRUID_FEROCIOUS_BITE, false, true);
            }
        }
    }

    //78674
    public class spell_dru_starsurge : SpellScript
    {


        public void OnHit()
        {
            if (GetCaster())
            {
                if (GetCaster().GetAuraCount(DruidSpells.SPELL_DRU_STARLORD_BUFF) < 3)
                {
                    GetCaster().CastSpell(null, DruidSpells.SPELL_DRU_STARLORD_BUFF, true);
                }
            }
        }


    }

    //191034
    public class spell_dru_starfall : SpellScript
    {


        public void OnHit()
        {
            if (GetCaster())
            {
                if (GetCaster().GetAuraCount(DruidSpells.SPELL_DRU_STARLORD_BUFF) < 3)
                {
                    GetCaster().CastSpell(null, DruidSpells.SPELL_DRU_STARLORD_BUFF, true);
                }
            }
        }


    }

    //274902
    public class spell_dru_photosynthesis : AuraScript
    {


        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            ////  if (!GetCaster()->HasAura(SPELL_DRU_PHOTOSYNTHESIS_MOD_HEAL_TICKS))
            //    GetCaster()->AddAura(SPELL_DRU_PHOTOSYNTHESIS_MOD_HEAL_TICKS);
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            if (GetCaster().HasAura(DruidSpells.SPELL_DRU_PHOTOSYNTHESIS_MOD_HEAL_TICKS))
            {
                GetCaster().RemoveAura(DruidSpells.SPELL_DRU_PHOTOSYNTHESIS_MOD_HEAL_TICKS);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }

    //10682, 204066
    public class at_dru_lunar_beam : AreaTriggerAI
    {
        public at_dru_lunar_beam(AreaTrigger at) : base(at)
        {
        }

        public override void OnCreate()
        {
            at.SetPeriodicProcTimer(1000);
        }

        public override void OnPeriodicProc()
        {
            if (at.GetCaster())
            {
                at.GetCaster().CastSpell(at.GetPosition(), DruidSpells.SPELL_DRU_LUNAR_BEAM_DAMAGE_HEAL, true);
            }
        }
    }

    //3020, 102793
    public class at_dru_ursol_vortex : AreaTriggerAI
    {
        public at_dru_ursol_vortex(AreaTrigger at) : base(at)
        {
        }

        public override void OnUnitEnter(Unit target)
        {
            VALIDATE_CASTER();

            if (caster.IsInCombatWith(target))
            {
                caster.CastSpell(target, DruidSpells.SPELL_DRU_URSOL_VORTEX_DEBUFF, true);
            }
        }

        public override void OnUnitExit(Unit target)
        {
            target.RemoveAurasDueToSpell(DruidSpells.SPELL_DRU_URSOL_VORTEX_DEBUFF);
            if (!_hasPull && target.IsValidAttackTarget(at.GetCaster()))
            {
                _hasPull = true;
                target.CastSpell(at.GetPosition(), DruidSpells.SPELL_DRU_URSOL_VORTEX_PULL, true);
            }
        }

        private bool _hasPull = false;
    }

    //102359
    public class spell_dru_mass_entanglement : SpellScript
    {


        public void OnCast()
        {
            List<Unit> targetList = new List<Unit>();
            GetCaster().GetAttackableUnitListInRange(targetList, 15.0f);
            if (targetList.Count != 0)
            {
                foreach (var targets in targetList)
                {
                    GetCaster().AddAura(DruidSpells.SPELL_DRU_MASS_ENTANGLEMENT, targets);
                }
            }
        }


    }

    public class dru_predator : PlayerScript
    {
        public dru_predator() : base("dru_predator")
        {
        }

        public void OnPVPKill(Player killer, Player killed)
        {
            if (killer.GetClass() == Class.Druid)
            {
                return;
            }

            if (!killer.HasAura(DruidSpells.SPELL_DRU_PREDATOR))
            {
                return;
            }

            if (killer.GetSpellHistory().HasCooldown(DruidSpells.SPELL_DRU_TIGER_FURY))
            {
                killer.GetSpellHistory().ResetCooldown(DruidSpells.SPELL_DRU_TIGER_FURY);
            }
        }

        public void OnCreatureKill(Player killer, Creature killed)
        {
            if (killer.GetClass() == Class.Druid)
            {
                return;
            }

            if (!killer.HasAura(DruidSpells.SPELL_DRU_PREDATOR))
            {
                return;
            }

            if (killer.GetSpellHistory().HasCooldown(DruidSpells.SPELL_DRU_TIGER_FURY))
            {
                killer.GetSpellHistory().ResetCooldown(DruidSpells.SPELL_DRU_TIGER_FURY);
            }
        }
    }

    // Teleport : Moonglade - 18960
    public class spell_dru_teleport_moonglade : SpellScriptLoader
    {
        public spell_dru_teleport_moonglade() : base("spell_dru_teleport_moonglade")
        {
        }

        public class spell_dru_teleport_moonglade : SpellScript
        {


            public void AfterCast()
            {
                Player _player = GetCaster().ToPlayer();
                if (_player != null)
                {
                    _player.TeleportTo(1, 7964.063f, -2491.099f, 487.83f, _player.GetOrientation());
                }
            }

            public override void Register()
            {
                AfterCast += SpellCastFn(this.HandleAfterCast);
            }
        }



        public SpellScript GetSpellScript()
        {
            return new spell_dru_teleport_moonglade();
        }
    }
    // 202430 - Nature's Balance
    public class spell_dru_natures_balance : AuraScript
    {


        private enum Spells
        {
            SPELL_DRUID_NATURES_BALANCE = 202430
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(Spells.SPELL_DRUID_NATURES_BALANCE);
        }

        private void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster == null || !caster.IsAlive() || caster.GetMaxPower(POWER_LUNAR_POWER) == 0)
            {
                return;
            }

            if (caster.IsInCombat())
            {
                int amount = Math.Max(caster.GetAuraEffect(Spells.SPELL_DRUID_NATURES_BALANCE, 0).GetAmount(), 0);
                // don't regen when permanent aura target has full power
                if (caster.GetPower(POWER_LUNAR_POWER) == caster.GetMaxPower(POWER_LUNAR_POWER))
                {
                    return;
                }

                caster.ModifyPower(POWER_LUNAR_POWER, amount);
            }
            else
            {
                if (caster.GetPower(POWER_LUNAR_POWER) > 500)
                {
                    return;
                }

                caster.SetPower(POWER_LUNAR_POWER, 500);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandlePeriodic, 0, SPELL_AURA_PERIODIC_ENERGIZE));
        }
    }
*/

    [Script] // 22812 - Barkskin
    internal class spell_dru_barkskin : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BramblesPassive);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicDummy));
        }

        private void HandlePeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();

            if (target.HasAura(SpellIds.BramblesPassive))
                target.CastSpell(target, SpellIds.BramblesDamageAura, true);
        }
    }

    [Script] // 77758 - Berserk
    internal class spell_dru_berserk : SpellScript, ISpellBeforeCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearForm);
        }

        public void BeforeCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.BearForm, true);
        }
    }

    [Script] // 203953 - Brambles - SPELL_DRUID_BRAMBLES_PASSIVE
    internal class spell_dru_brambles : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BramblesRelect, SpellIds.BramblesDamageAura);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectAbsorbHandler(HandleAbsorb, 0, false, AuraScriptHookType.EffectAbsorb));
            AuraEffects.Add(new EffectAbsorbHandler(HandleAfterAbsorb, 0, false, AuraScriptHookType.EffectAfterAbsorb));
        }

        private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            // Prevent Removal
            PreventDefaultAction();
        }

        private void HandleAfterAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            // reflect back Damage to the Attacker
            Unit target = GetTarget();
            Unit attacker = dmgInfo.GetAttacker();

            if (attacker != null)
                target.CastSpell(attacker, SpellIds.BramblesRelect, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorbAmount));
        }
    }

    [Script] // 155835 - Bristling Fur
    internal class spell_dru_bristling_fur : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BristlingFurGainRage);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // BristlingFurRage = 100 * Damage / MaxHealth.
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo != null)
            {
                Unit target = GetTarget();
                uint rage = (uint)(target.GetMaxPower(PowerType.Rage) * (float)damageInfo.GetDamage() / (float)target.GetMaxHealth());

                if (rage > 0)
                    target.CastSpell(target, SpellIds.BristlingFurGainRage, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)rage));
            }
        }
    }

    [Script] // 768 - CatForm - SPELL_DRUID_CAT_FORM
    internal class spell_dru_cat_form : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Prowl);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleAfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void HandleAfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveOwnedAura(SpellIds.Prowl);
        }
    }

    [Script] // 1850 - Dash
    public class spell_dru_dash : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModIncreaseSpeed));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            // do not set speed if not in cat form
            if (GetUnitOwner().GetShapeshiftForm() != ShapeShiftForm.CatForm)
                amount = 0;
        }
    }

    internal class spell_dru_eclipse_common
    {
        public static void SetSpellCount(Unit unitOwner, uint spellId, uint amount)
        {
            Aura aura = unitOwner.GetAura(spellId);

            if (aura == null)
                unitOwner.CastSpell(unitOwner, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, (int)amount));
            else
                aura.SetStackAmount((byte)amount);
        }
    }

    [Script] // 48517 Eclipse (Solar) + 48518 Eclipse (Lunar)
    internal class spell_dru_eclipse_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseDummy);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleRemoved, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void HandleRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            AuraEffect auraEffDummy = GetTarget().GetAuraEffect(SpellIds.EclipseDummy, 0);

            if (auraEffDummy == null)
                return;

            uint spellId = GetSpellInfo().Id == SpellIds.EclipseSolarAura ? SpellIds.EclipseLunarSpellCnt : SpellIds.EclipseSolarSpellCnt;
            spell_dru_eclipse_common.SetSpellCount(GetTarget(), spellId, (uint)auraEffDummy.GetAmount());
        }
    }

    [Script] // 79577 - Eclipse - SPELL_DRUID_ECLIPSE_DUMMY
    internal class spell_dru_eclipse_dummy : AuraScript, IAuraOnProc, IAuraEnterLeaveCombat, IHasAuraEffects
    {
        private class InitializeEclipseCountersEvent : BasicEvent
        {
            private readonly uint _count;
            private readonly Unit _owner;

            public InitializeEclipseCountersEvent(Unit owner, uint count)
            {
                _owner = owner;
                _count = count;
            }

            public override bool Execute(ulong e_time, uint p_time)
            {
                spell_dru_eclipse_common.SetSpellCount(_owner, SpellIds.EclipseSolarSpellCnt, _count);
                spell_dru_eclipse_common.SetSpellCount(_owner, SpellIds.EclipseLunarSpellCnt, _count);

                return true;
            }
        }

        public void EnterLeaveCombat(bool isNowInCombat)
        {
            if (!isNowInCombat)
                GetTarget().CastSpell(GetTarget(), SpellIds.EclipseOoc, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarAura, SpellIds.EclipseLunarAura);
        }

        public void OnProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            if (spellInfo != null)
            {
                if (spellInfo.SpellFamilyFlags & new FlagArray128(0x4, 0x0, 0x0, 0x0)) // Starfire
                    OnSpellCast(SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarAura);
                else if (spellInfo.SpellFamilyFlags & new FlagArray128(0x1, 0x0, 0x0, 0x0)) // Wrath
                    OnSpellCast(SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarAura);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            AuraEffects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // counters are applied with a delay
            GetTarget().m_Events.AddEventAtOffset(new InitializeEclipseCountersEvent(GetTarget(), (uint)aurEff.GetAmount()), TimeSpan.FromSeconds(1));
        }

        private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.EclipseSolarSpellCnt);
            GetTarget().RemoveAura(SpellIds.EclipseLunarSpellCnt);
        }

        private void OnSpellCast(uint cntSpellId, uint otherCntSpellId, uint eclipseAuraSpellId)
        {
            Unit target = GetTarget();
            Aura aura = target.GetAura(cntSpellId);

            if (aura != null)
            {
                uint remaining = aura.GetStackAmount();

                if (remaining == 0)
                    return;

                if (remaining > 1)
                {
                    aura.SetStackAmount((byte)(remaining - 1));
                }
                else
                {
                    // cast eclipse
                    target.CastSpell(target, eclipseAuraSpellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                    // Remove stacks from other one as well
                    // reset remaining power on other spellId
                    target.RemoveAura(cntSpellId);
                    target.RemoveAura(otherCntSpellId);
                }
            }
        }
    }

    [Script] // 329910 - Eclipse out of combat - SPELL_DRUID_ECLIPSE_OOC
    internal class spell_dru_eclipse_ooc : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EclipseDummy, SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarSpellCnt);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(Tick, 0, AuraType.PeriodicDummy));
        }

        private void Tick(AuraEffect aurEff)
        {
            Unit owner = GetTarget();
            AuraEffect auraEffDummy = owner.GetAuraEffect(SpellIds.EclipseDummy, 0);

            if (auraEffDummy == null)
                return;

            if (!owner.IsInCombat() &&
                (!owner.HasAura(SpellIds.EclipseSolarSpellCnt) || !owner.HasAura(SpellIds.EclipseLunarSpellCnt)))
            {
                // Restore 2 stacks to each spell when out of combat
                spell_dru_eclipse_common.SetSpellCount(owner, SpellIds.EclipseSolarSpellCnt, (uint)auraEffDummy.GetAmount());
                spell_dru_eclipse_common.SetSpellCount(owner, SpellIds.EclipseLunarSpellCnt, (uint)auraEffDummy.GetAmount());
            }
        }
    }

    [Script] // 203974 - Earthwarden
    internal class spell_dru_earthwarden : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ThrashCat, SpellIds.ThrashBear, SpellIds.EarthwardenAura);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.EarthwardenAura, true);
        }
    }

    [Script] // 22568 - Ferocious Bite
    internal class spell_dru_ferocious_bite : SpellScript, IHasSpellEffects
    {
        private float _damageMultiplier = 0.0f;
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IncarnationKingOfTheJungle) && Global.SpellMgr.GetSpellInfo(SpellIds.IncarnationKingOfTheJungle, Difficulty.None).GetEffects().Count > 1;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 1, SpellEffectName.PowerBurn, SpellScriptHookType.LaunchTarget));
            SpellEffects.Add(new EffectHandler(HandleHitTargetBurn, 1, SpellEffectName.PowerBurn, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleHitTargetDmg, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleHitTargetBurn(uint effIndex)
        {
            int newValue = (int)((float)GetEffectValue() * _damageMultiplier);
            SetEffectValue(newValue);
        }

        private void HandleHitTargetDmg(uint effIndex)
        {
            int newValue = (int)((float)GetHitDamage() * (1.0f + _damageMultiplier));
            SetHitDamage(newValue);
        }

        private void HandleLaunchTarget(uint effIndex)
        {
            Unit caster = GetCaster();

            int maxExtraConsumedPower = GetEffectValue();

            AuraEffect auraEffect = caster.GetAuraEffect(SpellIds.IncarnationKingOfTheJungle, 1);

            if (auraEffect != null)
            {
                float multiplier = 1.0f + (float)auraEffect.GetAmount() / 100.0f;
                maxExtraConsumedPower = (int)((float)maxExtraConsumedPower * multiplier);
                SetEffectValue(maxExtraConsumedPower);
            }

            _damageMultiplier = Math.Min(caster.GetPower(PowerType.Energy), maxExtraConsumedPower) / maxExtraConsumedPower;
        }
    }

    [Script] // 37336 - Druid Forms Trinket
    internal class spell_dru_forms_trinket : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormsTrinketBear, SpellIds.FormsTrinketCat, SpellIds.FormsTrinketMoonkin, SpellIds.FormsTrinketNone, SpellIds.FormsTrinketTree);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActor();

            switch (target.GetShapeshiftForm())
            {
                case ShapeShiftForm.BearForm:
                case ShapeShiftForm.DireBearForm:
                case ShapeShiftForm.CatForm:
                case ShapeShiftForm.MoonkinForm:
                case ShapeShiftForm.None:
                case ShapeShiftForm.TreeOfLife:
                    return true;
                default:
                    break;
            }

            return false;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit target = eventInfo.GetActor();
            uint triggerspell;

            switch (target.GetShapeshiftForm())
            {
                case ShapeShiftForm.BearForm:
                case ShapeShiftForm.DireBearForm:
                    triggerspell = SpellIds.FormsTrinketBear;

                    break;
                case ShapeShiftForm.CatForm:
                    triggerspell = SpellIds.FormsTrinketCat;

                    break;
                case ShapeShiftForm.MoonkinForm:
                    triggerspell = SpellIds.FormsTrinketMoonkin;

                    break;
                case ShapeShiftForm.None:
                    triggerspell = SpellIds.FormsTrinketNone;

                    break;
                case ShapeShiftForm.TreeOfLife:
                    triggerspell = SpellIds.FormsTrinketTree;

                    break;
                default:
                    return;
            }

            target.CastSpell(target, triggerspell, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 203964 - Galactic Guardian
    internal class spell_dru_galactic_guardian : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GalacticGuardianAura);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo != null)
            {
                Unit target = GetTarget();

                // free automatic moonfire on Target
                target.CastSpell(damageInfo.GetVictim(), SpellIds.MoonfireDamage, true);

                // Cast aura
                target.CastSpell(damageInfo.GetVictim(), SpellIds.GalacticGuardianAura, true);
            }
        }
    }

    [Script] // 24858 - Moonkin Form
    internal class spell_dru_glyph_of_stars : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfStars, SpellIds.GlyphOfStarsVisual);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();

            if (target.HasAura(SpellIds.GlyphOfStars))
                target.CastSpell(target, SpellIds.GlyphOfStarsVisual, true);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.GlyphOfStarsVisual);
        }
    }

    [Script] // 210706 - Gore
    internal class spell_dru_gore : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GoreProc, SpellIds.Mangle);
        }

        public override void Register()
        {
            AuraEffects.Add(new CheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit owner = GetTarget();
            owner.CastSpell(owner, SpellIds.GoreProc);
            owner.GetSpellHistory().ResetCooldown(SpellIds.Mangle, true);
        }
    }

    [Script] // 99 - Incapacitating Roar
    internal class spell_dru_incapacitating_roar : SpellScript, ISpellBeforeCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearForm);
        }

        public void BeforeCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.BearForm, true);
        }
    }

    [Script] // 29166 - Innervate
    internal class spell_dru_innervate : SpellScript, ISpellCheckCast, ISpellOnHit
    {
        public SpellCastResult CheckCast()
        {
            Player target = GetExplTargetUnit()?.ToPlayer();

            if (target == null)
                return SpellCastResult.BadTargets;

            ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(target.GetPrimarySpecialization());

            if (spec == null ||
                spec.Role != 1)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        public void OnHit()
        {
            Unit caster = GetCaster();

            if (caster != GetHitUnit())
            {
                AuraEffect innervateR2 = caster.GetAuraEffect(SpellIds.InnervateRank2, 0);

                if (innervateR2 != null)
                    caster.CastSpell(caster,
                                     SpellIds.Innervate,
                                     new CastSpellExtraArgs(TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnoreCastInProgress)
                                         .SetTriggeringSpell(GetSpell())
                                         .AddSpellMod(SpellValueMod.BasePoint0, -innervateR2.GetAmount()));
            }
        }
    }

    [Script] // 40442 - Druid Tier 6 Trinket
    internal class spell_dru_item_t6_trinket : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessingOfRemulos, SpellIds.BlessingOfElune, SpellIds.BlessingOfCenarius);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            if (spellInfo == null)
                return;

            uint spellId;
            int chance;

            // Starfire
            if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000004u))
            {
                spellId = SpellIds.BlessingOfRemulos;
                chance = 25;
            }
            // Rejuvenation
            else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000010u))
            {
                spellId = SpellIds.BlessingOfElune;
                chance = 25;
            }
            // Mangle (Bear) and Mangle (Cat)
            else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000440u))
            {
                spellId = SpellIds.BlessingOfCenarius;
                chance = 40;
            }
            else
            {
                return;
            }

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell((Unit)null, spellId, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 33763 - Lifebloom
    internal class spell_dru_lifebloom : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.LifebloomFinalHeal);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Final heal only on duration end
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire ||
                GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell)
                GetCaster().CastSpell(GetUnitOwner(), SpellIds.LifebloomFinalHeal, true);
        }
    }

    [Script] // 155580 - Lunar Inspiration
    internal class spell_dru_lunar_inspiration : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.LunarInspirationOverride);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.LunarInspirationOverride, true);
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.LunarInspirationOverride);
        }
    }

    [Script] //  8921 - Moonfire
    internal class spell_dru_moonfire : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MoonfireDamage);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.MoonfireDamage, true);
        }
    }

    [Script] // 16864 - Omen of Clarity
    internal class spell_dru_omen_of_clarity : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BalanceT10Bonus, SpellIds.BalanceT10BonusProc);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();

            if (target.HasAura(SpellIds.BalanceT10Bonus))
                target.CastSpell(null, SpellIds.BalanceT10BonusProc, true);
        }
    }

    [Script] // 5215 - Prowl
    internal class spell_dru_prowl : SpellScript, ISpellBeforeCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CatForm);
        }

        public void BeforeCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.CatForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.CatForm, true);
        }
    }

    [Script] // 1079 - Rip
    internal class spell_dru_rip : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Load()
        {
            Unit caster = GetCaster();

            return caster != null && caster.IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicDamage));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;

            Unit caster = GetCaster();

            if (caster != null)
            {
                // 0.01 * $AP * cp
                byte cp = (byte)caster.ToPlayer().GetComboPoints();

                // Idol of Feral Shadows. Can't be handled as SpellMod due its dependency from CPs
                AuraEffect idol = caster.GetAuraEffect(SpellIds.IdolOfFeralShadows, 0);

                if (idol != null)
                    amount += cp * idol.GetAmount();
                // Idol of Worship. Can't be handled as SpellMod due its dependency from CPs
                else if ((idol = caster.GetAuraEffect(SpellIds.IdolOfWorship, 0)) != null)
                    amount += cp * idol.GetAmount();

                amount += (int)MathFunctions.CalculatePct(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), cp);
            }
        }
    }

    [Script] // 52610 - Savage Roar
    internal class spell_dru_savage_roar : SpellScript, ISpellCheckCast
    {
        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();

            if (caster.GetShapeshiftForm() != ShapeShiftForm.CatForm)
                return SpellCastResult.OnlyShapeshift;

            return SpellCastResult.SpellCastOk;
        }
    }

    [Script]
    internal class spell_dru_savage_roar_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SavageRoar);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(AfterApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.SavageRoar, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.SavageRoar);
        }
    }

    [Script] // 106839 - Skull Bash
    internal class spell_dru_skull_bash : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SkullBashCharge, SpellIds.SkullBashInterrupt);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SkullBashCharge, true);
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SkullBashInterrupt, true);
        }
    }

    [Script] // 106898 - Stampeding Roar
    internal class spell_dru_stampeding_roar : SpellScript, ISpellBeforeCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearForm);
        }

        public void BeforeCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.BearForm, true);
        }
    }

    [Script] // 50286 - Starfall (Dummy)
    internal class spell_dru_starfall_dummy : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            targets.Resize(2);
        }

        private void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();

            // Shapeshifting into an animal form or mounting cancels the effect
            if (caster.GetCreatureType() == CreatureType.Beast ||
                caster.IsMounted())
            {
                SpellInfo spellInfo = GetTriggeringSpell();

                if (spellInfo != null)
                    caster.RemoveAurasDueToSpell(spellInfo.Id);

                return;
            }

            // Any effect which causes you to lose control of your character will supress the starfall effect.
            if (caster.HasUnitState(UnitState.Controlled))
                return;

            caster.CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }
    }

    [Script] //  93402 - Sunfire
    internal class spell_dru_sunfire : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SunfireDamage, true);
        }
    }

    [Script] // 61336 - Survival Instincts
    internal class spell_dru_survival_instincts_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SurvivalInstincts);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.SurvivalInstincts, true);
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.SurvivalInstincts);
        }
    }

    [Script] // 40121 - Swift Flight Form (Passive)
    internal class spell_dru_swift_flight_passive : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModIncreaseVehicleFlightSpeed));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player caster = GetCaster().ToPlayer();

            if (caster != null)
                if (caster.GetSkillValue(SkillType.Riding) >= 375)
                    amount = 310;
        }
    }

    [Script] // 28744 - Regrowth
    internal class spell_dru_t3_6p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessingOfTheClaw);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.OverrideClassScripts, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.BlessingOfTheClaw, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 28719 - Healing Touch
    internal class spell_dru_t3_8p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhilarate);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Spell spell = eventInfo.GetProcSpell();

            if (spell == null)
                return;

            Unit caster = eventInfo.GetActor();
            var spellPowerCostList = spell.GetPowerCost();
            var spellPowerCost = spellPowerCostList.First(cost => cost.Power == PowerType.Mana);

            if (spellPowerCost == null)
                return;

            int amount = MathFunctions.CalculatePct(spellPowerCost.Amount, aurEff.GetAmount());
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell((Unit)null, SpellIds.Exhilarate, args);
        }
    }

    // 37288 - Mana Restore
    [Script] // 37295 - Mana Restore
    internal class spell_dru_t4_2p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Infusion);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell((Unit)null, SpellIds.Infusion, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 70723 - Item - Druid T10 Balance 4P Bonus
    internal class spell_dru_t10_balance_4p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Languish);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                damageInfo.GetDamage() == 0)
                return;

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.Languish, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.Languish, args);
        }
    }

    [Script] // 70691 - Item T10 Restoration 4P Bonus
    internal class spell_dru_t10_restoration_4p_bonus : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            if (!GetCaster().ToPlayer().GetGroup())
            {
                targets.Clear();
                targets.Add(GetCaster());
            }
            else
            {
                targets.Remove(GetExplTargetUnit());
                List<Unit> tempTargets = new();

                foreach (var obj in targets)
                    if (obj.IsTypeId(TypeId.Player) &&
                        GetCaster().IsInRaidWith(obj.ToUnit()))
                        tempTargets.Add(obj.ToUnit());

                if (tempTargets.Empty())
                {
                    targets.Clear();
                    FinishCast(SpellCastResult.DontReport);

                    return;
                }

                Unit target = tempTargets.SelectRandom();
                targets.Clear();
                targets.Add(target);
            }
        }
    }

    [Script] // 70664 - Druid T10 Restoration 4P Bonus (Rejuvenation)
    internal class spell_dru_t10_restoration_4p_bonus_dummy : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RejuvenationT10Proc);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            if (spellInfo == null ||
                spellInfo.Id == SpellIds.RejuvenationT10Proc)
                return false;

            HealInfo healInfo = eventInfo.GetHealInfo();

            if (healInfo == null ||
                healInfo.GetHeal() == 0)
                return false;

            Player caster = eventInfo.GetActor().ToPlayer();

            if (!caster)
                return false;

            return caster.GetGroup() || caster != eventInfo.GetProcTarget();
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            int amount = (int)eventInfo.GetHealInfo().GetHeal();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)eventInfo.GetHealInfo().GetHeal());
            eventInfo.GetActor().CastSpell((Unit)null, SpellIds.RejuvenationT10Proc, args);
        }
    }

    [Script] // 77758 - Thrash
    internal class spell_dru_thrash : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ThrashBearAura);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleOnHitTarget(uint effIndex)
        {
            Unit hitUnit = GetHitUnit();

            if (hitUnit != null)
            {
                Unit caster = GetCaster();

                caster.CastSpell(hitUnit, SpellIds.ThrashBearAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            }
        }
    }

    [Script] // 192090 - Thrash (Aura) - SPELL_DRUID_THRASH_BEAR_AURA
    internal class spell_dru_thrash_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BloodFrenzyAura, SpellIds.BloodFrenzyRageGain);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
        }

        private void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();

            if (caster != null)
                if (caster.HasAura(SpellIds.BloodFrenzyAura))
                    caster.CastSpell(caster, SpellIds.BloodFrenzyRageGain, true);
        }
    }

    // 1066 - Aquatic Form
    // 33943 - Flight Form
    // 40120 - Swift Flight Form
    [Script] // 165961 - Stag Form
    internal class spell_dru_travel_form_AuraScript : AuraScript, IHasAuraEffects
    {
        private uint triggeredSpellId;
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormStag, SpellIds.FormAquaticPassive, SpellIds.FormAquatic, SpellIds.FormFlight, SpellIds.FormSwiftFlight);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        public static uint GetFormSpellId(Player player, Difficulty difficulty, bool requiresOutdoor)
        {
            // Check what form is appropriate
            if (player.HasSpell(SpellIds.FormAquaticPassive) &&
                player.IsInWater()) // Aquatic form
                return SpellIds.FormAquatic;

            if (!player.IsInCombat() &&
                player.GetSkillValue(SkillType.Riding) >= 225 &&
                CheckLocationForForm(player, difficulty, requiresOutdoor, SpellIds.FormFlight) == SpellCastResult.SpellCastOk) // Flight form
                return player.GetSkillValue(SkillType.Riding) >= 300 ? SpellIds.FormSwiftFlight : SpellIds.FormFlight;

            if (!player.IsInWater() &&
                CheckLocationForForm(player, difficulty, requiresOutdoor, SpellIds.FormStag) == SpellCastResult.SpellCastOk) // Stag form
                return SpellIds.FormStag;

            return 0;
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // If it stays 0, it removes Travel Form dummy in AfterRemove.
            triggeredSpellId = 0;

            // We should only handle aura interrupts.
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Interrupt)
                return;

            // Check what form is appropriate
            triggeredSpellId = GetFormSpellId(GetTarget().ToPlayer(), GetCastDifficulty(), true);

            // If chosen form is current aura, just don't remove it.
            if (triggeredSpellId == ScriptSpellId)
                PreventDefaultAction();
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (triggeredSpellId == ScriptSpellId)
                return;

            Player player = GetTarget().ToPlayer();

            if (triggeredSpellId != 0) // Apply new form
                player.CastSpell(player, triggeredSpellId, new CastSpellExtraArgs(aurEff));
            else // If not set, simply remove Travel Form dummy
                player.RemoveAura(SpellIds.TravelForm);
        }

        private static SpellCastResult CheckLocationForForm(Player targetPlayer, Difficulty difficulty, bool requireOutdoors, uint spell_id)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, difficulty);

            if (requireOutdoors && !targetPlayer.IsOutdoors())
                return SpellCastResult.OnlyOutdoors;

            return spellInfo.CheckLocation(targetPlayer.GetMapId(), targetPlayer.GetZoneId(), targetPlayer.GetAreaId(), targetPlayer);
        }
    }

    [Script] // 783 - Travel Form (dummy)
    internal class spell_dru_travel_form_dummy : SpellScript, ISpellCheckCast
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.FormAquaticPassive, SpellIds.FormAquatic, SpellIds.FormStag);
        }

        public SpellCastResult CheckCast()
        {
            Player player = GetCaster().ToPlayer();

            if (!player)
                return SpellCastResult.CustomError;

            uint spellId = (player.HasSpell(SpellIds.FormAquaticPassive) && player.IsInWater()) ? SpellIds.FormAquatic : SpellIds.FormStag;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetCastDifficulty());

            return spellInfo.CheckLocation(player.GetMapId(), player.GetZoneId(), player.GetAreaId(), player);
        }
    }

    [Script]
    internal class spell_dru_travel_form_dummy_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormStag, SpellIds.FormAquatic, SpellIds.FormFlight, SpellIds.FormSwiftFlight);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetTarget().ToPlayer();

            // Outdoor check already passed - Travel Form (dummy) has SPELL_ATTR0_OUTDOORS_ONLY attribute.
            uint triggeredSpellId = spell_dru_travel_form_AuraScript.GetFormSpellId(player, GetCastDifficulty(), false);

            player.CastSpell(player, triggeredSpellId, new CastSpellExtraArgs(aurEff));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // No need to check remove mode, it's safe for Auras to remove each other in AfterRemove hook.
            GetTarget().RemoveAura(SpellIds.FormStag);
            GetTarget().RemoveAura(SpellIds.FormAquatic);
            GetTarget().RemoveAura(SpellIds.FormFlight);
            GetTarget().RemoveAura(SpellIds.FormSwiftFlight);
        }
    }

    [Script] // 252216 - Tiger Dash
    internal class spell_dru_tiger_dash : SpellScript, ISpellBeforeCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CatForm);
        }

        public void BeforeCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.CatForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.CatForm, true);
        }
    }

    [Script] // 252216 - Tiger Dash (Aura)
    internal class spell_dru_tiger_dash_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
        }

        private void HandlePeriodic(AuraEffect aurEff)
        {
            AuraEffect effRunSpeed = GetEffect(0);

            if (effRunSpeed != null)
            {
                int reduction = aurEff.GetAmount();
                effRunSpeed.ChangeAmount(effRunSpeed.GetAmount() - reduction);
            }
        }
    }

    [Script] // 48438 - Wild Growth
    internal class spell_dru_wild_growth : SpellScript, IHasSpellEffects
    {
        private List<WorldObject> _targets;
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            if (spellInfo.GetEffects().Count <= 2 ||
                spellInfo.GetEffect(2).IsEffect() ||
                spellInfo.GetEffect(2).CalcValue() <= 0)
                return false;

            return true;
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(SetTargets, 1, Targets.UnitDestAreaAlly));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
                              {
                                  Unit target = obj.ToUnit();

                                  if (target)
                                      return !GetCaster().IsInRaidWith(target);

                                  return true;
                              });

            int maxTargets = GetEffectInfo(2).CalcValue(GetCaster());

            if (targets.Count > maxTargets)
            {
                targets.Sort(new HealthPctOrderPred());
                targets.RemoveRange(maxTargets, targets.Count - maxTargets);
            }

            _targets = targets;
        }

        private void SetTargets(List<WorldObject> targets)
        {
            targets.Clear();
            targets.AddRange(_targets);
        }
    }

    [Script]
    internal class spell_dru_wild_growth_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RestorationT102PBonus);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectUpdatePeriodicHandler(HandleTickUpdate, 0, AuraType.PeriodicHeal));
        }

        private void HandleTickUpdate(AuraEffect aurEff)
        {
            Unit caster = GetCaster();

            if (!caster)
                return;

            // calculate from base Damage, not from aurEff.GetAmount() (already modified)
            float damage = caster.CalculateSpellDamage(GetUnitOwner(), aurEff.GetSpellEffectInfo());

            // Wild Growth = first tick gains a 6% bonus, reduced by 2% each tick
            float reduction = 2.0f;
            AuraEffect bonus = caster.GetAuraEffect(SpellIds.RestorationT102PBonus, 0);

            if (bonus != null)
                reduction -= MathFunctions.CalculatePct(reduction, bonus.GetAmount());

            reduction *= (aurEff.GetTickNumber() - 1);

            MathFunctions.AddPct(ref damage, 6.0f - reduction);
            aurEff.SetAmount((int)damage);
        }
    }
}