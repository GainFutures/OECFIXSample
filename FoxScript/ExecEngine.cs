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
        private readonly Props _properties;

        public Props Properties => _properties;

        public bool IsFixAvailable => _fixEngine != null;

        public bool IsFastAvailable => _fastClient != null;

        public ExecEngine(Props properties, FixEngine fixEngine, FastClient fastClient)
        {
            _properties = properties;
            _fixEngine = fixEngine;
            _fastClient = fastClient;
            _input = new StreamWriter(new MemoryStream(), Encoding.UTF8) { AutoFlush = true };
            GC.KeepAlive(new Timer(TimerFastHeartbeats, null, 25 * 1000, 25 * 1000));
        }

        public static ExecEngine MakeFixOnly(IConfiguration configuration)
        {
            var properties = new Props();
            configuration.FillProperties(properties);

            var fixEngine = new FixEngine(properties);
            return new ExecEngine(properties, fixEngine, null);
        }

        public static ExecEngine MakeFastOnly(Props properties)
        {
            var fastClient = new FastClient(properties);
            return new ExecEngine(properties, null, fastClient);
        }

        public static ExecEngine MakeFixFast(IConfiguration configuration)
        {
            var properties = new Props();
            configuration.FillProperties(properties);

            var fixEngine = new FixEngine(properties);
            var fastClient = new FastClient(properties);
            return new ExecEngine(properties, fixEngine, fastClient);
        }

        private void TimerFastHeartbeats(object state)
        {
            if (IsFastAvailable && _fastClient.Connected)
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

        public void Execute(string line, string lineSource)
        {
            try
            {
                Parser parser = CreateParser(line, lineSource);
                parser.Parse();
            }
            catch (Exception e)
            {
                WriteLine("Execution aborted: {0}", e.Message);
            }
        }

        public void Run()
        {
            while (_running)
            {
                string line = ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                Execute(line, "CONSOLE");
            }
        }
    }
}