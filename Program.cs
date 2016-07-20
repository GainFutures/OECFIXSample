using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;
using OEC.FIX.Sample.FIX;
using OEC.FIX.Sample.FoxScript;
using OEC.FIX.Sample.FAST;
using System.Collections.Generic;
using QuickFix;

namespace OEC.FIX.Sample
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Console.WriteLine("FIX Sample Client, ver. {0}", Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine("FOXScript ver. {0}", ExecEngine.FOXScriptVersion);
			PrintAuthUsage();

            var app = new Program();

            //VP: Here are many examples how to run the app with different parameters

            //Ordinary way to start. FIX and FAST enabled; console available
            app.Start(ExecEngine.MakeFixFast(Configurations.PredefinedConfiguration), app.StartConsoleRoutine);

            // Connects multiple fixengines simultaneously; no command console 
            //app.Start(
            //    Configurations.MultipleTestSessions(100).Select(ExecEngine.MakeFixOnly),
            //    app.TestMultimpleConnectionsRoutine);
            
            // Settings from config file; console 
            //app.Start(
            //    ExecEngine.MakeFixFast(Configurations.AppSettingsConfiguration),
            //    app.StartConsoleRoutine);
            
            //// predefined TEST1 user; command console
            //app.Start(
            //    ExecEngine.MakeFixFast(Configurations.Test1OnLocalhost),
            //    app.StartConsoleRoutine);

            //app.Start(
            //    ExecEngine.MakeFixFast(Configurations.VitalyLocalHostConfiguration),
            //    app.StartConsoleRoutine);
          
            // predefined TEST1 user connects to FIX and then to FAST two times simulating 
            // drop of an old connection; no command console
            //var configuration = Configurations.Test1OnLocalhost;
            //var fixFast = ExecEngine.MakeFixFast(configuration);
            //var execEngines = new[]
            //{
            //    fixFast, 
            //    ExecEngine.MakeFastOnly(fixFast.Properties)
            //};
            //app.Start(execEngines, app.MultipleFastConnection);
		}

        private void Start(IEnumerable<ExecEngine> engines, Action<int, ExecEngine> routine)
        {
            var threads = engines.Select((engine, i) =>
            {
                Action<ExecEngine> threadRoutine = e => routine(i, e);
                return new Thread(() => Start(engine, threadRoutine));
            }).ToArray();
            
            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();
        }

        private void Start(ExecEngine execEngine, Action<ExecEngine> routine)
        {
            execEngine.LoadSeqNumbers();
            routine(execEngine);
            execEngine.StoreSeqNumbers();
        }

	    private void TestMultimpleConnectionsRoutine(int no, ExecEngine engine)
	    {
            const string source = "COMMAND";
            engine.Execute(string.Format("connect '{0}';", no + 1), source);
            engine.Execute("sleep [00:03:00];", source);
            engine.Execute("disconnect;", source);
        }

	    private void MultipleFastConnection(int no, ExecEngine engine)
	    {
            const string source = "COMMAND";
            if (engine.IsFixAvailable)
                engine.Execute(string.Format("connect '{0}';", no + 1), source);

            const int fixDelay = 5;
	        const int orderDelay = 3;
            TimeSpan delay = TimeSpan.FromSeconds(fixDelay + orderDelay * no);
	        var sleepString = string.Format("sleep [{0}];", delay);

            if (engine.IsFixAvailable)
                engine.Execute("disconnect;", source);

            engine.Execute(sleepString, source);
            engine.Execute("ConnectFast;", source);
        }

	    private void ExecuteFileRoutine(ExecEngine engine, string fileName)
	    {
	        string command = string.Format("exec '{0}'", fileName);
	        engine.Execute(command, "COMMAND");
	    }

	    private void StartConsoleRoutine(ExecEngine engine)
	    {
	        engine.Run();
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