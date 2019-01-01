/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


namespace Scripts.Northrend.Ulduar
{
    namespace Razorscale
    {
        /*class boss_razorscale_controller : BossAI
            {
                public boss_razorscale_controller(Creature creature) : base(creature, InstanceData.RazorscaleControl)
                {
                    me.SetDisplayId(me.GetCreatureTemplate().ModelId2);
                }

                public override void Reset()
                {
                    _Reset();
                    me.SetReactState(ReactStates.Passive);
                }

                public override void SpellHit(Unit caster, SpellInfo spell)
                {
                    switch (spell.Id)
                    {
                        case SPELL_FLAMED:
                            GameObject Harpoon = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GO_RAZOR_HARPOON_1));
                            if (Harpoon)
                                Harpoon.RemoveFromWorld();
                            Harpoon = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GO_RAZOR_HARPOON_2));
                            if (Harpoon)
                                Harpoon.RemoveFromWorld();
                            Harpoon = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GO_RAZOR_HARPOON_3));
                            if (Harpoon)
                                Harpoon.RemoveFromWorld();
                            Harpoon = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GO_RAZOR_HARPOON_4));
                            if (Harpoon)
                                Harpoon.RemoveFromWorld();

                            DoAction(ACTION_HARPOON_BUILD);
                            DoAction(ACTION_PLACE_BROKEN_HARPOON);
                            break;
                        case SPELL_HARPOON_SHOT_1:
                        case SPELL_HARPOON_SHOT_2:
                        case SPELL_HARPOON_SHOT_3:
                        case SPELL_HARPOON_SHOT_4:
                            DoCast(SPELL_HARPOON_TRIGGER);
                            break;
                    }
                }

                public override void JustDied(Unit killer)
                {
                    _JustDied();
                }

                public override void DoAction(int action)
                {
                    if (instance.GetBossState(BOSS_RAZORSCALE) != EncounterState.InProgress)
                        return;

                    switch (action)
                    {
                        case ACTION_HARPOON_BUILD:
                            events.ScheduleEvent(EVENT_BUILD_HARPOON_1, 50000);
                            if (me->GetMap()->GetSpawnMode() == DIFFICULTY_25_N)
                                events.ScheduleEvent(EVENT_BUILD_HARPOON_3, 90000);
                            break;
                        case ACTION_PLACE_BROKEN_HARPOON:
                            for (uint8 n = 0; n < RAID_MODE(2, 4); n++)
                                me->SummonGameObject(GO_RAZOR_BROKEN_HARPOON, PosHarpoon[n].GetPositionX(), PosHarpoon[n].GetPositionY(), PosHarpoon[n].GetPositionZ(), 2.286f, 0, 0, 0, 0, 180);
                            break;
                    }
                }

                public override void UpdateAI(uint Diff)
                {
                    _events.Update(Diff);

                    while (uint eventId = _events.ExecuteEvent())
                {
                        switch (eventId)
                        {
                            case EVENT_BUILD_HARPOON_1:
                                Talk(EMOTE_HARPOON);
                                if (GameObject * Harpoon = me->SummonGameObject(GO_RAZOR_HARPOON_1, PosHarpoon[0].GetPositionX(), PosHarpoon[0].GetPositionY(), PosHarpoon[0].GetPositionZ(), 4.790f, 0.0f, 0.0f, 0.0f, 0.0f, uint32(me->GetRespawnTime())))
                                {
                                    if (GameObject * BrokenHarpoon = Harpoon->FindNearestGameObject(GO_RAZOR_BROKEN_HARPOON, 5.0f)) //only nearest broken harpoon
                                        BrokenHarpoon->RemoveFromWorld();
                                    events.ScheduleEvent(EVENT_BUILD_HARPOON_2, 20000);
                                    events.CancelEvent(EVENT_BUILD_HARPOON_1);
                                }
                                return;
                            case EVENT_BUILD_HARPOON_2:
                                Talk(EMOTE_HARPOON);
                                if (GameObject * Harpoon = me->SummonGameObject(GO_RAZOR_HARPOON_2, PosHarpoon[1].GetPositionX(), PosHarpoon[1].GetPositionY(), PosHarpoon[1].GetPositionZ(), 4.659f, 0, 0, 0, 0, uint32(me->GetRespawnTime())))
                                {
                                    if (GameObject * BrokenHarpoon = Harpoon->FindNearestGameObject(GO_RAZOR_BROKEN_HARPOON, 5.0f))
                                        BrokenHarpoon->RemoveFromWorld();
                                    events.CancelEvent(EVENT_BUILD_HARPOON_2);
                                }
                                return;
                            case EVENT_BUILD_HARPOON_3:
                                Talk(EMOTE_HARPOON);
                                if (GameObject * Harpoon = me->SummonGameObject(GO_RAZOR_HARPOON_3, PosHarpoon[2].GetPositionX(), PosHarpoon[2].GetPositionY(), PosHarpoon[2].GetPositionZ(), 5.382f, 0, 0, 0, 0, uint32(me->GetRespawnTime())))
                                {
                                    if (GameObject * BrokenHarpoon = Harpoon->FindNearestGameObject(GO_RAZOR_BROKEN_HARPOON, 5.0f))
                                        BrokenHarpoon->RemoveFromWorld();
                                    events.ScheduleEvent(EVENT_BUILD_HARPOON_4, 20000);
                                    events.CancelEvent(EVENT_BUILD_HARPOON_3);
                                }
                                return;
                            case EVENT_BUILD_HARPOON_4:
                                Talk(EMOTE_HARPOON);
                                if (GameObject * Harpoon = me->SummonGameObject(GO_RAZOR_HARPOON_4, PosHarpoon[3].GetPositionX(), PosHarpoon[3].GetPositionY(), PosHarpoon[3].GetPositionZ(), 4.266f, 0, 0, 0, 0, uint32(me->GetRespawnTime())))
                                {
                                    if (GameObject * BrokenHarpoon = Harpoon->FindNearestGameObject(GO_RAZOR_BROKEN_HARPOON, 5.0f))
                                        BrokenHarpoon->RemoveFromWorld();
                                    events.CancelEvent(EVENT_BUILD_HARPOON_4);
                                }
                                return;
                        }
                    }
                }
            }

            public override CreatureAI Get(Creature creature)
            {
                return GetInstanceAI<boss_razorscale_controllerAI>(creature);
            }
        }*/

    }
}
