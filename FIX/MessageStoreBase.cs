using System;
using System.Collections;
using System.Collections.Generic;
using QuickFix;

namespace OEC.FIX.Sample.FIX
{
	abstract class MessageStoreBase : IMessageStore
	{
		public delegate void SeqNumEventHandler(SessionID sessionID, int seqnum);

		private readonly MemoryStore _cache = new MemoryStore();
		private DateTime _creationTimeUtc;
		private SessionID _sessionID;
		private TimeSpan _sessionStartUtc;
	    protected Props Properties { get; set; }

		protected abstract TimeSpan SessionStartLocal { get; }

	    protected MessageStoreBase(Props properties)
	    {
	        Properties = properties;
	    }

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
				_cache.SetNextSenderMsgSeqNum(sessionParams.SenderSeqNum);
				_cache.SetNextTargetMsgSeqNum(sessionParams.TargetSeqNum);
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
				SenderSeqNum = _cache.GetNextSenderMsgSeqNum(),
				TargetSeqNum = _cache.GetNextTargetMsgSeqNum()
			};
			return SaveSessionParams(_sessionID, sessionParams);
		}

		#region MessageStore Members
		public void Get(int begin, int end, List<string> messages)
		{
			_cache.Get(begin, end, messages);
		}

	    public DateTime? CreationTime
	    {
	        get { return GetCreationTime(); }
	    }

        public DateTime GetCreationTime()
		{
			return _creationTimeUtc;
		}

		public int GetNextSenderMsgSeqNum()
		{
			return _cache.GetNextSenderMsgSeqNum();
		}

		public int GetNextTargetMsgSeqNum()
		{
			return _cache.GetNextTargetMsgSeqNum();
		}

		public void IncrNextSenderMsgSeqNum()
		{
			_cache.IncrNextSenderMsgSeqNum();
			SetNextSenderMsgSeqNum(_cache.GetNextSenderMsgSeqNum());
		}

		public void IncrNextTargetMsgSeqNum()
		{
			_cache.IncrNextTargetMsgSeqNum();
			SetNextTargetMsgSeqNum(_cache.GetNextTargetMsgSeqNum());
		}

		public void Refresh()
		{
			_cache.Reset();
			RaiseSenderSeqNumChanged(_cache.GetNextSenderMsgSeqNum());
			RaiseTargetSeqNumChanged(_cache.GetNextTargetMsgSeqNum());

			PopulateCache();
		}

		public void Reset()
		{
			_cache.Reset();
			RaiseSenderSeqNumChanged(_cache.GetNextSenderMsgSeqNum());
			RaiseTargetSeqNumChanged(_cache.GetNextTargetMsgSeqNum());

			_creationTimeUtc = CalculateCreationTime(DateTime.UtcNow.AddSeconds(1), _sessionStartUtc);
			SaveSessionParams();
		}

		public bool Set(int sequence, string msg)
		{
			return _cache.Set(sequence, msg);
		}

		public void SetNextSenderMsgSeqNum(int next)
		{
			_cache.SetNextSenderMsgSeqNum(next);
			SaveSessionParams();
			RaiseSenderSeqNumChanged(_cache.GetNextSenderMsgSeqNum());
		}

		public void SetNextTargetMsgSeqNum(int next)
		{
			_cache.SetNextTargetMsgSeqNum(next);
			SaveSessionParams();
			RaiseTargetSeqNumChanged(_cache.GetNextTargetMsgSeqNum());
		}
		#endregion

		protected class SessionParams
		{
			public DateTime CreationTime;
			public int SenderSeqNum;
			public int TargetSeqNum;
		}

	    public void Dispose()
	    {
            _cache.Dispose();
        }
	}
}