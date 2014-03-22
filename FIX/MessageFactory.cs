using System;
using System.Linq;
using OEC.FIX.Sample.CFI;
using OEC.FIX.Sample.FIX.Fields;
using OEC.FIX.Sample.FoxScript;
using OEC.FIX.Sample.FoxScript.AllocationBlocks;
using QuickFix;
using QuickFix44;
using Message = QuickFix.Message;
using TimeInForce = QuickFix.TimeInForce;

namespace OEC.FIX.Sample.FIX
{
	internal static class FixVersion
	{
		public static readonly string FIX42 = "FIX.4.2";
		public static readonly string FIX44 = "FIX.4.4";

		public static string Current
		{
			get { return (string) Program.Props[Prop.BeginString].Value; }
		}
	}

	internal abstract class FixProtocol
	{
		public static readonly FixProtocol42 FixProtocol42 = new FixProtocol42();
		public static readonly FixProtocol44 FixProtocol44 = new FixProtocol44();

		public static FixProtocol Current
		{
			get
			{
				if (FixVersion.Current == FixVersion.FIX44)
				{
					return FixProtocol44;
				}
				if (FixVersion.Current == FixVersion.FIX42)
				{
					return FixProtocol42;
				}
				throw new ExecutionException("Unsupported FIX version '{0}'", FixVersion.Current ?? "NULL");
			}
		}

		public virtual void EnsureTrade(Message msg, char orderStatus, int? qty, double? price)
		{
			if (msg == null)
			{
				throw new ExecutionException("FIX message not specified.");
			}

			var ordStatus = msg.GetChar<OrdStatus>();
			if (ordStatus == null || ordStatus.getValue() != orderStatus)
			{
				throw new ExecutionException("Unexpected OrdStatus '{0}', must be '{1}'", ordStatus, orderStatus);
			}
		}

		public void EnsurePureOrderStatus(Message msg, char orderStatus)
		{
			if (msg == null)
				throw new ExecutionException("FIX message not specified.");

			var ordStatus = msg.GetChar<OrdStatus>();
			if (ordStatus == null || ordStatus.getValue() != orderStatus)
				throw new ExecutionException("Unexpected OrdStatus '{0}', must be '{1}'", ordStatus, orderStatus);
		}

		public virtual void EnsureOrderStatus(Message msg, char orderStatus)
		{
			if (msg == null)
			{
				throw new ExecutionException("FIX message not specified.");
			}

			var ordStatus = msg.GetChar<OrdStatus>();
			if (ordStatus == null || ordStatus.getValue() != orderStatus)
			{
				throw new ExecutionException("Unexpected OrdStatus '{0}', must be '{1}'", ordStatus, orderStatus);
			}

			var execType = msg.GetChar<ExecType>();
			if (execType == null || execType.getValue() != orderStatus)
			{
				throw new ExecutionException("Unexpected ExecType '{0}', must be '{1}'", execType, orderStatus);
			}
		}

		public virtual void EnsureModifyAccepted(Message msg, char orderStatus)
		{
			var ordStatus = msg.GetChar<OrdStatus>();
			if (ordStatus == null || ordStatus.getValue() != orderStatus)
			{
				throw new ExecutionException("Unexpected OrdStatus '{0}', must be '{1}'", ordStatus, orderStatus);
			}

			var execType = msg.GetChar<ExecType>();
			if (execType == null || execType.getValue() != ExecType.REPLACE)
			{
				throw new ExecutionException("Unexpected ExecType '{0}', must be '{1}'", execType, ExecType.REPLACE);
			}
		}

		public Message CollateralInquiry(BalanceCommand command)
		{
			var msg = new CollateralInquiry();

			msg.set(new Account(command.Account));
			msg.set(new CollInquiryID(Tools.GenerateUniqueID()));
			msg.set(new ResponseTransportType(ResponseTransportType.INBAND));
			msg.set(new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT));

			return msg;
		}

		public Message RequestForPositions(PositionsCommand command)
		{
			var msg = new RequestForPositions();

			msg.set(new Account(command.Account));
			msg.set(new AccountType(AccountType.ACCOUNT_IS_CARRIED_ON_CUSTOMER_SIDE_OF_BOOKS));
			msg.set(new PosReqID(Tools.GenerateUniqueID()));
			msg.set(new PosReqType(PosReqType.POSITIONS));
			msg.set(new ResponseTransportType(ResponseTransportType.INBAND));
			msg.set(new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT));
			msg.set(new TransactTime(DateTime.UtcNow));
			msg.set(new ClearingBusinessDate(DateTime.Today.ToString("yyyyMMdd")));

