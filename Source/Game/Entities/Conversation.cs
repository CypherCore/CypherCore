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

using Framework.Constants;
using Game.DataStorage;
using Game.Maps;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Entities
{
    public class Conversation : WorldObject
    {
        public Conversation() : base(false)
        {
            _duration = 0;

            objectTypeMask |= TypeMask.Conversation;
            objectTypeId = TypeId.Conversation;

            m_updateFlag.Stationary = true;
            m_updateFlag.Conversation = true;

            valuesCount = (int)ConversationFields.End;
            _dynamicValuesCount = (int)ConversationDynamicFields.End;
        }

        public override void AddToWorld()
        {
            //- Register the Conversation for guid lookup and for caster
            if (!IsInWorld)
            {
                GetMap().GetObjectsStore().Add(GetGUID(), this);
                base.AddToWorld();
            }
        }

        public override void RemoveFromWorld()
        {
            //- Remove the Conversation from the accessor and from all lists of objects in world
            if (IsInWorld)
            {
                base.RemoveFromWorld();
                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        public override bool IsNeverVisibleFor(WorldObject seer)
        {
            if (!_participants.Contains(seer.GetGUID()))
                return true;

            return base.IsNeverVisibleFor(seer);
        }

        public override void Update(uint diff)
        {
            if (GetDuration() > diff)
                _duration -= diff;
            else
                Remove(); // expired

            base.Update(diff);
        }

        void Remove()
        {
            if (IsInWorld)
            {
                AddObjectToRemoveList(); // calls RemoveFromWorld
            }
        }

        public static Conversation CreateConversation(uint conversationEntry, Unit creator, Position pos, List<ObjectGuid> participants, SpellInfo spellInfo = null)
        {
            ConversationTemplate conversationTemplate = Global.ConversationDataStorage.GetConversationTemplate(conversationEntry);
            if (conversationTemplate == null)
                return null;

            ulong lowGuid = creator.GetMap().GenerateLowGuid(HighGuid.Conversation);

            Conversation conversation = new Conversation();
            if (!conversation.Create(lowGuid, conversationEntry, creator.GetMap(), creator, pos, participants, spellInfo))
                return null;

            return conversation;
        }

        bool Create(ulong lowGuid, uint conversationEntry, Map map, Unit creator, Position pos, List<ObjectGuid> participants, SpellInfo spellInfo = null)
        {
            ConversationTemplate conversationTemplate = Global.ConversationDataStorage.GetConversationTemplate(conversationEntry);
            //ASSERT(conversationTemplate);

            _creatorGuid = creator.GetGUID();
            _participants = participants;

            SetMap(map);
            Relocate(pos);

            _Create(ObjectGuid.Create(HighGuid.Conversation, GetMapId(), conversationEntry, lowGuid));
            PhasingHandler.InheritPhaseShift(this, creator);

            SetEntry(conversationEntry);
            SetObjectScale(1.0f);

            SetUInt32Value(ConversationFields.LastLineEndTime, conversationTemplate.LastLineEndTime);
            _duration = conversationTemplate.LastLineEndTime;
            _textureKitId = conversationTemplate.TextureKitId;

            for (ushort actorIndex = 0; actorIndex < conversationTemplate.Actors.Count; ++actorIndex)
            {
                ConversationActorTemplate actor = conversationTemplate.Actors[actorIndex];
                if (actor != null)
                {
                    ConversationDynamicFieldActor actorField = new ConversationDynamicFieldActor();
                    actorField.ActorTemplate.CreatureId = actor.CreatureId;
                    actorField.ActorTemplate.CreatureModelId = actor.CreatureModelId;
                    actorField.Type = ConversationDynamicFieldActor.ActorType.CreatureActor;
                    SetDynamicStructuredValue(ConversationDynamicFields.Actors, actorIndex, actorField);
                }
            }

            for (ushort actorIndex = 0; actorIndex < conversationTemplate.ActorGuids.Count; ++actorIndex)
            {
                ulong actorGuid = conversationTemplate.ActorGuids[actorIndex];
                if (actorGuid == 0)
                    continue;

                foreach (var creature in map.GetCreatureBySpawnIdStore().LookupByKey(actorGuid))
                {
                    // we just need the last one, overriding is legit
                    AddActor(creature.GetGUID(), actorIndex);
                }
            }

            Global.ScriptMgr.OnConversationCreate(this, creator);

            List<ushort> actorIndices = new List<ushort>();
            foreach (ConversationLineTemplate line in conversationTemplate.Lines)
            {
                actorIndices.Add(line.ActorIdx);
                AddDynamicStructuredValue(ConversationDynamicFields.Lines, line);
            }

            // All actors need to be set
            foreach (ushort actorIndex in actorIndices)
            {
                ConversationDynamicFieldActor actor = GetDynamicStructuredValue<ConversationDynamicFieldActor>(ConversationDynamicFields.Actors, actorIndex);
                if (actor == null || actor.IsEmpty())
                {
                    Log.outError(LogFilter.Conversation, $"Failed to create conversation (Id: {conversationEntry}) due to missing actor (Idx: {actorIndex}).");
                    return false;
                }
            }

            if (!GetMap().AddToMap(this))
                return false;

            return true;
        }

        void AddActor(ObjectGuid actorGuid, ushort actorIdx)
        {
            ConversationDynamicFieldActor actorField = new ConversationDynamicFieldActor();
            actorField.ActorGuid = actorGuid;
            actorField.Type = ConversationDynamicFieldActor.ActorType.WorldObjectActor;
            SetDynamicStructuredValue(ConversationDynamicFields.Actors, actorIdx, actorField);
        }

        void AddParticipant(ObjectGuid participantGuid)
        {
            _participants.Add(participantGuid);
        }

        public uint GetScriptId()
        {
            return Global.ConversationDataStorage.GetConversationTemplate(GetEntry()).ScriptId;
        }

        uint GetDuration() { return _duration; }
        public uint GetTextureKitId() { return _textureKitId; }

        public ObjectGuid GetCreatorGuid() { return _creatorGuid; }

        public override float GetStationaryX() { return _stationaryPosition.GetPositionX(); }
        public override float GetStationaryY() { return _stationaryPosition.GetPositionY(); }
        public override float GetStationaryZ() { return _stationaryPosition.GetPositionZ(); }
        public override float GetStationaryO() { return _stationaryPosition.GetOrientation(); }
        void RelocateStationaryPosition(Position pos) { _stationaryPosition.Relocate(pos); }

        Position _stationaryPosition = new Position();
        ObjectGuid _creatorGuid;
        uint _duration;
        uint _textureKitId;
        List<ObjectGuid> _participants = new List<ObjectGuid>();
    }

    class ConversationDynamicFieldActor
    {
        public enum ActorType
        {
            WorldObjectActor = 0,
            CreatureActor = 1
        }

        public bool IsEmpty()
        {
            return ActorGuid.IsEmpty(); // this one is good enough
        }

        public ObjectGuid ActorGuid;
        public ActorTemplateStruct ActorTemplate;

        public struct ActorTemplateStruct
        {
            public uint CreatureId;
            public uint CreatureModelId;
        }

        public ActorType Type;
    }
}
