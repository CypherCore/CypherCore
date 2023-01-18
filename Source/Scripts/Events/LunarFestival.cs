// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Numerics;

namespace Scripts.Events.LunarFestival
{
    struct SpellIds
    {
        //Fireworks
        public const uint RocketBlue = 26344;
        public const uint RocketGreen = 26345;
        public const uint RocketPurple = 26346;
        public const uint RocketRed = 26347;
        public const uint RocketWhite = 26348;
        public const uint RocketYellow = 26349;
        public const uint RocketBigBlue = 26351;
        public const uint RocketBigGreen = 26352;
        public const uint RocketBigPurple = 26353;
        public const uint RocketBigRed = 26354;
        public const uint RocketBigWhite = 26355;
        public const uint RocketBigYellow = 26356;
        public const uint LunarFortune = 26522;

        //Omen
        public const uint OmenCleave = 15284;
        public const uint OmenStarfall = 26540;
        public const uint OmenSummonSpotlight = 26392;
        public const uint EluneCandle = 26374;

        //EluneCandle
        public const uint EluneCandleOmenHead = 26622;
        public const uint EluneCandleOmenChest = 26624;
        public const uint EluneCandleOmenHandR = 26625;
        public const uint EluneCandleOmenHandL = 26649;
        public const uint EluneCandleNormal = 26636;

    }

    struct CreatureIds
    {
        //Fireworks
        public const uint Omen = 15467;
        public const uint MinionOfOmen = 15466;
        public const uint FireworkBlue = 15879;
        public const uint FireworkGreen = 15880;
        public const uint FireworkPurple = 15881;
        public const uint FireworkRed = 15882;
        public const uint FireworkYellow = 15883;
        public const uint FireworkWhite = 15884;
        public const uint FireworkBigBlue = 15885;
        public const uint FireworkBigGreen = 15886;
        public const uint FireworkBigPurple = 15887;
        public const uint FireworkBigRed = 15888;
        public const uint FireworkBigYellow = 15889;
        public const uint FireworkBigWhite = 15890;

        public const uint ClusterBlue = 15872;
        public const uint ClusterRed = 15873;
        public const uint ClusterGreen = 15874;
        public const uint ClusterPurple = 15875;
        public const uint ClusterWhite = 15876;
        public const uint ClusterYellow = 15877;
        public const uint ClusterBigBlue = 15911;
        public const uint ClusterBigGreen = 15912;
        public const uint ClusterBigPurple = 15913;
        public const uint ClusterBigRed = 15914;
        public const uint ClusterBigWhite = 15915;
        public const uint ClusterBigYellow = 15916;
        public const uint ClusterElune = 15918;
    }

    struct GameObjectIds
    {
        //Fireworks
        public const uint FireworkLauncher1 = 180771;
        public const uint FireworkLauncher2 = 180868;
        public const uint FireworkLauncher3 = 180850;
        public const uint ClusterLauncher1 = 180772;
        public const uint ClusterLauncher2 = 180859;
        public const uint ClusterLauncher3 = 180869;
        public const uint ClusterLauncher4 = 180874;

        //Omen
        public const uint EluneTrap1 = 180876;
        public const uint EluneTrap2 = 180877;
    }

    struct MiscConst
    {
        //Fireworks
        public const uint AnimGoLaunchFirework = 3;
        public const uint ZoneMoonglade = 493;

        //Omen
        public static Position OmenSummonPos = new(7558.993f, -2839.999f, 450.0214f, 4.46f);
    }

    [Script]
    class npc_firework : ScriptedAI
    {
        public npc_firework(Creature creature) : base(creature) { }

        bool isCluster()
        {
            switch (me.GetEntry())
            {
                case CreatureIds.FireworkBlue:
                case CreatureIds.FireworkGreen:
                case CreatureIds.FireworkPurple:
                case CreatureIds.FireworkRed:
                case CreatureIds.FireworkYellow:
                case CreatureIds.FireworkWhite:
                case CreatureIds.FireworkBigBlue:
                case CreatureIds.FireworkBigGreen:
                case CreatureIds.FireworkBigPurple:
                case CreatureIds.FireworkBigRed:
                case CreatureIds.FireworkBigYellow:
                case CreatureIds.FireworkBigWhite:
                    return false;
                case CreatureIds.ClusterBlue:
                case CreatureIds.ClusterGreen:
                case CreatureIds.ClusterPurple:
                case CreatureIds.ClusterRed:
                case CreatureIds.ClusterYellow:
                case CreatureIds.ClusterWhite:
                case CreatureIds.ClusterBigBlue:
                case CreatureIds.ClusterBigGreen:
                case CreatureIds.ClusterBigPurple:
                case CreatureIds.ClusterBigRed:
                case CreatureIds.ClusterBigYellow:
                case CreatureIds.ClusterBigWhite:
                case CreatureIds.ClusterElune:
                default:
                    return true;
            }
        }

