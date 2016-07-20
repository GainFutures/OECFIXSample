using System;
using System.Collections;
using System.Collections.Generic;

namespace OEC.FIX.Sample
{
	internal class Props : IEnumerable<Prop>
	{
		private readonly Dictionary<string, Prop> _props = new Dictionary<string, Prop>();

		#region IEnumerable<Prop> Members

		public IEnumerator<Prop> GetEnumerator()
		{
			return _props.Values.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public Prop this[string name]
		{
			get
			{
				Prop prop;
				if (!_props.TryGetValue(name.ToUpperInvariant(), out prop))
				{
					throw new ExecutionException("Property '{0}' not found.", name);
				}
				return prop;
			}
		}

		public void AddProp<T>(string name, Func<T> getter, Action<T> setter)
		{
			_props[name.ToUpperInvariant()] = new ActionProp<T>(name, getter, setter);
		}

		public void AddProp<T>(string name, T value)
		{
			_props[name.ToUpperInvariant()] = new ValueProp<T>(name, value);
		}

        public bool Contains(string name)
        {
            return _props.ContainsKey(name);
        }
	}

	internal abstract class Prop
	{
		public static readonly string Host = "Host";
		public static readonly string Port = "Port";
		public static readonly string FastPort = "FastPort";
		public static readonly string ReconnectInterval = "ReconnectInterval";
		public static readonly string HeartbeatInterval = "HeartbeatInterval";
		public static readonly string MillisecondsInTimestamp = "MillisecondsInTimestamp";

		public static readonly string SessionStart = "SessionStart";
		public static readonly string SessionEnd = "SessionEnd";

		public static readonly string BeginString = "BeginString";
		public static readonly string SenderCompID = "SenderCompID";
		public static readonly string TargetCompID = "TargetCompID";

		public static readonly string SenderSeqNum = "SenderSeqNum";
		public static readonly string TargetSeqNum = "TargetSeqNum";
		public static readonly string ResponseTimeout = "ResponseTimeout";
		public static readonly string ConnectTimeout = "ConnectTimeout";

		public static readonly string FutureAccount = "FutureAccount";
		public static readonly string ForexAccount = "ForexAccount";

		public static readonly string FastHashCode = "FastHashCode";
        public static readonly string ResetSeqNumbers = "ResetSeqNumbers";

	    public static readonly string LogonTimeout = "LogonTimeout";

		public static readonly string SSL = "SSL";
	    public static readonly string Password = "Password";

		public readonly string Name;
		public readonly Type Type;

		protected Prop(Type type, string name)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}

			Type = type;
			Name = name;
		}

		public object Value
		{
			get { return GetValue(); }
			set
			{
				if (value == null)
				{
					throw new ExecutionException("Assigning property '{0}' to NULL.", Name);
				}
				if (value.GetType() != Type)
				{
					throw new ExecutionException("Invalid value type '{0}' for property '{1}'.", value.GetType(), Name);
				}
				SetValue(value);
			}
		}

		public void Parse(string value)
		{
			SetValue(Convert.ChangeType(value, Type));
		}

		protected abstract object GetValue();
		protected abstract void SetValue(object value);
	}

	internal class ValueProp<T> : Prop
	{
		private T _value;

		public ValueProp(string name, T value)
			: base(typeof (T), name)
		{
			_value = value;
		}

		protected override object GetValue()
		{
			return _value;
		}

		protected override void SetValue(object value)
		{
			_value = (T) value;
		}
	}

	internal class ActionProp<T> : Prop
	{
		private readonly Func<T> _getter;
		private readonly Action<T> _setter;

		public ActionProp(string name, Func<T> getter, Action<T> setter)
			: base(typeof (T), name)
		{
			if (getter == null)
			{
				throw new ArgumentNullException("getter");
			}
			if (setter == null)
			{
				throw new ArgumentNullException("setter");
			}

			_getter = getter;
			_setter = setter;
		}

		protected override object GetValue()
		{
			return _getter();
		}

		protected override void SetValue(object value)
		{
			_setter((T) value);
		}
	}
}