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

using Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;

namespace Game.DataStorage
{
    public class ConversationDataStorage : Singleton<ConversationDataStorage>
    {
        ConversationDataStorage() { }

        public void LoadConversationTemplates()
        {
            _conversationActorTemplateStorage.Clear();
            _conversationLineTemplateStorage.Clear();
            _conversationTemplateStorage.Clear();

            Dictionary<uint, ConversationActorTemplate[]> actorsByConversation = new Dictionary<uint, ConversationActorTemplate[]>();
            Dictionary<uint, ulong[]> actorGuidsByConversation = new Dictionary<uint, ulong[]>();

            SQLResult actorTemplates = DB.World.Query("SELECT Id, CreatureId, CreatureModelId FROM conversation_actor_template");
            if (!actorTemplates.IsEmpty())
            {
                uint oldMSTime = Time.GetMSTime();

                do
                {
                    uint id = actorTemplates.Read<uint>(0);
                    ConversationActorTemplate conversationActor = new ConversationActorTemplate();
                    conversationActor.Id = id;
                    conversationActor.CreatureId = actorTemplates.Read<uint>(1);
                    conversationActor.CreatureModelId = actorTemplates.Read<uint>(2);

                    _conversationActorTemplateStorage[id] = conversationActor;
                }
                while (actorTemplates.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Conversation actor templates in {1} ms", _conversationActorTemplateStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Conversation actor templates. DB table `conversation_actor_template` is empty.");
            }

            SQLResult lineTemplates = DB.World.Query("SELECT Id, StartTime, UiCameraID, ActorIdx, Flags FROM conversation_line_template");
            if (!lineTemplates.IsEmpty())
            {
                uint oldMSTime = Time.GetMSTime();

                do
                {
                    uint id = lineTemplates.Read<uint>(0);

                    if (!CliDB.ConversationLineStorage.ContainsKey(id))
                    {
                        Log.outError(LogFilter.Sql, "Table `conversation_line_template` has template for non existing ConversationLine (ID: {0}), skipped", id);
                        continue;
                    }

                    ConversationLineTemplate conversationLine = new ConversationLineTemplate();
                    conversationLine.Id = id;
                    conversationLine.StartTime = lineTemplates.Read<uint>(1);
                    conversationLine.UiCameraID = lineTemplates.Read<uint>(2);
                    conversationLine.ActorIdx = lineTemplates.Read<byte>(3);
                    conversationLine.Flags = lineTemplates.Read<byte>(4);

                    _conversationLineTemplateStorage[id] = conversationLine;
                }
                while (lineTemplates.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Conversation line templates in {1} ms", _conversationLineTemplateStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Conversation line templates. DB table `conversation_line_template` is empty.");
            }

            SQLResult actorResult = DB.World.Query("SELECT ConversationId, ConversationActorId, ConversationActorGuid, Idx FROM conversation_actors");
            if (!actorResult.IsEmpty())
            {
                uint oldMSTime = Time.GetMSTime();
                uint count = 0;

                do
                {
                    uint conversationId = actorResult.Read<uint>(0);
                    uint actorId = actorResult.Read<uint>(1);
                    ulong actorGuid = actorResult.Read<ulong>(2);
                    ushort idx = actorResult.Read<ushort>(3);

                    if (actorId != 0 && actorGuid != 0)
                    {
                        Log.outError(LogFilter.Sql, $"Table `conversation_actors` references both actor (ID: {actorId}) and actorGuid (GUID: {actorGuid}) for Conversation {conversationId}, skipped.");
                        continue;
                    }
                    if (actorId != 0)
                    {
                        ConversationActorTemplate conversationActorTemplate = _conversationActorTemplateStorage.LookupByKey(actorId);
                        if (conversationActorTemplate != null)
                        {
                            if (!actorsByConversation.ContainsKey(conversationId))
                                actorsByConversation[conversationId] = new ConversationActorTemplate[idx + 1];

                            ConversationActorTemplate[] actors = actorsByConversation[conversationId];
                            if (actors.Length <= idx)
                                Array.Resize(ref actors, idx + 1);

                            actors[idx] = conversationActorTemplate;
                            ++count;
                        }
                        else
                            Log.outError(LogFilter.Sql, "Table `conversation_actors` references an invalid actor (ID: {0}) for Conversation {1}, skipped", actorId, conversationId);
                    }
                    else if (actorGuid != 0)
                    {
                        CreatureData creData = Global.ObjectMgr.GetCreatureData(actorGuid);
                        if (creData != null)
                        {
                            if (!actorGuidsByConversation.ContainsKey(conversationId))
                                actorGuidsByConversation[conversationId] = new ulong[idx + 1];

                            var guids = actorGuidsByConversation[conversationId];
                            if (guids.Length <= idx)
                                Array.Resize(ref guids, idx + 1);

                            guids[idx] = actorGuid;
                            ++count;
                        }
                        else
                            Log.outError(LogFilter.Sql, $"Table `conversation_actors` references an invalid creature guid (GUID: {actorGuid}) for Conversation {conversationId}, skipped");
                    }

                }
                while (actorResult.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Conversation actors in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Conversation actors. DB table `conversation_actors` is empty.");
            }

            SQLResult templateResult = DB.World.Query("SELECT Id, FirstLineId, LastLineEndTime, TextureKitId, ScriptName FROM conversation_template");
            if (!templateResult.IsEmpty())
            {
                uint oldMSTime = Time.GetMSTime();

                do
                {
                    ConversationTemplate conversationTemplate = new ConversationTemplate();
                    conversationTemplate.Id = templateResult.Read<uint>(0);
                    conversationTemplate.FirstLineId = templateResult.Read<uint>(1);
                    conversationTemplate.LastLineEndTime = templateResult.Read<uint>(2);
                    conversationTemplate.TextureKitId = templateResult.Read<uint>(3);
                    conversationTemplate.ScriptId = Global.ObjectMgr.GetScriptId(templateResult.Read<string>(3));

                    conversationTemplate.Actors = actorsByConversation[conversationTemplate.Id].ToList();
                    conversationTemplate.ActorGuids = actorGuidsByConversation[conversationTemplate.Id].ToList();

                    ConversationLineRecord currentConversationLine = CliDB.ConversationLineStorage.LookupByKey(conversationTemplate.FirstLineId);
                    if (currentConversationLine == null)
                        Log.outError(LogFilter.Sql, "Table `conversation_template` references an invalid line (ID: {0}) for Conversation {1}, skipped", conversationTemplate.FirstLineId, conversationTemplate.Id);

                    while (currentConversationLine != null)
                    {
                        ConversationLineTemplate conversationLineTemplate = _conversationLineTemplateStorage.LookupByKey(currentConversationLine.Id);
                        if (conversationLineTemplate != null)
                            conversationTemplate.Lines.Add(conversationLineTemplate);
                        else
                            Log.outError(LogFilter.Sql, "Table `conversation_line_template` has missing template for line (ID: {0}) in Conversation {1}, skipped", currentConversationLine.Id, conversationTemplate.Id);

                        if (currentConversationLine.NextConversationLineID == 0)
                            break;

                        currentConversationLine = CliDB.ConversationLineStorage.LookupByKey(currentConversationLine.NextConversationLineID);
                    }

                    _conversationTemplateStorage[conversationTemplate.Id] = conversationTemplate;
                }
                while (templateResult.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Conversation templates in {1} ms", _conversationTemplateStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Conversation templates. DB table `conversation_template` is empty.");
            }
        }

        public ConversationTemplate GetConversationTemplate(uint conversationId)
        {
            return _conversationTemplateStorage.LookupByKey(conversationId);
        }

        Dictionary<uint, ConversationTemplate> _conversationTemplateStorage = new Dictionary<uint, ConversationTemplate>();
        Dictionary<uint, ConversationActorTemplate> _conversationActorTemplateStorage = new Dictionary<uint, ConversationActorTemplate>();
        Dictionary<uint, ConversationLineTemplate> _conversationLineTemplateStorage = new Dictionary<uint, ConversationLineTemplate>();
    }

    public class ConversationActorTemplate
    {
        public uint Id;
        public uint CreatureId;
        public uint CreatureModelId;
    }

    public class ConversationLineTemplate
    {
        public uint Id;          // Link to ConversationLine.db2
        public uint StartTime;   // Time in ms after conversation creation the line is displayed
        public uint UiCameraID;  // Link to UiCamera.db2
        public byte ActorIdx;    // Index from conversation_actors
        public byte Flags;
        public ushort Padding;
    }

    public class ConversationTemplate
    {
        public uint Id;
        public uint FirstLineId;     // Link to ConversationLine.db2
        public uint LastLineEndTime; // Time in ms after conversation creation the last line fades out
        public uint TextureKitId;    // Background texture
        public uint ScriptId;

        public List<ConversationActorTemplate> Actors = new List<ConversationActorTemplate>();
        public List<ulong> ActorGuids = new List<ulong>();
        public List<ConversationLineTemplate> Lines = new List<ConversationLineTemplate>();
    }


}
