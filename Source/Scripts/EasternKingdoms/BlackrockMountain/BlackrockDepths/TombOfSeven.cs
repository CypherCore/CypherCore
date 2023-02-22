// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.TombOfSeven
{
    internal struct SpellIds
    {
        //Gloomrel
        public const uint SmeltDarkIron = 14891;
        public const uint LearnSmelt = 14894;

        //Doomrel
        public const uint Shadowboltvolley = 15245;
        public const uint Immolate = 12742;
        public const uint Curseofweakness = 12493;
        public const uint Demonarmor = 13787;
        public const uint SummonVoidwalkers = 15092;
    }

    internal struct QuestIds
    {
        public const uint SpectralChalice = 4083;
    }

    internal struct TextIds
    {
        public const uint GossipSelectDoomrel = 1828;
        public const uint GossipMenuIdContinue = 1;

        public const uint GossipMenuChallenge = 1947;
        public const uint GossipMenuIdChallenge = 0;
    }

    internal struct MiscConst
    {
        public const uint DataSkillpointMin = 230;

        public const string GossipItemTeach1 = "Teach me the art of smelting dark iron";
        public const string GossipItemTeach2 = "Continue...";
        public const string GossipItemTeach3 = "[PH] Continue...";
        public const string GossipItemTribute = "I want to pay tribute";
    }

    internal enum Phases
    {
        PhaseOne = 1,
        PhaseTwo = 2
    }

    [Script]
    internal class boss_gloomrel : ScriptedAI
    {
        private readonly InstanceScript instance;

        public boss_gloomrel(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();

            switch (action)
            {
                case GossipAction.GOSSIP_ACTION_INFO_DEF + 1:
                    player.AddGossipItem(GossipOptionNpc.None, MiscConst.GossipItemTeach2, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 11);
                    player.SendGossipMenu(2606, me.GetGUID());

                    break;
                case GossipAction.GOSSIP_ACTION_INFO_DEF + 11:
                    player.CloseGossipMenu();
                    player.CastSpell(player, SpellIds.LearnSmelt, false);

                    break;
                case GossipAction.GOSSIP_ACTION_INFO_DEF + 2:
                    player.AddGossipItem(GossipOptionNpc.None, MiscConst.GossipItemTeach3, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 22);
                    player.SendGossipMenu(2604, me.GetGUID());

                    break;
                case GossipAction.GOSSIP_ACTION_INFO_DEF + 22:
                    player.CloseGossipMenu();
                    //are 5 minutes expected? go template may have data to despawn when used at quest
                    instance.DoRespawnGameObject(instance.GetGuidData(DataTypes.DataGoChalice), TimeSpan.FromMinutes(5));

                    break;
            }

            return true;
        }

        public override bool OnGossipHello(Player player)
        {
            if (player.GetQuestRewardStatus(QuestIds.SpectralChalice) &&
                player.GetSkillValue(SkillType.Mining) >= MiscConst.DataSkillpointMin &&
                !player.HasSpell(SpellIds.SmeltDarkIron))
                player.AddGossipItem(GossipOptionNpc.None, MiscConst.GossipItemTeach1, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);

            if (!player.GetQuestRewardStatus(QuestIds.SpectralChalice) &&
                player.GetSkillValue(SkillType.Mining) >= MiscConst.DataSkillpointMin)
                player.AddGossipItem(GossipOptionNpc.None, MiscConst.GossipItemTribute, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 2);

            player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());

            return true;
        }
    }

    [Script]
    internal class boss_doomrel : ScriptedAI
    {
        private readonly InstanceScript _instance;
        private bool _voidwalkers;

        public boss_doomrel(Creature creature) : base(creature)
        {
            Initialize();
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            Initialize();

            me.SetFaction((uint)FactionTemplates.Friendly);

            // was set before event start, so set again
            me.SetImmuneToPC(true);

            if (_instance.GetData(DataTypes.DataGhostkill) >= 7)
                me.ReplaceAllNpcFlags(NPCFlags.None);
            else
                me.ReplaceAllNpcFlags(NPCFlags.Gossip);
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(10),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Shadowboltvolley);
                                    task.Repeat(TimeSpan.FromSeconds(12));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(18),
                                task =>
                                {
                                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);

                                    if (target)
                                        DoCast(target, SpellIds.Immolate);

                                    task.Repeat(TimeSpan.FromSeconds(25));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(5),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Curseofweakness);
                                    task.Repeat(TimeSpan.FromSeconds(45));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(16),
                                task =>
                                {
                                    DoCast(me, SpellIds.Demonarmor);
                                    task.Repeat(TimeSpan.FromMinutes(5));
                                });
        }

        public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (!_voidwalkers &&
                !HealthAbovePct(50))
            {
                DoCastVictim(SpellIds.SummonVoidwalkers, new CastSpellExtraArgs(true));
                _voidwalkers = true;
            }
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            base.EnterEvadeMode(why);

            _instance.SetGuidData(DataTypes.DataEvenstarter, ObjectGuid.Empty);
        }

        public override void JustDied(Unit killer)
        {
            _instance.SetData(DataTypes.DataGhostkill, 1);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();

            switch (action)
            {
                case GossipAction.GOSSIP_ACTION_INFO_DEF + 1:
                    player.InitGossipMenu(TextIds.GossipSelectDoomrel);
                    player.AddGossipItem(TextIds.GossipSelectDoomrel, TextIds.GossipMenuIdContinue, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 2);
                    player.SendGossipMenu(2605, me.GetGUID());

                    break;
                case GossipAction.GOSSIP_ACTION_INFO_DEF + 2:
                    player.CloseGossipMenu();
                    //start event here
                    me.SetFaction((int)FactionTemplates.DarkIronDwarves);
                    me.SetImmuneToPC(false);
                    me.GetAI().AttackStart(player);

                    _instance.SetGuidData(DataTypes.DataEvenstarter, player.GetGUID());

                    break;
            }

            return true;
        }

        public override bool OnGossipHello(Player player)
        {
            player.InitGossipMenu(TextIds.GossipMenuChallenge);
            player.AddGossipItem(TextIds.GossipMenuChallenge, TextIds.GossipMenuIdChallenge, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);
            player.SendGossipMenu(2601, me.GetGUID());

            return true;
        }

        private void Initialize()
        {
            _voidwalkers = false;
        }
    }
}