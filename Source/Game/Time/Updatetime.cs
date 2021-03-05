using System;

namespace Game
{
    public class UpdateTime
    {
        private uint[] _updateTimeDataTable = new uint[500];
        private uint _averageUpdateTime;
        private uint _totalUpdateTime;
        private uint _updateTimeTableIndex;
        private uint _maxUpdateTime;
        private uint _maxUpdateTimeOfLastTable;
        private uint _maxUpdateTimeOfCurrentTable;

        private uint _recordedTime;

        public uint GetAverageUpdateTime()
        {
            return _averageUpdateTime;
        }

        public uint GetTimeWeightedAverageUpdateTime()
        {
            uint sum = 0, weightsum = 0;
            foreach (var diff in _updateTimeDataTable)
            {
                sum += diff * diff;
                weightsum += diff;
            }
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

            if (_updateTimeDataTable[_updateTimeDataTable.Length - 1] != 0)
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
            var thisTime = Time.GetMSTime();
            var diff = Time.GetMSTimeDiff(_recordedTime, thisTime);

            if (diff > minUpdateTime)
                Log.outInfo(LogFilter.Misc, $"Recored Update Time of {text}: {diff}.");

            _recordedTime = thisTime;
        }
    }

    public class WorldUpdateTime : UpdateTime
    {
        private uint _recordUpdateTimeInverval;
        private uint _recordUpdateTimeMin;
        private uint _lastRecordTime;

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
