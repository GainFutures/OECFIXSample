using System;
using System.IO;
using System.Reflection;
using OEC.FIX.Sample.FIX;
using OEC.FIX.Sample.FoxScript;
using OEC.FIX.Sample.FAST;

namespace OEC.FIX.Sample
{
	internal class Program
	{
		public static readonly Props Props = new Props();

		private static void Main(string[] args)
		{
			Console.WriteLine("FIX Sample Client, ver. {0}", Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine("FOXScript ver. {0}", ExecEngine.FOXScriptVersion);
			PrintAuthUsage();

			var fixEngine = new FixEngine();
			var fastClient = new FastClient();
			var execEngine = new ExecEngine(fixEngine, fastClient);

			CreatePredefinedProps();
			execEngine.LoadSeqNumbers();

			execEngine.Run();
			execEngine.StoreSeqNumbers();
		}

		private static void CreatePredefinedProps()
		{
			CreatePropsBase("api.gainfutures.com", "MY_SENDER_COMPID", "OEC_TEST", "API000001", "APIFX0001", false);
		}

		private static void CreatePropsBase(
			string host,
			string senderCompID,
			string tragetCompID,
			string futureAccount,
			string forexAccount,
			bool isSSL)
		{
			Props.AddProp(Prop.Host, host);
			Props.AddProp(Prop.Port, 9300);
			Props.AddProp(Prop.FastPort, 9301);
			Props.AddProp(Prop.FastHashCode, "");

			Props.AddProp(Prop.ReconnectInterval, 30);
			Props.AddProp(Prop.HeartbeatInterval, 30);
			Props.AddProp(Prop.MillisecondsInTimestamp, false);

			Props.AddProp(Prop.BeginString, FixVersion.FIX44);
			Props.AddProp(Prop.SenderCompID, senderCompID);
			Props.AddProp(Prop.TargetCompID, tragetCompID);

			Props.AddProp(Prop.SessionStart, new TimeSpan(1, 0, 0));
			Props.AddProp(Prop.SessionEnd, new TimeSpan(23, 0, 0));

			Props.AddProp(Prop.SenderSeqNum, 1);
			Props.AddProp(Prop.TargetSeqNum, 1);
			Props.AddProp(Prop.ResponseTimeout, TimeSpan.FromSeconds(15));
			Props.AddProp(Prop.ConnectTimeout, TimeSpan.FromSeconds(15));

			Props.AddProp(Prop.FutureAccount, futureAccount);
			Props.AddProp(Prop.ForexAccount, forexAccount);

			Props.AddProp(Prop.SSL, isSSL);
		}

		private static void PrintAuthUsage()
		{
			const string fileName = "AuthWorkflow.fox";
			const string fullFileName = "../../Tests/" + fileName;
			if (!File.Exists(fullFileName)) return;

			Console.WriteLine();
			Console.WriteLine("Hint of day from: " + fileName);
			Console.WriteLine();
			using (StreamReader file = File.OpenText(fullFileName))
				Console.Write(file.ReadToEnd());
			Console.WriteLine();
		}
	}
}