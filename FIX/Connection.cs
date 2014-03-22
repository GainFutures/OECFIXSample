using System;
using System.IO;
using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	internal class Connection : ConnectionBase<MessageStore>
	{
		private SslTunnel _sslTunnel;
		public Session Session { get; private set; }

		public void Create(int senderSeqNum, int targetSeqNum)
		{
			Create();

			Session = Session.lookupSession(SessionID);
			Session.setNextSenderMsgSeqNum(senderSeqNum);
			Session.setNextTargetMsgSeqNum(targetSeqNum);

			var ssl = (bool) Program.Props[Prop.SSL].Value;
			_sslTunnel = ssl ? new SslTunnel(Program.Props[Prop.Host].Value.ToString(), (int) Program.Props[Prop.Port].Value) : null;
		}

		public override void Open(string username, string password)
		{
			if (_sslTunnel != null)
				_sslTunnel.Open();
			base.Open(username, password);
		}

		protected override SessionSettings GetSessionSettings()
		{
			var stream = new MemoryStream(1024);
			var writer = new StreamWriter(stream);

			var ssl = (bool) Program.Props[Prop.SSL].Value;

			writer.WriteLine("[DEFAULT]");
			writer.WriteLine("ConnectionType={0}", "initiator");
			writer.WriteLine("HeartBtInt={0}", Program.Props[Prop.HeartbeatInterval].Value);
			if (ssl)
			{
				writer.WriteLine("SocketConnectHost=localhost");
				writer.WriteLine("SocketConnectPort={0}", SslTunnel.LocalPort);
			}
			else
			{
				writer.WriteLine("SocketConnectHost={0}", Program.Props[Prop.Host].Value);
				writer.WriteLine("SocketConnectPort={0}", Program.Props[Prop.Port].Value);
			}
			writer.WriteLine("ReconnectInterval={0}", Program.Props[Prop.ReconnectInterval].Value);
			writer.WriteLine("UseDataDictionary={0}", 'N');
			writer.WriteLine("MillisecondsInTimestamp={0}", (bool) Program.Props[Prop.MillisecondsInTimestamp].Value ? 'Y' : 'N');

			writer.WriteLine("[SESSION]");
			writer.WriteLine("BeginString={0}", Program.Props[Prop.BeginString].Value);
			writer.WriteLine("SenderCompID={0}", Program.Props[Prop.SenderCompID].Value);
			writer.WriteLine("TargetCompID={0}", Program.Props[Prop.TargetCompID].Value);

			writer.WriteLine("StartTime={0}", Tools.LocalToUtc((TimeSpan) Program.Props[Prop.SessionStart].Value));
			writer.WriteLine("EndTime={0}", Tools.LocalToUtc((TimeSpan) Program.Props[Prop.SessionEnd].Value));

			writer.Flush();
			stream.Seek(0, SeekOrigin.Begin);
			return new SessionSettings(stream);
		}
	}
}