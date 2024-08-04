// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;

namespace Scripts.Battlegrounds.AlteracValley
{
    struct SpellIds
    {
        public const uint Charge = 22911;
        public const uint Cleave = 40504;
        public const uint DemoralizingShout = 23511;
        public const uint Enrage = 8599;
        public const uint Whirlwind = 13736;

        public const uint NorthMarshal = 45828;
        public const uint SouthMarshal = 45829;
        public const uint StonehearthMarshal = 45830;
        public const uint IcewingMarshal = 45831;
        public const uint IcebloodWarmaster = 45822;
        public const uint TowerPointWarmaster = 45823;
        public const uint WestFrostwolfWarmaster = 45824;
        public const uint EastFrostwolfWarmaster = 45826;
    }

    enum CreatureIds
    {
        NorthMarshal = 14762,
        SouthMarshal = 14763,
        IcewingMarshal = 14764,
        StonehearthMarshal = 14765,
        EastFrostwolfWarmaster = 14772,
        IcebloodWarmaster = 14773,
        TowerPointWarmaster = 14776,
        WestFrostwolfWarmaster = 14777,

        Vanndar = 11948,
        Drekthar = 11946,
        Balinda = 11949,
        Galvangar = 11947,
        Morloch = 11657,
        UmiThorson = 13078,
        Keetar = 13079,
        TaskmasterSnivvle = 11677,
        AgiRumblestomp = 13086,
        MashaSwiftcut = 13088,
        Herald = 14848,

        StormpikeDefender = 12050,
        FrostwolfGuardian = 12053,
        SeasonedDefender = 13326,
        SeasonedGuardian = 13328,
        VeteranDefender = 13331,
        VeteranGuardian = 13332,
        ChampionDefender = 13422,
        ChampionGuardian = 13421
    }

    enum SharedActions
    {
        BuffYell = -30001,
        InteractCapturableObject = 1,
        CaptureCapturableObject = 2,

        TurnInScraps = 3,
        TurnInCommander1 = 4,
        TurnInCommander2 = 5,
        TurnInCommander3 = 6,
        TurnInBoss1,
        TurnInBoss2,
        TurnInNearMine,
        TurnInOtherMine,
        TurnInRiderHide,
        TurnInRiderTame
    }

    [Script]
    class npc_av_marshal_or_warmaster : ScriptedAI
    {
        public npc_av_marshal_or_warmaster(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Charge);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(11), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCast(me, SpellIds.DemoralizingShout);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
            {
                DoCast(me, SpellIds.Whirlwind);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
            {
                DoCast(me, SpellIds.Enrage);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                Position _homePosition = me.GetHomePosition();
                if (me.GetDistance2d(_homePosition.GetPositionX(), _homePosition.GetPositionY()) > 50.0f)
                {
                    EnterEvadeMode();
                    return;
                }
                task.Repeat(TimeSpan.FromSeconds(5));
            });
        }

        public override void JustAppeared()
        {
            Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }

    [Script]
    class go_av_capturable_object : GameObjectAI
    {
        public go_av_capturable_object(GameObject go) : base(go) { }

        public override void Reset()
        {
            me.SetActive(true);
        }

        public override bool OnGossipHello(Player player)
        {
            if (me.GetGoState() != GameObjectState.Ready)
                return true;

            ZoneScript zonescript = me.GetZoneScript();
            if (zonescript != null)
            {
                zonescript.DoAction(1, player, me);
                return false;
            }

            return true;
        }
    }

    [Script]
    class go_av_contested_object : GameObjectAI
    {
        public go_av_contested_object(GameObject go) : base(go) { }

        public override void Reset()
        {
            me.SetActive(true);
            _scheduler.Schedule(TimeSpan.FromMinutes(4), _ =>
            {
                ZoneScript zonescript = me.GetZoneScript();
                if (zonescript != null)
                    zonescript.DoAction(2, me, me);
            });
        }

        public override bool OnGossipHello(Player player)
        {
            if (me.GetGoState() != GameObjectState.Ready)
                return true;

            ZoneScript zonescript = me.GetZoneScript();
            if (zonescript != null)
            {
                zonescript.DoAction(1, player, me);
                return false;
            }

            return true;
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class at_av_exploit : AreaTriggerScript
    {
        public at_av_exploit() : base("at_av_exploit") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger)
        {
            var battleground = player.GetBattleground();
            if (battleground != null && battleground.GetStatus() == BattlegroundStatus.WaitJoin)
                battleground.TeleportPlayerToExploitLocation(player);

            return true;
        }
    }

    [Script("quest_alterac_valley_armor_scraps", SharedActions.TurnInScraps)]
    [Script("quest_alterac_valley_call_of_air_slidore_guse", SharedActions.TurnInCommander1)]
    [Script("quest_alterac_valley_call_of_air_vipore_jeztor", SharedActions.TurnInCommander2)]
    [Script("quest_alterac_valley_call_of_air_ichman_mulverick", SharedActions.TurnInCommander3)]
    [Script("quest_alterac_valley_boss_5", SharedActions.TurnInBoss1)]
    [Script("quest_alterac_valley_boss_1", SharedActions.TurnInBoss2)]
    [Script("quest_alterac_valley_near_mine", SharedActions.TurnInNearMine)]
    [Script("quest_alterac_valley_other_mine", SharedActions.TurnInOtherMine)]
    [Script("quest_alterac_valley_ram_harnesses", SharedActions.TurnInRiderHide)]
    [Script("quest_alterac_valley_empty_stables", SharedActions.TurnInRiderTame)]
    class quest_alterac_valley : QuestScript
    {
        uint _actionId;

        public quest_alterac_valley(string scriptName, SharedActions actionId) : base(scriptName)
        {
            _actionId = (uint)actionId;
        }

        public override void OnQuestStatusChange(Player player, Quest quest, QuestStatus oldStatus, QuestStatus newStatus)
        {
            if (newStatus != QuestStatus.Rewarded)
                return;

            ZoneScript zoneScript = player.FindZoneScript();
            if (zoneScript != null)
                zoneScript.DoAction(_actionId, player, player);
        }
    }
}