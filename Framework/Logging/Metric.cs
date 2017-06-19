using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

class Metric
{
    void Initialize(string realmName, Action overallStatusLogger)
    {
        _realmName = realmName;
        _batchTimer = new Timer();
        _overallStatusTimer = new Timer();
        _overallStatusLogger = overallStatusLogger;
        LoadFromConfigs();
    }

    bool Connect()
    {
        _dataStream.Connect(_hostname, _port);
        auto error = _dataStream.Client.error();
        if (error)
        {
            TC_LOG_ERROR("metric", "Error connecting to '%s:%s', disabling Metric. Error message : %s",
                _hostname.c_str(), _port.c_str(), error.message().c_str());
            _enabled = false;
            return false;
        }
        _dataStream.clear();
        return true;
    }

    void LoadFromConfigs()
    {
        bool previousValue = _enabled;
        _enabled = sConfigMgr.GetBoolDefault("Metric.Enable", false);
        _updateInterval = sConfigMgr.GetIntDefault("Metric.Interval", 10);
        if (_updateInterval < 1)
        {
            TC_LOG_ERROR("metric", "'Metric.Interval' config set to %d, overriding to 1.", _updateInterval);
            _updateInterval = 1;
        }

        _overallStatusTimerInterval = sConfigMgr.GetIntDefault("Metric.OverallStatusInterval", 1);
        if (_overallStatusTimerInterval < 1)
        {
            TC_LOG_ERROR("metric", "'Metric.OverallStatusInterval' config set to %d, overriding to 1.", _overallStatusTimerInterval);
            _overallStatusTimerInterval = 1;
        }

        // Schedule a send at this point only if the config changed from Disabled to Enabled.
        // Cancel any scheduled operation if the config changed from Enabled to Disabled.
        if (_enabled && !previousValue)
        {
            std::string connectionInfo = sConfigMgr.GetStringDefault("Metric.ConnectionInfo", "");
            if (connectionInfo.empty())
            {
                TC_LOG_ERROR("metric", "'Metric.ConnectionInfo' not specified in configuration file.");
                return;
            }

            Tokenizer tokens(connectionInfo, ';');
            if (tokens.size() != 3)
            {
                TC_LOG_ERROR("metric", "'Metric.ConnectionInfo' specified with wrong format in configuration file.");
                return;
            }

            _hostname.assign(tokens[0]);
            _port.assign(tokens[1]);
            _databaseName.assign(tokens[2]);
            Connect();

            ScheduleSend();
            ScheduleOverallStatusLog();
        }
    }

    void Update()
    {
        if (_overallStatusTimerTriggered)
        {
            _overallStatusTimerTriggered = false;
            _overallStatusLogger();
        }
    }

    void LogEvent(string category, string title, string description)
    {
        MetricData data = new MetricData();
        data.Category = category;
        data.Timestamp = system_clock::now();
        data.Type = METRIC_DATA_EVENT;
        data.Title = title;
        data.Text = description;

        _queuedData.Enqueue(data);
    }

