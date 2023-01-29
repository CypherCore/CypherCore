using System;
using Framework.Configuration;

namespace Game
{
    public class UpdateTime
    {
        private uint _averageUpdateTime;
        private uint _maxUpdateTime;
        private uint _maxUpdateTimeOfCurrentTable;
        private uint _maxUpdateTimeOfLastTable;

        private uint _recordedTime;
        private uint _totalUpdateTime;
        private readonly uint[] _updateTimeDataTable = new uint[500];
        private uint _updateTimeTableIndex;

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
        private uint _lastRecordTime;
        private uint _recordUpdateTimeInverval;
        private uint _recordUpdateTimeMin;

        public void LoadFromConfig()
        {
            _recordUpdateTimeInverval = ConfigMgr.GetDefaultValue("RecordUpdateTimeDiffInterval", 60000u);
            _recordUpdateTimeMin = ConfigMgr.GetDefaultValue("MinRecordUpdateTimeDiff", 100u);
        }

        public void SetRecordUpdateTimeInterval(uint t)
        {
            _recordUpdateTimeInverval = t;
        }

        public void RecordUpdateTime(uint gameTimeMs, uint diff, uint sessionCount)
        {
            if (_recordUpdateTimeInverval > 0 &&
                diff > _recordUpdateTimeMin)
                if (Time.GetMSTimeDiff(_lastRecordTime, gameTimeMs) > _recordUpdateTimeInverval)
                {
                    Log.outDebug(LogFilter.Misc, $"Update Time diff: {GetAverageUpdateTime()}. Players online: {sessionCount}.");
                    _lastRecordTime = gameTimeMs;
                }
        }

        public void RecordUpdateTimeDuration(string text)
        {
            RecordUpdateTimeDuration(text, _recordUpdateTimeMin);
        }
    }
}