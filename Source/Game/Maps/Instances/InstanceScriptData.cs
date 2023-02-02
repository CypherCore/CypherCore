// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Game.Maps
{
    class InstanceScriptDataReader
    {
        public enum Result
        {
            Ok,
            MalformedJson,
            RootIsNotAnObject,
            MissingHeader,
            UnexpectedHeader,
            MissingBossStates,
            BossStatesIsNotAnObject,
            UnknownBoss,
            BossStateIsNotAnObject,
            MissingBossState,
            BossStateValueIsNotANumber,
            AdditionalDataIsNotAnObject,
            AdditionalDataUnexpectedValueType
        }

        InstanceScript _instance;
        JsonDocument _doc;

        public InstanceScriptDataReader(InstanceScript instance)
        {
            _instance = instance;
        }

        public Result Load(string data)
        {
            /*
               Expected JSON

                {
                    "Header": "HEADER_STRING_SET_BY_SCRIPT",
                    "BossStates": [0,2,0,...] // indexes are boss ids, values are EncounterState
                    "AdditionalData: { // optional
                        "ExtraKey1": 123
                        "AnotherExtraKey": 2.0
                    }
                }
            */

            try
            {
                _doc = JsonDocument.Parse(data);
            }
            catch (JsonException ex)
            {
                Log.outError(LogFilter.Scripts, $"JSON parser error {ex.Message} at {ex.LineNumber} while loading data for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");
                return Result.MalformedJson;
            }

            if (_doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                Log.outError(LogFilter.Scripts, $"Root JSON value is not an object for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");
                return Result.RootIsNotAnObject;
            }

            Result result = ParseHeader();
            if (result != Result.Ok)
                return result;

            result = ParseBossStates();
            if (result != Result.Ok)
                return result;

            result = ParseAdditionalData();
            if (result != Result.Ok)
                return result;

            return Result.Ok;
        }

        Result ParseHeader()
        {
            if (!_doc.RootElement.TryGetProperty("Header", out JsonElement header))
            {
                Log.outError(LogFilter.Scripts, $"Missing data header for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");
                return Result.MissingHeader;
            }

            if (header.GetString() != _instance.GetHeader())
            {
                Log.outError(LogFilter.Scripts, $"Incorrect data header for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}], expected \"{_instance.GetHeader()}\" got \"{header.GetString()}\"");
                return Result.UnexpectedHeader;
            }

            return Result.Ok;
        }

        Result ParseBossStates()
        {
            if (!_doc.RootElement.TryGetProperty("BossStates", out JsonElement bossStates))
            {
                Log.outError(LogFilter.Scripts, $"Missing boss states for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");
                return Result.MissingBossStates;
            }

            if (bossStates.ValueKind != JsonValueKind.Array)
            {
                Log.outError(LogFilter.Scripts, $"Boss states is not an array for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");
                return Result.BossStatesIsNotAnObject;
            }

            for (int bossId = 0; bossId < bossStates.GetArrayLength(); ++bossId)
            {
                if (bossId >= _instance.GetEncounterCount())
                {
                    Log.outError(LogFilter.Scripts, $"Boss states has entry for boss with higher id ({bossId}) than number of bosses ({_instance.GetEncounterCount()}) for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");
                    return Result.UnknownBoss;
                }

                var bossState = bossStates[bossId];
                if (bossState.ValueKind != JsonValueKind.Number)
                {
                    Log.outError(LogFilter.Scripts, $"Boss state for boss ({bossId}) is not a number for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");
                    return Result.BossStateIsNotAnObject;
                }

                EncounterState state = (EncounterState)bossState.GetInt32();
                if (state == EncounterState.InProgress || state == EncounterState.Fail || state == EncounterState.Special)
                    state = EncounterState.NotStarted;

                if (state < EncounterState.ToBeDecided)
                    _instance.SetBossState((uint)bossId, state);
            }

            return Result.Ok;
        }

        Result ParseAdditionalData()
        {
            if (!_doc.RootElement.TryGetProperty("AdditionalData", out JsonElement moreData))
                return Result.Ok;

            if (moreData.ValueKind != JsonValueKind.Object)
            {
                Log.outError(LogFilter.Scripts, $"Additional data is not an object for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");
                return Result.AdditionalDataIsNotAnObject;
            }

            foreach (PersistentInstanceScriptValueBase valueBase in _instance.GetPersistentScriptValues())
            {
                if (moreData.TryGetProperty(valueBase.GetName(), out JsonElement value) && value.ValueKind != JsonValueKind.Null)
                {
                    if (value.ValueKind != JsonValueKind.Number)
                    {
                        Log.outError(LogFilter.Scripts, $"Additional data value for key {valueBase.GetName()} is not a number for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");
                        return Result.AdditionalDataUnexpectedValueType;
                    }

                    if (value.TryGetDouble(out double doubleValue))
                        valueBase.LoadValue(doubleValue);
                    else
                        valueBase.LoadValue(value.GetInt64());
                }
            }

            return Result.Ok;
        }

        uint GetInstanceId() { return _instance.instance.GetInstanceId(); }

        uint GetMapId() { return _instance.instance.GetId(); }

        string GetMapName() { return _instance.instance.GetMapName(); }

        uint GetDifficultyId() { return (uint)_instance.instance.GetDifficultyID(); }

        string GetDifficultyName() { return CliDB.DifficultyStorage.LookupByKey(_instance.instance.GetDifficultyID()).Name; }
    }

    class InstanceScriptDataWriter
    {
        InstanceScript _instance;
        JsonObject _doc = new();

        public InstanceScriptDataWriter(InstanceScript instance)
        {
            _instance = instance;
        }

        public string GetString()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                _doc.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public void FillData(bool withValues = true)
        {
            _doc.Add("Header", _instance.GetHeader());

            JsonArray bossStates = new();
            for (uint bossId = 0; bossId < _instance.GetEncounterCount(); ++bossId)
                bossStates.Add(JsonValue.Create((int)(withValues ? _instance.GetBossState(bossId) : EncounterState.NotStarted)));

            _doc.Add("BossStates", bossStates);

            if (!_instance.GetPersistentScriptValues().Empty())
            {
                JsonObject moreData = new();
                foreach (PersistentInstanceScriptValueBase additionalValue in _instance.GetPersistentScriptValues())
                {
                    if (withValues)
                    {
                        UpdateAdditionalSaveDataEvent data = additionalValue.CreateEvent();
                        if (data.Value is double)
                            moreData.Add(data.Key, (double)data.Value);
                        else
                            moreData.Add(data.Key, (long)data.Value);
                    }
                    else
                        moreData.Add(additionalValue.GetName(), null);
                }

                _doc.Add("AdditionalData", moreData);
            }
        }

        public void FillDataFrom(string data)
        {
            try
            {
                _doc = JsonNode.Parse(data).AsObject();
            }
            catch (JsonException)
            {
                FillData(false);
            }
        }

        public void SetBossState(UpdateBossStateSaveDataEvent data)
        {
            var array = _doc["BossStates"].AsArray();
            array[(int)data.BossId] = (int)data.NewState;
        }

        public void SetAdditionalData(UpdateAdditionalSaveDataEvent data)
        {
            var jObject = _doc["AdditionalData"].AsObject();
            if (data.Value is double)
                jObject[data.Key] = (double)data.Value;
            else
                jObject[data.Key] = (long)data.Value;
        }
    }
}