			return msg;
		}

		internal Message UserRequest(UserRequestCommand userRequestCommand)
		{
			var msg = new UserRequest(
				new UserRequestID(Tools.GenerateUniqueID()),
				new UserRequestType(userRequestCommand.UserRequestType),
				new Username(userRequestCommand.Name));
			if (userRequestCommand.UUID != null)
				msg.setField(new UUIDField(userRequestCommand.UUID));
			return msg;
		}

		internal Message MarginCalc(MarginCalcCommand marginCalcCommand)
		{
			var msg = new MarginCalcRequest(
				new MarginCalcReqID(Tools.GenerateUniqueID()),
				new Account(marginCalcCommand.Account));

			foreach (MarginCalcCommand.Position pos in marginCalcCommand.Positions)
			{
				var group = new MarginCalcRequest.NoPositions();

				AssignOrderContract(group, pos.Contract);

				group.setField(new MinQty(pos.MinQty));
				group.setField(new MaxQty(pos.MaxQty));

				msg.addGroup(group);
			}
			return msg;
		}

		public Message ContractRequest(ContractRequestCommand command)
		{
			var msg = new SecurityListRequest();

			msg.set(new SecurityReqID(Tools.GenerateUniqueID()));

			msg.set(new SecurityListRequestType(SecurityListRequestType.PRODUCT));
			if (command.SubscriptionRequestType != ContractRequestCommand.DEFAULT_SUBSCRIBTION_TYPE)
				msg.set(new SubscriptionRequestType(command.SubscriptionRequestType));
			msg.set(new Symbol(command.Name));

			if (command.UpdatesSinceTimestamp.HasValue)
				msg.setField(new UpdatesSinceTimestamp(command.UpdatesSinceTimestamp.Value));

			return msg;
		}

		public Message BaseContractRequest(BaseContractRequestCommand command)
		{
			var msg = new SecurityListRequest();

			msg.set(new SecurityReqID(Tools.GenerateUniqueID()));
			msg.set(new SecurityListRequestType(SecurityListRequestType.ALLSECURITIES));

			if (command.SubscriptionRequestType != ContractRequestCommand.DEFAULT_SUBSCRIBTION_TYPE)
				msg.set(new SubscriptionRequestType(command.SubscriptionRequestType));

			if (!string.IsNullOrEmpty(command.Exchange))
				msg.set(new SecurityExchange(command.Exchange));
			if (!string.IsNullOrEmpty(command.ContractGroup))
				msg.setField(new ContractGroupField(command.ContractGroup));
			if (command.CompoundType != CompoundType.UNKNOWN)
				msg.set(new SecuritySubType(command.CompoundTypeString));

			return msg;
		}

		public Message SymbolLookupRequest(SymbolLookupCommand command)
		{
			var msg = new SecurityListRequest();

			msg.set(new SecurityReqID(Tools.GenerateUniqueID()));

			msg.set(new SecurityListRequestType(SecurityListRequestType.SYMBOL));
			msg.set(new Text(command.Name));

			msg.setField(new SymbolLookupModeField(command.Mode));
			msg.setField(new MaxRecordsField(command.MaxRecords));

			if (command.ContractKinds.Count > 0)
			{
				var group = new SecurityTypes.NoSecurityTypes();
				foreach (Code code in command.ContractKinds
					.Select(k => Code.Create(k, command.OptionType))
					.Where(c => c != null))
				{
					group.set(new CFICode(code.ToFix()));
					msg.addGroup(group);
				}
			}

			if (!string.IsNullOrEmpty(command.Exchange))
				msg.set(new SecurityExchange(command.Exchange));
			if (!string.IsNullOrEmpty(command.ContractGroup))
				msg.setField(new ContractGroupField(command.ContractGroup));
			if (command.ContractType.HasValue)
				msg.setField(new ContractTypeField((int) command.ContractType.Value));
			if (command.ByBaseContractsOnly.HasValue)
				msg.setField(new ByBaseContractsOnlyField(command.ByBaseContractsOnly.Value));
			if (command.OptionsRequired.HasValue)
				msg.setField(new OptionsRequiredField(command.OptionsRequired.Value));

			if (command.ParentContract != null)
			{
				Contract parentContract = command.ParentContract;
				msg.setField(new UnderlyingCFICode(parentContract.Code.ToFix()));
				msg.setField(new UnderlyingSymbol(parentContract.Symbol));
				msg.setField(new UnderlyingMaturityMonthYear(parentContract.MaturityMonthYear.ToFix()));
				if (parentContract.Strike != null)
					msg.setField(new UnderlyingStrikePrice(parentContract.Strike.Value));
			}

			if (!string.IsNullOrEmpty(command.BaseContract))
				msg.setField(new Symbol(command.BaseContract));

			if (command.CompoundType != CompoundType.UNKNOWN)
				msg.set(new SecuritySubType(command.CompoundTypeString));

			return msg;
		}

		public Message OrderCancelRequest(CancelOrderCommand command, Message orig)
		{
			var msg = new OrderCancelRequest();

			msg.set(new ClOrdID(Tools.GenerateUniqueID()));
			msg.set(new TransactTime(DateTime.UtcNow));

			if (orig.isSetField(ClOrdID.FIELD))
			{
				msg.setField(new OrigClOrdID(orig.getString(ClOrdID.FIELD)));
			}

			CopyImmutableOrderFields(orig, msg);
			CopyFields(orig, msg, OrderQty.FIELD);

			return msg;
		}

		internal Message OrderStatusRequest(OrderStatusCommand orderStatusCommand, Message orig)
		{
			var msg = new OrderStatusRequest();
			msg.setField(new ClOrdID(orig.getString(ClOrdID.FIELD)));
			CopyImmutableOrderFields(orig, msg);
			return msg;
		}

		private void CopyImmutableOrderFields(Message orig, Message msg)
		{
			CopyFields(orig, msg,
				Side.FIELD,
				Account.FIELD,
				AllocText.FIELD,
				AllocType.FIELD,
				Symbol.FIELD,
				SecurityType.FIELD,
				CFICode.FIELD,
				QuickFix.MaturityMonthYear.FIELD,
				StrikePrice.FIELD,
				PutOrCall.FIELD);

			if (orig.hasGroup(NoAllocs.FIELD))
				for (uint i = 0; i < orig.groupCount(NoAllocs.FIELD); ++i)
				{
					var origGroup = new NewOrderSingle.NoAllocs();
					var group = new NewOrderSingle.NoAllocs();
					orig.getGroup(i + 1, NoAllocs.FIELD, origGroup);

					CopyFields(origGroup, group, AllocAccount.FIELD, AllocQty.FIELD);
					msg.addGroup(group);
				}
		}

		public Message OrderCancelReplaceRequest(ModifyOrderCommand command, Message orig)
		{
			var msg = new OrderCancelReplaceRequest();

			AssignOrderBody(command, msg);

			if (orig.isSetField(ClOrdID.FIELD))
			{
				msg.setField(new OrigClOrdID(orig.getString(ClOrdID.FIELD)));
			}

			return msg;
		}

		public Message AllocationInstruction(PostAllocationCommand command, Message orig)
		{
			var msg = new AllocationInstruction();

			msg.set(new AllocID(Tools.GenerateUniqueID()));

			if (command.Contract != null)
				AssignOrderContract(msg, command.Contract);

			if (orig.isSetField(ClOrdID.FIELD))
			{
				var group = new AllocationInstruction.NoOrders();
				group.set(new ClOrdID(orig.getString(ClOrdID.FIELD)));
				msg.addGroup(group);
			}

			AssignPostAllocationBlock(msg, command.AllocationBlock);

			return msg;
		}


		public Message NewOrderSingle(NewOrderCommand command)
		{
			var msg = new NewOrderSingle();

			AssignOrderBody(command, msg);

			return msg;
		}

		protected void AssignOrderBody(OrderCommand source, Message target)
		{
			target.setField(new ClOrdID(Tools.GenerateUniqueID()));
			target.setField(new Side(source.OrderSide.Side));
			target.setField(new TransactTime(DateTime.UtcNow));

			switch (source.OrderType.Type)
			{
				case OrderType.ICEBERG:
					target.setField(new OrdType(OrdType.LIMIT));
					if (source.OrderType.MaxFloor.HasValue)
					{
						target.setField(new MaxFloor(source.OrderType.MaxFloor.Value));
					}
					break;

				case OrderType.MARKET_ON_OPEN:
					target.setField(new OrdType(OrdType.MARKET));
					target.setField(new TimeInForce(TimeInForce.AT_THE_OPENING));
					break;

				case OrderType.MARKET_ON_CLOSE:
					target.setField(new OrdType(OrdType.MARKET));
					target.setField(new TimeInForce(TimeInForce.AT_THE_CLOSE));
					break;

				default:
					target.setField(new OrdType(source.OrderType.Type));
					break;
			}

			string account = source.Account;
			if (string.IsNullOrEmpty(account))
			{
				account = Tools.GetAccountFor(source.OrderContract.Symbol.Asset);
			}

			target.setField(new Account(account));
			target.setField(new OrderQty(source.OrderQty));

			if (source.OrderSide.Open.HasValue)
			{
				target.setField(new PositionEffect(source.OrderSide.Open.Value ? PositionEffect.OPEN : PositionEffect.CLOSE));
			}

			if (source.OrderType.Limit.HasValue)
			{
				target.setField(new Price(source.OrderType.Limit.Value));
			}
			if (source.OrderType.Stop.HasValue)
			{
				target.setField(new StopPx(source.OrderType.Stop.Value));
			}

			if (source.TimeInForce != null)
			{
				target.setField(new TimeInForce(source.TimeInForce.Type));
				if (source.TimeInForce.Expiration.HasValue)
				{
					if (source.TimeInForce.Expiration.Value.Kind == DateTimeKind.Unspecified)
					{
						target.setField(new ExpireDate(Tools.FormatLocalMktDate(source.TimeInForce.Expiration.Value)));
					}
					else
					{
						target.setField(new ExpireTime(source.TimeInForce.Expiration.Value));
					}
				}
			}

			AssignOrderContract(target, source.OrderContract);

			if (!string.IsNullOrEmpty(source.TradingSession))
			{
				var session = new Group(NoTradingSessions.FIELD, TradingSessionID.FIELD);
				session.setField(new TradingSessionID(source.TradingSession));
				target.addGroup(session);
			}

			if (source.AllocationBlock != null)
				AssignPreAllocationBlock(target, source.AllocationBlock);

			if (source.OrderType.TrailingStop != null)
			{
				target.setField(new ExecInst(ExecInst.TRAILING_STOP_PEG.ToString()));

				if (source.OrderType.TrailingStop.Amount.HasValue)
				{
					target.setField(new PegOffsetValue(source.OrderType.TrailingStop.Amount.Value));
				}

				if (source.OrderType.TrailingStop.TriggerType.HasValue)
				{
					target.setField(new TrailingTriggerType(source.OrderType.TrailingStop.TriggerType.Value));
					target.setField(new TrailingAmountInPercents(source.OrderType.TrailingStop.AmountInPercents));
				}
			}
		}

		protected virtual void AssignOrderContract(FieldMap message, OrderContract orderContract)
		{
			FixContract contract = GetFixContract(orderContract);

			if (!string.IsNullOrEmpty(contract.Symbol))
				message.setField(new Symbol(contract.Symbol));

			if (!string.IsNullOrEmpty(contract.CFICode))
				message.setField(new CFICode(contract.CFICode));

			if (contract.MonthYear.HasValue)
				message.setField(new QuickFix.MaturityMonthYear(Tools.FormatMonthYear(contract.MonthYear.Value)));

			if (contract.Strike.HasValue)
				message.setField(new StrikePrice(contract.Strike.Value));
		}

		private void AssignPreAllocationBlock(Message message, AllocationBlock<PreAllocationBlockItem> block)
		{
			message.setField(new AllocText(block.Name));
			message.setField(new AllocType((int) block.Rule));

			foreach (PreAllocationBlockItem item in block.Items)
			{
				var group = new NewOrderSingle.NoAllocs();
				group.set(new AllocAccount(item.Account));
				group.set(new AllocQty(item.Weight));
				message.addGroup(group);
			}
		}

		private void AssignPostAllocationBlock(Message message, AllocationBlock<PostAllocationBlockItem> block)
		{
			message.setField(new AllocType((int) block.Rule));

			foreach (PostAllocationBlockItem item in block.Items)
			{
				var group = new AllocationInstruction.NoAllocs();

				group.set(new AllocAccount(item.Account.Spec));

				if (!string.IsNullOrEmpty(item.Account.Firm))
					group.setField(new ClearingFirmID(item.Account.Firm));
				else if (!string.IsNullOrEmpty(item.Account.ClearingHouse))
					group.setField(new ClearingFirm(item.Account.ClearingHouse));

				group.set(new AllocPrice(item.Price));
				group.set(new AllocQty(item.Weight));

				message.addGroup(group);
			}
		}

		private FixContract GetFixContract(OrderContract contract)
		{
			var result = new FixContract {Symbol = contract.Symbol.Name};

			switch (contract.Symbol.Asset)
			{
				case ContractAsset.Future:
					if (contract.Symbol.Multileg)
					{
						result.CFICode = contract.Option ? Code.FutureOptionsMultileg : Code.FuturesMultileg;
					}
					else
					{
						if (contract.Option)
						{
							result.CFICode = contract.Put.Value ? Code.FutureOptionsPut : Code.FutureOptionsCall;
						}
						else
						{
							result.CFICode = Code.Futures;
						}
					}
					break;

				case ContractAsset.Forex:
					result.CFICode = Code.Forex;
					break;

				default:
					throw new ExecutionException("Unsupported asset '{0}'", contract.Symbol.Asset);
			}

			result.MonthYear = contract.Symbol.MonthYear;
			result.Strike = contract.Strike;

			return result;
		}

		private static void CopyFields(FieldMap source, FieldMap target, params int[] fields)
		{
			foreach (int field in fields)
			{
				if (source.isSetField(field))
				{
					target.setString(field, source.getString(field));
				}
			}
		}

		private struct FixContract
		{
			public string CFICode;
			public DateTime? MonthYear;
			public double? Strike;
			public string Symbol;
		}
	}

	internal class FixProtocol44 : FixProtocol
	{
		public override void EnsureTrade(Message msg, char orderStatus, int? qty, double? price)
		{
			base.EnsureTrade(msg, orderStatus, qty, price);

			var execType = msg.GetChar<ExecType>();
			if (execType == null || execType.getValue() != ExecType.TRADE)
			{
				throw new ExecutionException("Unexpected ExecType '{0}', must be '{1}'", execType, ExecType.TRADE);
			}
		}
	}

	internal class FixProtocol42 : FixProtocol
	{
		public override void EnsureTrade(Message msg, char orderStatus, int? qty, double? price)
		{
			base.EnsureTrade(msg, orderStatus, qty, price);

			var execType = msg.GetChar<ExecType>();
			if (execType == null || execType.getValue() != ExecType.FILL)
			{
				throw new ExecutionException("Unexpected ExecType '{0}', must be '{1}'", execType, ExecType.FILL);
			}

			var execTransType = msg.GetChar<ExecTransType>();
			if (execTransType == null || execTransType.getValue() != ExecTransType.NEW)
			{
				throw new ExecutionException("Unexpected ExecTransType '{0}', must be '{1}'", execTransType, ExecTransType.NEW);
			}
		}

		public override void EnsureOrderStatus(Message msg, char orderStatus)
		{
			base.EnsureOrderStatus(msg, orderStatus);

			var execTransType = msg.GetChar<ExecTransType>();
			if (execTransType == null || execTransType.getValue() != ExecTransType.NEW)
			{
				throw new ExecutionException("Unexpected ExecTransType '{0}', must be '{1}'", execTransType, ExecTransType.NEW);
			}
		}

		public override void EnsureModifyAccepted(Message msg, char orderStatus)
		{
			EnsureOrderStatus(msg, orderStatus);
		}

		protected override void AssignOrderContract(FieldMap target, OrderContract orderContract)
		{
			base.AssignOrderContract(target, orderContract);

			if (target.isSetField(CFICode.FIELD))
			{
				string cfiCode = target.getString(CFICode.FIELD);
				target.removeField(CFICode.FIELD);

				if (cfiCode == Code.Futures)
					target.setField(new SecurityType(SecurityType.FUTURE));
				else if (cfiCode == Code.FuturesMultileg || cfiCode == Code.FutureOptionsMultileg)
					target.setField(new SecurityType(SecurityType.MULTI_LEG_INSTRUMENT));
				else if (cfiCode == Code.FutureOptionsCall)
				{
					target.setField(new SecurityType(SecurityType.OPTIONS_ON_FUTURES));
					target.setField(new PutOrCall(PutOrCall.CALL));
				}
				else if (cfiCode == Code.FutureOptionsPut)
				{
					target.setField(new SecurityType(SecurityType.OPTIONS_ON_FUTURES));
					target.setField(new PutOrCall(PutOrCall.PUT));
				}
				else if (cfiCode == Code.Forex)
					target.setField(new SecurityType(SecurityType.FOREIGN_EXCHANGE_CONTRACT));
				else
					throw new ExecutionException("Unsupported CFICode '{0}'", cfiCode);
			}
		}
	}
}