        GameObject FindNearestLauncher()
        {
            GameObject launcher = null;

            if (isCluster())
            {
                GameObject launcher1 = GetClosestGameObjectWithEntry(me, GameObjectIds.ClusterLauncher1, 0.5f);
                GameObject launcher2 = GetClosestGameObjectWithEntry(me, GameObjectIds.ClusterLauncher2, 0.5f);
                GameObject launcher3 = GetClosestGameObjectWithEntry(me, GameObjectIds.ClusterLauncher3, 0.5f);
                GameObject launcher4 = GetClosestGameObjectWithEntry(me, GameObjectIds.ClusterLauncher4, 0.5f);

                if (launcher1)
                    launcher = launcher1;
                else if (launcher2)
                    launcher = launcher2;
                else if (launcher3)
                    launcher = launcher3;
                else if (launcher4)
                    launcher = launcher4;
            }
            else
            {
                GameObject launcher1 = GetClosestGameObjectWithEntry(me, GameObjectIds.FireworkLauncher1, 0.5f);
                GameObject launcher2 = GetClosestGameObjectWithEntry(me, GameObjectIds.FireworkLauncher2, 0.5f);
                GameObject launcher3 = GetClosestGameObjectWithEntry(me, GameObjectIds.FireworkLauncher3, 0.5f);

                if (launcher1)
                    launcher = launcher1;
                else if (launcher2)
                    launcher = launcher2;
                else if (launcher3)
                    launcher = launcher3;
            }

            return launcher;
        }

        uint GetFireworkSpell(uint entry)
        {
            switch (entry)
            {
                case CreatureIds.FireworkBlue:
                    return SpellIds.RocketBlue;
                case CreatureIds.FireworkGreen:
                    return SpellIds.RocketGreen;
                case CreatureIds.FireworkPurple:
                    return SpellIds.RocketPurple;
                case CreatureIds.FireworkRed:
                    return SpellIds.RocketRed;
                case CreatureIds.FireworkYellow:
                    return SpellIds.RocketYellow;
                case CreatureIds.FireworkWhite:
                    return SpellIds.RocketWhite;
                case CreatureIds.FireworkBigBlue:
                    return SpellIds.RocketBigBlue;
                case CreatureIds.FireworkBigGreen:
                    return SpellIds.RocketBigGreen;
                case CreatureIds.FireworkBigPurple:
                    return SpellIds.RocketBigPurple;
                case CreatureIds.FireworkBigRed:
                    return SpellIds.RocketBigRed;
                case CreatureIds.FireworkBigYellow:
                    return SpellIds.RocketBigYellow;
                case CreatureIds.FireworkBigWhite:
                    return SpellIds.RocketBigWhite;
                default:
                    return 0;
            }
        }