    void SendBatch()
    {
        std::stringstream batchedData;
        MetricData data;
        bool firstLoop = true;
        while (_queuedData.Dequeue(data))
        {
            if (!firstLoop)
                batchedData << "\n";

            batchedData << data.Category;
            if (!_realmName.empty())
                batchedData << ",realm=" << _realmName;

            batchedData << " ";

            switch (data.Type)
            {
                case METRIC_DATA_VALUE:
                    batchedData << "value=" << data.Value;
                    break;
                case METRIC_DATA_EVENT:
                    batchedData << "title=\"" << data.Title << "\",text=\"" << data.Text << "\"";
                    break;
            }

            batchedData << " ";

            batchedData << std::to_string(duration_cast<nanoseconds>(data.Timestamp.time_since_epoch()).count());

            firstLoop = false;
            delete data;
        }

        // Check if there's any data to send
        if (batchedData.tellp() == std::streampos(0))
        {
            ScheduleSend();
            return;
        }

        if (!_dataStream.good() && !Connect())
            return;

        var blah = new InfluxDB.Net.InfluxDb(_hostname, "root", "root", InfluxDB.Net.Enums.InfluxVersion.v010x);
        InfluxDB.Net.Models.Serie b = new InfluxDB.Net.Models.Serie();
        b.Columns
        _dataStream.UploadString( << "POST " << "/write?db=" << _databaseName << " HTTP/1.1\r\n";
        _dataStream << "Host: " << _hostname << ":" << _port << "\r\n";
        _dataStream << "Accept: */*\r\n";
        _dataStream << "Content-Type: application/octet-stream\r\n";
        _dataStream << "Content-Transfer-Encoding: binary\r\n";

        _dataStream << "Content-Length: " << std::to_string(batchedData.tellp()) << "\r\n\r\n";
        _dataStream << batchedData.rdbuf();

        std::string http_version;
        _dataStream >> http_version;
        unsigned int status_code = 0;
        _dataStream >> status_code;
        if (status_code != 204)
        {
            TC_LOG_ERROR("metric", "Error sending data, returned HTTP code: %u", status_code);
        }

        // Read and ignore the status description
        std::string status_description;
        std::getline(_dataStream, status_description);
        // Read headers
        std::string header;
        while (std::getline(_dataStream, header) && header != "\r")
        {
            if (header == "Connection: close\r")
                _dataStream.close();
        }

        ScheduleSend();
    }

    void ScheduleSend()
    {
        if (_enabled)
        {
            _batchTimer.expires_from_now(boost::posix_time::seconds(_updateInterval));
            _batchTimer.async_wait(std::bind(&SendBatch, this));
        }
        else
        {
            _dataStream.close();
            MetricData* data;
            // Clear the queue
            while (_queuedData.Dequeue(data))
                ;
        }
    }

    void ForceSend()
    {
        // Send what's queued only if io_service is stopped (so only on shutdown)
        if (_enabled && _batchTimer.get_io_service().stopped())
            SendBatch();
    }

    void ScheduleOverallStatusLog()
    {
        if (_enabled)
        {
            _overallStatusTimer.expires_from_now(boost::posix_time::seconds(_overallStatusTimerInterval));
            _overallStatusTimer.async_wait([this](const boost::system::error_code&)
        {
                _overallStatusTimerTriggered = true;
                ScheduleOverallStatusLog();
            });
        }
    }

    static string FormatInfluxDBValue<T>(T value) { return Convert.ToString(value) + 'i'; }

    static string FormatInfluxDBValue(string value)
    {
        return '"' + boost::replace_all_copy(value, "\"", "\\\"") + '"';
    }
    static string FormatInfluxDBValue(bool value) { return value ? "t" : "f"; }
    static string FormatInfluxDBValue(char[] value) { return FormatInfluxDBValue(new string(value)); }
    static string FormatInfluxDBValue(double value) { return Convert.ToString(value); }
    static string FormatInfluxDBValue(float value) { return FormatInfluxDBValue((double)value); }

    void LogValue<T>(string category, T value)
    {
        MetricData data = new MetricData();
        data.Category = category;
        data.Timestamp = DateTime.Now;
        data.Type = MetricDataType.Value;
        data.Value = FormatInfluxDBValue(value);

        _queuedData.Enqueue(data);
    }

    System.Net.WebClient _dataStream;
    MPSCQueue<MetricData> _queuedData;
    Timer _batchTimer;
    Timer _overallStatusTimer;
    int _updateInterval = 0;
    int _overallStatusTimerInterval = 0;
    bool _enabled = false;
    bool _overallStatusTimerTriggered = false;
    string _hostname;
    string _port;
    string _databaseName;
    Action _overallStatusLogger;
    string _realmName;
}

struct MetricData
{
    public string Category;
    public DateTime Timestamp;
    public MetricDataType Type;

    // LogValue-specific fields
    public string Value;

    // LogEvent-specific fields
    public string Title;
    public string Text;
}

enum MetricDataType
{
    Value,
    Event
}

