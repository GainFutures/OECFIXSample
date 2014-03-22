using System;
using System.IO;
using System.Text;
using System.Threading;
using OEC.FIX.Sample.FAST;
using OEC.FIX.Sample.FIX;

namespace OEC.FIX.Sample.FoxScript
{
	partial class ExecEngine
	{
		public static readonly Version FOXScriptVersion = new Version(1, 0);
		private readonly Timer _fastHeartbeats;

		public ExecEngine(FixEngine fixEngine, FastClient fastClient)
		{
			this._fixEngine = fixEngine;
			this._fastClient = fastClient;
			_input = new StreamWriter(new MemoryStream(), Encoding.UTF8) {AutoFlush = true};
			_fastHeartbeats = new Timer(TimerFastHeartbeats, null, 25*1000, 25*1000);
		}

		private void TimerFastHeartbeats(object state)
		{
			if (_fastClient.Connected)
				FASTHeartbeat();
		}

		public static void Write(string format, params object[] args)
		{
			Console.Write(format, args);
		}

		public static void WriteLine(string format, params object[] args)
		{
			Console.WriteLine(format, args);
		}

		public void Run()
		{
			while (_running)
			{
				string line = ReadLine();
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				try
				{
					Parser parser = CreateParser(line, "CONSOLE");
					parser.Parse();
				}
				catch (Exception e)
				{
					WriteLine("Execution aborted: {0}", e.Message);
				}
			}
		}
	}
}