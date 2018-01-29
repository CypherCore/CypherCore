/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

using System.Collections.Generic;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.BrokenIsles
{
    enum Phases
    {
        DALARAN_KARAZHAN = 5829
    }

    enum Quests
    {
        BLINK_OF_AN_EYE = 44663
    }

    [Script]
    class OnLegionArrival : PlayerScript
    {
        public OnLegionArrival() : base("OnLegionArrival") { }

        enum Contents
        {
            SPELL_MAGE_LEARN_GUARDIAN_HALL_TP = 204287,
            SPELL_WAR_LEARN_JUMP_TO_SKYHOLD = 192084,
            SPELL_CREATE_CLASS_HALL_ALLIANCE = 185506,
            SPELL_CREATE_CLASS_HALL_HORDE = 192191,

            CONVERSATION_KHADGAR_BLINK_OF_EYE = 3827,
        }

        public override void OnLevelChanged(Player player, byte oldLevel)
        {
            base.OnLevelChanged(player, oldLevel);

            if (oldLevel < 100 && player.getLevel() >= 100)
            {
                if (player.GetClass() == Class.Mage)
                    player.CastSpell( player, (uint)Contents.SPELL_MAGE_LEARN_GUARDIAN_HALL_TP );


                if (player.GetClass() == Class.Warrior)
                    player.CastSpell(player, (uint)Contents.SPELL_WAR_LEARN_JUMP_TO_SKYHOLD, true);

                player.CastSpell(player, player.GetTeam() == Team.Alliance ? (uint)Contents.SPELL_CREATE_CLASS_HALL_ALLIANCE : (uint)Contents.SPELL_CREATE_CLASS_HALL_HORDE, true);

                if (player.GetQuestStatus((uint)Quests.BLINK_OF_AN_EYE) == QuestStatus.None)
                {
                    Conversation.CreateConversation((uint)Contents.CONVERSATION_KHADGAR_BLINK_OF_EYE, player, player.GetPosition(), new List<ObjectGuid>{ player.GetGUID() });

                    Quest quest = Global.ObjectMgr.GetQuestTemplate((uint) Quests.BLINK_OF_AN_EYE);
                    if ( quest != null )
                        player.AddQuest(quest, null);
                }
            }
        }
    }

    [Script]
    class spell_dalaran_teleportation : SpellScript
    {
        void EffectTeleportDalaranKarazhan(uint effIndex)
        {
            Player player = GetCaster().ToPlayer();

            if (player != null)
            {
                if (player.getLevel() < 100 || player.GetQuestStatus((uint) Quests.BLINK_OF_AN_EYE) != QuestStatus.Incomplete)
                    PreventHitEffect(effIndex);
                else
                {
                    SpellTargetPosition targetPosition = Global.SpellMgr.GetSpellTargetPosition(GetSpellInfo().Id, effIndex);

                    if (targetPosition != null)
                    {
                        Map map = Global.MapMgr.FindMap(targetPosition.target_mapId, 0);
                        map?.LoadGrid( targetPosition.target_X, targetPosition.target_Y);
                    }
                }
            }
        }

        void EffectTeleportDalaranLegion(uint effIndex)
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                if (player.getLevel() < 100 || player.GetQuestStatus((uint)Quests.BLINK_OF_AN_EYE) == QuestStatus.Incomplete)
                    PreventHitEffect(effIndex);
            }
        }

        public override void Register()
        {
            OnEffectLaunch.Add(new EffectHandler(EffectTeleportDalaranKarazhan, 0, SpellEffectName.TriggerSpell));
            OnEffectLaunch.Add(new EffectHandler(EffectTeleportDalaranLegion, 1, SpellEffectName.TriggerSpell));
        }
    }

    [Script] // 246854
    class go_dalaran_karazhan : GameObjectScript
    {
        public go_dalaran_karazhan() : base("go_dalaran_karazhan"){  }

        // This is also called on object Creation. Set dalaran to active to enable far sight
        public override void OnGameObjectStateChanged(GameObject go, GameObjectState state)
        {
            base.OnGameObjectStateChanged(go, state);

            if( !go.isActiveObject())
                go.setActive(true);
        }
    }

    [Script]// 113986 - Khadgar
    class npc_dalaran_karazhan_khadgar : CreatureScript
    {
        public npc_dalaran_karazhan_khadgar() : base("npc_dalaran_karazhan_khadgar") { }

        enum Spells
        {
            SPELL_PLAY_DALARAN_TELEPORTATION_SCENE = 227861
        }

        public override bool OnGossipSelect(Player player, Creature creature, uint sender, uint action)
        {
            player.CastSpell( player, (uint)Spells.SPELL_PLAY_DALARAN_TELEPORTATION_SCENE, true);
            return true;
        }
    }

    [Script]
    class npc_dalaran_karazhan_kirintor_guardian : ScriptedAI
    {
        public npc_dalaran_karazhan_kirintor_guardian( Creature creature ) : base( creature) { }

        public override void MoveInLineOfSight(Unit who)
        {
            base.MoveInLineOfSight(who);
            if (who.IsPlayer() && who.GetPositionZ() < 700.0f)
            {
                SpellTargetPosition targetPosition = Global.SpellMgr.GetSpellTargetPosition(228329, 0); //Teleport to Dalaran
                if( targetPosition != null )
                    who.NearTeleportTo(targetPosition.target_X, targetPosition.target_Y, targetPosition.target_Z, targetPosition.target_Orientation);
            }
        }
    }

    [Script]
    class scene_dalaran_kharazan_teleportion : SceneScript
    {
        public scene_dalaran_kharazan_teleportion() : base("scene_dalaran_kharazan_teleportion") { }

        public override void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
        {
            base.OnSceneTriggerEvent(player, sceneInstanceID, sceneTemplate, triggerName);
            if (triggerName == "invisibledalaran")
                player.SetInPhase((uint) Phases.DALARAN_KARAZHAN, true, false);
        }

        public override void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            base.OnSceneComplete(player, sceneInstanceID, sceneTemplate);
            CompleteScene( player );
        }

        public override void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            base.OnSceneCancel(player, sceneInstanceID, sceneTemplate);
            CompleteScene( player );
        }

        void CompleteScene(Player player)
        {
            player.KilledMonsterCredit(114506);
            player.TeleportTo(1220, -827.82f, 4369.25f, 738.64f, 1.893364f);
        }
    }

}
