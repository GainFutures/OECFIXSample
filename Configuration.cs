using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using OEC.FIX.Sample.FIX;

namespace OEC.FIX.Sample
{
    internal interface IConfiguration
    {
        void FillProperties(Props properties);
    }

    internal static class Configurations
    {
        public static IConfiguration PredefinedConfiguration
        {
            get
            {
                var parameters = new ConfigurationParameters
                {
                    Host = "api.gainfutures.com",
                    SenderCompID = "MY_SENDER_COMPID",
                    TargetCompID = "OEC_TEST",
                    FutureAccount = "API000001",
                    ForexAccount = "APIFX0001",
                };
                return new ConstValueConfiguration(parameters);
            }
        }

        public static IConfiguration AppSettingsConfiguration => new AppSettingsConfiguration(ConfigurationManager.AppSettings);

        public static IEnumerable<IConfiguration> MultipleTestSessions(int count)
        {
            return Enumerable.Range(1, count).Select(no =>
            {
                var parameters = new ConfigurationParameters
                {
                    Host = "api.gainfutures.com",
                    SenderCompID = $"TEST{no}",
                    TargetCompID = "OEC",
                    FutureAccount = "API000001",
                    ForexAccount = "APIFX0001"
                };
                return new ConstValueConfiguration(parameters);
            });
        }
    }

    internal class ConfigurationParameters
    {
        public string Host { get; set; }
        public string SenderCompID { get; set; }
        public string TargetCompID { get; set; }
        public string UUID { get; set; }

        public string FutureAccount { get; set; }
        public string ForexAccount { get; set; }

        public bool IsSSL { get; set; }

        ///default password
        public string Password { get; set; }
    }

    internal class ConstValueConfiguration : IConfiguration
    {
        private readonly string _host;
        private readonly string _senderCompID;
        private readonly string _tragetCompID;
        private readonly string _UUID;

        private readonly string _futureAccount;
        private readonly string _forexAccount;

        private readonly bool _isSSL;
        private readonly string _password;


        public ConstValueConfiguration(ConfigurationParameters configParameters)
        {
            _host = configParameters.Host;

            _senderCompID = configParameters.SenderCompID;
            _tragetCompID = configParameters.TargetCompID;
            _UUID = configParameters.UUID;

            _futureAccount = configParameters.FutureAccount;
            _forexAccount = configParameters.ForexAccount;
            _isSSL = configParameters.IsSSL;
            _password = configParameters.Password;
        }

        public void FillProperties(Props properties)
        {
            properties.AddProp(Prop.Host, _host);
            properties.AddProp(Prop.Port, 9300);
            properties.AddProp(Prop.FastPort, 9301);
            properties.AddProp(Prop.FastHashCode, "");

            properties.AddProp(Prop.ReconnectInterval, 30);
            properties.AddProp(Prop.HeartbeatInterval, 30);
            properties.AddProp(Prop.MillisecondsInTimestamp, false);

            properties.AddProp(Prop.BeginString, FixVersion.FIX44);
            properties.AddProp(Prop.SenderCompID, _senderCompID);
            properties.AddProp(Prop.TargetCompID, _tragetCompID);

            properties.AddProp(Prop.SessionStart, new TimeSpan(1, 0, 0));
            properties.AddProp(Prop.SessionEnd, new TimeSpan(23, 0, 0));

            properties.AddProp(Prop.ResponseTimeout, TimeSpan.FromSeconds(15));
            properties.AddProp(Prop.ConnectTimeout, TimeSpan.FromSeconds(15));
            properties.AddProp(Prop.LogonTimeout, 30);

            properties.AddProp(Prop.SenderSeqNum, 1);
            properties.AddProp(Prop.TargetSeqNum, 1);

            properties.AddProp(Prop.FutureAccount, _futureAccount);
            properties.AddProp(Prop.ForexAccount, _forexAccount);

            properties.AddProp(Prop.SSL, _isSSL);
            properties.AddProp(Prop.ResetSeqNumbers, true);

            properties.AddProp(Prop.Password, _password);
            properties.AddProp(Prop.UUID, _UUID);
        }
    }

    internal class AppSettingsConfiguration : IConfiguration
    {
        private readonly NameValueCollection _settings;

        public AppSettingsConfiguration(NameValueCollection settings)
        {
            _settings = settings;
        }

        private void AddStringProp(Props properties, string keyName)
        {
            properties.AddProp(keyName, _settings[keyName]);
        }

        private void AddIntProp(Props properties, string keyName)
        {
            properties.AddProp(keyName, int.Parse(_settings[keyName]));
        }

        private void AddBoolProp(Props properties, string keyValue)
        {
            properties.AddProp(keyValue, bool.Parse(_settings[keyValue]));
        }

        private void AddTimeSpanProp(Props properties, string keyValue)
        {
            properties.AddProp(keyValue, TimeSpan.Parse(_settings[keyValue]));
        }

        public void FillProperties(Props properties)
        {
            AddStringProp(properties, Prop.Host);
            AddIntProp(properties, Prop.Port);
            AddIntProp(properties, Prop.FastPort);
            AddStringProp(properties, Prop.FastHashCode);

            AddIntProp(properties, Prop.ReconnectInterval);
            AddIntProp(properties, Prop.HeartbeatInterval);
            AddBoolProp(properties, Prop.MillisecondsInTimestamp);

            AddStringProp(properties, Prop.BeginString);
            AddStringProp(properties, Prop.SenderCompID);
            AddStringProp(properties, Prop.TargetCompID);

            AddTimeSpanProp(properties, Prop.SessionStart);
            AddTimeSpanProp(properties, Prop.SessionEnd);

            AddTimeSpanProp(properties, Prop.ResponseTimeout);
            AddTimeSpanProp(properties, Prop.ConnectTimeout);
            AddIntProp(properties, Prop.LogonTimeout);

            AddIntProp(properties, Prop.SenderSeqNum);
            AddIntProp(properties, Prop.TargetSeqNum);

            AddStringProp(properties, Prop.FutureAccount);
            AddStringProp(properties, Prop.ForexAccount);

            AddBoolProp(properties, Prop.SSL);

            AddBoolProp(properties, Prop.ResetSeqNumbers);
        }
    }
}
