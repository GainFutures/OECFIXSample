using System;
using OEC.FIX.Sample;
using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	internal class MessageStore : MessageStoreBase
	{
		protected override TimeSpan SessionStartLocal
		{
			get { return (TimeSpan) Program.Props[Prop.SessionStart].Value; }
		}

		protected override SessionParams LoadSessionParams(SessionID sessionID)
		{
			return new SessionParams
			{
				CreationTime = DateTime.UtcNow,
				SenderSeqNum = 1,
				TargetSeqNum = 1
			};
		}

		protected override bool SaveSessionParams(SessionID sessionID, SessionParams sessionParams)
		{
			return true;
		}
	}
}