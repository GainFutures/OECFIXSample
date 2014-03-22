using System;
using OEC.FIX.Sample.FoxScript;
using OpenFAST;
using OpenFAST.Template;

namespace OEC.FIX.Sample.FAST
{
	internal class FastMessageFactory
	{
		private readonly ITemplateRegistry _templateRegistry;
		private int _counter = 1;

		public FastMessageFactory(ITemplateRegistry templateRegistry)
		{
			_templateRegistry = templateRegistry;
		}

		public Message CancelMDMessage(Message msg)
		{
			if (!msg.IsDefined(FastFieldsNames.MDREQID))
			{
				throw new ExecutionException("FAST message not contains '{0}'", FastFieldsNames.MDREQID);
			}
			string mdReqID = msg.GetString(FastFieldsNames.MDREQID);
			MessageTemplate template = _templateRegistry.GetTemplate("MarketDataRequest_Cancel");
			var message = new Message(template);
			message.SetString(FastFieldsNames.MDREQID, mdReqID);
			return message;
		}

		public Message MarketDataRequest(MDMessageCommand cmd)
		{
			MessageTemplate template = _templateRegistry.GetTemplate("MarketDataRequest");
			var message = new Message(template);


			string symbolStr = String.Format("{0}{1}", cmd.BaseSymbol, cmd.ExpirationMonth);
			if (cmd.Option)
			{
				symbolStr += String.Format("{0}{1}", (cmd.StrikeSide.Put ? "P" : "C"), cmd.StrikeSide.Strike);
			}
			message.SetString("MDReqID", string.Format("{0}_{1}_{2}", symbolStr, cmd.SubscriptionType, ++_counter));
			message.SetInteger("SubscriptionRequestType", 1);
			message.SetInteger("MarketDepth", cmd.MarketDepth);
			message.SetInteger("MDUpdateType", cmd.UpdateType);

			message.SetInteger("SubscriptionType", cmd.SubscriptionType);

			DateTime? startTime = cmd.StartTime;
			if (startTime.HasValue)
			{
				long st = OFReflector.ToFastDateTime(startTime.Value);
				message.SetLong("StartTime", st);
			}

			Sequence templateMDEntries = template.GetSequence("MDEntries");
			var mdEntries = new SequenceValue(templateMDEntries);

			foreach (string mdentryType in cmd.MDEntries)
			{
				var mdEntry = new GroupValue(templateMDEntries.Group);
				mdEntry.SetString("MDEntryType", mdentryType);
				mdEntries.Add(mdEntry);
			}

			message.SetFieldValue("MDEntries", mdEntries);

			Sequence templateInstrument = template.GetSequence("Instruments");
			var groupInstrument = new GroupValue(templateInstrument.Group);
			groupInstrument.SetString("Symbol", cmd.BaseSymbol);

			string cfiCode = "FXXXXS";
			if (cmd.Option)
			{
				cfiCode = cmd.StrikeSide.Put ? "OPXFXS" : "OCXFXS";
				groupInstrument.SetDecimal("StrikePrice", (decimal) cmd.StrikeSide.Strike);
			}
			groupInstrument.SetString("CFICode", cfiCode);

			groupInstrument.SetInteger("MaturityMonthYear", cmd.ExpirationMonth);
			var mdInstruments = new SequenceValue(templateInstrument);
			mdInstruments.Add(groupInstrument);
			message.SetFieldValue("Instruments", mdInstruments);
			return message;
		}

		public Message LogonMessage(string username, string password)
		{
			var message = new Message(_templateRegistry.GetTemplate("Logon"));
			message.SetLong("SendingTime", OFReflector.ToFastDateTime(DateTime.UtcNow));
			message.SetInteger("HeartbeatInt", 30);
			message.SetString("Username", username);
			message.SetString("Password", password);
			return message;
		}

		public Message Hearbeat()
		{
			MessageTemplate template = _templateRegistry.GetTemplate("Heartbeat");
			var message = new Message(template);
			message.SetLong("SendingTime", OFReflector.ToFastDateTime(DateTime.UtcNow));
			return message;
		}
	}
}