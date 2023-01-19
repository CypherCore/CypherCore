// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.Spell;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Warlock
{
    struct SpellIds
    {
        public const uint INFERNO_AURA = 270545;
        public const uint CREATE_HEALTHSTONE = 23517;
        public const uint DEMONIC_CIRCLE_ALLOW_CAST = 62388;
        public const uint DEMONIC_CIRCLE_SUMMON = 48018;
        public const uint DEMONIC_CIRCLE_TELEPORT = 48020;
        public const uint DEVOUR_MAGIC_HEAL = 19658;
        public const uint GLYPH_OF_DEMON_TRAINING = 56249;
        public const uint GLYPH_OF_SOUL_SWAP = 56226;
        public const uint GLYPH_OF_SUCCUBUS = 56250;
        public const uint IMPROVED_HEALTH_FUNNEL_BUFF_R1 = 60955;
        public const uint IMPROVED_HEALTH_FUNNEL_BUFF_R2 = 60956;
        public const uint IMPROVED_HEALTH_FUNNEL_R1 = 18703;
        public const uint IMPROVED_HEALTH_FUNNEL_R2 = 18704;
        public const uint RAIN_OF_FIRE = 5740;
        public const uint THALKIELS_CONSUMPTION_DAMAGE = 211715;
        public const uint THALKIES_DISCORD_DAMAGE = 211727;
        public const uint RAIN_OF_FIRE_DAMAGE = 42223;
        public const uint RAIN_OF_FIRE_ENERGIZE = 270548;
        public const uint SEED_OF_CORRUPTION_DAMAGE = 27285;
        public const uint SEED_OF_CORRUPTION_GENERIC = 32865;
        public const uint SOULSHATTER_EFFECT = 32835;
        public const uint SOUL_SWAP_CD_MARKER = 94229;
        public const uint SOUL_SWAP_OVERRIDE = 86211;
        public const uint SOUL_SWAP_MOD_COST = 92794;
        public const uint SOUL_SWAP_DOT_MARKER = 92795;
        public const uint UNSTABLE_AFFLICTION = 30108;
        public const uint UNSTABLE_AFFLICTION_DISPEL = 31117;
        public const uint DEMONBOLT_ENERGIZE = 280127;
        public const uint ABSOLUTE_CORRUPTION = 196103;
        public const uint CHAOS_BOLT = 116858;
        public const uint FELHUNTER_SPELL_LOCK = 19647;
        public const uint SUCCUBUS_WHIPLASH = 6360;
        public const uint FEL_LORD_CLEAVE = 213688;
        public const uint AFTERMATH_STUN = 85387;
        public const uint ETERNAL_STRUGGLE_PROC = 196304;
        public const uint AGONY = 980;
        public const uint ARCHIMONDES_VENGEANCE_COOLDOWN = 116405;
        public const uint ARCHIMONDES_VENGEANCE_DAMAGE = 124051;
        public const uint ARCHIMONDES_VENGEANCE_PASSIVE = 116403;
        public const uint BACKDRAFT = 117828;
        public const uint BACKDRAFT_AURA = 196406;
        public const uint WILD_IMP_SUMMON = 104317;
        public const uint ESSENCE_DRAIN = 221711;
        public const uint ESSENCE_DRAIN_DEBUFF = 221715;
        public const uint SOUL_SKIN = 218567;
        public const uint BANE_OF_DOOM_EFFECT = 18662;
        public const uint HOWL_OF_TERROR = 5484;
        public const uint BURNING_RUSH = 111400;
        public const uint GLYPH_OF_FELGUARD = 56246;
        public const uint CALL_DREADSTALKERS = 104316;
        public const uint TEAR_CHAOS_BARRAGE = 187394;
        public const uint TEAR_CHAOS_BOLT = 215279;
        public const uint CORRUPTION_DOT = 146739;
        public const uint ARENA_DAMPENING = 110310;
        public const uint VOIDWALKER_SUFFERING = 17735;
        public const uint SOUL_LINK_HEAL = 108447;
        public const uint LASERBEAM = 212529;
        public const uint CONTAGION = 196105;
        public const uint SOULFIRE_ENERGIZE = 281490;
        public const uint SOUL_LINK_BUFF = 108446;
        public const uint TORMENTED_SOULS = 216695;
        public const uint DEMON_SKIN = 219272;
        public const uint IMMOLATION_TRIGGERED = 20153;
        public const uint INFERNAL_FURNACE = 211119;
        public const uint T14_BONUS = 123141;
        public const uint HARVEST_LIFE = 108371;
        public const uint IMMOLATION = 19483;
        public const uint HELLFIRE_DAMAGE = 5857;
        public const uint CURSE_OF_SHADOWS_DAMAGE = 236615;
        public const uint RAGING_SOUL = 148463;
        public const uint CONTAGION_DEBUFF = 233494;
        public const uint STOLEN_POWER = 211530;
        public const uint STOLEN_POWER_COUNTER = 211529;
        public const uint STOLEN_POWER_BUFF = 211583;
        public const uint CALL_DREADSTALKERS_SUMMON = 193331;
        public const uint SHARPENED_DREADFANGS = 211123;
        public const uint CORRUPTION_TRIGGERED = 146739;
        public const uint CHANNEL_DEMONFIRE_ACTIVATOR = 228312;
        public const uint PET_DOOMBOLT = 85692;
        public const uint DEVOURER_OF_LIFE_PROC = 215165;
        public const uint T16_4P_TRIGGERED = 145164;
        public const uint PLANESWALKER = 196675;
        public const uint FELGUARD_FELSTORM = 89751;
        public const uint PLANESWALKER_BUFF = 196674;
        public const uint SOUL_EFFIGY_DAMAGE = 205260;
        public const uint SOUL_EFFIGY_VISUAL = 205277;
        public const uint CHANNEL_DEMONFIRE_DAMAGE = 196448;
        public const uint COMMAND_DEMON_OVERRIDER = 119904;
        public const uint UNSTABLE_AFFLICTION_DOT1 = 233490;
        public const uint PET_LESSER_INVISIBILITY = 7870;
        public const uint UNSTABLE_AFFLICTION_DOT2 = 233496;
        public const uint UNSTABLE_AFFLICTION_DOT3 = 233497;
        public const uint UNSTABLE_AFFLICTION_DOT4 = 233498;
        public const uint UNSTABLE_AFFLICTION_DOT5 = 233499;
        public const uint CONFLAGRATE = 17962;
        public const uint CONFLAGRATE_FIRE_AND_BRIMSTONE = 108685;
        public const uint CORRUPTION = 172;
        public const uint CORRUPTION_DAMAGE = 146739;
        public const uint CURSE_OF_DOOM_EFFECT = 18662;
        public const uint COMPOUNDING_HORROR = 199281;
        public const uint COMPOUNDING_HORROR_DAMAGE = 231489;
        public const uint DARK_REGENERATION = 108359;
        public const uint DARK_SOUL_INSTABILITY = 113858;
        public const uint DREADSTALKER_CHARGE = 194247;
        public const uint DARK_SOUL_KNOWLEDGE = 113861;
        public const uint DARK_SOUL_MISERY = 113860;
        public const uint UNSTABLE_AFFLICTION_RANK2 = 231791;
        public const uint UNSTABLE_AFFLICTION_ENERGIZE = 31117;
        public const uint DECIMATE_AURA = 108869;
        public const uint DEMON_SOUL_FELGUARD = 79452;
        public const uint DEMON_SOUL_FELHUNTER = 79460;
        public const uint DEMON_SOUL_IMP = 79459;
        public const uint T15_2P_BONUS = 138129;
        public const uint T15_2P_BONUS_TRIGGERED = 138483;
        public const uint DEMON_SOUL_SUCCUBUS = 79453;
        public const uint DEMON_SOUL_VOIDWALKER = 79454;
        public const uint DEMONBOLT = 157695;
        public const uint DEMONIC_CALL = 114925;
        public const uint DEMONIC_CALLING = 205145;
        public const uint DEMONIC_CALLING_TRIGGER = 205146;
        public const uint SOUL_LEECH_SHIELD = 108366;
        public const uint SHARPENED_DREADFANGS_BUFF = 215111;
        public const uint DEMONIC_EMPOWERMENT_FELGUARD = 54508;
        public const uint DEADWIND_HARVERST = 216708;
        public const uint INFERNAL_METEOR_STRIKE = 171017;
        public const uint SOUL_EFFIGY_AURA = 205247;
        public const uint DEMONIC_EMPOWERMENT_FELHUNTER = 54509;
        public const uint DEMONIC_EMPOWERMENT_IMP = 54444;
        public const uint THE_EXPANDABLES_BUFF = 211218;
        public const uint DEMONIC_EMPOWERMENT_SUCCUBUS = 54435;
        public const uint DEMONIC_EMPOWERMENT_VOIDWALKER = 54443;
        public const uint DEMONIC_GATEWAY_PERIODIC_CHARGE = 113901;
        public const uint DEMONIC_GATEWAY_SUMMON_GREEN = 113886;
        public const uint DEMONIC_GATEWAY_SUMMON_PURPLE = 113890;
        public const uint DEMONIC_GATEWAY_JUMP_GREEN = 113896;
        public const uint DEMONIC_GATEWAY_JUMP_PURPLE = 120729;
        public const uint DEMONIC_LEAP_JUMP = 109163;
        public const uint DEMONSKIN = 219272;
        public const uint DOOM_DOUBLED = 218572;
        public const uint DEMONWRATH_AURA = 193440;
        public const uint DEMONWRATH_SOULSHARD = 194379;
        public const uint DESTRUCTION_PASSIVE = 137046;
        public const uint DISRUPTED_NETHER = 114736;
        public const uint DOOM = 603;
        public const uint DOOM_SOUL_SHARD = 193318;
        public const uint DRAIN_LIFE_HEAL = 89653;
        public const uint ERADICATION = 196412;
        public const uint ERADICATION_DEBUFF = 196414;
        public const uint EYE_LASER = 205231;
        public const uint FEAR = 5782;
        public const uint FEAR_BUFF = 118699;
        public const uint FEAR_EFFECT = 118699;
        public const uint FEL_FIREBOLT = 104318;
        public const uint FEL_SYNERGY_HEAL = 54181;
        public const uint FIRE_AND_BRIMSTONE = 196408;
        public const uint GLYPH_OF_CONFLAGRATE = 56235;
        public const uint GLYPH_OF_HEALTHSTONE = 56224;
        public const uint GLYPH_OF_FEAR = 56244;
        public const uint GLYPH_OF_FEAR_EFFECT = 130616;
        public const uint GLYPH_OF_SHADOWFLAME = 63311;
        public const uint SOUL_FLAME_PROC = 199581;
        public const uint GLYPH_OF_SIPHON_LIFE = 63106;
        public const uint GLYPH_OF_SOULWELL = 58094;
        public const uint GLYPH_OF_SOULWELL_VISUAL = 34145;
        public const uint GRIMOIRE_FELGUARD = 111898;
        public const uint GRIMOIRE_FELHUNTER = 111897;
        public const uint GRIMOIRE_IMP = 111859;
        public const uint GRIMOIRE_OF_SACRIFICE = 108503;
        public const uint SEED_OF_CORRUPTION = 27243;
        public const uint LORD_OF_THE_FLAMES_CD = 226802;
        public const uint LORD_OF_THE_FLAMES = 224103;
        public const uint LORD_OF_THE_FLAMES_SUMMON = 224105;
        public const uint GRIMOIRE_OF_SYNERGY_BUFF = 171982;
        public const uint GRIMOIRE_SUCCUBUS = 111896;
        public const uint HAND_OF_GULDAN = 105174;
        public const uint GRIMOIRE_VOIDWALKER = 111895;
        public const uint HAND_OF_DOOM = 196283;
        public const uint HAND_OF_GULDAN_DAMAGE = 86040;
        public const uint HAND_OF_GULDAN_SUMMON = 196282;
        public const uint HARVEST_LIFE_HEAL = 125314;
        public const uint HAUNT = 48181;
        public const uint SHADOWBURN = 17877;
        public const uint HAVOC = 80240;
        public const uint HEALTH_FUNNEL_HEAL = 217979;
        public const uint FIREBOLT_BONUS = 231795;
        public const uint ROT_AND_DECAY = 212371;
        public const uint IMMOLATE = 348;
        public const uint SOULBURN = 74434;
        public const uint SOULBURN_UNENDING_BREATH = 104242;
        public const uint SOULBURN_DEMONIC_CIRCLE = 79438;
        public const uint IMMOLATE_DOT = 157736;
        public const uint DEMONIC_GATEWAY_DEBUFF = 113942;
        public const uint IMMOLATE_FIRE_AND_BRIMSTONE = 108686;
        public const uint IMMOLATE_PROC = 193541;
        public const uint WRATH_OF_CONSUMPTION_PROC = 199646;
        public const uint IMPENDING_DOOM = 196270;
        public const uint IMPENDING_DOOM_SUMMON = 196271;
        public const uint IMPLOSION_DAMAGE = 196278;
        public const uint IMPLOSION_JUMP = 205205;
        public const uint IMPLOSION = 196277;
        public const uint DOOMGUARD_SHADOW_LOCK = 171138;
        public const uint NIGHTFALL_BUFF = 264571;
        public const uint IMPROVED_DREADSTALKERS = 196272;
        public const uint IMPROVED_SOUL_FIRE_PCT = 85383;
        public const uint IMPROVED_SOUL_FIRE_STATE = 85385;
        public const uint INCINERATE = 29722;
        public const uint ITEM_S12_TIER_4 = 131632;
        public const uint KIL_JAEDENS_CUNNING_PASSIVE = 108507;
        public const uint LIFE_TAP_ENERGIZE = 31818;
        public const uint LIFE_TAP_ENERGIZE_2 = 32553;
        public const uint METAMORPHOSIS = 103958;
        public const uint MOLTEN_CORE = 122355;
        public const uint MOLTEN_CORE_AURA = 122351;
        public const uint NETHER_TALENT = 91713;
        public const uint NETHER_WARD = 91711;
        public const uint DEMONIC_GATEWAY_VISUAL = 113900;
        public const uint NIGHTFALL = 108558;
        public const uint PHANTOMATIC_SINGULARITY = 205179;
        public const uint PHANTOMATIC_SINGULARITY_DAMAGE = 205246;
        public const uint SHADOWBOLT = 686;
        public const uint POWER_TRIP = 196605;
        public const uint POWER_TRIP_ENERGIZE = 216125;
        public const uint PYROCLASM = 123686;
        public const uint SEED_OF_CORRUPTION_DETONATION = 27285;
        public const uint INTERNAL_COMBUSTION_DMG = 266136;
        public const uint ROARING_BLAZE = 205184;
        public const uint SEED_OF_CURRUPTION = 27243;
        public const uint SEED_OF_CURRUPTION_DAMAGE = 27285;
        public const uint T16_4P_INTERNAL_CD = 145165;
        public const uint SHADOW_BOLT = 686;
        public const uint SHADOW_BOLT_SHOULSHARD = 194192;
        public const uint SHADOW_TRANCE = 17941;
        public const uint SHADOW_WARD = 6229;
        public const uint SHADOWBURN_ENERGIZE = 125882;
        public const uint SHADOWY_INSPIRATION = 196269;
        public const uint SHADOWY_INSPIRATION_EFFECT = 196606;
        public const uint SHIELD_OF_SHADOW = 115232;
        public const uint SOULSHATTER_ENERGIZE = 212921;
        public const uint SOULSHATTER_HASTE = 236471;
        public const uint SIPHON_LIFE_HEAL = 63106;
        public const uint CASTING_CIRCLE = 221703;
        public const uint SOUL_CONDUIT_REFUND = 215942;
        public const uint SOUL_LEECH = 228974;
        public const uint SOUL_LEECH_ABSORB = 108366;
        public const uint SOUL_LEECH_AURA = 108370;
        public const uint SOUL_LINK_DUMMY_AURA = 108446;
        public const uint SOULSHATTER = 32835;
        public const uint DIMENSIONAL_RIFT = 196586;
        public const uint SOULSNATCHER_PROC = 196234;
        public const uint SOULWELL_CREATE_HEALTHSTONE = 34130;
        public const uint METAMORPHOSIS_SPELL_REPLACEMENTS = 103965;
        public const uint METAMORPHOSIS_ADDITIONAL_AURA = 54879;
        public const uint METAMORPHOSIS_ADDITIONAL_AURA_2 = 54817;
        public const uint SOW_THE_SEEDS = 196226;
        public const uint SPAWN_PURPLE_DEMONIC_GATEWAY = 113890;
        public const uint SUMMON_DREADSTALKER = 193332;
        public const uint SUPPLANT_DEMONIC_COMMAND = 119904;
        public const uint THREATENING_PRESENCE = 112042;
        public const uint TWILIGHT_WARD_METAMORPHOSIS_S12 = 131624;
        public const uint TWILIGHT_WARD_S12 = 131623;
        public const uint UNSTABLE_AFFLICTION_DAMAGE_1 = 233490;
        public const uint UNSTABLE_AFFLICTION_DAMAGE_2 = 233496;
        public const uint SOUL_FIRE = 6353;
        public const uint SOUL_FIRE_METAMORPHOSIS = 104027;
        public const uint SINGE_MAGIC = 212620;
        public const uint UNSTABLE_AFFLICTION_DAMAGE_3 = 233497;
        public const uint UNSTABLE_AFFLICTION_DAMAGE_4 = 233498;
        public const uint UNSTABLE_AFFLICTION_DAMAGE_5 = 233499;
        public const uint INTERNAL_COMBUSTION_TALENT_AURA = 266134;
        public const uint WRITHE_IN_AGONY = 196102;
        public const uint ERADICATION_AURA = 196412;
        public const uint FATAL_ECHOES = 199257;
        public const uint DOOM_ENERGIZE = 193318;
        public const uint IMP_CAUTERIZE_MASTER = 119899;
        public const uint ERADICATION_DEBUF = 196414;
        public const uint SWEET_SOULS = 199220;
        public const uint SWEET_SOULS_HEAL = 199221;
        public const uint PVP_4P_BONUS = 143395;
        public const uint SHADOW_EMBRACE = 32388;
        public const uint SHADOW_EMBRACE_TARGET_DEBUFF = 32390;
        public const uint CreateHealthstone = 23517;
        public const uint DemonicCircleAllowCast = 62388;
        public const uint DemonicCircleSummon = 48018;
        public const uint DemonicCircleTeleport = 48020;
        public const uint DevourMagicHeal = 19658;
        public const uint DrainSoulEnergize = 205292;
        public const uint GlyphOfDemonTraining = 56249;
        public const uint GlyphOfSoulSwap = 56226;
        public const uint GlyphOfSuccubus = 56250;
        public const uint ImmolatePeriodic = 157736;
        public const uint ImprovedHealthFunnelBuffR1 = 60955;
        public const uint ImprovedHealthFunnelBuffR2 = 60956;
        public const uint ImprovedHealthFunnelR1 = 18703;
        public const uint ImprovedHealthFunnelR2 = 18704;
        public const uint RainOfFire = 5740;
        public const uint RainOfFireDamage = 42223;
        public const uint SeedOfCorruptionDamage = 27285;
        public const uint SeedOfCorruptionGeneric = 32865;
        public const uint ShadowBoltEnergize = 194192;
        public const uint Soulshatter = 32835;
        public const uint SoulSwapCdMarker = 94229;
        public const uint SoulSwapOverride = 86211;
        public const uint SoulSwapModCost = 92794;
        public const uint SoulSwapDotMarker = 92795;
        public const uint UnstableAffliction = 30108;
        public const uint UnstableAfflictionDispel = 31117;
        public const uint Shadowflame = 37378;
        public const uint Flameshadow = 37379;
        public const uint GenReplenishment = 57669;
        public const uint PriestShadowWordDeath = 32409;

        public const uint NPC_WARLOCK_DEMONIC_GATEWAY_PURPLE = 59271;
        public const uint NPC_WARLOCK_DEMONIC_GATEWAY_GREEN = 59262;
        // pets
        public const uint NPC_WARLOCK_PET_IMP = 416;
        public const uint NPC_WARLOCK_PET_FEL_IMP = 58959;
        public const uint NPC_WARLOCK_PET_VOIDWALKER = 1860;
        public const uint NPC_WARLOCK_PET_VOIDLORD = 58960;
        public const uint NPC_WARLOCK_PET_SUCCUBUS = 1863;
        public const uint NPC_WARLOCK_PET_SHIVARRA = 58963;
        public const uint NPC_WARLOCK_PET_FEL_HUNTER = 417;
        public const uint NPC_WARLOCK_PET_OBSERVER = 58964;
        public const uint NPC_WARLOCK_PET_FELGUARD = 17252;
        public const uint NPC_WARLOCK_PET_WRATHGUARD = 58965;
    }

    [Script] // 710 - Banish
    class spell_warl_banish : SpellScript
    {
        void HandleBanish(SpellMissInfo missInfo)
        {
            if (missInfo != SpellMissInfo.Immune)
                return;

            Unit target = GetHitUnit();
            if (target)
            {
                // Casting Banish on a banished target will Remove applied aura
                Aura banishAura = target.GetAura(GetSpellInfo().Id, GetCaster().GetGUID());
                if (banishAura != null)
                    banishAura.Remove();
            }
        }

        public override void Register()
        {
            BeforeHit.Add(new BeforeHitHandler(HandleBanish));
        }
    }

    [Script] // 116858 - Chaos Bolt
    class spell_warl_chaos_bolt : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleDummy(uint effIndex)
        {
            SetHitDamage(GetHitDamage() + MathFunctions.CalculatePct(GetHitDamage(), GetCaster().ToPlayer().m_activePlayerData.SpellCritPercentage));
        }

        void CalcCritChance(Unit victim, ref float critChance)
        {
            critChance = 100.0f;
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.SchoolDamage));
            OnCalcCritChance.Add(new OnCalcCritChanceHandler(CalcCritChance));
        }
    }

    [Script] // 77220 - Mastery: Chaotic Energies
    class spell_warl_chaotic_energies : AuraScript
    {
        void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            AuraEffect auraEffect = GetEffect(1);
            if (auraEffect == null || !GetTargetApplication().HasEffect(1))
            {
                PreventDefaultAction();
                return;
            }

            // You take ${$s2/3}% reduced damage
            float damageReductionPct = (float)auraEffect.GetAmount() / 3;
            // plus a random amount of up to ${$s2/3}% additional reduced damage
            damageReductionPct += RandomHelper.FRand(0.0f, damageReductionPct);

            absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), damageReductionPct);
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new EffectAbsorbHandler(HandleAbsorb, 2));
        }
    }

    [Script] // 6201 - Create Healthstone
    class spell_warl_create_healthstone : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CreateHealthstone);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleScriptEffect(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.CreateHealthstone, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 48018 - Demonic Circle: Summon
    class spell_warl_demonic_circle_summon : AuraScript
    {
        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // If effect is Removed by expire Remove the summoned demonic circle too.
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Reapply))
                GetTarget().RemoveGameObject(GetId(), true);

            GetTarget().RemoveAura(SpellIds.DemonicCircleAllowCast);
        }

        void HandleDummyTick(AuraEffect aurEff)
        {
            GameObject circle = GetTarget().GetGameObject(GetId());
            if (circle)
            {
                // Here we check if player is in demonic circle teleport range, if so add
                // WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST; allowing him to cast the WARLOCK_DEMONIC_CIRCLE_TELEPORT.
                // If not in range Remove the WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST.

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.DemonicCircleTeleport, GetCastDifficulty());

                if (GetTarget().IsWithinDist(circle, spellInfo.GetMaxRange(true)))
                {
                    if (!GetTarget().HasAura(SpellIds.DemonicCircleAllowCast))
                        GetTarget().CastSpell(GetTarget(), SpellIds.DemonicCircleAllowCast, true);
                }
                else
                    GetTarget().RemoveAura(SpellIds.DemonicCircleAllowCast);
            }
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 48020 - Demonic Circle: Teleport
    class spell_warl_demonic_circle_teleport : AuraScript
    {
        void HandleTeleport(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetTarget().ToPlayer();
            if (player)
            {
                GameObject circle = player.GetGameObject(SpellIds.DemonicCircleSummon);
                if (circle)
                {
                    player.NearTeleportTo(circle.GetPositionX(), circle.GetPositionY(), circle.GetPositionZ(), circle.GetOrientation());
                    player.RemoveMovementImpairingAuras(false);
                }
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleTeleport, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 67518, 19505 - Devour Magic
    class spell_warl_devour_magic : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfDemonTraining, SpellIds.DevourMagicHeal) && spellInfo.GetEffects().Count > 1;
        }

        void OnSuccessfulDispel(uint effIndex)
        {
            Unit caster = GetCaster();
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, GetEffectInfo(1).CalcValue(caster));

            caster.CastSpell(caster, SpellIds.DevourMagicHeal, args);

            // Glyph of Felhunter
            Unit owner = caster.GetOwner();
            if (owner)
                if (owner.GetAura(SpellIds.GlyphOfDemonTraining) != null)
                    owner.CastSpell(owner, SpellIds.DevourMagicHeal, args);
        }

        public override void Register()
        {
            OnEffectSuccessfulDispel.Add(new EffectHandler(OnSuccessfulDispel, 0, SpellEffectName.Dispel));
        }
    }

    [Script] // 198590 - Drain Soul
    class spell_warl_drain_soul : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DrainSoulEnergize);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
                return;

            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(caster, SpellIds.DrainSoulEnergize, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 48181 - Haunt
    class spell_warl_haunt : SpellScript
    {
        void HandleAfterHit()
        {
            Aura aura = GetHitAura();
            if (aura != null)
            {
                AuraEffect aurEff = aura.GetEffect(1);
                if (aurEff != null)
                    aurEff.SetAmount(MathFunctions.CalculatePct(GetHitDamage(), aurEff.GetAmount()));
            }
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleAfterHit));
        }
    }

    [Script] // 755 - Health Funnel
    class spell_warl_health_funnel : AuraScript
    {
        void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (!caster)
                return;

            Unit target = GetTarget();
            if (caster.HasAura(SpellIds.ImprovedHealthFunnelR2))
                target.CastSpell(target, SpellIds.ImprovedHealthFunnelBuffR2, true);
            else if (caster.HasAura(SpellIds.ImprovedHealthFunnelR1))
                target.CastSpell(target, SpellIds.ImprovedHealthFunnelBuffR1, true);
        }

        void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveAurasDueToSpell(SpellIds.ImprovedHealthFunnelBuffR1);
            target.RemoveAurasDueToSpell(SpellIds.ImprovedHealthFunnelBuffR2);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (!caster)
                return;
            //! HACK for self damage, is not blizz :/
            uint damage = (uint)caster.CountPctFromMaxHealth(aurEff.GetBaseAmount());

            Player modOwner = caster.GetSpellModOwner();
            if (modOwner)
                modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.PowerCost0, ref damage);

            SpellNonMeleeDamage damageInfo = new(caster, caster, GetSpellInfo(), GetAura().GetSpellVisual(), GetSpellInfo().SchoolMask, GetAura().GetCastId());
            damageInfo.periodicLog = true;
            damageInfo.damage = damage;
            caster.DealSpellDamage(damageInfo, false);
            caster.SendSpellNonMeleeDamageLog(damageInfo);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(ApplyEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(RemoveEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.ObsModHealth));
        }
    }

    [Script] // 6262 - Healthstone
    class spell_warl_healthstone_heal : SpellScript
    {
        void HandleOnHit()
        {
            int heal = (int)MathFunctions.CalculatePct(GetCaster().GetCreateHealth(), GetHitHeal());
            SetHitHeal(heal);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleOnHit));
        }
    }

    [SpellScript(348)] // 348 - Immolate
    class spell_warl_immolate : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ImmolatePeriodic);
        }

        void HandleOnEffectHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ImmolatePeriodic, GetSpell());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleOnEffectHit, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 6358 - Seduction (Special Ability)
    class spell_warl_seduction : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfSuccubus, SpellIds.PriestShadowWordDeath);
        }

        void HandleScriptEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target)
            {
                if (caster.GetOwner() && caster.GetOwner().HasAura(SpellIds.GlyphOfSuccubus))
                {
                    target.RemoveAurasByType(AuraType.PeriodicDamage, ObjectGuid.Empty, target.GetAura(SpellIds.PriestShadowWordDeath)); // SW:D shall not be Removed.
                    target.RemoveAurasByType(AuraType.PeriodicDamagePercent);
                    target.RemoveAurasByType(AuraType.PeriodicLeech);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script] // 27285 - Seed of Corruption
    class spell_warl_seed_of_corruption : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            if (GetExplTargetUnit())
                targets.Remove(GetExplTargetUnit());
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        }
    }

    [Script] // 27243 - Seed of Corruption
    class spell_warl_seed_of_corruption_dummy : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SeedOfCorruptionDamage);
        }

        void CalculateBuffer(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            if (caster == null)
                return;

            amount = caster.SpellBaseDamageBonusDone(GetSpellInfo().GetSchoolMask()) * GetEffectInfo(0).CalcValue(caster) / 100;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            int amount = (int)(aurEff.GetAmount() - damageInfo.GetDamage());
            if (amount > 0)
            {
                aurEff.SetAmount(amount);
                if (!GetTarget().HealthBelowPctDamaged(1, damageInfo.GetDamage()))
                    return;
            }

            Remove();

            Unit caster = GetCaster();
            if (!caster)
                return;

            caster.CastSpell(eventInfo.GetActionTarget(), SpellIds.SeedOfCorruptionDamage, true);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateBuffer, 2, AuraType.Dummy));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 2, AuraType.Dummy));
        }
    }

    // 32863 - Seed of Corruption
    // 36123 - Seed of Corruption
    // 38252 - Seed of Corruption
    // 39367 - Seed of Corruption
    // 44141 - Seed of Corruption
    // 70388 - Seed of Corruption
    [Script] // Monster spells, triggered only on amount drop (not on death)
    class spell_warl_seed_of_corruption_generic : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SeedOfCorruptionGeneric);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            int amount = aurEff.GetAmount() - (int)damageInfo.GetDamage();
            if (amount > 0)
            {
                aurEff.SetAmount(amount);
                return;
            }

            Remove();

            Unit caster = GetCaster();
            if (!caster)
                return;

            caster.CastSpell(eventInfo.GetActionTarget(), SpellIds.SeedOfCorruptionGeneric, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    [SpellScript(686)] // 686 - Shadow Bolt
    class spell_warl_shadow_bolt : SpellScript, IAfterCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShadowBoltEnergize);
        }

        public void AfterCast()
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.ShadowBoltEnergize, true);
        }
    }

    [Script] // 86121 - Soul Swap
    class spell_warl_soul_swap : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfSoulSwap, SpellIds.SoulSwapCdMarker, SpellIds.SoulSwapOverride);
        }

        void HandleHit(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.SoulSwapOverride, true);
            GetHitUnit().CastSpell(GetCaster(), SpellIds.SoulSwapDotMarker, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 86211 - Soul Swap - Also acts as a dot container
    public class spell_warl_soul_swap_override : AuraScript
    {
        //! Forced to, pure virtual functions must have a body when linking
        public override void Register() { }

        public void AddDot(uint id) { _dotList.Add(id); }
        public List<uint> GetDotList() { return _dotList; }
        public Unit GetOriginalSwapSource() { return _swapCaster; }
        public void SetOriginalSwapSource(Unit victim) { _swapCaster = victim; }
        List<uint> _dotList = new();
        Unit _swapCaster;
    }

    [Script] //! Soul Swap Copy Spells - 92795 - Simply copies spell IDs.
    class spell_warl_soul_swap_dot_marker : SpellScript
    {
        void HandleHit(uint effIndex)
        {
            Unit swapVictim = GetCaster();
            Unit warlock = GetHitUnit();
            if (!warlock || !swapVictim)
                return;

            var appliedAuras = swapVictim.GetAppliedAuras();
            spell_warl_soul_swap_override swapSpellScript = null;
            Aura swapOverrideAura = warlock.GetAura(SpellIds.SoulSwapOverride);
            if (swapOverrideAura != null)
                swapSpellScript = swapOverrideAura.GetScript<spell_warl_soul_swap_override>();

            if (swapSpellScript == null)
                return;

            FlagArray128 classMask = GetEffectInfo().SpellClassMask;

            foreach (var itr in appliedAuras)
            {
                SpellInfo spellProto = itr.Value.GetBase().GetSpellInfo();
                if (itr.Value.GetBase().GetCaster() == warlock)
                    if (spellProto.SpellFamilyName == SpellFamilyNames.Warlock && (spellProto.SpellFamilyFlags & classMask))
                        swapSpellScript.AddDot(itr.Key);
            }

            swapSpellScript.SetOriginalSwapSource(swapVictim);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy));
        }
    }

    [SpellScript(86213)] // 86213 - Soul Swap Exhale
    class spell_warl_soul_swap_exhale : SpellScript, ICheckCastHander
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SoulSwapModCost, SpellIds.SoulSwapOverride);
        }

        public SpellCastResult CheckCast()
        {
            Unit currentTarget = GetExplTargetUnit();
            Unit swapTarget = null;
            Aura swapOverride = GetCaster().GetAura(SpellIds.SoulSwapOverride);
            if (swapOverride != null)
            {
                spell_warl_soul_swap_override swapScript = swapOverride.GetScript<spell_warl_soul_swap_override>();
                if (swapScript != null)
                    swapTarget = swapScript.GetOriginalSwapSource();
            }

            // Soul Swap Exhale can't be cast on the same target than Soul Swap
            if (swapTarget && currentTarget && swapTarget == currentTarget)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        void onEffectHit(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.SoulSwapModCost, true);
            bool hasGlyph = GetCaster().HasAura(SpellIds.GlyphOfSoulSwap);

            List<uint> dotList = new();
            Unit swapSource = null;
            Aura swapOverride = GetCaster().GetAura(SpellIds.SoulSwapOverride);
            if (swapOverride != null)
            {
                spell_warl_soul_swap_override swapScript = swapOverride.GetScript<spell_warl_soul_swap_override>();
                if (swapScript == null)
                    return;
                dotList = swapScript.GetDotList();
                swapSource = swapScript.GetOriginalSwapSource();
            }

            if (dotList.Empty())
                return;

            foreach (var itr in dotList)
            {
                GetCaster().AddAura(itr, GetHitUnit());
                if (!hasGlyph && swapSource)
                    swapSource.RemoveAurasDueToSpell(itr);
            }

            // Remove Soul Swap Exhale buff
            GetCaster().RemoveAurasDueToSpell(SpellIds.SoulSwapOverride);

            if (hasGlyph) // Add a cooldown on Soul Swap if caster has the glyph
                GetCaster().CastSpell(GetCaster(), SpellIds.SoulSwapCdMarker, false);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(onEffectHit, 0, SpellEffectName.SchoolDamage));
        }
    }

    [SpellScript(29858)] // 29858 - Soulshatter
    class spell_warl_soulshatter : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Soulshatter);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target)
                if (target.CanHaveThreatList() && target.GetThreatManager().GetThreat(caster) > 0.0f)
                    caster.CastSpell(target, SpellIds.Soulshatter, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [SpellScript(37377, "spell_warl_t4_2p_bonus_shadow", SpellIds.Flameshadow)]// 37377 - Shadowflame
    [SpellScript(39437, "spell_warl_t4_2p_bonus_fire", SpellIds.Shadowflame)]// 39437 - Shadowflame Hellfire and RoF
    class spell_warl_t4_2p_bonus : AuraScript
    {
        public spell_warl_t4_2p_bonus(uint triggerSpell)
        {
            _triggerSpell = triggerSpell;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_triggerSpell);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();
            caster.CastSpell(caster, _triggerSpell, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        uint _triggerSpell;
    }

    [SpellScript(new uint[] { 30108, 34438, 34439, 35183 })] // 30108, 34438, 34439, 35183 - Unstable Affliction
    class spell_warl_unstable_affliction : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.UnstableAfflictionDispel);
        }

        void HandleDispel(DispelInfo dispelInfo)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                AuraEffect aurEff = GetEffect(1);
                if (aurEff != null)
                {
                    // backfire damage and silence
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount() * 9);
                    caster.CastSpell(dispelInfo.GetDispeller(), SpellIds.UnstableAfflictionDispel, args);
                }
            }
        }

        public override void Register()
        {
            AfterDispel.Add(new AuraDispelHandler(HandleDispel));
        }
    }

    [SpellScript(5740)] // 5740 - Rain of Fire Updated 7.1.5
    class spell_warl_rain_of_fire : AuraScript
    {
        void HandleDummyTick(AuraEffect aurEff)
        {
            List<AreaTrigger> rainOfFireAreaTriggers = GetTarget().GetAreaTriggers(SpellIds.RainOfFire);
            List<ObjectGuid> targetsInRainOfFire = new();

            foreach (AreaTrigger rainOfFireAreaTrigger in rainOfFireAreaTriggers)
            {
                var insideTargets = rainOfFireAreaTrigger.GetInsideUnits();
                targetsInRainOfFire.AddRange(insideTargets);
            }

            foreach (ObjectGuid insideTargetGuid in targetsInRainOfFire)
            {
                Unit insideTarget = Global.ObjAccessor.GetUnit(GetTarget(), insideTargetGuid);
                if (insideTarget)
                    if (!GetTarget().IsFriendlyTo(insideTarget))
                        GetTarget().CastSpell(insideTarget, SpellIds.RainOfFireDamage, true);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 3, AuraType.PeriodicDummy));
        }
    }

    // Grimoire of Service - 108501
    [SpellScript(108501)]
    class spell_warl_grimoire_of_service_aura : AuraScript
    {
        public void Handlearn(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player player = GetCaster().ToPlayer();
            if (GetCaster().ToPlayer())
            {
                player.LearnSpell(SpellIds.GRIMOIRE_IMP, false);
                player.LearnSpell(SpellIds.GRIMOIRE_VOIDWALKER, false);
                player.LearnSpell(SpellIds.GRIMOIRE_SUCCUBUS, false);
                player.LearnSpell(SpellIds.GRIMOIRE_FELHUNTER, false);
                if (player.GetPrimarySpecialization() == (uint)TalentSpecialization.WarlockDemonology)
                {
                    player.LearnSpell(SpellIds.GRIMOIRE_FELGUARD, false);
                }
            }
        }
        public void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player player = GetCaster().ToPlayer();
            if (GetCaster().ToPlayer())
            {
                player.RemoveSpell(SpellIds.GRIMOIRE_IMP, false, false);
                player.RemoveSpell(SpellIds.GRIMOIRE_VOIDWALKER, false, false);
                player.RemoveSpell(SpellIds.GRIMOIRE_SUCCUBUS, false, false);
                player.RemoveSpell(SpellIds.GRIMOIRE_FELHUNTER, false, false);
                player.RemoveSpell(SpellIds.GRIMOIRE_FELGUARD, false, false);
            }
        }
        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(Handlearn, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 205179
    [SpellScript(205179)]
    public class aura_warl_phantomatic_singularity : AuraScript
    {
        public void OnTick(AuraEffect UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (GetCaster())
            {
                caster.CastSpell(GetTarget().GetPosition(), SpellIds.PHANTOMATIC_SINGULARITY_DAMAGE, true);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnTick, 0, AuraType.PeriodicLeech));
        }
    }

    // Demonic Calling - 205145
    [SpellScript(205145)]
    public class spell_warl_demonic_calling : AuraScriptLoader
    {
        public spell_warl_demonic_calling() : base("spell_warl_demonic_calling")
        {
        }

        public class spell_warl_demonic_calling_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo UnnamedParameter)
            {
                return SpellManager.Instance.GetSpellInfo(SpellIds.DEMONIC_CALLING_TRIGGER, Difficulty.None) != null;
            }

            public bool CheckProc(ProcEventInfo eventInfo)
            {
                Unit caster = GetCaster();
                if (caster == null)
                {
                    return false;
                }
                if (eventInfo.GetSpellInfo() != null && (eventInfo.GetSpellInfo().Id == SpellIds.DEMONBOLT || eventInfo.GetSpellInfo().Id == SpellIds.SHADOW_BOLT) && RandomHelper.randChance(20))
                {
                    caster.CastSpell(caster, SpellIds.DEMONIC_CALLING_TRIGGER, true);
                }
                return false;
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_warl_demonic_calling_AuraScript();
        }
    }


    // Eye Laser - 205231
    [SpellScript(205231)]
    public class spell_warl_eye_laser : SpellScriptLoader
    {
        public spell_warl_eye_laser() : base("spell_warl_eye_laser")
        {
        }

        public class spell_warl_eye_laser_SpellScript : SpellScript
        {

            public void HandleTargets(List<WorldObject> targets)
            {
                Unit caster = GetOriginalCaster();
                if (caster == null)
                {
                    return;
                }

                targets.RemoveAll(new UnitAuraCheck<WorldObject>(false, SpellIds.DOOM, caster.GetGUID()));
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(HandleTargets, 0, Targets.UnitTargetEnemy));
            }
        }

        //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
        //ORIGINAL LINE: SpellScript* GetSpellScript() const override
        public override SpellScript GetSpellScript()
        {
            return new spell_warl_eye_laser_SpellScript();
        }
    }

    // Grimoire of Synergy - 171975
    [SpellScript(171975, "spell_warl_grimoire_of_synergy")]
    public class spell_warl_grimoire_of_synergy : SpellScriptLoader
    {
        public spell_warl_grimoire_of_synergy() : base("spell_warl_grimoire_of_synergy")
        {
        }

        public class spell_warl_grimoire_of_synergy_SpellScript : SpellScript, IOnCast
        {

            public void OnCast()
            {
                Unit caster = GetCaster();
                if (caster == null)
                {
                    return;
                }

                Player player = caster.ToPlayer();

                if (caster.ToPlayer())
                {
                    Guardian pet = player.GetGuardianPet();
                    player.AddAura(GetSpellInfo().Id, player);

                    if (pet != null)
                        player.AddAura(GetSpellInfo().Id, pet);
                }

            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_warl_grimoire_of_synergy_SpellScript();
        }
    }


    // Grimoire of Synergy - 171975
    [SpellScript(171975, "spell_warl_grimoire_of_synergy")]
    public class aura_warl_grimoire_of_synergy : AuraScriptLoader
    {
        public aura_warl_grimoire_of_synergy() : base("spell_warl_grimoire_of_synergy")
        {
        }

        public class spell_warl_grimoire_of_synergy_AuraScript : AuraScript
        {
            public bool CheckProc(ProcEventInfo eventInfo)
            {
                Unit actor = eventInfo.GetActor();
                if (actor == null)
                {
                    return false;
                }
                if (actor.IsPet() || actor.IsGuardian())
                {
                    Unit owner = actor.GetOwner();
                    if (owner == null)
                    {
                        return false;
                    }
                    if (RandomHelper.randChance(10))
                    {
                        owner.CastSpell(owner, SpellIds.GRIMOIRE_OF_SYNERGY_BUFF, true);
                    }
                    return true;
                }

                Player player = actor.ToPlayer();

                if (actor.ToPlayer())
                {
                    Guardian guardian = player.GetGuardianPet();
                    if (guardian == null)
                    {
                        return false;
                    }
                    if (RandomHelper.randChance(10))
                    {
                        player.CastSpell(guardian, SpellIds.GRIMOIRE_OF_SYNERGY_BUFF, true);
                    }
                    return true;
                }
                return false;
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
            }
        }

        //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
        //ORIGINAL LINE: AuraScript* GetAuraScript() const override
        public override AuraScript GetAuraScript()
        {
            return new spell_warl_grimoire_of_synergy_AuraScript();
        }
    }

    // 196277 - Implosion
    [SpellScript(SpellIds.IMPLOSION)]
    public class spell_warl_implosion : SpellScriptLoader
    {
        public spell_warl_implosion() : base("spell_warl_implosion")
        {
        }

        public class spell_warl_implosion_SpellScript : SpellScript
        {

            public void HandleHit(uint UnnamedParameter)
            {
                Unit caster = GetCaster();
                Unit target = GetHitUnit();
                if (caster == null || target == null)
                {
                    return;
                }

                List<Creature> imps = caster.GetCreatureListWithEntryInGrid(55659); // Wild Imps
                foreach (Creature imp in imps)
                {
                    if (imp.ToTempSummon().GetSummoner() == caster)
                    {
                        imp.InterruptNonMeleeSpells(false);
                        imp.VariableStorage.Set("controlled", true);
                        imp.VariableStorage.Set("ForceUpdateTimers", true);
                        imp.CastSpell(target, SpellIds.IMPLOSION_JUMP, true);
                        imp.GetMotionMaster().MoveJump(target, 300.0f, 1.0f, EventId.Jump);
                        ObjectGuid casterGuid = caster.GetGUID();
                       
                        imp.GetAI().Scheduler.Schedule(TimeSpan.FromMilliseconds(500), task =>
                        {
                            imp.CastSpell(imp, SpellIds.IMPLOSION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(casterGuid));
                            imp.DisappearAndDie();
                        });
                    }
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy));
            }
        }

        //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
        //ORIGINAL LINE: SpellScript* GetSpellScript() const override
        public override SpellScript GetSpellScript()
        {
            return new spell_warl_implosion_SpellScript();
        }
    }


    public class ImplosionDamageEvent : BasicEvent
    {
        public ImplosionDamageEvent(Unit caster, Unit target)
        {
            this._caster = caster;
            this._target = target;
        }

        public override bool Execute(ulong UnnamedParameter, uint UnnamedParameter2)
        {
            if (_caster && _target)
            {
                _caster.CastSpell(_target, SpellIds.IMPLOSION_DAMAGE, true);
                _target.ToCreature().DisappearAndDie();
            }
            return true;
        }
        private Unit _caster;
        private Unit _target;
    }


    // Grimoire of Service summons - 111859, 111895, 111896, 111897, 111898
    [SpellScript(new uint[] { 111859, 111895, 111896, 111897, 111898 })]
    public class spell_warl_grimoire_of_service : SpellScriptLoader
    {
        public spell_warl_grimoire_of_service() : base("spell_warl_grimoire_of_service")
        {
        }

        public override bool IsDatabaseBound()
        {
            return false;
        }

        public class spell_warl_grimoire_of_service_SpellScript : SpellScript, IOnSummon
        {
            public enum eServiceSpells
            {
                SPELL_IMP_SINGE_MAGIC = 89808,
                SPELL_VOIDWALKER_SUFFERING = 17735,
                SPELL_SUCCUBUS_SEDUCTION = 6358,
                SPELL_FELHUNTER_SPELL_LOCK = 19647,
                SPELL_FELGUARD_AXE_TOSS = 89766
            }

            public override bool Validate(SpellInfo UnnamedParameter)
            {
                return SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_FELGUARD_AXE_TOSS, Difficulty.None) != null ||
                        SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_FELHUNTER_SPELL_LOCK, Difficulty.None) != null ||
                        SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_IMP_SINGE_MAGIC, Difficulty.None) != null ||
                        SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_SUCCUBUS_SEDUCTION, Difficulty.None) != null ||
                        SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_VOIDWALKER_SUFFERING, Difficulty.None) != null;
            }

            public void HandleSummon(Creature creature)
            {
                Unit caster = GetCaster();
                Unit target = GetExplTargetUnit();
                if (caster == null || creature == null || target == null)
                {
                    return;
                }

                switch (GetSpellInfo().Id)
                {
                    case SpellIds.GRIMOIRE_IMP: // Imp
                        creature.CastSpell(caster, (uint)eServiceSpells.SPELL_IMP_SINGE_MAGIC, true);
                        break;
                    case SpellIds.GRIMOIRE_VOIDWALKER: // Voidwalker
                        creature.CastSpell(target, (uint)eServiceSpells.SPELL_VOIDWALKER_SUFFERING, true);
                        break;
                    case SpellIds.GRIMOIRE_SUCCUBUS: // Succubus
                        creature.CastSpell(target, (uint)eServiceSpells.SPELL_SUCCUBUS_SEDUCTION, true);
                        break;
                    case SpellIds.GRIMOIRE_FELHUNTER: // Felhunter
                        creature.CastSpell(target, (uint)eServiceSpells.SPELL_FELHUNTER_SPELL_LOCK, true);
                        break;
                    case SpellIds.GRIMOIRE_FELGUARD: // Felguard
                        creature.CastSpell(target, (uint)eServiceSpells.SPELL_FELGUARD_AXE_TOSS, true);
                        break;
                }
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_warl_grimoire_of_service_SpellScript();
        }
    }

}


