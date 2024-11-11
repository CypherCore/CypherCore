// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public class Conversation : WorldObject
    {
        public Conversation() : base(false)
        {
            ObjectTypeMask |= TypeMask.Conversation;
            ObjectTypeId = TypeId.Conversation;

            m_updateFlag.Stationary = true;
            m_updateFlag.Conversation = true;

            m_conversationData = new ConversationData();
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

        public override void Update(uint diff)
        {
            Global.ScriptMgr.OnConversationUpdate(this, diff);

            if (GetDuration() > TimeSpan.FromMilliseconds(diff))
            {
                _duration -= TimeSpan.FromMilliseconds(diff);
                DoWithSuppressingObjectUpdates(() =>
                {
                    // Only sent in CreateObject
                    ApplyModUpdateFieldValue(m_values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.Progress), diff, true);
                    m_conversationData.ClearChanged(m_conversationData.Progress);
                });
            }
            else
            {
                Remove(); // expired
                return;
            }

            base.Update(diff);
        }

        public void Remove()
        {
            if (IsInWorld)
            {
                AddObjectToRemoveList(); // calls RemoveFromWorld
            }
        }

        public static Conversation CreateConversation(uint conversationEntry, Unit creator, Position pos, ObjectGuid privateObjectOwner, SpellInfo spellInfo = null, bool autoStart = true)
        {
            ConversationTemplate conversationTemplate = Global.ConversationDataStorage.GetConversationTemplate(conversationEntry);
            if (conversationTemplate == null)
                return null;

            ulong lowGuid = creator.GetMap().GenerateLowGuid(HighGuid.Conversation);

            Conversation conversation = new();
            conversation.Create(lowGuid, conversationEntry, creator.GetMap(), creator, pos, privateObjectOwner, spellInfo);
            if (autoStart && !conversation.Start())
                return null;

            return conversation;
        }

        void Create(ulong lowGuid, uint conversationEntry, Map map, Unit creator, Position pos, ObjectGuid privateObjectOwner, SpellInfo spellInfo = null)
        {
            ConversationTemplate conversationTemplate = Global.ConversationDataStorage.GetConversationTemplate(conversationEntry);
            //ASSERT(conversationTemplate);

            _creatorGuid = creator.GetGUID();
            SetPrivateObjectOwner(privateObjectOwner);

            SetMap(map);
            Relocate(pos);
            RelocateStationaryPosition(pos);

            _Create(ObjectGuid.Create(HighGuid.Conversation, GetMapId(), conversationEntry, lowGuid));
            PhasingHandler.InheritPhaseShift(this, creator);

            UpdatePositionData();
            SetZoneScript();

            SetEntry(conversationEntry);
            SetObjectScale(1.0f);

            _textureKitId = conversationTemplate.TextureKitId;

            foreach (var actor in conversationTemplate.Actors)
                new ConversationActorFillVisitor(this, creator, map, actor).Invoke(actor);

            Global.ScriptMgr.OnConversationCreate(this, creator);

            List<ConversationLine> lines = new();
            foreach (ConversationLineTemplate line in conversationTemplate.Lines)
            {
                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.ConversationLine, line.Id, creator))
                    continue;

                var  convoLine = CliDB.ConversationLineStorage.LookupByKey(line.Id); // never null for conversationTemplate->Lines

                ConversationLine lineField = new();
                lineField.ConversationLineID = line.Id;
                lineField.BroadcastTextID = convoLine.BroadcastTextID;
                lineField.UiCameraID = line.UiCameraID;
                lineField.ActorIndex = line.ActorIdx;
                lineField.Flags = line.Flags;
                lineField.ChatType = line.ChatType;

                for (Locale locale = Locale.enUS; locale < Locale.Total; locale = locale + 1)
                {
                    if (locale == Locale.None)
                        continue;

                    _lineStartTimes[(locale, line.Id)] = _lastLineEndTimes[(int)locale];
                    if (locale == Locale.enUS)
                        lineField.StartTime = (uint)_lastLineEndTimes[(int)locale].TotalMilliseconds;

                    int broadcastTextDuration = Global.DB2Mgr.GetBroadcastTextDuration(convoLine.BroadcastTextID, locale);
                    if (broadcastTextDuration != 0)
                        _lastLineEndTimes[(int)locale] += TimeSpan.FromMilliseconds(broadcastTextDuration);

                    _lastLineEndTimes[(int)locale] += TimeSpan.FromMilliseconds(convoLine.AdditionalDuration);
                }

                lines.Add(lineField);
            }

            _duration = _lastLineEndTimes.Max();
            SetUpdateFieldValue(m_values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.LastLineEndTime), (uint)_duration.TotalMilliseconds);
            SetUpdateFieldValue(m_values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.Lines), lines);

            // conversations are despawned 5-20s after LastLineEndTime
            _duration += TimeSpan.FromSeconds(10);

            Global.ScriptMgr.OnConversationCreate(this, creator);
        }

        public bool Start()
        {
            ConversationTemplate conversationTemplate = Global.ConversationDataStorage.GetConversationTemplate(GetEntry()); // never null, already checked in ::Create / ::CreateConversation
            if (!conversationTemplate.Flags.HasFlag(ConversationFlags.AllowWithoutSpawnedActor))
            {
                foreach (ConversationLine line in m_conversationData.Lines.GetValue())
                {
                    ConversationActorField actor = line.ActorIndex < m_conversationData.Actors.Size() ? m_conversationData.Actors[line.ActorIndex] : null;
                    if (actor == null || (actor.CreatureID == 0 && actor.ActorGUID.IsEmpty() && actor.NoActorObject == 0))
                    {
                        Log.outError(LogFilter.Conversation, $"Failed to create conversation (Id: {GetEntry()}) due to missing actor (Idx: {line.ActorIndex}).");
                        return false;
                    }
                }
            }

            if (IsInWorld)
            {
                Log.outError(LogFilter.Conversation, $"Attempted to start conversation (Id: {GetEntry()}) multiple times.");
                return true; // returning true to not cause delete in Conversation::CreateConversation if convo is already started in ConversationScript::OnConversationCreate
            }

            if (!GetMap().AddToMap(this))
                return false;

            Global.ScriptMgr.OnConversationStart(this);
            return true;
        }

        public void AddActor(int actorId, uint actorIdx, ObjectGuid actorGuid)
        {
            ConversationActorField actorField = m_values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.Actors, (int)actorIdx);
            SetUpdateFieldValue(ref actorField.CreatureID, 0u);
            SetUpdateFieldValue(ref actorField.CreatureDisplayInfoID, 0u);
            SetUpdateFieldValue(ref actorField.ActorGUID, actorGuid);
            SetUpdateFieldValue(ref actorField.Id, actorId);
            SetUpdateFieldValue(ref actorField.Type, ConversationActorType.WorldObject);
            SetUpdateFieldValue(ref actorField.NoActorObject, 0u);
        }

        public void AddActor(int actorId, uint actorIdx, ConversationActorType type, uint creatureId, uint creatureDisplayInfoId)
        {
            ConversationActorField actorField = m_values.ModifyValue(m_conversationData).ModifyValue(m_conversationData.Actors, (int)actorIdx);
            SetUpdateFieldValue(ref actorField.CreatureID, creatureId);
            SetUpdateFieldValue(ref actorField.CreatureDisplayInfoID, creatureDisplayInfoId);
            SetUpdateFieldValue(ref actorField.ActorGUID, ObjectGuid.Empty);
            SetUpdateFieldValue(ref actorField.Id, actorId);
            SetUpdateFieldValue(ref actorField.Type, type);
            SetUpdateFieldValue(ref actorField.NoActorObject, type == ConversationActorType.WorldObject ? 1 : 0u);
        }

        public TimeSpan GetLineStartTime(Locale locale, int lineId)
        {
            return _lineStartTimes.LookupByKey((locale, lineId));
        }

        public TimeSpan GetLastLineEndTime(Locale locale)
        {
            return _lastLineEndTimes[(int)locale];
        }

        public int GetLineDuration(Locale locale, int lineId)
        {
            var convoLine = CliDB.ConversationLineStorage.LookupByKey(lineId);
            if (convoLine == null)
            {
                Log.outError(LogFilter.Conversation, $"Conversation::GetLineDuration: Tried to get duration for invalid ConversationLine id {lineId}.");
                return 0;
            }

            int textDuration = Global.DB2Mgr.GetBroadcastTextDuration(convoLine.BroadcastTextID, locale);
            if (textDuration == 0)
                return 0;

            return textDuration + convoLine.AdditionalDuration;
        }

        public TimeSpan GetLineEndTime(Locale locale, int lineId)
        {
            TimeSpan lineStartTime = GetLineStartTime(locale, lineId);
            if (lineStartTime == TimeSpan.Zero)
            {
                Log.outError(LogFilter.Conversation, $"Conversation::GetLineEndTime: Unable to get line start time for locale {locale}, lineid {lineId} (Conversation ID: {GetEntry()}).");
                return TimeSpan.Zero;
            }
            return lineStartTime + TimeSpan.FromMilliseconds(GetLineDuration(locale, lineId));
        }

        public Locale GetPrivateObjectOwnerLocale()
        {
            Locale privateOwnerLocale = Locale.enUS;
            Player owner = Global.ObjAccessor.GetPlayer(this, GetPrivateObjectOwner());
            if (owner != null)
                privateOwnerLocale = owner.GetSession().GetSessionDbLocaleIndex();
            return privateOwnerLocale;
        }

        public Unit GetActorUnit(int actorIdx)
        {
            if (m_conversationData.Actors.Size() <= actorIdx)
            {
                Log.outError(LogFilter.Conversation, $"Conversation::GetActorUnit: Tried to access invalid actor idx {actorIdx} (Conversation ID: {GetEntry()}).");
                return null;
            }
            return Global.ObjAccessor.GetUnit(this, m_conversationData.Actors[actorIdx].ActorGUID);
        }

        public Creature GetActorCreature(int actorIdx)
        {
            Unit actor = GetActorUnit(actorIdx);
            if (actor == null)
                return null;

            return actor.ToCreature();
        }

        public uint GetScriptId()
        {
            return Global.ConversationDataStorage.GetConversationTemplate(GetEntry()).ScriptId;
        }

        public override void BuildValuesCreate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            m_objectData.WriteCreate(data, flags, this, target);
            m_conversationData.WriteCreate(data, flags, this, target);
        }

        public override void BuildValuesUpdate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            data.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(data, flags, this, target);

            if (m_values.HasChanged(TypeId.Conversation))
                m_conversationData.WriteUpdate(data, flags, this, target);
        }

        void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedConversationMask, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedConversationMask.IsAnySet())
                valuesMask.Set((int)TypeId.Conversation);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Conversation])
                m_conversationData.WriteUpdate(buffer, requestedConversationMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_conversationData);
            base.ClearUpdateMask(remove);
        }

        TimeSpan GetDuration() { return _duration; }
        public uint GetTextureKitId() { return _textureKitId; }

        public override ObjectGuid GetCreatorGUID() { return _creatorGuid; }
        public override ObjectGuid GetOwnerGUID() { return GetCreatorGUID(); }
        public override uint GetFaction() { return 0; }

        public override float GetStationaryX() { return _stationaryPosition.GetPositionX(); }
        public override float GetStationaryY() { return _stationaryPosition.GetPositionY(); }
        public override float GetStationaryZ() { return _stationaryPosition.GetPositionZ(); }
        public override float GetStationaryO() { return _stationaryPosition.GetOrientation(); }
        void RelocateStationaryPosition(Position pos) { _stationaryPosition.Relocate(pos); }

        ConversationData m_conversationData;

        Position _stationaryPosition = new();
        ObjectGuid _creatorGuid;
        TimeSpan _duration;
        uint _textureKitId;

        Dictionary<(Locale locale, uint lineId), TimeSpan> _lineStartTimes = new();
        TimeSpan[] _lastLineEndTimes = new TimeSpan[(int)Locale.Total];

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            Conversation Owner;
            ObjectFieldData ObjectMask = new();
            ConversationData ConversationMask = new();

            public ValuesUpdateForPlayerWithMaskSender(Conversation owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), ConversationMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
    }

    class ConversationActorFillVisitor
    {
        Conversation _conversation;
        Unit _creator;
        Map _map;
        ConversationActorTemplate _actor;

        public ConversationActorFillVisitor(Conversation conversation, Unit creator, Map map, ConversationActorTemplate actor)
        {
            _conversation = conversation;
            _creator = creator;
            _map = map;
            _actor = actor;

        }

        public void Invoke(ConversationActorTemplate template)
        {
            if (template.WorldObjectTemplate == null)
                Invoke(template.WorldObjectTemplate);

            if (template.NoObjectTemplate == null)
                Invoke(template.NoObjectTemplate);

            if (template.ActivePlayerTemplate == null)
                Invoke(template.ActivePlayerTemplate);

            if (template.TalkingHeadTemplate == null)
                Invoke(template.TalkingHeadTemplate);
        }

        public void Invoke(ConversationActorWorldObjectTemplate worldObject)
        {
            Creature bestFit = null;

            foreach (var creature in _map.GetCreatureBySpawnIdStore().LookupByKey(worldObject.SpawnId))
            {
                bestFit = creature;

                // If creature is in a personal phase then we pick that one specifically
                if (creature.GetPhaseShift().GetPersonalGuid() == _creator.GetGUID())
                    break;
            }

            if (bestFit != null)
                _conversation.AddActor(_actor.Id, _actor.Index, bestFit.GetGUID());
        }

        public void Invoke(ConversationActorNoObjectTemplate noObject)
        {
            _conversation.AddActor(_actor.Id, _actor.Index, ConversationActorType.WorldObject, noObject.CreatureId, noObject.CreatureDisplayInfoId);
        }

        public void Invoke(ConversationActorActivePlayerTemplate activePlayer)
        {
            _conversation.AddActor(_actor.Id, _actor.Index, ObjectGuid.Create(HighGuid.Player, 0xFFFFFFFFFFFFFFFF));
        }

        public void Invoke(ConversationActorTalkingHeadTemplate talkingHead)
        {
            _conversation.AddActor(_actor.Id, _actor.Index, ConversationActorType.TalkingHead, talkingHead.CreatureId, talkingHead.CreatureDisplayInfoId);
        }
    }
}