        uint GetFireworkGameObjectId()
        {
            uint spellId = 0;

            switch (me.GetEntry())
            {
                case CreatureIds.ClusterBlue:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBlue);
                    break;
                case CreatureIds.ClusterGreen:
                    spellId = GetFireworkSpell(CreatureIds.FireworkGreen);
                    break;
                case CreatureIds.ClusterPurple:
                    spellId = GetFireworkSpell(CreatureIds.FireworkPurple);
                    break;
                case CreatureIds.ClusterRed:
                    spellId = GetFireworkSpell(CreatureIds.FireworkRed);
                    break;
                case CreatureIds.ClusterYellow:
                    spellId = GetFireworkSpell(CreatureIds.FireworkYellow);
                    break;
                case CreatureIds.ClusterWhite:
                    spellId = GetFireworkSpell(CreatureIds.FireworkWhite);
                    break;
                case CreatureIds.ClusterBigBlue:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigBlue);
                    break;
                case CreatureIds.ClusterBigGreen:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigGreen);
                    break;
                case CreatureIds.ClusterBigPurple:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigPurple);
                    break;
                case CreatureIds.ClusterBigRed:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigRed);
                    break;
                case CreatureIds.ClusterBigYellow:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigYellow);
                    break;
                case CreatureIds.ClusterBigWhite:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigWhite);
                    break;
                case CreatureIds.ClusterElune:
                    spellId = GetFireworkSpell(RandomHelper.URand(CreatureIds.FireworkBlue, CreatureIds.FireworkWhite));
                    break;
            }

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

            if (spellInfo != null && spellInfo.GetEffect(0).Effect == SpellEffectName.SummonObjectWild)
                return (uint)spellInfo.GetEffect(0).MiscValue;

            return 0;
        }

        public override void Reset()
        {
            GameObject launcher = FindNearestLauncher();
            if (launcher)
            {
                launcher.SendCustomAnim(MiscConst.AnimGoLaunchFirework);
                me.SetOrientation(launcher.GetOrientation() + MathF.PI / 2);
            }
            else
                return;

            if (isCluster())
            {
                // Check if we are near Elune'ara lake south, if so try to summon Omen or a minion
                if (me.GetZoneId() == MiscConst.ZoneMoonglade)
                {
                    if (!me.FindNearestCreature(CreatureIds.Omen, 100.0f) && me.GetDistance2d(MiscConst.OmenSummonPos.GetPositionX(), MiscConst.OmenSummonPos.GetPositionY()) <= 100.0f)
                    {
                        switch (RandomHelper.URand(0, 9))
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                Creature minion = me.SummonCreature(CreatureIds.MinionOfOmen, me.GetPositionX() + RandomHelper.FRand(-5.0f, 5.0f), me.GetPositionY() + RandomHelper.FRand(-5.0f, 5.0f), me.GetPositionZ(), 0.0f, TempSummonType.CorpseTimedDespawn, TimeSpan.FromSeconds(20));
                                if (minion)
                                    minion.GetAI().AttackStart(me.SelectNearestPlayer(20.0f));
                                break;
                            case 9:
                                me.SummonCreature(CreatureIds.Omen, MiscConst.OmenSummonPos);
                                break;
                        }
                    }
                }
                if (me.GetEntry() == CreatureIds.ClusterElune)
                    DoCast(SpellIds.LunarFortune);

                float displacement = 0.7f;
                for (byte i = 0; i < 4; i++)
                    me.SummonGameObject(GetFireworkGameObjectId(), me.GetPositionX() + (i % 2 == 0 ? displacement : -displacement), me.GetPositionY() + (i > 1 ? displacement : -displacement), me.GetPositionZ() + 4.0f, me.GetOrientation(), Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(me.GetOrientation(), 0.0f, 0.0f)), TimeSpan.FromSeconds(1));
            }
            else
                //me.CastSpell(me, GetFireworkSpell(me.GetEntry()), true);
                me.CastSpell(me.GetPosition(), GetFireworkSpell(me.GetEntry()), new CastSpellExtraArgs(true));
        }
    }

    [Script]
    class npc_omen : ScriptedAI
    {
        public npc_omen(Creature creature) : base(creature)
        {
            me.SetImmuneToPC(true);
            me.GetMotionMaster().MovePoint(1, 7549.977f, -2855.137f, 456.9678f);
        }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            if (type != MovementGeneratorType.Point)
                return;

            if (pointId == 1)
            {
                me.SetHomePosition(me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetOrientation());
                me.SetImmuneToPC(false);
                Player player = me.SelectNearestPlayer(40.0f);
                if (player)
                    AttackStart(player);
            }
        }

        public override void JustEngagedWith(Unit attacker)
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIds.OmenCleave);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10), 1, task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                if (target)
                    DoCast(target, SpellIds.OmenStarfall);
                task.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(16));
            });
        }

        public override void JustDied(Unit killer)
        {
            DoCast(SpellIds.OmenSummonSpotlight);
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            if (spellInfo.Id == SpellIds.EluneCandle)
            {
                if (me.HasAura(SpellIds.OmenStarfall))
                    me.RemoveAurasDueToSpell(SpellIds.OmenStarfall);

                _scheduler.RescheduleGroup(1, TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(16));
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_giant_spotlight : ScriptedAI
    {
        public npc_giant_spotlight(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromMinutes(5), task =>
            {
                GameObject trap = me.FindNearestGameObject(GameObjectIds.EluneTrap1, 5.0f);
                if (trap)
                    trap.RemoveFromWorld();

                trap = me.FindNearestGameObject(GameObjectIds.EluneTrap2, 5.0f);
                if (trap)
                    trap.RemoveFromWorld();

                Creature omen = me.FindNearestCreature(CreatureIds.Omen, 5.0f, false);
                if (omen)
                    omen.DespawnOrUnsummon();

                me.DespawnOrUnsummon();
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script] // 26374 - Elune's Candle
    class spell_lunar_festival_elune_candle : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EluneCandleOmenHead, SpellIds.EluneCandleOmenChest, SpellIds.EluneCandleOmenHandR, SpellIds.EluneCandleOmenHandL, SpellIds.EluneCandleNormal);
        }

        void HandleScript(uint effIndex)
        {
            uint spellId = 0;

            if (GetHitUnit().GetEntry() == CreatureIds.Omen)
            {
                switch (RandomHelper.URand(0, 3))
                {
                    case 0:
                        spellId = SpellIds.EluneCandleOmenHead;
                        break;
                    case 1:
                        spellId = SpellIds.EluneCandleOmenChest;
                        break;
                    case 2:
                        spellId = SpellIds.EluneCandleOmenHandR;
                        break;
                    case 3:
                        spellId = SpellIds.EluneCandleOmenHandL;
                        break;
                }
            }
            else
                spellId = SpellIds.EluneCandleNormal;

            GetCaster().CastSpell(GetHitUnit(), spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy));
        }
    }
}
