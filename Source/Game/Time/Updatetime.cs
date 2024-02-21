// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game
{
    public class UpdateTime
    {
        uint[] _updateTimeDataTable = new uint[500];
        uint _averageUpdateTime;
        uint _totalUpdateTime;
        uint _updateTimeTableIndex;
        uint _maxUpdateTime;
        uint _maxUpdateTimeOfLastTable;
        uint _maxUpdateTimeOfCurrentTable;

        uint _recordedTime;

        public uint GetAverageUpdateTime()
        {
            return _averageUpdateTime;
        }

        public uint GetTimeWeightedAverageUpdateTime()
        {
            uint sum = 0, weightsum = 0;
            foreach (uint diff in _updateTimeDataTable)
            {
                sum += diff * diff;
                weightsum += diff;
            }

            if (weightsum == 0)
                return 0;

            return sum / weightsum;
        }

        public uint GetMaxUpdateTime()
        {
            return _maxUpdateTime;
        }

        public uint GetMaxUpdateTimeOfCurrentTable()
        {
            return Math.Max(_maxUpdateTimeOfCurrentTable, _maxUpdateTimeOfLastTable);
        }

        public uint GetLastUpdateTime()
        {
            return _updateTimeDataTable[_updateTimeTableIndex != 0 ? _updateTimeTableIndex - 1 : _updateTimeDataTable.Length - 1u];
        }

        public void UpdateWithDiff(uint diff)
        {
            _totalUpdateTime = _totalUpdateTime - _updateTimeDataTable[_updateTimeTableIndex] + diff;
            _updateTimeDataTable[_updateTimeTableIndex] = diff;

            if (diff > _maxUpdateTime)
                _maxUpdateTime = diff;

            if (diff > _maxUpdateTimeOfCurrentTable)
                _maxUpdateTimeOfCurrentTable = diff;

            if (++_updateTimeTableIndex >= _updateTimeDataTable.Length)
            {
                _updateTimeTableIndex = 0;
                _maxUpdateTimeOfLastTable = _maxUpdateTimeOfCurrentTable;
                _maxUpdateTimeOfCurrentTable = 0;
            }

            if (_updateTimeDataTable[^1] != 0)
                _averageUpdateTime = (uint)(_totalUpdateTime / _updateTimeDataTable.Length);
            else if (_updateTimeTableIndex != 0)
                _averageUpdateTime = _totalUpdateTime / _updateTimeTableIndex;
        }

        public void RecordUpdateTimeReset()
        {
            _recordedTime = Time.GetMSTime();
        }

        public void RecordUpdateTimeDuration(string text, uint minUpdateTime)
        {
            uint thisTime = Time.GetMSTime();
            uint diff = Time.GetMSTimeDiff(_recordedTime, thisTime);

            if (diff > minUpdateTime)
                Log.outInfo(LogFilter.Misc, $"Recored Update Time of {text}: {diff}.");

            _recordedTime = thisTime;
        }
    }

    public class WorldUpdateTime : UpdateTime
    {
        uint _recordUpdateTimeInverval;
        uint _recordUpdateTimeMin;
        uint _lastRecordTime;

        public void LoadFromConfig()
        {
            _recordUpdateTimeInverval = WorldConfig.GetDefaultValue("RecordUpdateTimeDiffInterval", 60000u);
            _recordUpdateTimeMin = WorldConfig.GetDefaultValue("MinRecordUpdateTimeDiff", 100u);
        }

        public void SetRecordUpdateTimeInterval(uint t)
        {
            _recordUpdateTimeInverval = t;
        }

        public void RecordUpdateTime(uint gameTimeMs, uint diff, uint sessionCount)
        {
            if (_recordUpdateTimeInverval > 0 && diff > _recordUpdateTimeMin)
            {
                if (Time.GetMSTimeDiff(_lastRecordTime, gameTimeMs) > _recordUpdateTimeInverval)
                {
                    Log.outDebug(LogFilter.Misc, $"Update time diff: {GetAverageUpdateTime()}. Players online: {sessionCount}.");
                    _lastRecordTime = gameTimeMs;
                }
            }
        }

        public void RecordUpdateTimeDuration(string text)
        {
            RecordUpdateTimeDuration(text, _recordUpdateTimeMin);
        }
    }
}
