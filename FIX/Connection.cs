using System;
using System.IO;
using QuickFix;

namespace OEC.FIX.Sample.FIX
{
    internal class Connection : ConnectionBase
    {
        private readonly Props _properties;

        public Connection(Props properties)
        {
            _properties = properties;
        }

        public void Create()
        {
            Create(_properties);
        }

        protected override SessionSettings GetSessionSettings()
        {
            var stream = new MemoryStream(1024);
            var writer = new StreamWriter(stream);

            writer.WriteLine("[DEFAULT]");
            writer.WriteLine("ConnectionType=initiator");
            writer.WriteLine($"HeartBtInt={_properties[Prop.HeartbeatInterval].Value}");

            writer.WriteLine($"SocketConnectHost={_properties[Prop.Host].Value}");
            writer.WriteLine($"SocketConnectPort={_properties[Prop.Port].Value}");

            writer.WriteLine($"ReconnectInterval={_properties[Prop.ReconnectInterval].Value}");
            writer.WriteLine("UseDataDictionary=N");
            writer.WriteLine($"MillisecondsInTimestamp={((bool)_properties[Prop.MillisecondsInTimestamp].Value ? "Y" : "N")}");

            writer.WriteLine("[SESSION]");
            writer.WriteLine($"BeginString={_properties[Prop.BeginString].Value}");
            writer.WriteLine($"SenderCompID={_properties[Prop.SenderCompID].Value}");
            writer.WriteLine($"TargetCompID={_properties[Prop.TargetCompID].Value}");
            writer.WriteLine($"LogonTimeout={_properties[Prop.LogonTimeout].Value}");

            writer.WriteLine($"StartTime={Tools.LocalToUtc((TimeSpan)_properties[Prop.SessionStart].Value)}");
            writer.WriteLine($"EndTime={Tools.LocalToUtc((TimeSpan)_properties[Prop.SessionEnd].Value)}");

            if (_properties.Contains(Prop.ResetSeqNumbers))
            {
                var resetSeqNums = (bool)_properties[Prop.ResetSeqNumbers].Value ? "Y" : "N";
                writer.WriteLine($"ResetOnLogout={resetSeqNums}");
                writer.WriteLine($"ResetOnDisconnect={resetSeqNums}");
            }

            writer.WriteLine($"SSLEnable={(_properties.Contains(Prop.SSL) && (bool)_properties[Prop.SSL].Value ? "Y" : "N")}");
            writer.WriteLine("SSLServerName=gainfutures.com");
            writer.WriteLine("SSLValidateCertificates=N");

            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return new SessionSettings(new StreamReader(stream));
        }
    }
}