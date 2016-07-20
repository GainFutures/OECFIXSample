using System;
using System.IO;
using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	internal class Connection : ConnectionBase
	{
        private readonly Props _properties;
		private SslTunnel _sslTunnel;
		public Session Session { get; private set; }

        public Connection(Props properties)
        {
            _properties = properties;
        }
        
		public void Create(int senderSeqNum, int targetSeqNum)
		{
			Create(_properties);

			Session = Session.lookupSession(SessionID);
			Session.setNextSenderMsgSeqNum(senderSeqNum);
			Session.setNextTargetMsgSeqNum(targetSeqNum);

			var ssl = (bool) _properties[Prop.SSL].Value;
            _sslTunnel = ssl
                ? new SslTunnel(_properties[Prop.Host].Value.ToString(), (int) _properties[Prop.Port].Value)
                : null;
		}

		public override void Open(string password, string uuid)
		{
			if (_sslTunnel != null)
				_sslTunnel.Open();
			base.Open(password, uuid);
		}

		protected override SessionSettings GetSessionSettings()
		{
			var stream = new MemoryStream(1024);
			var writer = new StreamWriter(stream);

		    var ssl = (bool) _properties[Prop.SSL].Value;

			writer.WriteLine("[DEFAULT]");
			writer.WriteLine("ConnectionType={0}", "initiator");
            writer.WriteLine("HeartBtInt={0}", _properties[Prop.HeartbeatInterval].Value);
			if (ssl)
			{
				writer.WriteLine("SocketConnectHost=localhost");
				writer.WriteLine("SocketConnectPort={0}", SslTunnel.LocalPort);
			}
			else
			{
				writer.WriteLine("SocketConnectHost={0}", _properties[Prop.Host].Value);
				writer.WriteLine("SocketConnectPort={0}", _properties[Prop.Port].Value);
			}
			writer.WriteLine("ReconnectInterval={0}", _properties[Prop.ReconnectInterval].Value);
			writer.WriteLine("UseDataDictionary={0}", 'N');
			writer.WriteLine("MillisecondsInTimestamp={0}", (bool) _properties[Prop.MillisecondsInTimestamp].Value ? 'Y' : 'N');

			writer.WriteLine("[SESSION]");
			writer.WriteLine("BeginString={0}", _properties[Prop.BeginString].Value);
			writer.WriteLine("SenderCompID={0}", _properties[Prop.SenderCompID].Value);
			writer.WriteLine("TargetCompID={0}", _properties[Prop.TargetCompID].Value);
            writer.WriteLine("LogonTimeout={0}", _properties[Prop.LogonTimeout].Value);

			writer.WriteLine("StartTime={0}", Tools.LocalToUtc((TimeSpan) _properties[Prop.SessionStart].Value));
			writer.WriteLine("EndTime={0}", Tools.LocalToUtc((TimeSpan) _properties[Prop.SessionEnd].Value));

            if (_properties.Contains(Prop.ResetSeqNumbers))
            {
                var resetSeqNums = (bool)_properties[Prop.ResetSeqNumbers].Value ? "Y" : "N";
                writer.WriteLine("ResetOnLogout={0}", resetSeqNums);
                writer.WriteLine("ResetOnDisconnect={0}", resetSeqNums);
            }

			writer.Flush();
			stream.Seek(0, SeekOrigin.Begin);
			return new SessionSettings(stream);
		}
	}
}