using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

namespace OEC.FIX.Sample
{
	internal class SslTunnel
	{
		public const int LocalPort = 9400;
		public readonly string Host;
		public readonly int Port;
		private Thread _thread;

		public SslTunnel(string host, int port)
		{
			Host = host;
			Port = port;
		}

		public void Open()
		{
			var client = new TcpClient();
			client.Connect(Host, Port);
			NetworkStream s = client.GetStream();
			var stream = new SslStream(s);
			stream.AuthenticateAsClient("openecry.com");

			var listener = new TcpListener(LocalPort);
			listener.Start();

			_thread = new Thread(x =>
			{
				TcpClient incoming = listener.AcceptTcpClient();
				NetworkStream i = incoming.GetStream();
				var ibu = new byte[4096];
				i.BeginRead(ibu, 0, ibu.Length, async => ReadI(i, ibu, i.EndRead(async), stream), null);
				var obu = new byte[4096];
				stream.BeginRead(obu, 0, obu.Length, async => ReadI(stream, obu, stream.EndRead(async), i), null);
				while (client.Connected && incoming.Connected)
					Thread.Sleep(16);
				if (!client.Connected)
					incoming.Close();
				if (!incoming.Connected)
					client.Close();
			}) {IsBackground = true};
			_thread.Start();
		}

		private void ReadI(Stream input, byte[] buffer, int len, Stream output)
		{
			if (len > 0)
				output.Write(buffer, 0, len);
			input.BeginRead(buffer, 0, buffer.Length, async => ReadI(input, buffer, input.EndRead(async), output), null);
		}
	}
}