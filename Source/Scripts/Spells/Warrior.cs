// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    internal struct SpellIds
    {
        public const uint BLADESTORM_PERIODIC_WHIRLWIND = 50622;
        public const uint BLOODTHIRST_HEAL = 117313;
        public const uint CHARGE = 34846;
        public const uint COLOSSUS_SMASH_BUFF = 208086;
        public const uint CHARGE_EFFECT = 218104;
        public const uint CHARGE_EFFECT_BLAZING_TRAIL = 198337;
        public const uint CHARGE_PAUSE_RAGE_DECAY = 109128;
        public const uint CHARGE_ROOT_EFFECT = 105771;
        public const uint CHARGE_SLOW_EFFECT = 236027;
        public const uint COLOSSUS_SMASH = 167105;
        public const uint UNCHACKLED_FURY = 76856;
        public const uint COLOSSUS_SMASH_EFFECT = 208086;
        public const uint EXECUTE = 20647;
        public const uint GLYPH_OF_THE_BLAZING_TRAIL = 123779;
        public const uint GLYPH_OF_HEROIC_LEAP = 159708;
        public const uint GLYPH_OF_HEROIC_LEAP_BUFF = 133278;
        public const uint HEROIC_LEAP_JUMP = 94954;
        public const uint IMPENDING_VICTORY = 202168;
        public const uint IMPENDING_VICTORY_HEAL = 202166;
        public const uint IMPROVED_HEROIC_LEAP = 157449;
        public const uint MORTAL_STRIKE = 12294;
        public const uint MORTAL_WOUNDS = 213667;
        public const uint RALLYING_CRY = 97463;
        public const uint SHOCKWAVE = 46968;
        public const uint SHOCKWAVE_STUN = 132168;
        public const uint STOICISM = 70845;
        public const uint STORM_BOLT_STUN = 132169;
        public const uint SWEEPING_STRIKES_EXTRA_ATTACK_1 = 12723;
        public const uint SWEEPING_STRIKES_EXTRA_ATTACK_2 = 26654;
        public const uint TAUNT = 355;
        public const uint TRAUMA_EFFECT = 215537;
        public const uint WAR_MACHINE_AURA = 215566;
        public const uint WAR_MACHINE = 262231;
        public const uint SPELL_WARRRIOR_WAR_MACHINE_BUFF = 262232;
        public const uint VICTORIOUS = 32216;
        public const uint VICTORY_RUSH_HEAL = 118779;
        public const uint SIEGEBREAKER_BUFF = 280773;
        public const uint ALLOW_RAGING_BLOW = 131116;
        public const uint ANGER_MANAGEMENT = 152278;
        public const uint BERZERKER_RAGE_EFFECT = 23691;
        public const uint BLOODTHIRST = 23885;
        public const uint BLOODTHIRST_DAMAGE = 23881;
        public const uint BLOOD_AND_THUNDER = 84615;
        public const uint BOUNDING_STRIDE = 202163;
        public const uint BOUNDING_STRIDE_SPEED = 202164;
        public const uint DEEP_WOUNDS = 115767;
        public const uint DEEP_WOUNDS_PERIODIC = 12721;
        public const uint DEEP_WOUNDS_RANK_1 = 12162;
        public const uint DEEP_WOUNDS_RANK_2 = 12850;
        public const uint DEEP_WOUNDS_RANK_3 = 12868;
        public const uint DEEP_WOUNDS_RANK_PERIODIC = 12721;
        public const uint DEVASTATE = 20243;
        public const uint DOUBLE_TIME = 103827;
        public const uint DRAGON_ROAR_KNOCK_BACK = 118895;
        public const uint ENRAGE = 184361;
        public const uint ENRAGE_AURA = 184362;
        public const uint EXECUTE_FURY = 5308;
        public const uint EXECUTE_PVP = 217955;
        public const uint FOCUSED_RAGE_ARMS = 207982;
        public const uint FOCUSED_RAGE_PROTECTION = 204488;
        public const uint FROTHING_BERSERKER = 215572;
        public const uint FURIOUS_SLASH = 100130;
        public const uint GLYPH_OF_EXECUTION = 58367;
        public const uint GLYPH_OF_HINDERING_STRIKES = 58366;
        public const uint GLYPH_OF_MORTAL_STRIKE = 58368;
        public const uint HEAVY_REPERCUSSIONS = 203177;
        public const uint HEROIC_LEAP_DAMAGE = 52174;
        public const uint HEROIC_LEAP_SPEED = 133278;
        public const uint IGNORE_PAIN = 190456;
        public const uint INTERCEPT_STUN = 105771;
        public const uint INTERVENE_TRIGGER = 147833;
        public const uint ITEM_PVP_SET_4P_BONUS = 133277;
        public const uint JUGGERNAUT_CRIT_BONUS_BUFF = 65156;
        public const uint JUGGERNAUT_CRIT_BONUS_TALENT = 64976;
        public const uint JUMP_TO_SKYHOLD_AURA = 215997;
        public const uint JUMP_TO_SKYHOLD_JUMP = 192085;
        public const uint JUMP_TO_SKYHOLD_TELEPORT = 216016;
        public const uint LAST_STAND = 12975;
        public const uint LAST_STAND_TRIGGERED = 12976;
        public const uint MASSACRE = 206315;
        public const uint WHIRLWIND_PASSIVE = 85739;
        public const uint MOCKING_BANNER_TAUNT = 114198;
        public const uint MORTAL_STRIKE_AURA = 12294;
        public const uint NEW_BLADESTORM = 222634;
        public const uint OLD_BLADESTORM = 227847;
        public const uint OPPORTUNITY_STRIKE_DAMAGE = 76858;
        public const uint OVERPOWER_PROC = 60503;
        public const uint RALLYING_CRY_TRIGGER = 97462;
        public const uint RAMPAGE = 184367;
        public const uint RAVAGER = 152277;
        public const uint RAVAGER_DAMAGE = 156287;
        public const uint RAVAGER_ENERGIZE = 248439;
        public const uint RAVAGER_PARRY = 227744;
        public const uint RAVAGER_SUMMON = 227876;
        public const uint REND = 94009;
        public const uint RENEWED_FURY = 202288;
        public const uint RENEWED_FURY_EFFECT = 202289;
        public const uint RETALIATION_DAMAGE = 22858;
        public const uint SEASONED_SOLDIER = 279423;
        public const uint SECOND_WIND_DAMAGED = 202149;
        public const uint SECOND_WIND_HEAL = 202147;
        public const uint SHIELD_BLOCKC_TRIGGERED = 132404;
        public const uint SHIELD_SLAM = 23922;
        public const uint SLAM = 23922;
        public const uint SLAM_ARMS = 1464;
        public const uint SLUGGISH = 129923;
        public const uint SUNDER_ARMOR = 58567;
        public const uint SWEEPING_STRIKES_EXTRA_ATTACK = 26654;
        public const uint SWORD_AND_BOARD = 199127;
        public const uint TACTICIAN_CD = 184783;
        public const uint TASTE_FOR_BLOOD = 206333;
        public const uint TASTE_FOR_BLOOD_DAMAGE_DONE = 125831;
        public const uint THUNDERSTRUCK = 199045;
        public const uint THUNDERSTRUCK_STUN = 199042;
        public const uint THUNDER_CLAP = 6343;
        public const uint TRAUMA_DOT = 215537;
        public const uint UNRELENTING_ASSAULT_RANK_1 = 46859;
        public const uint UNRELENTING_ASSAULT_RANK_2 = 46860;
        public const uint UNRELENTING_ASSAULT_TRIGGER_1 = 64849;
        public const uint UNRELENTING_ASSAULT_TRIGGER_2 = 64850;
        public const uint VENGEANCE = 76691;
        public const uint VENGEANCE_AURA = 202572;
        public const uint VENGEANCE_FOCUSED_RAGE = 202573;
        public const uint VENGEANCE_IGNORE_PAIN = 202574;
        public const uint VICTORIOUS_STATE = 32215;
        public const uint VICTORY_RUSH_DAMAGE = 34428;
        public const uint VIGILANCE_PROC = 50725;
        public const uint WAR_MACHINE_TALENT_AURA = 215556;
        public const uint WARBRINGER = 103828;
        public const uint WARBRINGER_ROOT = 105771;
        public const uint WARBRINGER_SNARE = 137637;
        public const uint WEAKENED_BLOWS = 115798;
        public const uint WHIRLWIND = 190411;
        public const uint WHIRLWIND_ARMS = 1680;
        public const uint WHIRLWIND_MAINHAND = 199667;
        public const uint WHIRLWIND_OFFHAND = 44949;
        public const uint WRECKING_BALL_EFFECT = 215570;
        public const uint COMMANDING_SHOUT = 97463;
        public const uint GLYPH_OF_MIGHTY_VICTORY = 58104;
        public const uint INTO_THE_FRAY = 202602;
        public const uint NPC_WARRIOR_RAVAGER = 76168;
        public const uint COLD_STEEL_HOT_BLOOD_MAIN = 288080;
        public const uint COLD_STEEL_HOT_BLOOD = 288085;
        public const uint COLD_STEEL_HOT_BLOOD_GIVE_POWER = 288087;
        public const uint GUSHING_WOUND = 288091;
        // 8.0
        public const uint FURIOUS_CHARGE = 202224;
        public const uint FURIOUS_CHARGE_BUFF = 202225;
        public const uint FRESH_MEAT = 215568;
        public const uint MEAT_CLEAVER = 280392;
        public const uint THIRST_FOR_BATTLE = 199202;
        public const uint THIRST_FOR_BATTLE_BUFF = 199203;
        public const uint BARBARIAN = 280745;
        public const uint BARBARIAN_ALLOW_HEROIC_LEAP = 280746;
        public const uint BATTLE_TRANCE = 213857;
        public const uint BATTLE_TRANCE_BUFF = 213858;
        public const uint ENDLESS_RAGE = 202296;
        public const uint ENDLESS_RAGE_GIVE_POWER = 280283;
        public const uint SUDDEN_DEATH = 280721;
        public const uint SUDDEN_DEATH_PROC = 280776;
        public const uint WAR_BANNER_BUFF = 236321;
    }

    internal struct Misc
    {
        public const uint SpellVisualBlazingCharge = 26423;
    }

    //280772 - Siegebreaker
    [SpellScript(280772)]
    public class spell_warr_siegebreaker : SpellScript, IOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            caster.CastSpell(null, SpellIds.SIEGEBREAKER_BUFF, true);
        }
    }

    //197690
    [SpellScript(197690)]
    public class spell_defensive_state : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect aura, AuraEffectHandleModes auraMode)
        {
            Unit caster = GetCaster();

            if (caster != null)
            {
                AuraEffect defensiveState = caster?.GetAura(197690)?.GetEffect(0);

                if (defensiveState != null)
                    defensiveState.GetAmount();
            }
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnApply, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));
        }
    }

    [SpellScript(23881)] // 23881 - Bloodthirst
    internal class spell_warr_bloodthirst : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BLOODTHIRST_HEAL);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 3, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.BLOODTHIRST_HEAL, true);
        }
    }

    [SpellScript(100)] // 100 - Charge
    internal class spell_warr_charge : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CHARGE_EFFECT, SpellIds.CHARGE_EFFECT_BLAZING_TRAIL);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            uint spellId = SpellIds.CHARGE_EFFECT;

            if (GetCaster().HasAura(SpellIds.GLYPH_OF_THE_BLAZING_TRAIL))
                spellId = SpellIds.CHARGE_EFFECT_BLAZING_TRAIL;

            GetCaster().CastSpell(GetHitUnit(), spellId, true);
        }
    }

    [SpellScript(126661)] // 126661 - Warrior Charge Drop Fire Periodic
    internal class spell_warr_charge_drop_fire_periodic : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(DropFireVisual, 0, AuraType.PeriodicTriggerSpell));
        }

        private void DropFireVisual(AuraEffect aurEff)
        {
            PreventDefaultAction();

            if (GetTarget().IsSplineEnabled())
                for (uint i = 0; i < 5; ++i)
                {
                    int timeOffset = (int)(6 * i * aurEff.GetPeriod() / 25);
                    Vector4 loc = GetTarget().MoveSpline.ComputePosition(timeOffset);
                    GetTarget().SendPlaySpellVisual(new Position(loc.X, loc.Y, loc.Z), 0.0f, Misc.SpellVisualBlazingCharge, 0, 0, 1.0f, true);
                }
        }
    }

    // 198337 - Charge Effect (dropping Blazing Trail)
    [Script] // 218104 - Charge Effect
    internal class spell_warr_charge_effect : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CHARGE_PAUSE_RAGE_DECAY, SpellIds.CHARGE_ROOT_EFFECT, SpellIds.CHARGE_SLOW_EFFECT);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleCharge, 0, SpellEffectName.Charge, SpellScriptHookType.LaunchTarget));
        }

        private void HandleCharge(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            caster.CastSpell(caster, SpellIds.CHARGE_PAUSE_RAGE_DECAY, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 0));
            caster.CastSpell(target, SpellIds.CHARGE_ROOT_EFFECT, true);
            caster.CastSpell(target, SpellIds.CHARGE_SLOW_EFFECT, true);
        }
    }

    [Script] // 167105 - Colossus Smash 7.1.5
    internal class spell_warr_colossus_smash_SpellScript : SpellScript, IOnHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.COLOSSUS_SMASH_EFFECT);
        }

        public void OnHit()
        {
            Unit target = GetHitUnit();

            if (target)
                GetCaster().CastSpell(target, SpellIds.COLOSSUS_SMASH_EFFECT, true);
        }
    }

    [Script] // 6544 Heroic leap
    internal class spell_warr_heroic_leap : SpellScript, ICheckCastHander, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HEROIC_LEAP_JUMP);
        }

        public SpellCastResult CheckCast()
        {
            WorldLocation dest = GetExplTargetDest();

            if (dest != null)
            {
                if (GetCaster().HasUnitMovementFlag(MovementFlag.Root))
                    return SpellCastResult.Rooted;

                if (GetCaster().GetMap().Instanceable())
                {
                    float range = GetSpellInfo().GetMaxRange(true, GetCaster()) * 1.5f;

                    PathGenerator generatedPath = new(GetCaster());
                    generatedPath.SetPathLengthLimit(range);

                    bool result = generatedPath.CalculatePath(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ(), false);

                    if (generatedPath.GetPathType().HasAnyFlag(PathType.Short))
                        return SpellCastResult.OutOfRange;
                    else if (!result ||
                             generatedPath.GetPathType().HasAnyFlag(PathType.NoPath))
                        return SpellCastResult.NoPath;
                }
                else if (dest.GetPositionZ() > GetCaster().GetPositionZ() + 4.0f)
                {
                    return SpellCastResult.NoPath;
                }

                return SpellCastResult.SpellCastOk;
            }

            return SpellCastResult.NoValidTargets;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
        {
            WorldLocation dest = GetHitDest();

            if (dest != null)
                GetCaster().CastSpell(dest.GetPosition(), SpellIds.HEROIC_LEAP_JUMP, new CastSpellExtraArgs(true));
        }
    }

    [Script] // Heroic Leap (triggered by Heroic Leap (6544)) - 178368
    internal class spell_warr_heroic_leap_jump : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GLYPH_OF_HEROIC_LEAP,
                                     SpellIds.GLYPH_OF_HEROIC_LEAP_BUFF,
                                     SpellIds.IMPROVED_HEROIC_LEAP,
                                     SpellIds.TAUNT);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(AfterJump, 1, SpellEffectName.JumpDest, SpellScriptHookType.EffectHit));
        }

        private void AfterJump(uint effIndex)
        {
            if (GetCaster().HasAura(SpellIds.GLYPH_OF_HEROIC_LEAP))
                GetCaster().CastSpell(GetCaster(), SpellIds.GLYPH_OF_HEROIC_LEAP_BUFF, true);

            if (GetCaster().HasAura(SpellIds.IMPROVED_HEROIC_LEAP))
                GetCaster().GetSpellHistory().ResetCooldown(SpellIds.TAUNT, true);
        }
    }

    [Script] // 202168 - Impending Victory
    internal class spell_warr_impending_victory : SpellScript, IAfterCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IMPENDING_VICTORY_HEAL);
        }

        public void AfterCast()
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, SpellIds.IMPENDING_VICTORY_HEAL, true);
            caster.RemoveAurasDueToSpell(SpellIds.VICTORIOUS);
        }
    }

    // 5246 - Intimidating Shout
    [Script]
    internal class spell_warr_intimidating_shout : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitSrcAreaEnemy));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitSrcAreaEnemy));
        }

        private void FilterTargets(List<WorldObject> unitList)
        {
            unitList.Remove(GetExplTargetWorldObject());
        }
    }

    // 70844 - Item - Warrior T10 Protection 4P Bonus
    [Script] // 7.1.5
    internal class spell_warr_item_t10_prot_4p_bonus : AuraScript, IAuraOnProc
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.STOICISM) && spellInfo.GetEffects().Count > 1;
        }

        public void OnProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit target = eventInfo.GetActionTarget();
            int bp0 = (int)MathFunctions.CalculatePct(target.GetMaxHealth(), GetEffectInfo(1).CalcValue());
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
            target.CastSpell((Unit)null, SpellIds.STOICISM, args);
        }
    }

    [Script] // 12294 - Mortal Strike 7.1.5
    internal class spell_warr_mortal_strike : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MORTAL_WOUNDS);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();

            if (target)
                GetCaster().CastSpell(target, SpellIds.MORTAL_WOUNDS, true);
        }
    }

    [Script] // 97462 - Rallying Cry
    internal class spell_warr_rallying_cry : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RALLYING_CRY);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)GetHitUnit().CountPctFromMaxHealth(GetEffectValue()));

            GetCaster().CastSpell(GetHitUnit(), SpellIds.RALLYING_CRY, args);
        }
    }

    [Script] // 46968 - Shockwave
    internal class spell_warr_shockwave : SpellScript, IAfterCast, IHasSpellEffects
    {
        private uint _targetCount;

        public override bool Validate(SpellInfo spellInfo)
        {
            if (!ValidateSpellInfo(SpellIds.SHOCKWAVE, SpellIds.SHOCKWAVE_STUN))
                return false;

            return spellInfo.GetEffects().Count > 3;
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        // Cooldown reduced by 20 sec if it strikes at least 3 targets.
        public void AfterCast()
        {
            if (_targetCount >= (uint)GetEffectInfo(0).CalcValue())
                GetCaster().ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(-GetEffectInfo(3).CalcValue()));
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleStun, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleStun(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SHOCKWAVE_STUN, true);
            ++_targetCount;
        }
    }

    [Script] // 107570 - Storm Bolt
    internal class spell_warr_storm_bolt : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.STORM_BOLT_STUN);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.STORM_BOLT_STUN, true);
        }
    }

    // 52437 - Sudden Death
    [Script]
    internal class spell_warr_sudden_death : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.COLOSSUS_SMASH);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply)); // correct?
        }

        private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Remove cooldown on Colossus Smash
            Player player = GetTarget().ToPlayer();

            if (player)
                player.GetSpellHistory().ResetCooldown(SpellIds.COLOSSUS_SMASH, true);
        }
    }

    // 12328, 18765, 35429 - Sweeping Strikes
    [Script]
    internal class spell_warr_sweeping_strikes : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        private Unit _procTarget;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SWEEPING_STRIKES_EXTRA_ATTACK_1, SpellIds.SWEEPING_STRIKES_EXTRA_ATTACK_2);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            _procTarget = eventInfo.GetActor().SelectNearbyTarget(eventInfo.GetProcTarget());

            return _procTarget;
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo != null)
            {
                SpellInfo spellInfo = damageInfo.GetSpellInfo();

                if (spellInfo != null &&
                    (spellInfo.Id == SpellIds.BLADESTORM_PERIODIC_WHIRLWIND || (spellInfo.Id == SpellIds.EXECUTE && !_procTarget.HasAuraState(AuraStateType.Wounded20Percent))))
                {
                    // If triggered by Execute (while Target is not under 20% hp) or Bladestorm deals normalized weapon Damage
                    GetTarget().CastSpell(_procTarget, SpellIds.SWEEPING_STRIKES_EXTRA_ATTACK_2, new CastSpellExtraArgs(aurEff));
                }
                else
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage());
                    GetTarget().CastSpell(_procTarget, SpellIds.SWEEPING_STRIKES_EXTRA_ATTACK_1, args);
                }
            }
        }
    }

    [Script] // 215538 - Trauma
    internal class spell_warr_trauma : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TRAUMA_EFFECT);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActionTarget();
            //Get 25% of Damage from the spell casted (Slam & Whirlwind) plus Remaining Damage from Aura
            int damage = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()) / Global.SpellMgr.GetSpellInfo(SpellIds.TRAUMA_EFFECT, GetCastDifficulty()).GetMaxTicks());
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, damage);
            GetCaster().CastSpell(target, SpellIds.TRAUMA_EFFECT, args);
        }
    }

    [Script] // 28845 - Cheat Death
    internal class spell_warr_t3_prot_8p_bonus : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetActionTarget().HealthBelowPct(20))
                return true;

            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo != null &&
                damageInfo.GetDamage() != 0)
                if (GetTarget().HealthBelowPctDamaged(20, damageInfo.GetDamage()))
                    return true;

            return false;
        }
    }

    [Script] // 32215 - Victorious State
    internal class spell_warr_victorious_state : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IMPENDING_VICTORY);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleOnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        private void HandleOnProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            if (procInfo.GetActor().GetTypeId() == TypeId.Player &&
                procInfo.GetActor().ToPlayer().GetPrimarySpecialization() == (uint)TalentSpecialization.WarriorFury)
                PreventDefaultAction();

            procInfo.GetActor().GetSpellHistory().ResetCooldown(SpellIds.IMPENDING_VICTORY, true);
        }
    }

    [Script] // 34428 - Victory Rush
    internal class spell_warr_victory_rush : SpellScript, IAfterCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VICTORIOUS, SpellIds.VICTORY_RUSH_HEAL);
        }

        public void AfterCast()
        {
            Unit caster = GetCaster();

            caster.CastSpell(caster, SpellIds.VICTORY_RUSH_HEAL, true);
            caster.RemoveAurasDueToSpell(SpellIds.VICTORIOUS);
        }
    }
}