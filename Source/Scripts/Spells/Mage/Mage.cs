// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;

using Game.Scripting;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;
using Game.Spells;
using static Game.AI.SmartAction;
using static Game.Scripting.Interfaces.ISpell.EffectHandler;

namespace Scripts.Spells.Mage
{
    internal struct MageSpells
    {
        public const uint AlterTimeAura = 110909;
        public const uint AlterTimeVisual = 347402;
        public const uint ArcaneAlterTimeAura = 342246;
        public const uint ArcaneBarrageEnergize = 321529;
        public const uint ArcaneBarrageR3 = 321526;
        public const uint ArcaneCharge = 36032;
        public const uint ArcaneMage = 137021;
        public const uint BlazingBarrierTrigger = 235314;
        public const uint Blink = 1953;
        public const uint BlizzardDamage = 190357;
        public const uint BlizzardSlow = 12486;
        public const uint Cauterized = 87024;
        public const uint CauterizeDot = 87023;
        public const uint Chilled = 205708;
        public const uint CometStormDamage = 153596;
        public const uint CometStormVisual = 228601;
        public const uint ConeOfCold = 120;
        public const uint ConeOfColdSlow = 212792;
        public const uint ConjureRefreshment = 116136;
        public const uint ConjureRefreshmentTable = 167145;
        public const uint DradonhawkForm = 32818;
        public const uint EverwarmSocks = 320913;
        public const uint FingersOfFrost = 44544;
        public const uint FireBlast = 108853;
        public const uint Firestarter = 205026;
        public const uint FrostNova = 122;
        public const uint GiraffeForm = 32816;
        public const uint IceBarrier = 11426;
        public const uint IceBlock = 45438;
        public const uint Ignite = 12654;
        public const uint IncantersFlow = 116267;
        public const uint LivingBombExplosion = 44461;
        public const uint LivingBombPeriodic = 217694;
        public const uint ManaSurge = 37445;
        public const uint MasterOfTime = 342249;
        public const uint RayOfFrostBonus = 208141;
        public const uint RayOfFrostFingersOfFrost = 269748;
        public const uint Reverberate = 281482;
        public const uint RingOfFrostDummy = 91264;
        public const uint RingOfFrostFreeze = 82691;
        public const uint RingOfFrostSummon = 113724;
        public const uint SerpentForm = 32817;
        public const uint SheepForm = 32820;
        public const uint SquirrelForm = 32813;
        public const uint TemporalDisplacement = 80354;
        public const uint WorgenForm = 32819;
        public const uint IceLanceTrigger = 228598;
        public const uint ThermalVoid = 155149;
        public const uint IcyVeins = 12472;
        public const uint ChainReactionDummy = 278309;
        public const uint ChainReaction = 278310;
        public const uint TouchOfTheMagiExplode = 210833;

        //Misc
        public const uint HunterInsanity = 95809;
        public const uint ShamanExhaustion = 57723;
        public const uint ShamanSated = 57724;
        public const uint PetNetherwindsFatigued = 160455;
        public static uint SPELL_MAGE_COLD_SNAP = 235219;
        public static uint SPELL_MAGE_FROST_NOVA = 122;
        public static uint SPELL_MAGE_CONE_OF_COLD = 120;
        public static uint SPELL_MAGE_CONE_OF_COLD_SLOW = 212792;
        public static uint SPELL_MAGE_ICE_BARRIER = 11426;
        public static uint SPELL_MAGE_ICE_BLOCK = 45438;
        public static uint SPELL_MAGE_GLACIAL_INSULATION = 235297;
        public static uint SPELL_MAGE_BONE_CHILLING = 205027;
        public static uint SPELL_MAGE_BONE_CHILLING_BUFF = 205766;
        public static uint SPELL_MAGE_CHILLED = 205708;
        public static uint SPELL_MAGE_ICE_LANCE = 30455;
        public static uint SPELL_MAGE_ICE_LANCE_TRIGGER = 228598;
        public static uint SPELL_MAGE_THERMAL_VOID = 155149;
        public static uint SPELL_MAGE_ICY_VEINS = 12472;
        public static uint SPELL_MAGE_GLACIAL_SPIKE = 199786;
        public static uint SPELL_MAGE_ICICLE_PERIODIC_TRIGGER = 148023;
        public static uint SPELL_MAGE_FLURRY_DEBUFF_PROC = 228354;
        public static uint SPELL_MAGE_FLURRY = 44614;
        public static uint SPELL_MAGE_FLURRY_DAMAGE = 228672;
        public static uint SPELL_MAGE_FLURRY_CHILL_PROC = 228358;
        public static uint SPELL_MAGE_FLURRY_VISUAL = 228596;
        public static uint SPELL_MAGE_SHIELD_OF_ALODI = 195354;
        public static uint SPELL_MAGE_BRAIN_FREEZE = 190447;
        public static uint SPELL_MAGE_BRAIN_FREEZE_AURA = 190446;
        public static uint SPELL_MAGE_BRAIN_FREEZE_IMPROVED = 231584;
        public static uint SPELL_MAGE_EBONBOLT_DAMAGE = 257538;
        public static uint SPELL_MAGE_JOUSTER = 214626;
        public static uint SPELL_MAGE_CHAIN_REACTION = 195419;
        public static uint SPELL_MAGE_CHILLED_TO_THE_CORE = 195448;
        public static uint SPELL_MAGE_GLARITY_OF_THOUGHT = 195351;
        public static uint SPELL_MAGE_ICE_NOVA = 157997;
        public static uint SPELL_MAGE_FROZEN_TOUCH = 205030;
        public static uint SPELL_MAGE_FROZEN_ORB = 84714;
        public static uint SPELL_MAGE_FROZEN_ORB_DAMAGE = 84721;
        public static uint SPELL_MAGE_BLIZZARD_RANK_2 = 236662;
        public static uint SPELL_MAGE_UNSTABLE_MAGIC = 157976;
        public static uint SPELL_MAGE_UNSTABLE_MAGIC_DAMAGE_FIRE = 157977;
        public static uint SPELL_MAGE_UNSTABLE_MAGIC_DAMAGE_FROST = 157978;
        public static uint SPELL_MAGE_UNSTABLE_MAGIC_DAMAGE_ARCANE = 157979;
        public static uint SPELL_MAGE_FINGERS_OF_FROST = 112965;
        public static uint SPELL_MAGE_FINGERS_OF_FROST_AURA = 44544;
        public static uint SPELL_MAGE_FINGERS_OF_FROST_VISUAL_UI = 126084;
        public static uint SPELL_MAGE_FROST_BOMB_AURA = 112948;
        public static uint SPELL_MAGE_FROST_BOMB_TRIGGERED = 113092;
        public static uint SPELL_MAGE_FROSTBOLT = 116;
        public static uint SPELL_MAGE_FROSTBOLT_TRIGGER = 228597;
        public static uint SPELL_BLAZING_BARRIER_TRIGGER = 235314;
        public static uint SPELL_MAGE_SCORCH = 2948;
        public static uint SPELL_MAGE_FIREBALL = 133;
        public static uint SPELL_MAGE_FIRE_BLAST = 108853;
        public static uint SPELL_MAGE_FLAMESTRIKE = 2120;
        public static uint SPELL_MAGE_PYROBLAST = 11366;
        public static uint SPELL_MAGE_PHOENIX_FLAMES = 194466;
        public static uint SPELL_MAGE_DRAGON_BREATH = 31661;
        public static uint SPELL_MAGE_PYROMANIAC = 205020;
        public static uint SPELL_MAGE_ALEXSTRASZAS_FURY = 235870;
        public static uint SPELL_MAGE_LIVING_BOMB_DAMAGE = 44461;
        public static uint SPELL_MAGE_LIVING_BOMB_DOT = 217694;
        public static uint SPELL_MAGE_METEOR_DAMAGE = 153564;
        public static uint SPELL_MAGE_METEOR_TIMER = 177345;
        public static uint SPELL_MAGE_METEOR_VISUAL = 174556;
        public static uint SPELL_MAGE_METEOR_BURN = 155158;
        public static uint SPELL_MAGE_COMET_STORM = 153595;
        public static uint SPELL_MAGE_COMET_STORM_DAMAGE = 153596;
        public static uint SPELL_MAGE_COMET_STORM_VISUAL = 242210;
        public static uint SPELL_MAGE_POLYMORPH_CRITTERMORPH = 120091;
        public static uint SPELL_MAGE_HEATING_UP = 48107;
        public static uint SPELL_MAGE_HOT_STREAK = 48108;
        public static uint SPELL_MAGE_ENHANCED_PYROTECHNICS_AURA = 157644;
        public static uint SPELL_MAGE_INCANTERS_FLOW_BUFF = 116267;
        public static uint SPELL_MAGE_RUNE_OF_POWER_BUFF = 116014;
        public static uint SPELL_MAGE_OVERPOWERED = 155147;
        public static uint SPELL_MAGE_ARCANE_POWER = 12042;
        public static uint SPELL_MAGE_CHRONO_SHIFT = 235711;
        public static uint SPELL_MAGE_CHRONO_SHIFT_SLOW = 236299;
        public static uint SPELL_MAGE_CHRONO_SHIFT_BUFF = 236298;
        public static uint SPELL_MAGE_ARCANE_BLAST = 30451;
        public static uint SPELL_MAGE_ARCANE_BARRAGE = 44425;
        public static uint SPELL_MAGE_ARCANE_BARRAGE_TRIGGERED = 241241;
        public static uint SPELL_MAGE_PRESENCE_OF_MIND = 205025;
        public static uint SPELL_MAGE_ARCANE_MISSILES_VISUAL_TWO = 79808;
        public static uint SPELL_MAGE_ARCANE_MISSILES_VISUAL_ONE = 170571;
        public static uint SPELL_MAGE_ARCANE_MISSILES_VISUAL_THREE = 170572;
        public static uint SPELL_MAGE_ARCANE_MISSILES_TRIGGER = 7268;
        public static uint SPELL_MAGE_ARCANE_MISSILES = 5143;
        public static uint SPELL_MAGE_ARCANE_MISSILES_POWER = 208030;
        public static uint SPELL_MAGE_ARCANE_MISSILES_CHARGES = 79683;
        public static uint SPELL_MAGE_ARCANE_ORB_DAMAGE = 153640;
        public static uint SPELL_MAGE_ARCANE_AMPLIFICATION = 236628;
        public static uint SPELL_MAGE_RING_OF_FROST_FREEZE = 82691;
        public static uint SPELL_MAGE_RING_OF_FROST_IMMUNE = 91264;
        public static uint SPELL_MAGE_RING_OF_FROST = 113724;
        public static uint SPELL_MAGE_FIRE_MAGE_PASSIVE = 137019;
        public static uint SPELL_MAGE_FIRE_ON = 205029;
        public static uint SPELL_MAGE_FIRESTARTER = 205026;
        public static uint SPELL_MAGE_CAUTERIZE = 87023;
        public static uint SPELL_MAGE_MIRROR_IMAGE_LEFT = 58834;
        public static uint SPELL_MAGE_MIRROR_IMAGE_RIGHT = 58833;
        public static uint SPELL_MAGE_MIRROR_IMAGE_FRONT = 58831;
        public static uint SPELL_MAGE_COMBUSTION = 190319;
        public static uint SPELL_MAGE_WATER_JET = 135029;
        public static uint SPELL_MAGE_ICE_FLOES = 108839;
        public static uint SPELL_MAGE_CONJURE_REFRESHMENT_GROUP = 167145;
        public static uint SPELL_MAGE_CONJURE_REFRESHMENT_SOLO = 116136;
        public static uint SPELL_MAGE_HYPOTHERMIA = 41425;
        public static uint SPELL_INFERNO = 253220;
        public static uint SPELL_MAGE_BLAZING_BARRIER = 235313;
        public static uint SPELL_MAGE_BLAZING_SOUL = 235365;
        public static uint SPELL_MAGE_CONTROLLED_BURN = 205033;
        public static uint SPELL_MAGE_FLAME_PATCH = 205037;
        public static uint SPELL_MAGE_FLAME_PATCH_TRIGGER = 205470;
        public static uint SPELL_MAGE_FLAME_PATCH_AOE_DMG = 205472;
        public static uint SPELL_MAGE_CINDERSTORM = 198929;
        public static uint SPELL_MAGE_CINDERSTORM_DMG = 198928;
        public static uint SPELL_MAGE_IGNITE_DOT = 12654;
        public static uint SPELL_MAGE_REVERBERATE = 281482;
        public static uint SPELL_MAGE_RESONANCE = 205028;
        public static uint SPELL_MAGE_CLEARCASTING_BUFF = 277726;
        public static uint SPELL_MAGE_CLEARCASTING_EFFECT = 263725;
        public static uint SPELL_MAGE_CLEARCASTING_PVP_STACK_EFFECT = 276743;
        public static uint SPELL_MAGE_ARCANE_EMPOWERMENT = 276741;
        public static uint SPELL_MAGE_MANA_SHIELD_TALENT = 235463;
        public static uint SPELL_MAGE_MANA_SHIELD_BURN = 235470;
        public static uint SPELL_MAGE_RULE_OF_THREES = 264354;
        public static uint SPELL_MAGE_RULE_OF_THREES_BUFF = 264774;
        public static uint SPELL_MAGE_SPLITTING_ICE = 56377;
        public static uint SPELL_ARCANE_CHARGE = 36032;
        public static uint SPELL_MAGE_SQUIRREL_FORM = 32813;
        public static uint SPELL_MAGE_GIRAFFE_FORM = 32816;
        public static uint SPELL_MAGE_SERPENT_FORM = 32817;
        public static uint SPELL_MAGE_DRAGONHAWK_FORM = 32818;
        public static uint SPELL_MAGE_WORGEN_FORM = 32819;
        public static uint SPELL_MAGE_SHEEP_FORM = 32820;
        public static uint SPELL_MAGE_WILDFIRE = 288755;
        public static uint SPELL_BLASTER_MASTER = 274596;
        public static uint SPELL_BLASTER_MASTER_MASTERY_BUFF = 274598;
        public static uint SPELL_CAUTERIZING_BLINK_PROC = 280177;
        public static uint SPELL_IMPASSIVE_VISAGE_HEAL = 270117;
        public static uint SPELL_FIREMIND_TRIGGER = 278539;
        public static uint SPELL_FIREMIND_MOD_INTELECT = 279715;
        public static uint SPELL_PACKED_ICE_TRIGGER = 272968;
        public static uint SPELL_GLACIAL_ASSAULT_TRIGGER = 279854;
        public static uint SPELL_HEART_OF_DARKNESS_TRIGGER = 317137;
        public static uint SPELL_EQUIPOISE_TRIGGER = 286027;
        public static uint SPELL_EQUIPOISE_INCREASE_ARCANE_BLAST_DAMAGE = 264352;
        public static uint SPELL_EQUIPOISE_REDUCE_MANA_COST_ARCANE_BLAST = 264353;
        public static uint SPELL_FLASH_FREEZE_TRIGGER = 288164;
        public static uint SPELL_GUTRIPER_TRIGGER = 266937;
        public static uint SPELL_ARCANE_PUMMELING_TRIGGER = 270669;
        public static uint SPELL_ELEMENTAL_WHIRL_TRIGGER = 263984;
        public static uint SPELL_GALVANIZING_SPARK_TRIGGER = 278536;
        public static uint SPELL_OVERWHELMING_POWER_TRIGGER = 266180;
        public static uint SPELL_VAMPIRIC_SPEED_TRIGGER = 268599;
        public static uint SPELL_VAMPIRIC_SPEED_HEAL = 269238;
        public static uint SPELL_VAMPIRIC_SPEED_SPEED = 269239;
        public static uint SPELL_ELDRITCH_WARDING_TRIGGER = 274379;
        public static uint SPELL_BLOOD_SIPHON_TRIGGER = 264108;
        public static uint SPELL_ARCANE_PRESSURE_TRIGGER = 274594;
        public static uint SPELL_FLAMES_OF_ALACRITY_TRIGGER = 272932;
        public static uint SPELL_MAGE_PRISMATIC_CLOAK = 198064;
        public static uint SPELL_MAGE_PRISMATIC_CLOAK_BUFF = 198065;
        public static uint SPELL_MAGE_CHAIN_REACTION_BFA = 278309;
        public static uint SPELL_MAGE_CHAIN_REACTION_MOD_LANCE = 278310;
        public static uint SPELL_SNOWDRIFT = 389794;
        public static uint SPELL_FROZEN_IN_ICE = 71042;
        public static uint SPELL_ALTER_TIME = 108978;
        public static uint SPELL_MAGE_ICE_NINE = 214664;
        public static uint SPELL_MAGE_BLACK_ICE = 195615;
        public static uint SPELL_MAGE_ICICLE_DAMAGE = 148022;
        public static uint SPELL_MAGE_ICICLE_AURA = 205473;
        public static uint SPELL_MAGE_GLACIAL_SPIKE_PROC = 199844;
        public static uint SplittingIce = 56377;
        public static uint IciclesStack = 205473;
        public static uint IciclesDamage = 148022;
        public static uint MasteryIcicles = 76613;
    }

