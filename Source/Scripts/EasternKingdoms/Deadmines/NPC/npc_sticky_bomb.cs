// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_helix_gearbreaker;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(47314)]
    public class npc_sticky_bomb : NullCreatureAI
    {
        public npc_sticky_bomb(Creature pCreature) : base(pCreature)
        {
            _instance = pCreature.GetInstanceScript();
        }

        private InstanceScript _instance;

        private uint _phase;
        private uint _uiTimer;

        public override void Reset()
        {
            _phase = 1;
            _uiTimer = 500;

            if (!me)
            {
                return;
            }

            DoCast(me, eSpels.CHEST_BOMB);
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (!me)
            {
                return;
            }

            if (_uiTimer < uiDiff)
            {
                switch (_phase)
                {
                    case 1:
                        DoCast(me, eSpels.ARMING_VISUAL_YELLOW);
                        _uiTimer = 700;
                        break;

                    case 2:
                        DoCast(me, eSpels.ARMING_VISUAL_ORANGE);
                        _uiTimer = 600;
                        break;

                    case 3:
                        DoCast(me, eSpels.ARMING_VISUAL_RED);
                        _uiTimer = 500;
                        break;

                    case 4:
                        DoCast(me, eSpels.BOMB_ARMED_STATE);
                        _uiTimer = 400;
                        break;

                    case 5:
                        DoCast(me, me.GetMap().IsHeroic() ? eSpels.STICKY_BOMB_EXPLODE_H : eSpels.STICKY_BOMB_EXPLODE);
                        _uiTimer = 300;
                        break;

                    case 6:
                        me.DespawnOrUnsummon();
                        break;
                }
                _phase++;
            }
            else
            {
                _uiTimer -= uiDiff;
            }
        }
    }
}
