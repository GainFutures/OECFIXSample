using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using OpenFAST;
using OpenFAST.Error;
using OpenFAST.Sessions;
using OpenFAST.Sessions.Tcp;
using OpenFAST.Template;
using OpenFAST.Template.Loader;

namespace OEC.FIX.Sample.FAST
{
	internal class FastClient
	{
		private const string LocalFilename = ".\\templates.xml";
		private readonly ClientMessageHandler _clientMessageHandler;

		private readonly SessionControlProtocol11 _protocol;
		private readonly ITemplateRegistry _templateRegistry;
		public FastMessageFactory MessageFactory;
		private OpenFAST.Sessions.FastClient _fc;
		private Session _ses;

		public FastClient()
		{
			var templateLoader = new XmlMessageTemplateLoader {LoadTemplateIdFromAuxId = true};

			TryDownload();

			Stream template;

			if (File.Exists(LocalFilename))
				template = new FileStream(LocalFilename, FileMode.Open, FileAccess.Read);
			else
				template = Assembly.GetExecutingAssembly().GetManifestResourceStream("OEC.FIX.Sample.template.template.xml");
			templateLoader.Load(template);
			_templateRegistry = templateLoader.TemplateRegistry;
			MessageFactory = new FastMessageFactory(_templateRegistry);
			_clientMessageHandler = new ClientMessageHandler();
			_protocol = new SessionControlProtocol11();
			_protocol.RegisterSessionTemplates(_templateRegistry);
		}

		private void TryDownload()
		{
			if (!File.Exists(LocalFilename))
			{
				try
				{
					var client = new WebClient();
					client.DownloadFile("http://api.openecry.com/Sections/Misc/DownloadFile.aspx?ClientUpdate=0_5008_1", LocalFilename);
				}
				catch (Exception)
				{
				}
			}
		}

		public bool Connected
		{
			get { return _ses != null && _ses.IsListening; }
		}


		public void Connect(string username, string password)
		{
			string host = Program.Props[Prop.Host].Value.ToString();

			int port;
			try
			{
				port = (int) Program.Props[Prop.FastPort].Value;
			}
			catch
			{
				port = (int) Program.Props[Prop.Port].Value;
			}


			_fc = new OpenFAST.Sessions.FastClient("client", _protocol, new TcpEndpoint(host, port))
			{
				InboundTemplateRegistry = _templateRegistry,
				OutboundTemplateRegistry = _templateRegistry
			};

			_ses = _fc.Connect();

			_ses.ErrorHandler = new ClientErrorHandler();

			_ses.MessageHandler = _clientMessageHandler;

			SendLogonMessage(username, password);
		}

		private void EnsureConnected()
		{
			if (_ses == null || !_ses.IsListening)
			{
				throw new ExecutionException("Not connected to FAST server.");
			}
		}

		public void SendMessage(Message message)
		{
			SendMessage(message, null, false);
		}

		public void SendMessage(Message message, string outputFileName, bool isCancel)
		{
			EnsureConnected();
			if (!String.IsNullOrWhiteSpace(outputFileName))
			{
				_clientMessageHandler.AddOutputFiles(message.GetString(FastFieldsNames.MDREQID), outputFileName);
			}
			_ses.MessageOutputStream.WriteMessage(message);
			if (isCancel)
			{
				_clientMessageHandler.RemoveOutputFiles(message.GetString(FastFieldsNames.MDREQID));
			}


			Console.WriteLine("<fast out>: {0}", message);
		}

		public Message WaitMessage(string msgType, TimeSpan timeout, Predicate<Message> predicate)
		{
			EnsureConnected();
			return _clientMessageHandler.WaitMessage(msgType, timeout, predicate);
		}

		private void SendLogonMessage(string username, string password)
		{
			SendMessage(MessageFactory.LogonMessage(username, password));
		}


		internal void SendHearbeat()
		{
			SendMessage(MessageFactory.Hearbeat());
		}


		public string GetMsgTypeValue(string templateName)
		{
			MessageTemplate templ;
			if (_templateRegistry.TryGetTemplate(templateName, out templ))
			{
				if (templ.HasField(FastFieldsNames.MSGTYPE))
				{
					Field tf = templ.GetField(FastFieldsNames.MSGTYPE);
					var stf = tf as Scalar;
					if (stf != null)
					{
						return stf.DefaultValue.ToString();
					}
				}
			}

			return null;
		}

		public void Disconnect()
		{
			if (_ses != null && _ses.IsListening)
			{
				_ses.MessageOutputStream.WriteMessage(_protocol.CloseMessage);
				int step = 0;
				while (_ses.IsListening && step < 20)
				{
					Thread.Sleep(100);
					step++;
				}
				_ses.Close();
				_clientMessageHandler.Clear();
				Console.WriteLine("FAST: Disconnect");
			}
			else
			{
				Console.WriteLine("FAST: : Already disconnected from FAST server.");
			}
		}

		#region Nested type: ClientErrorHandler

		private class ClientErrorHandler : IErrorHandler
		{
			public void OnError(Exception exception, StaticError error, string format, params object[] args)
			{
				if (format != null)
					Console.WriteLine(format, args);
				else
					Console.WriteLine(error);
			}

			public void OnError(Exception exception, DynError error, string format, params object[] args)
			{
				if (format != null)
					Console.WriteLine(format, args);
				else
					Console.WriteLine(error);
			}

			public void OnError(Exception exception, RepError error, string format, params object[] args)
			{
				if (format != null)
					Console.WriteLine(format, args);
				else
					Console.WriteLine(error);
			}
		}

		#endregion
	}
}