    public struct TemporalDisplacementSpells
    {
        public static uint SPELL_MAGE_TEMPORAL_DISPLACEMENT = 80354;
        public static uint SPELL_HUNTER_INSANITY = 95809;
        public static uint SPELL_PRIEST_SHADOW_WORD_DEATH = 32409;
        public static uint SPELL_SHAMAN_EXHAUSTION = 57723;
        public static uint SPELL_SHAMAN_SATED = 57724;
        public static uint SPELL_PET_NETHERWINDS_FATIGUED = 160455;
    }

    [Script]
    public class playerscript_mage_arcane : ScriptObjectAutoAdd, IPlayerOnAfterModifyPower
    {
        public playerscript_mage_arcane() : base("playerscript_mage_arcane")
        {
        }

        public void OnAfterModifyPower(Player player, PowerType power, int oldValue, int newValue, bool regen)
        {
            if (power != PowerType.ArcaneCharges)
            {
                return;
            }

            // Going up in charges is handled by aura 190427
            // Decreasing power seems weird clientside does not always match serverside power amount (client stays at 1, server is at 0)
            if (newValue != 0)
            {
                Aura arcaneCharge = player.GetAura(MageSpells.SPELL_ARCANE_CHARGE);
                if (arcaneCharge != null)
                {
                    arcaneCharge.SetStackAmount((byte)newValue);
                }
            }
            else
            {
                player.RemoveAurasDueToSpell(MageSpells.SPELL_ARCANE_CHARGE);
            }

            if (player.HasAura(MageSpells.SPELL_MAGE_RULE_OF_THREES))
            {
                if (newValue == 3 && oldValue == 2)
                {
                    player.CastSpell(player, MageSpells.SPELL_MAGE_RULE_OF_THREES_BUFF, true);
                }
            }
        }
    }

    // Chrono Shift - 235711
    [SpellScript(235711)]
    public class spell_mage_chrono_shift : AuraScript, IAuraCheckProc
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            bool _spellCanProc = (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_ARCANE_BARRAGE || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_ARCANE_BARRAGE_TRIGGERED);

