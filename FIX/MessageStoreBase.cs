using System;
using System.Collections;
using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	public abstract class MessageStoreBase : QuickFix.MessageStore
	{
		public delegate void SeqNumEventHandler(SessionID sessionID, int seqnum);

		private readonly MemoryStore _cache = new MemoryStore();
		private DateTime _creationTimeUtc;
		private SessionID _sessionID;
		private TimeSpan _sessionStartUtc;
		protected abstract TimeSpan SessionStartLocal { get; }

		public void Init(SessionID sessionID)
		{
			_sessionStartUtc = DateTime.Now.Date.Add(SessionStartLocal).ToUniversalTime().TimeOfDay;
			_sessionID = sessionID;
			PopulateCache();
		}

		public static event SeqNumEventHandler SenderSeqNumChanged;
		public static event SeqNumEventHandler TargetSeqNumChanged;

		protected void RaiseSenderSeqNumChanged(int next)
		{
			SeqNumEventHandler handler = SenderSeqNumChanged;
			if (handler != null)
			{
				handler(_sessionID, next);
			}
		}

		protected void RaiseTargetSeqNumChanged(int next)
		{
			SeqNumEventHandler handler = TargetSeqNumChanged;
			if (handler != null)
			{
				handler(_sessionID, next);
			}
		}

		protected static DateTime CalculateCreationTime(DateTime currentUtc, TimeSpan sessionStartTimeUtc)
		{
			if (currentUtc.Kind != DateTimeKind.Utc)
			{
				throw new ArgumentException("DateTimeKind is not UTC.", "currentUtc");
			}

			DateTime start = currentUtc.Date.Add(sessionStartTimeUtc);
			if (currentUtc < start)
				start -= TimeSpan.FromDays(1);

			return start;
		}

		private void PopulateCache()
		{
			SessionParams sessionParams = LoadSessionParams(_sessionID);
			if (sessionParams != null)
			{
				_cache.setNextSenderMsgSeqNum(sessionParams.SenderSeqNum);
				_cache.setNextTargetMsgSeqNum(sessionParams.TargetSeqNum);
				_creationTimeUtc = sessionParams.CreationTime;
			}
			else
			{
				_creationTimeUtc = CalculateCreationTime(DateTime.UtcNow, _sessionStartUtc);
				SaveSessionParams();
			}
		}

		protected abstract SessionParams LoadSessionParams(SessionID sessionID);
		protected abstract bool SaveSessionParams(SessionID sessionID, SessionParams sessionParams);

		private bool SaveSessionParams()
		{
			var sessionParams = new SessionParams
			{
				CreationTime = _creationTimeUtc,
				SenderSeqNum = _cache.getNextSenderMsgSeqNum(),
				TargetSeqNum = _cache.getNextTargetMsgSeqNum()
			};
			return SaveSessionParams(_sessionID, sessionParams);
		}

		#region MessageStore Members

		public void get(int begin, int end, ArrayList messages)
		{
			_cache.get(begin, end, messages);
		}

		public DateTime getCreationTime()
		{
			return _creationTimeUtc;
		}

		public int getNextSenderMsgSeqNum()
		{
			return _cache.getNextSenderMsgSeqNum();
		}

		public int getNextTargetMsgSeqNum()
		{
			return _cache.getNextTargetMsgSeqNum();
		}

		public void incrNextSenderMsgSeqNum()
		{
			_cache.incrNextSenderMsgSeqNum();
			setNextSenderMsgSeqNum(_cache.getNextSenderMsgSeqNum());
		}

		public void incrNextTargetMsgSeqNum()
		{
			_cache.incrNextTargetMsgSeqNum();
			setNextTargetMsgSeqNum(_cache.getNextTargetMsgSeqNum());
		}

		public void refresh()
		{
			_cache.reset();
			RaiseSenderSeqNumChanged(_cache.getNextSenderMsgSeqNum());
			RaiseTargetSeqNumChanged(_cache.getNextTargetMsgSeqNum());

			PopulateCache();
		}

		public void reset()
		{
			_cache.reset();
			RaiseSenderSeqNumChanged(_cache.getNextSenderMsgSeqNum());
			RaiseTargetSeqNumChanged(_cache.getNextTargetMsgSeqNum());

			_creationTimeUtc = CalculateCreationTime(DateTime.UtcNow.AddSeconds(1), _sessionStartUtc);
			SaveSessionParams();
		}

		public bool set(int sequence, string msg)
		{
			return _cache.set(sequence, msg);
		}

		public void setNextSenderMsgSeqNum(int next)
		{
			_cache.setNextSenderMsgSeqNum(next);
			SaveSessionParams();
			RaiseSenderSeqNumChanged(_cache.getNextSenderMsgSeqNum());
		}

		public void setNextTargetMsgSeqNum(int next)
		{
			_cache.setNextTargetMsgSeqNum(next);
			SaveSessionParams();
			RaiseTargetSeqNumChanged(_cache.getNextTargetMsgSeqNum());
		}

		#endregion

		protected class SessionParams
		{
			public DateTime CreationTime;
			public int SenderSeqNum;
			public int TargetSeqNum;
		}
	}
}