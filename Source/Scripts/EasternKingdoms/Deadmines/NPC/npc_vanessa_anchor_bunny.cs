// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(51624)]
    public class npc_vanessa_anchor_bunny : ScriptedAI
    {
        public npc_vanessa_anchor_bunny(Creature creature) : base(creature)
        {
        }

        private uint _achievementTimer;
        private bool _startTimerAchievement;
        private bool _getAchievementPlayers;

        public override void Reset()
        {
            _startTimerAchievement = false;
            _getAchievementPlayers = true;
            _achievementTimer = 300000;
        }

        public override void SetData(uint uiI, uint uiValue)
        {
            if (uiValue == boss_vanessa_vancleef.eAchievementMisc.START_TIMER_ACHIEVEMENT && _startTimerAchievement == false)
            {
                _startTimerAchievement = true;
            }
            if (uiValue == boss_vanessa_vancleef.eAchievementMisc.ACHIEVEMENT_READY_GET && _getAchievementPlayers == true && _startTimerAchievement == true)
            {
                Map map = me.GetMap();
                AchievementRecord vigorous_vancleef_vindicator = Global.AchievementMgr.GetAchievementByReferencedId(boss_vanessa_vancleef.eAchievementMisc.ACHIEVEMENT_VIGOROUS_VANCLEEF_VINDICATOR).FirstOrDefault();

                if (map != null && map.IsDungeon() && map.GetDifficultyID() == Difficulty.Heroic)
                {
                    var players = map.GetPlayers();
                    if (!players.Empty())
                    {
                        foreach (var player in map.GetPlayers())
                        {

                            if (player != null)
                            {
                                if (player.GetDistance(me) < 200.0f)
                                {
                                    player.CompletedAchievement(vigorous_vancleef_vindicator);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (_startTimerAchievement == true && _getAchievementPlayers == true)
            {
                if (_achievementTimer <= diff)
                {
                    _getAchievementPlayers = false;
                }
                else
                {
                    _achievementTimer -= diff;
                }
            }
        }
    }
}