            if (_spellCanProc)
            {
                return true;
            }
            return false;
        }


    }

    // Arcane Missiles - 5143
    [SpellScript(5143)]
    public class spell_mage_arcane_missiles : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            //@TODO: Remove when proc system can handle arcane missiles.....
            caster.RemoveAura(MageSpells.SPELL_MAGE_CLEARCASTING_BUFF);
            caster.RemoveAura(MageSpells.SPELL_MAGE_CLEARCASTING_EFFECT);
            Aura pvpClearcast = caster.GetAura(MageSpells.SPELL_MAGE_CLEARCASTING_PVP_STACK_EFFECT);
            if (pvpClearcast != null)
            {
                pvpClearcast.ModStackAmount(-1);
            }
            caster.RemoveAura(MageSpells.SPELL_MAGE_RULE_OF_THREES_BUFF);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    // Arcane Missiles Damage - 7268
    [SpellScript(7268)]
    public class spell_mage_arcane_missiles_damage : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void CheckTarget(ref WorldObject target)
        {
            if (target == GetCaster())
            {
                target = null;
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectTargetSelectHandler(CheckTarget, 0, Targets.UnitChannelTarget));
        }
    }

    // Clearcasting - 79684
    [SpellScript(79684)]
    public class spell_mage_clearcasting : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => throw new NotImplementedException();

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            int eff0 = GetSpellInfo().GetEffect(0).CalcValue();
            if (eff0 != 0)
            {
                int reqManaToSpent = 0;
                int manaUsed = 0;

                // For each ${$c*100/$s1} mana you spend, you have a 1% chance
                // Means: I cast a spell which costs 1000 Mana, for every 500 mana used I have 1% chance =  2% chance to proc
                foreach (SpellPowerCost powerCost in GetSpellInfo().CalcPowerCost(GetCaster(), GetSpellInfo().GetSchoolMask()))
                {
                    if (powerCost.Power == PowerType.Mana)
                    {
                        reqManaToSpent = powerCost.Amount * 100 / eff0;
                    }
                }

                // Something changed in DBC, Clearcasting should cost 1% of base mana 8.0.1
                if (reqManaToSpent == 0)
                {
                    return false;
                }

                foreach (SpellPowerCost powerCost in eventInfo.GetSpellInfo().CalcPowerCost(GetCaster(), eventInfo.GetSpellInfo().GetSchoolMask()))
                {
                    if (powerCost.Power == PowerType.Mana)
                    {
                        manaUsed = powerCost.Amount;
                    }
                }

                var chance = Math.Floor(manaUsed / reqManaToSpent * (double)1);
                return RandomHelper.randChance(chance);
            }

            return false;
        }

        private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
        {
            Unit actor = eventInfo.GetActor();
            actor.CastSpell(actor, MageSpells.SPELL_MAGE_CLEARCASTING_BUFF, true);
            if (actor.HasAura(MageSpells.SPELL_MAGE_ARCANE_EMPOWERMENT))
            {
                actor.CastSpell(actor, MageSpells.SPELL_MAGE_CLEARCASTING_PVP_STACK_EFFECT, true);
            }
            else
            {
                actor.CastSpell(actor, MageSpells.SPELL_MAGE_CLEARCASTING_EFFECT, true);
            }
        }

        public override void Register()
        {

            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    // Arcane Blast - 30451
    [SpellScript(30451)]
    public class spell_mage_arcane_blast : SpellScript, ISpellOnCast
    {


        public void OnCast()
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Aura threes = caster.GetAura(MageSpells.SPELL_MAGE_RULE_OF_THREES_BUFF);
                if (threes != null)
                {
                    threes.Remove();
                }
            }
        }


    }
    /*
    // Presence of mind - 205025
    public class spell_mage_presence_of_mind : AuraScript
    {


        private bool HandleProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_ARCANE_BLAST)
            {
                return true;
            }
            return false;
        }


    }

    public class CheckArcaneBarrageImpactPredicate
    {
        public CheckArcaneBarrageImpactPredicate(Unit caster, Unit mainTarget)
        {
            this._caster = caster;
            this._mainTarget = mainTarget;
        }

        public bool functorMethod(Unit target)
        {
            if (_caster == null || _mainTarget == null)
            {
                return true;
            }

            if (!_caster.IsValidAttackTarget(target))
            {
                return true;
            }

            if (!target.IsWithinLOSInMap(_caster))
            {
                return true;
            }

            if (!_caster.isInFront(target))
            {
                return true;
            }

            if (target.GetGUID() == _caster.GetGUID())
            {
                return true;
            }

            if (target.GetGUID() == _mainTarget.GetGUID())
            {
                return true;
            }

            return false;
        }

        private Unit _caster;
        private Unit _mainTarget;
    }

    // Fire Blast - 108853
    public class spell_mage_fire_blast : SpellScript
    {


        private void HandleHit(uint UnnamedParameter)
        {
            // this is already handled by Pyroblast Clearcasting Driver - 44448
            //bool procCheck = false;

            //if (Unit* caster = GetCaster())
            //{
            //    if (!caster->HasAura(SPELL_MAGE_HEATING_UP) && !caster->HasAura(SPELL_MAGE_HOT_STREAK))
            //    {
            //        caster->CastSpell(caster, SPELL_MAGE_HEATING_UP, true);
            //        procCheck = true;
            //    }


            //    if (caster->HasAura(SPELL_MAGE_HEATING_UP) && !caster->HasAura(SPELL_MAGE_HOT_STREAK) && !procCheck)
            //    {
            //        caster->RemoveAurasDueToSpell(SPELL_MAGE_HEATING_UP);
            //        caster->CastSpell(caster, SPELL_MAGE_HOT_STREAK, true);
            //    }
            //}
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    // Enhanced Pyrotechnics - 157642
    public class spell_mage_enhanced_pyrotechnics : AuraScript
    {


        private bool HandleProc(ProcEventInfo eventInfo)
        {
            Unit caster = GetCaster();

            if (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL)
            {
                if ((eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0)
                {
                    if (caster.HasAura(MageSpells.SPELL_MAGE_ENHANCED_PYROTECHNICS_AURA))
                    {
                        caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_ENHANCED_PYROTECHNICS_AURA);
                    }
                    return false;
                }
                return true;
            }
            return false;
        }


    }

    public class auraData
    {
        public auraData(uint id, int duration)
        {
            this.m_id = id;
            this.m_duration = duration;
        }
        public uint m_id;
        public int m_duration;
    }

    // Meteor - 153561
    public class spell_mage_meteor : SpellScript
    {


        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(MageSpells.SPELL_MAGE_METEOR_DAMAGE);
        }

        private void HandleDummy()
        {
            Unit caster = GetCaster();
            WorldLocation dest = GetExplTargetDest();
            if (caster == null || dest == null)
            {
                return;
            }

            caster.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), MageSpells.SPELL_MAGE_METEOR_TIMER, true);
        }

        public override void Register()
        {
            AfterCast += SpellCastFn(this.HandleDummy);
        }
    }

    // Meteor Damage - 153564
    public class spell_mage_meteor_damage : SpellScript
    {


        private int _targets;

        private void HandleHit(uint UnnamedParameter)
        {
            Unit unit = GetHitUnit();
            if (unit == null)
            {
                return;
            }

            SetHitDamage(GetHitDamage() / _targets);
        }

        private void CountTargets(List<WorldObject> targets)
        {
            _targets = targets.Count;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, TARGET_UNIT_DEST_AREA_ENEMY));
        }
    }

    // Frenetic Speed - 236058
    public class spell_mage_frenetic_speed : AuraScript
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_SCORCH;
        }


    }

    // Conflagration - 205023
    public class spell_mage_conflagration : AuraScript
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() && eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL;
        }


    }

    // Pyroblast 11366
    public class spell_mage_pyroblast : SpellScript
    {


        private void HandleOnHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (caster.HasAura(MageSpells.SPELL_MAGE_HOT_STREAK))
            {
                caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HOT_STREAK);

                if (caster.HasAura(MageSpells.SPELL_MAGE_PYROMANIAC))
                {
                    AuraEffect pyromaniacEff0 = caster.GetAuraEffect(MageSpells.SPELL_MAGE_PYROMANIAC, 0);
                    if (pyromaniacEff0 != null)
                    {
                        if (RandomHelper.randChance(pyromaniacEff0.GetAmount()))
                        {
                            if (caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP))
                            {
                                caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HEATING_UP);
                            }

                            caster.CastSpell(caster, MageSpells.SPELL_MAGE_HOT_STREAK, true);
                        }
                    }
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
        }
    }

    // Flamestrike 2120
    public class spell_mage_flamestrike : SpellScript
    {


        private void HandleOnHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (caster.HasAura(MageSpells.SPELL_MAGE_HOT_STREAK))
            {
                caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HOT_STREAK);

                if (caster.HasAura(MageSpells.SPELL_MAGE_PYROMANIAC))
                {
                    AuraEffect pyromaniacEff0 = caster.GetAuraEffect(MageSpells.SPELL_MAGE_PYROMANIAC, 0);
                    if (pyromaniacEff0 != null)
                    {
                        if (RandomHelper.randChance(pyromaniacEff0.GetAmount()))
                        {
                            if (caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP))
                            {
                                caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HEATING_UP);
                            }

                            caster.CastSpell(caster, MageSpells.SPELL_MAGE_HOT_STREAK, true);
                        }
                    }
                }
            }
        }

        private void HandleDummy()
        {
            Unit caster = GetCaster();
            WorldLocation dest = GetExplTargetDest();
            if (caster == null || dest == null)
            {
                return;
            }

            if (caster.HasAura(MageSpells.SPELL_MAGE_FLAME_PATCH))
            {
                WorldLocation dest = GetExplTargetDest();
                if (dest != null)
                {
                    caster.CastSpell(dest.GetPosition(), MageSpells.SPELL_MAGE_FLAME_PATCH_TRIGGER, true);
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
            AfterCast += SpellCastFn(this.HandleDummy);
        }
    }

    // Kindling - 155148
    public class spell_mage_kindling : AuraScript
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIRE_BLAST || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_PHOENIX_FLAMES;
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            caster.GetSpellHistory().ModifyCooldown(MageSpells.SPELL_MAGE_COMBUSTION, -Seconds(aurEff.GetAmount()));
        }

        public override void Register()
        {

            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    // Pyroblast Clearcasting Driver - 44448
    public class spell_mage_pyroblast_clearcasting_driver : AuraScript
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit caster = GetCaster();

            bool _spellCanProc = (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_SCORCH || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIRE_BLAST || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FLAMESTRIKE || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_PYROBLAST || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_PHOENIX_FLAMES || (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_DRAGON_BREATH && caster.HasAura(MageSpells.SPELL_MAGE_ALEXSTRASZAS_FURY)));

            if (_spellCanProc)
            {
                return true;
            }
            return false;
        }

        private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
        {
            bool procCheck = false;

            Unit caster = GetCaster();

            if ((eventInfo.GetHitMask() & PROC_HIT_NORMAL) != 0)
            {
                if (caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP))
                {
                    caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HEATING_UP);
                }
                return;
            }

            if (!caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP) && !caster.HasAura(MageSpells.SPELL_MAGE_HOT_STREAK))
            {
                caster.CastSpell(caster, MageSpells.SPELL_MAGE_HEATING_UP, true);

                procCheck = true;

                AuraEffect burn = caster.GetAuraEffect(MageSpells.SPELL_MAGE_CONTROLLED_BURN, 0);
                if (burn != null)
                {
                    if (RandomHelper.randChance(burn.GetAmount()))
                    {
                        procCheck = false;
                    }
                }
            }


            if (caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP) && !caster.HasAura(MageSpells.SPELL_MAGE_HOT_STREAK) && !procCheck)
            {
                caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HEATING_UP);
                caster.CastSpell(caster, MageSpells.SPELL_MAGE_HOT_STREAK, true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));

        }
    }

    // Chilled - 205708
    public class spell_mage_chilled : AuraScript
    {


        private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (caster.HasAura(MageSpells.SPELL_MAGE_BONE_CHILLING))
            {
                //@TODO REDUCE BONE CHILLING DAMAGE PER STACK TO 0.5% from 1%
                caster.CastSpell(caster, MageSpells.SPELL_MAGE_BONE_CHILLING_BUFF, true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleApply, 0, SPELL_AURA_MOD_DECREASE_SPEED, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    // Flurry - 44614
    public class spell_mage_flurry : SpellScript
    {


        private void HandleDummy(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            bool isImproved = false;
            if (caster == null || target == null)
            {
                return;
            }

            if (caster.HasAura(MageSpells.SPELL_MAGE_BRAIN_FREEZE_AURA))
            {
                caster.RemoveAura(MageSpells.SPELL_MAGE_BRAIN_FREEZE_AURA);
                if (caster.HasSpell(MageSpells.SPELL_MAGE_BRAIN_FREEZE_IMPROVED))
                {
                    isImproved = true;
                }
            }

            ObjectGuid targetGuid = target.GetGUID();
            if (targetGuid != ObjectGuid.Empty)
            {
                for (byte i = 1; i < 3; ++i) // basepoint value is 3 all the time, so, set it 3 because sometimes it won't read
                {


                    caster.GetScheduler().Schedule(TimeSpan.FromMilliseconds(i * 250), (TaskContext context) =>
                    {
                        Unit caster = GetContextUnit();
                        if (caster != null)
                        {
                            Unit target = ObjectAccessor.Instance.GetUnit(caster, targetGuid;
                            if (target != null)
                            {
                                caster.CastSpell(target, MageSpells.SPELL_MAGE_FLURRY_VISUAL, false);
                                if (isImproved)
                                {
                                    caster.CastSpell(target, MageSpells.SPELL_MAGE_FLURRY_CHILL_PROC, false);
                                }
                            }
                        }
                    });
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

    // Jouster - 214626
    public class spell_mage_jouster : AuraScript
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_ICE_LANCE;
        }


    }

    // Jouster Buff - 195391
    public class spell_mage_jouster_buff : AuraScript
    {


        private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            AuraEffect jousterRank = caster.GetAuraEffect(MageSpells.SPELL_MAGE_JOUSTER, 0);
            if (jousterRank != null)
            {
                amount = jousterRank.GetAmount();
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, SPELL_AURA_MOD_DAMAGE_PERCENT_TAKEN));
        }
    }

    // Chain Reaction - 195419
    public class spell_mage_chain_reaction : AuraScript
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FROSTBOLT || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FROSTBOLT_TRIGGER;
        }


    }

    // Chilled to the Core - 195448
    public class spell_mage_chilled_to_the_core : AuraScript
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_ICY_VEINS;
        }


    }

    // Combustion - 190319
    public class spell_mage_combustion : SpellScriptLoader
    {
        public spell_mage_combustion() : base("spell_mage_combustion")
        {
        }

        public class spell_mage_combustion : AuraScript
        {


            private void CalcAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
            {
                Unit caster = GetCaster();
                if (caster == null)
                {
                    return;
                }

                if (!caster.IsPlayer())
                {
                    return;
                }

                int crit = caster.ToPlayer().GetRatingBonusValue(CR_CRIT_SPELL);
                amount += crit;
            }

            private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
            {
                GetCaster().RemoveAurasDueToSpell(MageSpells.SPELL_INFERNO);
            }

            public override void Register()
            {
                AuraEffects.Add(new EffectCalcAmountHandler(CalcAmount, 1, SPELL_AURA_MOD_RATING));
                AuraEffects.Add(new EffectApplyHandler(HandleRemove, 1, SPELL_AURA_MOD_RATING, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            }
        }



        public override AuraScript GetAuraScript()
        {
            return new spell_mage_combustion();
        }
    }

    // Fire mage (passive) - 137019
    public class spell_mage_fire_mage_passive : SpellScriptLoader
    {
        public spell_mage_fire_mage_passive() : base("spell_mage_fire_mage_passive")
        {
        }

        public class spell_mage_fire_mage_passive : AuraScript
        {


            public override bool Validate(SpellInfo UnnamedParameter)
            {
                // if (!Global.SpellMgr->GetSpellInfo(SPELL_MAGE_FIRE_MAGE_PASSIVE, Difficulty.None) ||
                //    !Global.SpellMgr->GetSpellInfo(SPELL_MAGE_FIRE_BLAST, Difficulty.None))
                //  return false;
                return true;
            }


            public spell_mage_fire_mage_passive()
            {
            }


            private SpellModifier mod = null;

            private void HandleApply(AuraEffect aurEffect, AuraEffectHandleModes UnnamedParameter)
            {
                Player player = GetCaster().ToPlayer();
                if (player == null)
                {
                    return;
                }

                SpellModifierByClassMask mod = new SpellModifierByClassMask(aurEffect.GetBase());
                mod.op = SpellModOp.CritChance;
                mod.type = SpellModType.Flat;
                mod.spellId = MageSpells.SPELL_MAGE_FIRE_MAGE_PASSIVE;
                mod.value = 200;
                mod.mask[0] = 0x2;

                player.AddSpellMod(mod, true);
            }

            private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
            {
                Player player = GetCaster().ToPlayer();
                if (player == null)
                {
                    return;
                }

                if (mod != null)
                {
                    player.AddSpellMod(mod, false);
                }
            }

            public override void Register()
            {
                AuraEffects.Add(new EffectApplyHandler(HandleApply, 4, AuraType.Dummy, AuraEffectHandleModes.Real));
                AuraEffects.Add(new EffectApplyHandler(HandleRemove, 4, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            }
        }



        public override AuraScript GetAuraScript()
        {
            return new spell_mage_fire_mage_passive();
        }
    }

    // Flame On - 205029
    public class spell_mage_fire_on : SpellScriptLoader
    {
        public spell_mage_fire_on() : base("spell_mage_fire_on")
        {
        }

        public class spell_mage_fire_on : SpellScript
        {


            public override bool Validate(SpellInfo UnnamedParameter)
            {
                return ValidateSpellInfo(MageSpells.SPELL_MAGE_FIRE_ON, MageSpells.SPELL_MAGE_FIRE_BLAST);
            }

            private void HandleDummy(uint UnnamedParameter)
            {
                Unit caster = GetCaster();
                Unit target = GetHitUnit();
                if (caster == null || target == null || caster.GetTypeId() != TypeId.Player)
                {
                    return;
                }

                // caster->ToPlayer()->GetSpellHistory()->ResetCharges(Global.SpellMgr->GetSpellInfo(SPELL_MAGE_FIRE_BLAST, Difficulty.None)->ChargeCategoryId);
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleDummy, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            }
        }



        public override SpellScript GetSpellScript()
        {
            return new spell_mage_fire_on();
        }
    }

    // Mirror Image - 55342
    public class spell_mage_mirror_image_summon : SpellScriptLoader
    {
        public spell_mage_mirror_image_summon() : base("spell_mage_mirror_image_summon")
        {
        }

        public class spell_mage_mirror_image_summon : SpellScript
        {


            private void HandleDummy(uint UnnamedParameter)
            {
                Unit caster = GetCaster();
                if (caster != null)
                {
                    caster.CastSpell(caster, MageSpells.SPELL_MAGE_MIRROR_IMAGE_LEFT, true);
                    caster.CastSpell(caster, MageSpells.SPELL_MAGE_MIRROR_IMAGE_FRONT, true);
                    caster.CastSpell(caster, MageSpells.SPELL_MAGE_MIRROR_IMAGE_RIGHT, true);
                }
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            }
        }



        public override SpellScript GetSpellScript()
        {
            return new spell_mage_mirror_image_summon();
        }
    }

    // Meteor - 177345
    // AreaTriggerID - 3467
    public class at_mage_meteor_timer : AreaTriggerEntityScript
    {
        public at_mage_meteor_timer() : base("at_mage_meteor_timer")
        {
        }

        public class at_mage_meteor_timerAI : AreaTriggerAI
        {
            public at_mage_meteor_timerAI(AreaTrigger areatrigger) : base(areatrigger)
            {
            }

            public override void OnCreate()
            {
                Unit caster = at.GetCaster();
                if (caster == null)
                {
                    return;
                }

                if (TempSummon * tempSumm = caster.SummonCreature(WORLD_TRIGGER, at.GetPosition(), TEMPSUMMON_TIMED_DESPAWN, 5s))
                {
                    tempSumm.SetFaction(caster.GetFaction());
                    tempSumm.SetSummonerGUID(caster.GetGUID());
                    PhasingHandler.InheritPhaseShift(tempSumm, caster);
                    caster.CastSpell(tempSumm, MageSpells.SPELL_MAGE_METEOR_VISUAL, true);
                }

            }

            public override void OnRemove()
            {
                Unit caster = at.GetCaster();
                if (caster == null)
                {
                    return;
                }

                if (TempSummon * tempSumm = caster.SummonCreature(WORLD_TRIGGER, at.GetPosition(), TEMPSUMMON_TIMED_DESPAWN, 5s))
                {
                    tempSumm.SetFaction(caster.GetFaction());
                    tempSumm.SetSummonerGUID(caster.GetGUID());
                    PhasingHandler.InheritPhaseShift(tempSumm, caster);
                    caster.CastSpell(tempSumm, MageSpells.SPELL_MAGE_METEOR_DAMAGE, true);
                }
            }
        }



        public override AreaTriggerAI GetAI(AreaTrigger areatrigger)
        {
            return new at_mage_meteor_timerAI(areatrigger);
        }
    }

    // Meteor Burn - 175396
    // AreaTriggerID - 1712
    public class at_mage_meteor_burn : AreaTriggerEntityScript
    {
        public at_mage_meteor_burn() : base("at_mage_meteor_burn")
        {
        }

        public class at_mage_meteor_burnAI : AreaTriggerAI
        {
            public at_mage_meteor_burnAI(AreaTrigger areatrigger) : base(areatrigger)
            {
            }

            public override void OnUnitEnter(Unit unit)
            {
                Unit caster = at.GetCaster();

                if (caster == null || unit == null)
                {
                    return;
                }

                if (caster.GetTypeId() != TypeId.Player)
                {
                    return;
                }

                if (caster.IsValidAttackTarget(unit))
                {
                    caster.CastSpell(unit, MageSpells.SPELL_MAGE_METEOR_BURN, true);
                }
            }

            public override void OnUnitExit(Unit unit)
            {
                Unit caster = at.GetCaster();

                if (caster == null || unit == null)
                {
                    return;
                }

                if (caster.GetTypeId() != TypeId.Player)
                {
                    return;
                }

                Aura meteor = unit.GetAura(MageSpells.SPELL_MAGE_METEOR_BURN, caster.GetGUID());
                if (meteor != null)
                {
                    meteor.SetDuration(0);
                }
            }
        }



        public override AreaTriggerAI GetAI(AreaTrigger areatrigger)
        {
            return new at_mage_meteor_burnAI(areatrigger);
        }
    }

    // Blizzard - 190356
    // AreaTriggerID - 4658
    public class at_mage_blizzard : AreaTriggerEntityScript
    {
        public at_mage_blizzard() : base("at_mage_blizzard")
        {
        }

        public class at_mage_blizzardAI : AreaTriggerAI
        {
            public at_mage_blizzardAI(AreaTrigger areatrigger) : base(areatrigger)
            {
                timeInterval = 1000;
            }

            public int timeInterval;

            public enum UsingSpells
            {
                SPELL_MAGE_BLIZZARD_DAMAGE = 190357
            }

            public override void OnCreate()
            {
                at.SetDuration(8000);
            }

            public override void OnUpdate(uint diff)
            {
                Unit caster = at.GetCaster();

                if (caster == null)
                {
                    return;
                }

                if (!caster.IsPlayer())
                {
                    return;
                }

                timeInterval += (int)diff;
                if (timeInterval < 1000)
                {
                    return;
                }

                if (TempSummon * tempSumm = caster.SummonCreature(WORLD_TRIGGER, at.GetPosition(), TEMPSUMMON_TIMED_DESPAWN, 8100ms))
                {
                    tempSumm.SetFaction(caster.GetFaction());
                    tempSumm.SetSummonerGUID(caster.GetGUID());
                    PhasingHandler.InheritPhaseShift(tempSumm, caster);
                    caster.CastSpell(tempSumm, UsingSpells.SPELL_MAGE_BLIZZARD_DAMAGE, true);
                }

                timeInterval -= 1000;
            }
        }



        public override AreaTriggerAI GetAI(AreaTrigger areatrigger)
        {
            return new at_mage_blizzardAI(areatrigger);
        }
    }

    // Rune of Power - 116011
    // AreaTriggerID - 304
    public class at_mage_rune_of_power : AreaTriggerAI
    {
        public at_mage_rune_of_power(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

        public enum UsingSpells
        {
            SPELL_MAGE_RUNE_OF_POWER_AURA = 116014
        }

        public override void OnCreate()
        {
            //at->SetSpellXSpellVisualId(25943);
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                if (unit.GetGUID() == caster.GetGUID())
                {
                    caster.CastSpell(unit, UsingSpells.SPELL_MAGE_RUNE_OF_POWER_AURA, true);
                }
            }
        }

        public override void OnUnitExit(Unit unit)
        {
            if (unit.HasAura(UsingSpells.SPELL_MAGE_RUNE_OF_POWER_AURA))
            {
                unit.RemoveAurasDueToSpell(UsingSpells.SPELL_MAGE_RUNE_OF_POWER_AURA);
            }
        }
    }

    // Frozen Orb - 84714
    // AreaTriggerID - 8661
    public class at_mage_frozen_orb : AreaTriggerAI
    {
        public at_mage_frozen_orb(AreaTrigger areatrigger) : base(areatrigger)
        {
            damageInterval = 500;
        }

        public uint damageInterval;
        public bool procDone = false;

        public override void OnInitialize()
        {
            Unit caster = at.GetCaster();
            if (caster == null)
            {
                return;
            }

            Position pos = caster.GetPosition();

            at.MovePositionToFirstCollision(pos, 40.0f, 0.0f);
            at.SetDestination(pos, 4000);
        }

        public override void OnCreate()
        {
            //at->SetSpellXSpellVisualId(40291);
        }

        public override void OnUpdate(uint diff)
        {
            Unit caster = at.GetCaster();
            if (caster == null || !caster.IsPlayer())
            {
                return;
            }

            if (damageInterval <= diff)
            {
                if (!procDone)
                {
                    foreach (ObjectGuid guid in at.GetInsideUnits())
                    {
                        Unit unit = ObjectAccessor.Instance.GetUnit(caster, guid;
                        if (unit != null)
                        {
                            if (caster.IsValidAttackTarget(unit))
                            {
                                if (caster.HasAura(MageSpells.SPELL_MAGE_FINGERS_OF_FROST_AURA))
                                {
                                    caster.CastSpell(caster, MageSpells.SPELL_MAGE_FINGERS_OF_FROST_VISUAL_UI, true);
                                }

                                caster.CastSpell(caster, MageSpells.SPELL_MAGE_FINGERS_OF_FROST_AURA, true);

                                // at->UpdateTimeToTarget(8000); TODO
                                procDone = true;
                                break;
                            }
                        }
                    }
                }

                caster.CastSpell(at.GetPosition(), MageSpells.SPELL_MAGE_FROZEN_ORB_DAMAGE, true);
                damageInterval = 500;
            }
            else
            {
                damageInterval -= diff;
            }
        }
    }

    // Arcane Orb - 153626
    // AreaTriggerID - 1612
    public class at_mage_arcane_orb : AreaTriggerAI
    {
        public at_mage_arcane_orb(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                if (caster.IsValidAttackTarget(unit))
                {
                    caster.CastSpell(unit, MageSpells.SPELL_MAGE_ARCANE_ORB_DAMAGE, true);
                }
            }
        }
    }

    // 31216 - Mirror Image
    public class npc_mirror_image : CreatureScript
    {
        public npc_mirror_image() : base("npc_mirror_image")
        {
        }

        public enum eSpells
        {
            SPELL_MAGE_FROSTBOLT = 59638,
            SPELL_MAGE_FIREBALL = 133,
            SPELL_MAGE_ARCANE_BLAST = 30451,
            SPELL_MAGE_GLYPH = 63093,
            SPELL_INITIALIZE_IMAGES = 102284,
            SPELL_CLONE_CASTER = 60352,
            SPELL_INHERIT_MASTER_THREAT = 58838
        }

        public class npc_mirror_imageAI : CasterAI
        {
            public npc_mirror_imageAI(Creature creature) : base(creature)
            {
            }

            public override void IsSummonedBy(WorldObject owner)
            {
                if (owner == null || !owner.IsPlayer())
                {
                    return;
                }

                if (!me.HasUnitState(UnitState.UNIT_STATE_FOLLOW))
                {
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveFollow(owner.ToUnit(), PET_FOLLOW_DIST, me.GetFollowAngle(), MovementSlot.MOTION_SLOT_ACTIVE);
                }

                // me->SetMaxPower(me->GetPowerType(), owner->GetMaxPower(me->GetPowerType()));
                me.SetFullPower(me.GetPowerType());
                me.SetMaxHealth(owner.ToUnit().GetMaxHealth());
                me.SetHealth(owner.ToUnit().GetHealth());
                me.SetReactState(ReactStates.REACT_DEFENSIVE);

                me.CastSpell(owner, SPELL_INHERIT_MASTER_THREAT, true);

                // here mirror image casts on summoner spell (not present in client dbc) 49866
                // here should be auras (not present in client dbc): 35657, 35658, 35659, 35660 selfcasted by mirror images (stats related?)

                for (uint attackType = 0; attackType < WeaponAttackType.MAX_ATTACK; ++attackType)
                {
                    WeaponAttackType attackTypeEnum = (WeaponAttackType)attackType;
                    me.SetBaseWeaponDamage(attackTypeEnum, WeaponDamageRange.WeaponDamageRange.MaxDamage, owner.ToUnit().GetWeaponDamageRange(attackTypeEnum, WeaponDamageRange.WeaponDamageRange.MaxDamage));
                    me.SetBaseWeaponDamage(attackTypeEnum, WeaponDamageRange.WeaponDamageRange.MinDamage, owner.ToUnit().GetWeaponDamageRange(attackTypeEnum, WeaponDamageRange.WeaponDamageRange.MinDamage));
                }

                me.UpdateAttackPowerAndDamage();
            }

            public override void JustEngagedWith(Unit who)
            {
                Unit owner = me.GetOwner();
                if (owner == null)
                {
                    return;
                }

                Player ownerPlayer = owner.ToPlayer();
                if (ownerPlayer == null)
                {
                    return;
                }

                eSpells spellId = eSpells.SPELL_MAGE_FROSTBOLT;
                switch (ownerPlayer.GetPrimarySpecialization())
                {
                    case TALENT_SPEC_MAGE_ARCANE:
                        spellId = eSpells.SPELL_MAGE_ARCANE_BLAST;
                        break;
                    case TALENT_SPEC_MAGE_FIRE:
                        spellId = (npc_mirror_image.eSpells)eSpells.SPELL_MAGE_FIREBALL;
                        break;
                    default:
                        break;
                }

                _Events.ScheduleEvent(spellId, 0s); ///< Schedule cast
                me.GetMotionMaster().Clear();
            }

            public override void EnterEvadeMode(EvadeReason UnnamedParameter)
            {
                if (me.IsInEvadeMode() || !me.IsAlive())
                {
                    return;
                }

                Unit owner = me.GetOwner();

                me.CombatStop(true);
                if (owner != null && !me.HasUnitState(UNIT_STATE_FOLLOW))
                {
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveFollow(owner.ToUnit(), PET_FOLLOW_DIST, me.GetFollowAngle(), MovementSlot.MOTION_SLOT_ACTIVE);
                }
            }

            public override void Reset()
            {
                Unit owner = me.GetOwner();
                if (owner != null)
                {
                    owner.CastSpell(me, SPELL_INITIALIZE_IMAGES, true);
                    owner.CastSpell(me, SPELL_CLONE_CASTER, true);
                }
            }



            public override bool CanAIAttack(Unit target)
            {
                /// Am I supposed to attack this target? (ie. do not attack polymorphed target)
                return target != null && !target.HasBreakableByDamageCrowdControlAura();
            }

            public override void UpdateAI(uint diff)
            {
                _Events.Update(diff);

                Unit l_Victim = me.GetVictim();
                if (l_Victim != null)
                {
                    if (CanAIAttack(l_Victim))
                    {
                        /// If not already casting, cast! ("I'm a cast machine")
                        if (!me.HasUnitState(UnitState.Casting))
                        {
                            uint spellId = _Events.ExecuteEvent();
                            if (_Events.ExecuteEvent())
                            {
                                DoCast(spellId);
                                uint castTime = me.GetCurrentSpellCastTime(spellId);
                                _Events.ScheduleEvent(spellId, (5s), Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None).ProcCooldown);
                            }
                        }
                    }
                    else
                    {
                        /// My victim has changed state, I shouldn't attack it anymore
                        if (me.HasUnitState(UnitState.Casting))
                        {
                            me.CastStop();
                        }

                        me.GetAI().EnterEvadeMode();
                    }
                }
                else
                {
                    /// Let's choose a new target
                    Unit target = me.SelectVictim();
                    if (target == null)
                    {
                        /// No target? Let's see if our owner has a better target for us
                        Unit owner = me.GetOwner();
                        if (owner != null)
                        {
                            Unit ownerVictim = owner.GetVictim();
                            if (ownerVictim != null && me.CanCreatureAttack(ownerVictim))
                            {
                                target = ownerVictim;
                            }
                        }
                    }

                    if (target != null)
                    {
                        me.GetAI().AttackStart(target);
                    }
                }
            }
        }

        private EventMap _events = new EventMap();



        private override CreatureAI GetAI(Creature creature)
        {
            return new npc_mirror_imageAI(creature);
        }
    }

    // Flame Patch
    // AreaTriggerID - 6122
    public class at_mage_flame_patch : AreaTriggerAI
    {
        public at_mage_flame_patch(AreaTrigger areatrigger) : base(areatrigger)
        {
        }


        public override void OnCreate()
        {
            timeInterval = 1000;
        }

        public int timeInterval;

        public override void OnUpdate(uint diff)
        {
            Unit caster = at.GetCaster();

            if (caster == null)
            {
                return;
            }

            if (caster.GetTypeId() != TypeId.Player)
            {
                return;
            }

            timeInterval += (int)diff;
            if (timeInterval < 1000)
            {
                return;
            }

            caster.CastSpell(at.GetPosition(), MageSpells.SPELL_MAGE_FLAME_PATCH_AOE_DMG, true);

            timeInterval -= 1000;
        }
    }

    // Cinderstorm - 198929
    // AreaTriggerID - 5487
    public class at_mage_cinderstorm : AreaTriggerAI
    {
        public at_mage_cinderstorm(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                if (caster.IsValidAttackTarget(unit))
                {
                    caster.CastSpell(unit, MageSpells.SPELL_MAGE_CINDERSTORM_DMG, true);
                }
            }
        }
    }

    // Cinderstorm - 198928
    public class spell_mage_cinderstorm : SpellScript
    {


        private void HandleDamage(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            if (target.HasAura(MageSpells.SPELL_MAGE_IGNITE_DOT))
            {
                //    int32 pct = Global.SpellMgr->GetSpellInfo(SPELL_MAGE_CINDERSTORM, Difficulty.None)->GetEffect(0).CalcValue(caster);
                int dmg = GetHitDamage();
                // MathFunctions.AddPct(ref dmg, pct);
                SetHitDamage(dmg);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    // 257537 - Ebonbolt
    public class spell_mage_ebonbolt : SpellScript
    {


        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(MageSpells.SPELL_MAGE_SPLITTING_ICE, MageSpells.SPELL_MAGE_EBONBOLT_DAMAGE, MageSpells.SPELL_MAGE_BRAIN_FREEZE_AURA);
        }

        private void DoCast()
        {
            GetCaster().CastSpell(GetCaster(), MageSpells.SPELL_MAGE_BRAIN_FREEZE_AURA, true);
        }

        private void DoEffectHitTarget(uint UnnamedParameter)
        {
            Unit explTarget = GetExplTargetUnit();
            Unit hitUnit = GetHitUnit();
            if (hitUnit == null || explTarget == null)
            {
                return;
            }

            if (GetCaster().HasAura(MageSpells.SPELL_MAGE_SPLITTING_ICE))
            {
                GetCaster().VariableStorage.Set<ObjectGuid>("explTarget", explTarget.GetGUID());
            }
            GetCaster().CastSpell(hitUnit, MageSpells.SPELL_MAGE_EBONBOLT_DAMAGE, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(DoEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));

        }
    }

    // 257538 - Ebonbolt Damage
    public class spell_mage_ebonbolt_damage : SpellScript
    {


        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(MageSpells.SPELL_MAGE_SPLITTING_ICE);
        }

        private void DoEffectHitTarget(uint UnnamedParameter)
        {
            Unit hitUnit = GetHitUnit();
            ObjectGuid primaryTarget = GetCaster().VariableStorage.GetValue<ObjectGuid>("explTarget");
            int damage = GetHitDamage();
            if (hitUnit == null || primaryTarget == null)
            {
                return;
            }

            // if (int32 eff1 = Global.SpellMgr->GetSpellInfo(SPELL_MAGE_SPLITTING_ICE, Difficulty.None)->GetEffect(1).CalcValue())
            //  if (hitUnit->GetGUID() != primaryTarget)
            //    SetHitDamage(MathFunctions.CalculatePct(damage, eff1));
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(DoEffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    // Firestarter - 203283
    public class spell_mage_firestarter_pvp : AuraScript
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FIREBALL;
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            caster.GetSpellHistory().ModifyCooldown(MageSpells.SPELL_MAGE_COMBUSTION, -Seconds(-aurEff.GetAmount() - 5000));
        }

        public override void Register()
        {

            AuraEffects.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    //1953
    public class spell_mage_blink : SpellScript
    {


        private void HandleLeap()
        {
            Unit caster = GetCaster();

            if (GetCaster().HasAura(MageSpells.SPELL_MAGE_BLAZING_SOUL))
            {
                GetCaster().AddAura(MageSpells.SPELL_MAGE_BLAZING_BARRIER, caster);
            }

            if (GetCaster().HasAura(MageSpells.SPELL_MAGE_PRISMATIC_CLOAK))
            {
                GetCaster().AddAura(MageSpells.SPELL_MAGE_PRISMATIC_CLOAK_BUFF, caster);
            }
        }


    }

    //389794
    public class spell_mage_snowdrift : SpellScriptLoader
    {
        public spell_mage_snowdrift() : base("spell_mage_snowdrift")
        {
        }

        public class spell_mage_snowdrift : AuraScript
        {


            private void OnTick(AuraEffect aurEff)
            {
                Unit target = GetTarget();
                Unit caster = GetCaster();

                if (target == null || caster == null)
                {
                    return;
                }

                // Slow enemies by 70%
                target.ApplySpellImmune(0, SpellImmunity.State, SPELL_AURA_MOD_DECREASE_SPEED, true);
                target.ApplySpellImmune(0, SpellImmunity.State, SPELL_AURA_MOD_SPEED_SLOW_ALL, true);
                target.ApplySpellImmune(0, SpellImmunity.State, SPELL_AURA_MOD_ROOT, true);

                // Deal (20% of Spell power) Frost damage every 1 sec
                int damage = caster.SpellDamageBonusDone(target, aurEff.GetSpellInfo(), 0, DOT, aurEff.GetSpellEffectInfo(), GetStackAmount()) * aurEff.GetAmount();
                damage = target.SpellDamageBonusTaken(caster, aurEff.GetSpellInfo(), damage, DOT);
                caster.DealDamage(target, target, damage, null, DOT, SPELL_SCHOOL_MASK_FROST, aurEff.GetSpellInfo(), false);

                // Check if target has been caught in Snowdrift for 3 sec consecutively
                if (aurEff.GetTickNumber() >= 3)
                {
                    // Apply Frozen in Ice and stun for 4 sec
                    target.CastSpell(target, MageSpells.SPELL_FROZEN_IN_ICE, true);
                    target.RemoveAura(MageSpells.SPELL_SNOWDRIFT);
                }
            }

            public override void Register()
            {
                AuraEffects.Add(new EffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDamage));
            }
        }



        public AuraScript GetAuraScript()
        {
            return new spell_mage_snowdrift();
        }
    }


    //108978
    public class spell_mage_alter_time : SpellScriptLoader
    {
        public spell_mage_alter_time() : base("spell_mage_alter_time")
        {
        }

        public class spell_mage_alter_time : SpellScript
        {


            private void HandleDummy(uint UnnamedParameter)
            {
                Unit caster = GetCaster();
                Unit target = GetHitUnit();

                if (caster == null || target == null)
                {
                    return;
                }

                // Check if the spell has been cast before
                Aura alterTime = target.GetAura(MageSpells.SPELL_ALTER_TIME);
                if (alterTime != null)
                {
                    // Check if the target has moved a long distance
                    if (target.GetDistance(alterTime.GetCaster()) > 50.0f)
                    {
                        target.RemoveAura(MageSpells.SPELL_ALTER_TIME);
                        return;
                    }

                    // Check if the target has died
                    if (target.IsDead())
                    {
                        target.RemoveAura(MageSpells.SPELL_ALTER_TIME);
                        return;
                    }

                    // Return the target to their location and health from when the spell was first cast
                    target.SetHealth(alterTime.GetEffect(0).GetAmount());
                    target.NearTeleportTo(alterTime.GetCaster().GetPositionX(), alterTime.GetCaster().GetPositionY(), alterTime.GetCaster().GetPositionZ(), alterTime.GetCaster().GetOrientation());
                    target.RemoveAura(MageSpells.SPELL_ALTER_TIME);
                }
                else
                {
                    // Save the target's current location and health
                    caster.AddAura(MageSpells.SPELL_ALTER_TIME, target);
                    target.SetAuraStack(MageSpells.SPELL_ALTER_TIME, target, target.GetHealth());
                }
            }

            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            }
        }



        public SpellScript GetSpellScript()
        {
            return new spell_mage_alter_time();
        }
    }

    //390218 10xx
    public class spell_mage_overflowing_energy : SpellScriptLoader
    {
        public spell_mage_overflowing_energy() : base("spell_mage_overflowing_energy")
        {
        }

        public class spell_mage_overflowing_energy : AuraScript
        {


            public bool CheckProc(ProcEventInfo eventInfo)
            {
                if (eventInfo.GetSpellInfo().Id == 390218)
                {
                    return false;
                }

                if (eventInfo.GetHitMask() & ProcFlagsHit.Critical != 0)
                {
                    return false;
                }

                if (eventInfo.GetDamageInfo() != null.GetSpellInfo())
                {
                    return false;
                }

                return true;
            }

            private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();

                int amount = aurEff.GetAmount();
                if (eventInfo.GetDamageInfo().GetSpellInfo().Id == 390218)
                {
                    amount = 0;
                }

                Unit target = GetTarget();

                GetTarget().CastSpell(target, 390218, SPELLVALUE_AURA_STACK);
            }

            public override void Register()
            {

                AuraEffects.Add(new EffectProcHandler(HandleProc, 0, SPELL_AURA_MOD_SPELL_CRIT_CHANCE, AuraScriptHookType.EffectProc));
            }
        }



        public override AuraScript GetAuraScript()
        {
            return new spell_mage_overflowing_energy();
        }
    }
    // Mastery: Icicles - 76613
    public class spell_mastery_icicles_proc : AuraScript
    {


        public bool CheckProc(ProcEventInfo eventInfo)
        {
            bool _spellCanProc = (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FROSTBOLT || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FROSTBOLT_TRIGGER);

            if (_spellCanProc)
            {
                return true;
            }
            return false;
        }

        private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetDamageInfo().GetVictim();
            Unit caster = eventInfo.GetDamageInfo().GetAttacker();
            if (target == null || caster == null)
            {
                return;
            }

            Player player = caster.ToPlayer();

            if (player == null)
            {
                return;
            }

            // Calculate damage
            int hitDamage = eventInfo.GetDamageInfo().GetDamage() + eventInfo.GetDamageInfo().GetAbsorb();

            // if hitDamage == 0 we have a miss, so we need to except this variant
            if (hitDamage != 0)
            {
                bool icilesAddSecond = false;

                if (caster.HasAura(MageSpells.SPELL_MAGE_ICE_NINE))
                {
                    if (RandomHelper.randChance(20))
                    {
                        icilesAddSecond = true;
                    }
                }

                hitDamage *= (player.m_activePlayerData.Mastery * 2.25f) / 100.0f;

                // Prevent huge hits on player after hitting low level creatures
                if (player.GetLevel() > target.GetLevel())
                {
                    hitDamage = Math.Min((int)hitDamage, (int)target.GetMaxHealth());
                }

                // We need to get the first free icicle slot
                sbyte icicleFreeSlot = -1; // -1 means no free slot
                sbyte icicleSecondFreeSlot = -1; // -1 means no free slot
                for (sbyte l_I = 0; l_I < 5; ++l_I)
                {
                    if (!player.HasAura(Globals.IcicleAuras[l_I]))
                    {
                        icicleFreeSlot = l_I;
                        if (icilesAddSecond && icicleFreeSlot != 5)
                        {
                            icicleSecondFreeSlot = (sbyte)(l_I + 1);
                        }
                        break;
                    }
                }

                switch (icicleFreeSlot)
                {
                    case -1:
                        {
                            // We need to find the icicle with the smallest duration.
                            sbyte smallestIcicle = 0;
                            int minDuration = 0xFFFFFF;
                            for (sbyte i = 0; i < 5; i++)
                            {
                                Aura tmpCurrentAura = player.GetAura(Globals.IcicleAuras[i]);
                                if (tmpCurrentAura != null)
                                {
                                    if (minDuration > tmpCurrentAura.GetDuration())
                                    {
                                        minDuration = tmpCurrentAura.GetDuration();
                                        smallestIcicle = i;
                                    }
                                }
                            }

                            // Launch the icicle with the smallest duration
                            AuraEffect currentIcicleAuraEffect = player.GetAuraEffect(Globals.IcicleAuras[smallestIcicle], 0);
                            if (currentIcicleAuraEffect != null)
                            {
                                float basePoints = currentIcicleAuraEffect.GetAmount();

                                if (caster.HasAura(MageSpells.SPELL_MAGE_BLACK_ICE))
                                {
                                    if (RandomHelper.randChance(20))
                                    {
                                        basePoints *= 2F;
                                    }
                                }

                                player.CastSpell(target, Globals.IcicleHits[smallestIcicle], true);
                                player.CastSpell(target, MageSpells.SPELL_MAGE_ICICLE_DAMAGE, basePoints);
                                player.RemoveAura(Globals.IcicleAuras[smallestIcicle]);
                            }

                            icicleFreeSlot = smallestIcicle;
                            // No break because we'll add the icicle in the next case
                        }

                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        {

                            Aura currentIcicleAura = player.AddAura(Globals.IcicleAuras[icicleFreeSlot], player);
                            if (currentIcicleAura != null)
                            {
                                AuraEffect effect = currentIcicleAura.GetEffect(0);
                                if (effect != null)
                                {
                                    effect.SetAmount(hitDamage);
                                }

                                player.AddAura(MageSpells.SPELL_MAGE_ICICLE_AURA, player);

                                if (caster.HasSpell(MageSpells.SPELL_MAGE_GLACIAL_SPIKE))
                                {
                                    Aura glacialSpikeProc = player.GetAura(MageSpells.SPELL_MAGE_ICICLE_AURA);
                                    if (glacialSpikeProc != null)
                                    {
                                        if (glacialSpikeProc.GetStackAmount() == 5)
                                        {
                                            player.CastSpell(player, MageSpells.SPELL_MAGE_GLACIAL_SPIKE_PROC, true);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                }

                switch (icicleSecondFreeSlot)
                {
                    case -1:
                        {
                            if (icilesAddSecond)
                            {
                                // We need to find the icicle with the smallest duration.
                                sbyte smallestIcicle = 0;
                                int minDuration = 0xFFFFFF;
                                for (sbyte i = 0; i < 5; i++)
                                {
                                    Aura tmpCurrentAura = player.GetAura(Globals.IcicleAuras[i]);
                                    if (tmpCurrentAura != null)
                                    {
                                        if (minDuration > tmpCurrentAura.GetDuration())
                                        {
                                            minDuration = tmpCurrentAura.GetDuration();
                                            smallestIcicle = i;
                                        }
                                    }
                                }

                                // Launch the icicle with the smallest duration
                                AuraEffect currentIcicleAuraEffect = player.GetAuraEffect(Globals.IcicleAuras[smallestIcicle], 0);
                                if (currentIcicleAuraEffect != null)
                                {
                                    float basePoints = currentIcicleAuraEffect.GetAmount();

                                    if (caster.HasAura(MageSpells.SPELL_MAGE_BLACK_ICE))
                                    {
                                        if (RandomHelper.randChance(20))
                                        {
                                            basePoints *= 2F;
                                        }
                                    }

                                    player.CastSpell(target, Globals.IcicleHits[smallestIcicle], true);
                                    player.CastSpell(target, MageSpells.SPELL_MAGE_ICICLE_DAMAGE, basePoints);
                                    player.RemoveAura(Globals.IcicleAuras[smallestIcicle]);
                                }

                                icicleSecondFreeSlot = smallestIcicle;
                                // No break because we'll add the icicle in the next case
                            }
                        }

                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        {
                            Aura currentIcicleAura = player.AddAura(Globals.IcicleAuras[icicleSecondFreeSlot], player);
                            if (currentIcicleAura != null)
                            {
                                AuraEffect effect = currentIcicleAura.GetEffect(0);
                                if (effect != null)
                                {
                                    effect.SetAmount(hitDamage);
                                }

                                player.AddAura(MageSpells.SPELL_MAGE_ICICLE_AURA, player);

                                if (caster.HasSpell(MageSpells.SPELL_MAGE_GLACIAL_SPIKE))
                                {
                                    Aura glacialSpikeProc = player.GetAura(MageSpells.SPELL_MAGE_ICICLE_AURA);
                                    if (glacialSpikeProc != null)
                                    {
                                        if (glacialSpikeProc.GetStackAmount() == 5)
                                        {
                                            player.CastSpell(player, MageSpells.SPELL_MAGE_GLACIAL_SPIKE_PROC, true);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                }
            }
        }

        public override void Register()
        {

            AuraEffects.Add(new EffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    // Frozen Orb (damage) - 84721
    public class spell_mage_frozen_orb : SpellScript
    {


        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            caster.CastSpell(target, MageSpells.SPELL_MAGE_CHILLED, true);

            // Fingers of Frost
            if (caster.HasSpell(MageSpells.SPELL_MAGE_FINGERS_OF_FROST))
            {
                float fingersFrostChance = 10.0f;

                if (caster.HasAura(MageSpells.SPELL_MAGE_FROZEN_TOUCH))
                {
                    AuraEffect frozenEff0 = caster.GetAuraEffect(MageSpells.SPELL_MAGE_FROZEN_TOUCH, 0);
                    if (frozenEff0 != null)
                    {
                        int pct = frozenEff0.GetAmount();
                        MathFunctions.AddPct(ref fingersFrostChance, pct);
                    }
                }

                if (RandomHelper.randChance(fingersFrostChance))
                {
                    caster.CastSpell(caster, MageSpells.SPELL_MAGE_FINGERS_OF_FROST_VISUAL_UI, true);
                    caster.CastSpell(caster, MageSpells.SPELL_MAGE_FINGERS_OF_FROST_AURA, true);
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    // Ice Nova | Supernova - 157997 | 157980
    public class spell_mage_nova_talent : SpellScript
    {


        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            Unit explTarget = GetExplTargetUnit();
            if (target == null || caster == null || explTarget == null)
            {
                return;
            }

            int eff2 = GetSpellInfo().GetEffect(2).CalcValue();
            if (GetSpellInfo().GetEffect(2).CalcValue())
            {
                int dmg = GetHitDamage();
                if (target == explTarget)
                {
                    dmg = MathFunctions.CalculatePct(dmg, eff2);
                }
                SetHitDamage(dmg);
            }
        }


    }
    */

    // 110909 - Alter Time Aura
    [Script] // 342246 - Alter Time Aura
    internal class spell_mage_alter_time_aura : AuraScript, IHasAuraEffects
    {
        private ulong _health;
        private Position _pos;
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.AlterTimeVisual, MageSpells.MasterOfTime, MageSpells.Blink);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit unit = GetTarget();
            _health = unit.GetHealth();
            _pos = new Position(unit.GetPosition());
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit unit = GetTarget();

            if (unit.GetDistance(_pos) <= 100.0f &&
                GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
            {
                unit.SetHealth(_health);
                unit.NearTeleportTo(_pos);

                if (unit.HasAura(MageSpells.MasterOfTime))
                {
                    SpellInfo blink = Global.SpellMgr.GetSpellInfo(MageSpells.Blink, Difficulty.None);
                    unit.GetSpellHistory().ResetCharges(blink.ChargeCategoryId);
                }

                unit.CastSpell(unit, MageSpells.AlterTimeVisual);
            }
        }
    }

    // 127140 - Alter Time Active
    [Script] // 342247 - Alter Time Active
    internal class spell_mage_alter_time_active : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.AlterTimeAura, MageSpells.ArcaneAlterTimeAura);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(RemoveAlterTimeAura, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        private void RemoveAlterTimeAura(uint effIndex)
        {
            Unit unit = GetCaster();
            unit.RemoveAura(MageSpells.AlterTimeAura, ObjectGuid.Empty, 0, AuraRemoveMode.Expire);
            unit.RemoveAura(MageSpells.ArcaneAlterTimeAura, ObjectGuid.Empty, 0, AuraRemoveMode.Expire);
        }
    }

    [Script] // 44425 - Arcane Barrage
    internal class spell_mage_arcane_barrage : SpellScript, ISpellAfterCast, IHasSpellEffects
    {
        private ObjectGuid _primaryTarget;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.ArcaneBarrageR3, MageSpells.ArcaneBarrageEnergize) && spellInfo.GetEffects().Count > 1;
        }

        public void AfterCast()
        {
            Unit caster = GetCaster();

            // Consume all arcane charges
            int arcaneCharges = -caster.ModifyPower(PowerType.ArcaneCharges, -caster.GetMaxPower(PowerType.ArcaneCharges), false);

            if (arcaneCharges != 0)
            {
                AuraEffect auraEffect = caster.GetAuraEffect(MageSpells.ArcaneBarrageR3, 0, caster.GetGUID());

                if (auraEffect != null)
                    caster.CastSpell(caster, MageSpells.ArcaneBarrageEnergize, new CastSpellExtraArgs(SpellValueMod.BasePoint0, arcaneCharges * auraEffect.GetAmount() / 100));
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(MarkPrimaryTarget, 1, SpellEffectName.Dummy, SpellScriptHookType.LaunchTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleEffectHitTarget(uint effIndex)
        {
            if (GetHitUnit().GetGUID() != _primaryTarget)
                SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), GetEffectInfo(1).CalcValue(GetCaster())));
        }

        private void MarkPrimaryTarget(uint effIndex)
        {
            _primaryTarget = GetHitUnit().GetGUID();
        }
    }

    [Script] // 195302 - Arcane Charge
    internal class spell_mage_arcane_charge_clear : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.ArcaneCharge);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(RemoveArcaneCharge, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void RemoveArcaneCharge(uint effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell(MageSpells.ArcaneCharge);
        }
    }

    [Script] // 1449 - Arcane Explosion
    internal class spell_mage_arcane_explosion : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            if (!ValidateSpellInfo(MageSpells.ArcaneMage, MageSpells.Reverberate))
                return false;

            if (spellInfo.GetEffects().Count <= 1)
                return false;

            return spellInfo.GetEffect(1).IsEffect(SpellEffectName.SchoolDamage);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(CheckRequiredAuraForBaselineEnergize, 0, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleReverberate, 2, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
        }

        private void CheckRequiredAuraForBaselineEnergize(uint effIndex)
        {
            if (GetUnitTargetCountForEffect(1) == 0 ||
                !GetCaster().HasAura(MageSpells.ArcaneMage))
                PreventHitDefaultEffect(effIndex);
        }

        private void HandleReverberate(uint effIndex)
        {
            bool procTriggered = false;

            Unit caster = GetCaster();
            AuraEffect triggerChance = caster.GetAuraEffect(MageSpells.Reverberate, 0);

            if (triggerChance != null)
            {
                AuraEffect requiredTargets = caster.GetAuraEffect(MageSpells.Reverberate, 1);

                if (requiredTargets != null)
                    procTriggered = GetUnitTargetCountForEffect(1) >= requiredTargets.GetAmount() && RandomHelper.randChance(triggerChance.GetAmount());
            }

            if (!procTriggered)
                PreventHitDefaultEffect(effIndex);
        }
    }

    [Script] // 235313 - Blazing Barrier
    internal class spell_mage_blazing_barrier : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.BlazingBarrierTrigger);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            AuraEffects.Add(new EffectProcHandler(HandleProc, 1, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            Unit caster = GetCaster();

            if (caster)
                amount = (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetDamageInfo().GetVictim();
            Unit target = eventInfo.GetDamageInfo().GetAttacker();

            if (caster && target)
                caster.CastSpell(target, MageSpells.BlazingBarrierTrigger, true);
        }
    }

    // 190356 - Blizzard
    [Script] // 4658 - AreaTrigger Create Properties
    internal class areatrigger_mage_blizzard : AreaTriggerAI
    {
        private TimeSpan _tickTimer;

        public areatrigger_mage_blizzard(AreaTrigger areatrigger) : base(areatrigger)
        {
            _tickTimer = TimeSpan.FromMilliseconds(1000);
        }

        public override void OnUpdate(uint diff)
        {
            _tickTimer -= TimeSpan.FromMilliseconds(diff);

            while (_tickTimer <= TimeSpan.Zero)
            {
                Unit caster = at.GetCaster();

                caster?.CastSpell(at.GetPosition(), MageSpells.BlizzardDamage, new CastSpellExtraArgs());

                _tickTimer += TimeSpan.FromMilliseconds(1000);
            }
        }
    }

    [Script] // 190357 - Blizzard (Damage)
    internal class spell_mage_blizzard_damage : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.BlizzardSlow);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleSlow, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleSlow(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), MageSpells.BlizzardSlow, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }
    }

    [Script] // 198063 - Burning Determination
    internal class spell_mage_burning_determination : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            if (spellInfo != null)
                if (spellInfo.GetAllEffectsMechanicMask().HasAnyFlag(((1u << (int)Mechanics.Interrupt) | (1 << (int)Mechanics.Silence))))
                    return true;

            return false;
        }
    }

    [Script] // 86949 - Cauterize
    internal class spell_mage_cauterize : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(SuppressSpeedBuff, 2, SpellEffectName.TriggerSpell, SpellScriptHookType.Launch));
        }

        private void SuppressSpeedBuff(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
        }
    }

    [Script]
    internal class spell_mage_cauterize_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 2 && ValidateSpellInfo(MageSpells.CauterizeDot, MageSpells.Cauterized, spellInfo.GetEffect(2).TriggerSpell);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectAbsorbHandler(HandleAbsorb, 0, false, AuraScriptHookType.EffectAbsorb));
        }

        private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            AuraEffect effectInfo = GetEffect(1);

            if (effectInfo == null ||
                !GetTargetApplication().HasEffect(1) ||
                dmgInfo.GetDamage() < GetTarget().GetHealth() ||
                dmgInfo.GetDamage() > GetTarget().GetMaxHealth() * 2 ||
                GetTarget().HasAura(MageSpells.Cauterized))
            {
                PreventDefaultAction();

                return;
            }

            GetTarget().SetHealth(GetTarget().CountPctFromMaxHealth(effectInfo.GetAmount()));
            GetTarget().CastSpell(GetTarget(), GetEffectInfo(2).TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            GetTarget().CastSpell(GetTarget(), MageSpells.CauterizeDot, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            GetTarget().CastSpell(GetTarget(), MageSpells.Cauterized, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }
    }

    [Script] // 235219 - Cold Snap
    internal class spell_mage_cold_snap : SpellScript, IHasSpellEffects
    {
        private static readonly uint[] SpellsToReset =
        {
            MageSpells.ConeOfCold, MageSpells.IceBarrier, MageSpells.IceBlock
        };

        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellsToReset) && ValidateSpellInfo(MageSpells.FrostNova);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
        }

        private void HandleDummy(uint effIndex)
        {
            foreach (uint spellId in SpellsToReset)
                GetCaster().GetSpellHistory().ResetCooldown(spellId, true);

            GetCaster().GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(MageSpells.FrostNova, GetCastDifficulty()).ChargeCategoryId);
        }
    }

    internal class CometStormEvent : BasicEvent
    {
        private readonly Unit _caster;
        private readonly Position _dest;
        private byte _count;
        private ObjectGuid _originalCastId;

        public CometStormEvent(Unit caster, ObjectGuid originalCastId, Position dest)
        {
            _caster = caster;
            _originalCastId = originalCastId;
            _dest = dest;
        }

        public override bool Execute(ulong time, uint diff)
        {
            Position destPosition = new(_dest.GetPositionX() + RandomHelper.FRand(-3.0f, 3.0f), _dest.GetPositionY() + RandomHelper.FRand(-3.0f, 3.0f), _dest.GetPositionZ());
            _caster.CastSpell(destPosition, MageSpells.CometStormVisual, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetOriginalCastId(_originalCastId));
            ++_count;

            if (_count >= 7)
                return true;

            _caster.m_Events.AddEvent(this, TimeSpan.FromMilliseconds(time) + RandomHelper.RandTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(275)));

            return false;
        }
    }

    [Script] // 153595 - Comet Storm (launch)
    internal class spell_mage_comet_storm : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.CometStormVisual);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(EffectHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        private void EffectHit(uint effIndex)
        {
            GetCaster().m_Events.AddEventAtOffset(new CometStormEvent(GetCaster(), GetSpell().m_castId, GetHitDest()), RandomHelper.RandTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(275)));
        }
    }

    [Script] // 228601 - Comet Storm (Damage)
    internal class spell_mage_comet_storm_damage : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.CometStormDamage);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        private void HandleEffectHitTarget(uint effIndex)
        {
            GetCaster().CastSpell(GetHitDest(), MageSpells.CometStormDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetOriginalCastId(GetSpell().m_originalCastId));
        }
    }

    [Script] // 120 - Cone of Cold
    internal class spell_mage_cone_of_cold : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.ConeOfColdSlow);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleSlow, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleSlow(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), MageSpells.ConeOfColdSlow, true);
        }
    }

    [Script] // 190336 - Conjure Refreshment
    internal class spell_mage_conjure_refreshment : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.ConjureRefreshment, MageSpells.ConjureRefreshmentTable);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();

            if (caster)
            {
                Group group = caster.GetGroup();

                if (group)
                    caster.CastSpell(caster, MageSpells.ConjureRefreshmentTable, true);
                else
                    caster.CastSpell(caster, MageSpells.ConjureRefreshment, true);
            }
        }
    }

    [Script] // 112965 - Fingers of Frost
    internal class spell_mage_fingers_of_frost_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.FingersOfFrost);
        }

        public override void Register()
        {
            AuraEffects.Add(new CheckEffectProcHandler(CheckFrostboltProc, 0, AuraType.Dummy));
            AuraEffects.Add(new CheckEffectProcHandler(CheckFrozenOrbProc, 1, AuraType.Dummy));
            AuraEffects.Add(new EffectProcHandler(Trigger, 0, AuraType.Dummy, AuraScriptHookType.EffectAfterProc));
            AuraEffects.Add(new EffectProcHandler(Trigger, 1, AuraType.Dummy, AuraScriptHookType.EffectAfterProc));
        }

        private bool CheckFrostboltProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Mage, new FlagArray128(0, 0x2000000, 0, 0)) && RandomHelper.randChance(aurEff.GetAmount());
        }

        private bool CheckFrozenOrbProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Mage, new FlagArray128(0, 0, 0x80, 0)) && RandomHelper.randChance(aurEff.GetAmount());
        }

        private void Trigger(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            eventInfo.GetActor().CastSpell(GetTarget(), MageSpells.FingersOfFrost, new CastSpellExtraArgs(aurEff));
        }
    }

    // 133 - Fireball
    [Script] // 11366 - Pyroblast
    internal class spell_mage_firestarter : SpellScript, ISpellCalcCritChance
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.Firestarter);
        }

        public void CalcCritChance(Unit victim, ref float critChance)
        {
            AuraEffect aurEff = GetCaster().GetAuraEffect(MageSpells.Firestarter, 0);

            if (aurEff != null)
                if (victim.GetHealthPct() >= aurEff.GetAmount())
                    critChance = 100.0f;
        }
    }

    [Script] // 321712 - Pyroblast
    internal class spell_mage_firestarter_dots : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.Firestarter);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcCritChanceHandler(CalcCritChance, SpellConst.EffectAll, AuraType.PeriodicDamage));
        }

        private void CalcCritChance(AuraEffect aurEff, Unit victim, ref float critChance)
        {
            AuraEffect aurEff0 = GetCaster().GetAuraEffect(MageSpells.Firestarter, 0);

            if (aurEff0 != null)
                if (victim.GetHealthPct() >= aurEff0.GetAmount())
                    critChance = 100.0f;
        }
    }

    // 205029 - Flame On
    internal class spell_mage_flame_on : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.FireBlast) && CliDB.SpellCategoryStorage.HasRecord(Global.SpellMgr.GetSpellInfo(MageSpells.FireBlast, Difficulty.None).ChargeCategoryId) && spellInfo.GetEffects().Count > 2;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ChargeRecoveryMultiplier));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            amount = -(int)MathFunctions.GetPctOf(GetEffectInfo(2).CalcValue() * Time.InMilliseconds, CliDB.SpellCategoryStorage.LookupByKey(Global.SpellMgr.GetSpellInfo(MageSpells.FireBlast, Difficulty.None).ChargeCategoryId).ChargeRecoveryTime);
        }
    }

    [Script] // 116 - Frostbolt
    internal class spell_mage_frostbolt : SpellScript, ISpellOnHit
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(MageSpells.Chilled);
        }

        public void OnHit()
        {
            Unit target = GetHitUnit();

            if (target != null)
                GetCaster().CastSpell(target, MageSpells.Chilled, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }
    }

    [Script] // 11426 - Ice Barrier
    internal class spell_mage_ice_barrier : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(MageSpells.Chilled);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.SchoolAbsorb, AuraScriptHookType.EffectProc));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            Unit caster = GetCaster();

            if (caster)
                amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 10.0f);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit caster = eventInfo.GetDamageInfo().GetVictim();
            Unit target = eventInfo.GetDamageInfo().GetAttacker();

            if (caster && target)
                caster.CastSpell(target, MageSpells.Chilled, true);
        }
    }

    [Script] // 45438 - Ice Block
    internal class spell_mage_ice_block : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.EverwarmSocks);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectTargetSelectHandler(PreventStunWithEverwarmSocks, 0, Targets.UnitCaster));
            SpellEffects.Add(new ObjectTargetSelectHandler(PreventEverwarmSocks, 5, Targets.UnitCaster));
            SpellEffects.Add(new ObjectTargetSelectHandler(PreventEverwarmSocks, 6, Targets.UnitCaster));
        }

        private void PreventStunWithEverwarmSocks(ref WorldObject target)
        {
            if (GetCaster().HasAura(MageSpells.EverwarmSocks))
                target = null;
        }

        private void PreventEverwarmSocks(ref WorldObject target)
        {
            if (!GetCaster().HasAura(MageSpells.EverwarmSocks))
                target = null;
        }
    }

    [Script] // Ice Lance - 30455
    internal class spell_mage_ice_lance : SpellScript, IHasSpellEffects
    {
        private readonly List<ObjectGuid> _orderedTargets = new();
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.IceLanceTrigger, MageSpells.ThermalVoid, MageSpells.IcyVeins, MageSpells.ChainReactionDummy, MageSpells.ChainReaction, MageSpells.FingersOfFrost);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(IndexTarget, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.LaunchTarget));
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void IndexTarget(uint effIndex)
        {
            _orderedTargets.Add(GetHitUnit().GetGUID());
        }

        private void HandleOnHit(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            int index = _orderedTargets.IndexOf(target.GetGUID());

            if (index == 0 // only primary Target triggers these benefits
                &&
                target.HasAuraState(AuraStateType.Frozen, GetSpellInfo(), caster))
            {
                // Thermal Void
                Aura thermalVoid = caster.GetAura(MageSpells.ThermalVoid);

                if (!thermalVoid.GetSpellInfo().GetEffects().Empty())
                {
                    Aura icyVeins = caster.GetAura(MageSpells.IcyVeins);

                    icyVeins?.SetDuration(icyVeins.GetDuration() + thermalVoid.GetSpellInfo().GetEffect(0).CalcValue(caster) * Time.InMilliseconds);
                }

                // Chain Reaction
                if (caster.HasAura(MageSpells.ChainReactionDummy))
                    caster.CastSpell(caster, MageSpells.ChainReaction, true);
            }

            // put Target index for chain value Multiplier into EFFECT_1 base points, otherwise triggered spell doesn't know which Damage Multiplier to apply
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint1, index);
            caster.CastSpell(target, MageSpells.IceLanceTrigger, args);
        }
    }

    [Script] // 228598 - Ice Lance
    internal class spell_mage_ice_lance_damage : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(ApplyDamageMultiplier, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void ApplyDamageMultiplier(uint effIndex)
        {
            SpellValue spellValue = GetSpellValue();

            if ((spellValue.CustomBasePointsMask & (1 << 1)) != 0)
            {
                int originalDamage = GetHitDamage();
                float targetIndex = (float)spellValue.EffectBasePoints[1];
                float multiplier = MathF.Pow(GetEffectInfo().CalcDamageMultiplier(GetCaster(), GetSpell()), targetIndex);
                SetHitDamage((int)(originalDamage * multiplier));
            }
        }
    }

    [Script] // 11119 - Ignite
    internal class spell_mage_ignite : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.Ignite);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget();
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            SpellInfo igniteDot = Global.SpellMgr.GetSpellInfo(MageSpells.Ignite, GetCastDifficulty());
            int pct = aurEff.GetAmount();

            int amount = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), pct) / igniteDot.GetMaxTicks());

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            GetTarget().CastSpell(eventInfo.GetProcTarget(), MageSpells.Ignite, args);
        }
    }

    // 37447 - Improved Mana Gems
    [Script] // 61062 - Improved Mana Gems
    internal class spell_mage_imp_mana_gems : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.ManaSurge);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell((Unit)null, MageSpells.ManaSurge, true);
        }
    }

    [Script] // 1463 - Incanter's Flow
    internal class spell_mage_incanters_flow : AuraScript, IHasAuraEffects
    {
        private sbyte modifier = 1;
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.IncantersFlow);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandlePeriodicTick, 0, AuraType.PeriodicDummy));
        }

        private void HandlePeriodicTick(AuraEffect aurEff)
        {
            // Incanter's flow should not cycle out of combat
            if (!GetTarget().IsInCombat())
                return;

            Aura aura = GetTarget().GetAura(MageSpells.IncantersFlow);

            if (aura != null)
            {
                uint stacks = aura.GetStackAmount();

                // Force always to values between 1 and 5
                if ((modifier == -1 && stacks == 1) ||
                    (modifier == 1 && stacks == 5))
                {
                    modifier *= -1;

                    return;
                }

                aura.ModStackAmount(modifier);
            }
            else
            {
                GetTarget().CastSpell(GetTarget(), MageSpells.IncantersFlow, true);
            }
        }
    }

    [Script] // 44457 - Living Bomb
    internal class spell_mage_living_bomb : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.LivingBombPeriodic);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetCaster().CastSpell(GetHitUnit(), MageSpells.LivingBombPeriodic, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint2, 1));
        }
    }

    [Script] // 44461 - Living Bomb
    internal class spell_mage_living_bomb_explosion : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.NeedsExplicitUnitTarget() && ValidateSpellInfo(MageSpells.LivingBombPeriodic);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
            SpellEffects.Add(new EffectHandler(HandleSpread, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            targets.Remove(GetExplTargetWorldObject());
        }

        private void HandleSpread(uint effIndex)
        {
            if (GetSpellValue().EffectBasePoints[0] > 0)
                GetCaster().CastSpell(GetHitUnit(), MageSpells.LivingBombPeriodic, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint2, 0));
        }
    }

    [Script] // 217694 - Living Bomb
    internal class spell_mage_living_bomb_periodic : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.LivingBombExplosion);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 2, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit caster = GetCaster();

            if (caster)
                caster.CastSpell(GetTarget(), MageSpells.LivingBombExplosion, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount()));
        }
    }

    // @todo move out of here and rename - not a mage spell
    [Script] // 32826 - Polymorph (Visual)
    internal class spell_mage_polymorph_visual : SpellScript, IHasSpellEffects
    {
        private const uint NPC_AUROSALIA = 18744;

        private readonly uint[] PolymorhForms =
        {
            MageSpells.SquirrelForm, MageSpells.GiraffeForm, MageSpells.SerpentForm, MageSpells.DradonhawkForm, MageSpells.WorgenForm, MageSpells.SheepForm
        };

        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PolymorhForms);
        }

        public override void Register()
        {
            // add dummy effect spell handler to Polymorph visual
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Unit target = GetCaster().FindNearestCreature(NPC_AUROSALIA, 30.0f);

            if (target)
                if (target.IsTypeId(TypeId.Unit))
                    target.CastSpell(target, PolymorhForms[RandomHelper.IRand(0, 5)], true);
        }
    }

    [Script] // 235450 - Prismatic Barrier
    internal class spell_mage_prismatic_barrier : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            Unit caster = GetCaster();

            if (caster)
                amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
        }
    }

    [Script] // 205021 - Ray of Frost
    internal class spell_mage_ray_of_frost : SpellScript, ISpellOnHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.RayOfFrostFingersOfFrost);
        }

        public void OnHit()
        {
            Unit caster = GetCaster();

            caster?.CastSpell(caster, MageSpells.RayOfFrostFingersOfFrost, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }
    }

    [Script]
    internal class spell_mage_ray_of_frost_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.RayOfFrostBonus, MageSpells.RayOfFrostFingersOfFrost);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.PeriodicDamage));
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 1, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();

            if (caster != null)
                if (aurEff.GetTickNumber() > 1) // First tick should deal base Damage
                    caster.CastSpell(caster, MageSpells.RayOfFrostBonus, true);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            caster?.RemoveAurasDueToSpell(MageSpells.RayOfFrostFingersOfFrost);
        }
    }

    [Script] // 136511 - Ring of Frost
    internal class spell_mage_ring_of_frost : AuraScript, IHasAuraEffects
    {
        private ObjectGuid _ringOfFrostGUID;
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.RingOfFrostSummon, MageSpells.RingOfFrostFreeze) && !Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, Difficulty.None).GetEffects().Empty();
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.ProcTriggerSpell));
            AuraEffects.Add(new EffectApplyHandler(Apply, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            TempSummon ringOfFrost = GetRingOfFrostMinion();

            if (ringOfFrost)
                GetTarget().CastSpell(ringOfFrost.GetPosition(), MageSpells.RingOfFrostFreeze, new CastSpellExtraArgs(true));
        }

        private void Apply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            List<TempSummon> minions = new();
            GetTarget().GetAllMinionsByEntry(minions, (uint)Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, GetCastDifficulty()).GetEffect(0).MiscValue);

            // Get the last summoned RoF, save it and despawn older ones
            foreach (var summon in minions)
            {
                TempSummon ringOfFrost = GetRingOfFrostMinion();

                if (ringOfFrost)
                {
                    if (summon.GetTimer() > ringOfFrost.GetTimer())
                    {
                        ringOfFrost.DespawnOrUnsummon();
                        _ringOfFrostGUID = summon.GetGUID();
                    }
                    else
                    {
                        summon.DespawnOrUnsummon();
                    }
                }
                else
                {
                    _ringOfFrostGUID = summon.GetGUID();
                }
            }
        }

        private TempSummon GetRingOfFrostMinion()
        {
            Creature creature = ObjectAccessor.GetCreature(GetOwner(), _ringOfFrostGUID);

            if (creature)
                return creature.ToTempSummon();

            return null;
        }
    }

    [Script] // 82691 - Ring of Frost (freeze efect)
    internal class spell_mage_ring_of_frost_freeze : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.RingOfFrostSummon, MageSpells.RingOfFrostFreeze) && !Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, Difficulty.None).GetEffects().Empty();
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            WorldLocation dest = GetExplTargetDest();
            float outRadius = Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, GetCastDifficulty()).GetEffect(0).CalcRadius();
            float inRadius = 6.5f;

            targets.RemoveAll(target =>
                              {
                                  Unit unit = target.ToUnit();

                                  if (!unit)
                                      return true;

                                  return unit.HasAura(MageSpells.RingOfFrostDummy) || unit.HasAura(MageSpells.RingOfFrostFreeze) || unit.GetExactDist(dest) > outRadius || unit.GetExactDist(dest) < inRadius;
                              });
        }
    }

    [Script]
    internal class spell_mage_ring_of_frost_freeze_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.RingOfFrostDummy);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                if (GetCaster())
                    GetCaster().CastSpell(GetTarget(), MageSpells.RingOfFrostDummy, true);
        }
    }

    [Script] // 157980 - Supernova
    internal class spell_mage_supernova : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDamage(uint effIndex)
        {
            if (GetExplTargetUnit() == GetHitUnit())
            {
                int damage = GetHitDamage();
                MathFunctions.AddPct(ref damage, GetEffectInfo(0).CalcValue());
                SetHitDamage(damage);
            }
        }
    }

    [Script] // 80353 - Time Warp
    internal class spell_mage_time_warp : SpellScript, ISpellAfterHit, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.TemporalDisplacement, MageSpells.HunterInsanity, MageSpells.ShamanExhaustion, MageSpells.ShamanSated, MageSpells.PetNetherwindsFatigued);
        }

        public void AfterHit()
        {
            Unit target = GetHitUnit();

            if (target)
                target.CastSpell(target, MageSpells.TemporalDisplacement, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void RemoveInvalidTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.TemporalDisplacement));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.HunterInsanity));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.ShamanExhaustion));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.ShamanSated));
        }
    }

    [Script] // 210824 - Touch of the Magi (Aura)
    internal class spell_mage_touch_of_the_magi_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.TouchOfTheMagiExplode);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
            AuraEffects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo != null)
                if (damageInfo.GetAttacker() == GetCaster() &&
                    damageInfo.GetVictim() == GetTarget())
                {
                    uint extra = MathFunctions.CalculatePct(damageInfo.GetDamage(), 25);

                    if (extra > 0)
                        aurEff.ChangeAmount(aurEff.GetAmount() + (int)extra);
                }
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            int amount = aurEff.GetAmount();

            if (amount == 0 ||
                GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit caster = GetCaster();

            caster?.CastSpell(GetTarget(), MageSpells.TouchOfTheMagiExplode, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, amount));
        }
    }

    [Script] // 33395 Water Elemental's Freeze
    internal class spell_mage_water_elemental_freeze : SpellScript, ISpellAfterHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.FingersOfFrost);
        }

        public void AfterHit()
        {
            Unit owner = GetCaster().GetOwner();

            if (!owner)
                return;

            owner.CastSpell(owner, MageSpells.FingersOfFrost, true);
        }
    }
}