using System;
using System.Linq;
using OEC.FIX.Sample.CFI;
using OEC.FIX.Sample.FIX.Fields;
using OEC.FIX.Sample.FoxScript;
using OEC.FIX.Sample.FoxScript.AllocationBlocks;
using QuickFix;
using QuickFix.FIX44;
using QuickFix.Fields;
using Message = QuickFix.Message;
using TimeInForce = QuickFix.Fields.TimeInForce;
using Tags = QuickFix.Fields.Tags;

namespace OEC.FIX.Sample.FIX
{
    internal static class FixVersion
    {
        public static readonly string FIX42 = "FIX.4.2";
        public static readonly string FIX44 = "FIX.4.4";

        public static FixProtocol Create(Props properties)
        {
            var current = (string)properties[Prop.BeginString].Value;
            if (current == FIX44)
                return new FixProtocol44(properties);
            if (current == FIX42)
                return new FixProtocol42(properties);

            throw new ExecutionException("Unsupported FIX version '{0}'", current ?? "NULL");
        }

        public static string Current(Props properties)
        {
            return (string)properties[Prop.BeginString].Value;
        }
    }

    internal abstract class FixProtocol
    {
        private readonly Props _properties;

        public abstract string BeginString { get; }

        protected FixProtocol(Props properties)
        {
            _properties = properties;
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
            return new CollateralInquiry
            {
                Account = new Account(command.Account),
                CollInquiryID = new CollInquiryID(Tools.GenerateUniqueID()),
                ResponseTransportType = new ResponseTransportType(ResponseTransportType.INBAND),
                SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT)
            };
        }

        public Message RequestForPositions(PositionsCommand command)
        {
            return new RequestForPositions
            {
                Account = new Account(command.Account),
                AccountType = new AccountType(AccountType.ACCOUNT_IS_CARRIED_ON_CUSTOMER_SIDE_OF_BOOKS),
                PosReqID = new PosReqID(Tools.GenerateUniqueID()),
                PosReqType = new PosReqType(PosReqType.POSITIONS),
                ResponseTransportType = new ResponseTransportType(ResponseTransportType.INBAND),
                SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT),
                TransactTime = new TransactTime(DateTime.UtcNow),
                ClearingBusinessDate = new ClearingBusinessDate(DateTime.Today.ToString("yyyyMMdd"))
            };
        }

        internal Message UserRequest(UserRequestCommand userRequestCommand)
        {
            var msg = new UserRequest(
                new UserRequestID(Tools.GenerateUniqueID()),
                new UserRequestType(userRequestCommand.UserRequestType),
                new Username(userRequestCommand.Name));
            if (userRequestCommand.UUID != null)
                msg.SetField(new UUIDField(userRequestCommand.UUID));
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

                group.SetField(new MinQty(pos.MinQty));
                group.SetField(new MaxQty(pos.MaxQty));

                msg.AddGroup(group);
            }
            return msg;
        }

        public Message ContractRequest(ContractRequestCommand command)
        {
            var msg = new SecurityListRequest
            {
                SecurityReqID = new SecurityReqID(Tools.GenerateUniqueID()),
                SecurityListRequestType = new SecurityListRequestType(SecurityListRequestType.PRODUCT),
                Symbol = new Symbol(command.Name)
            };

            if (command.SubscriptionRequestType != ContractRequestCommand.DEFAULT_SUBSCRIBTION_TYPE)
                msg.SubscriptionRequestType = new SubscriptionRequestType(command.SubscriptionRequestType);

            if (command.UpdatesSinceTimestamp.HasValue)
                msg.SetField(new UpdatesSinceTimestamp(command.UpdatesSinceTimestamp.Value));

            return msg;
        }

        public Message BaseContractRequest(BaseContractRequestCommand command)
        {
            var msg = new SecurityListRequest
            {
                SecurityReqID = new SecurityReqID(Tools.GenerateUniqueID()),
                SecurityListRequestType = new SecurityListRequestType(SecurityListRequestType.ALL_SECURITIES)
            };

            if (command.SubscriptionRequestType != ContractRequestCommand.DEFAULT_SUBSCRIBTION_TYPE)
                msg.SubscriptionRequestType = new SubscriptionRequestType(command.SubscriptionRequestType);

            if (!string.IsNullOrEmpty(command.Exchange))
                msg.SecurityExchange = new SecurityExchange(command.Exchange);
            if (!string.IsNullOrEmpty(command.ContractGroup))
                msg.SetField(new ContractGroupField(command.ContractGroup));
            if (command.CompoundType != CompoundType.UNKNOWN)
                msg.SecuritySubType = new SecuritySubType(command.CompoundTypeString);

            return msg;
        }

        public Message SymbolLookupRequest(SymbolLookupCommand command)
        {
            var msg = new SecurityListRequest
            {
                SecurityReqID = new SecurityReqID(Tools.GenerateUniqueID()),
                SecurityListRequestType = new SecurityListRequestType(SecurityListRequestType.SYMBOL),
                Text = new Text(command.Name)
            };

            msg.SetField(new SymbolLookupModeField(command.Mode));
            msg.SetField(new MaxRecordsField(command.MaxRecords));

            if (command.ContractKinds.Count > 0)
            {
                var group = new SecurityTypes.NoSecurityTypesGroup();
                foreach (Code code in command.ContractKinds
                    .Select(k => Code.Create(k, command.OptionType))
                    .Where(c => c != null))
                {
                    group.CFICode = new CFICode(code.ToFix());
                    msg.AddGroup(group);
                }
            }

            if (!string.IsNullOrEmpty(command.Exchange))
                msg.SecurityExchange = new SecurityExchange(command.Exchange);
            if (!string.IsNullOrEmpty(command.ContractGroup))
                msg.SetField(new ContractGroupField(command.ContractGroup));
            if (command.ContractType.HasValue)
                msg.SetField(new ContractTypeField((int)command.ContractType.Value));
            if (command.ByBaseContractsOnly.HasValue)
                msg.SetField(new ByBaseContractsOnlyField(command.ByBaseContractsOnly.Value));
            if (command.OptionsRequired.HasValue)
                msg.SetField(new OptionsRequiredField(command.OptionsRequired.Value));

            if (command.ParentContract != null)
            {
                Contract parentContract = command.ParentContract;
                msg.SetField(new UnderlyingCFICode(parentContract.Code.ToFix()));
                msg.SetField(new UnderlyingSymbol(parentContract.Symbol));
                msg.SetField(new UnderlyingMaturityMonthYear(parentContract.MaturityMonthYear.ToFix()));
                if (parentContract.Strike != null)
                    msg.SetField(new UnderlyingStrikePrice(Convert.ToDecimal(parentContract.Strike.Value)));
            }

            if (!string.IsNullOrEmpty(command.BaseContract))
                msg.Symbol = new Symbol(command.BaseContract);

            if (command.CompoundType != CompoundType.UNKNOWN)
                msg.SecuritySubType = new SecuritySubType(command.CompoundTypeString);

            return msg;
        }

        public Message OrderCancelRequest(CancelOrderCommand command, Message orig)
        {
            var msg = new OrderCancelRequest
            {
                ClOrdID = new ClOrdID(Tools.GenerateUniqueID()),
                TransactTime = new TransactTime(DateTime.UtcNow)
            };


            if (orig.IsSetField(Tags.ClOrdID))
                msg.OrigClOrdID = new OrigClOrdID(orig.GetString(Tags.ClOrdID));

            CopyImmutableOrderFields(orig, msg);
            CopyFields(orig, msg, Tags.OrderQty);

            return msg;
        }

        internal Message OrderStatusRequest(OrderStatusCommand orderStatusCommand, Message orig)
        {
            var msg = new OrderStatusRequest { ClOrdID = new ClOrdID(orig.GetString(Tags.ClOrdID)) };
            CopyImmutableOrderFields(orig, msg);
            return msg;
        }

        internal Message OrderMassStatusRequest(OrderMassStatusCommand command)
        {
            var request = new OrderMassStatusRequest { MassStatusReqID = new MassStatusReqID(Tools.GenerateUniqueID()) };

            if (command.OrderSide != null)
            {
                request.Side = new Side(command.OrderSide.Side);
                if (command.OrderSide.Open.HasValue)
                    request.SetField(new PositionEffect(command.OrderSide.Open.Value ? PositionEffect.OPEN : PositionEffect.CLOSE));
            }

            if (command.OrderContract != null)
                AssignOrderContract(request, command.OrderContract);

            if (command.AllocationBlock != null)
                AssignPreAllocationBlock(request, command.AllocationBlock, group => request.AddGroup(group));

            var account = string.IsNullOrEmpty(command.Account) && command.OrderContract != null
                ? GetAccountFor(command.OrderContract.Symbol.Asset)
                : command.Account;
            if (account != null)
                request.Account = new Account(account);

            return request;
        }

        private void CopyImmutableOrderFields(Message orig, Message msg)
        {
            CopyFields(orig, msg,
                Tags.Side,
                Tags.Account,
                Tags.AllocText,
                Tags.AllocType,
                Tags.Symbol,
                Tags.SecurityType,
                Tags.CFICode,
                Tags.MaturityMonthYear,
                Tags.StrikePrice,
                Tags.PutOrCall);

            if (orig.GroupCount(Tags.NoAllocs) > 0)
                for (int i = 0; i < orig.GroupCount(Tags.NoAllocs); ++i)
                {
                    var origGroup = new NewOrderSingle.NoAllocsGroup();
                    var group = new NewOrderSingle.NoAllocsGroup();
                    orig.GetGroup(i + 1, origGroup);

                    CopyFields(origGroup, group, Tags.AllocAccount, Tags.AllocQty);
                    msg.AddGroup(group);
                }
        }

        public Message OrderCancelReplaceRequest(ModifyOrderCommand command, Message orig)
        {
            var msg = new OrderCancelReplaceRequest();

            AssignOrderBody(command, msg, group => msg.AddGroup(group));

            if (orig.IsSetField(Tags.ClOrdID))
            {
                msg.OrigClOrdID = new OrigClOrdID(orig.GetString(Tags.ClOrdID));
            }

            return msg;
        }

        public Message AllocationInstruction(PostAllocationCommand command, Message orig)
        {
            var msg = new AllocationInstruction { AllocID = new AllocID(Tools.GenerateUniqueID()) };

            if (command.Contract != null)
                AssignOrderContract(msg, command.Contract);

            if (orig.IsSetField(Tags.ClOrdID))
            {
                var group = new AllocationInstruction.NoOrdersGroup();
                group.ClOrdID = new ClOrdID(orig.GetString(Tags.ClOrdID));
                msg.AddGroup(group);
            }

            AssignPostAllocationBlock(msg, command.AllocationBlock);

            return msg;
        }


        public Message NewOrderSingle(NewOrderCommand command)
        {
            var msg = new NewOrderSingle();

            AssignOrderBody(command, msg, group => msg.AddGroup(group));

            return msg;
        }

        public Message NewOrderList(BracketOrderCommand command, Action<string, Message> addMsgVar)
        {
            var cnt = command.BracketCommands.Count;
            if (cnt < 2)
                throw new ExecutionException("Count of groups is wrong ({0}).", cnt);

            var msg = new NewOrderList
            {
                TotNoOrders = new TotNoOrders(cnt),
                ListExecInst = new ListExecInst(command.Type.ToString()),
                ListID = new ListID(Tools.GenerateUniqueID()),
                BidType = new BidType(BidType.NO_BIDDING_PROCESS)
            };

            int lsq = 1;
            foreach (var cmd in command.BracketCommands)
            {
                var order = new NewOrderList.NoOrdersGroup();
                AssignOrderBody(cmd, order, group => order.AddGroup(group));
                order.ListSeqNo = new ListSeqNo(lsq++);
                msg.AddGroup(order);

                if (!string.IsNullOrWhiteSpace(cmd.MsgVarName))
                {
                    var neworder = NewOrderSingle(cmd);
                    neworder.SetField(new ClOrdID(order.ClOrdID.getValue()));
                    addMsgVar(cmd.MsgVarName, neworder);
                }
            }
            return msg;
        }

        protected void AssignOrderBody(OrderCommand source, FieldMap target, Action<Group> addGroup)
        {
            target.SetField(new ClOrdID(Tools.GenerateUniqueID()));
            target.SetField(new Side(source.OrderSide.Side));
            target.SetField(new TransactTime(DateTime.UtcNow));

            switch (source.OrderType.Type)
            {
                case OrderType.ICEBERG:
                    target.SetField(new OrdType(OrdType.LIMIT));
                    if (source.OrderType.MaxFloor.HasValue)
                    {
                        target.SetField(new MaxFloor(source.OrderType.MaxFloor.Value));
                    }
                    break;

                case OrderType.MARKET_ON_OPEN:
                    target.SetField(new OrdType(OrdType.MARKET));
                    target.SetField(new TimeInForce(TimeInForce.AT_THE_OPENING));
                    break;

                case OrderType.MARKET_ON_CLOSE:
                    target.SetField(new OrdType(OrdType.MARKET));
                    target.SetField(new TimeInForce(TimeInForce.AT_THE_CLOSE));
                    break;

                default:
                    target.SetField(new OrdType(source.OrderType.Type));
                    break;
            }

            string account = source.Account;
            if (string.IsNullOrEmpty(account))
            {
                account = GetAccountFor(source.OrderContract.Symbol.Asset);
            }

            target.SetField(new Account(account));
            target.SetField(new OrderQty(source.OrderQty));

            if (source.OrderSide.Open.HasValue)
            {
                target.SetField(new PositionEffect(source.OrderSide.Open.Value ? PositionEffect.OPEN : PositionEffect.CLOSE));
            }

            if (source.OrderType.Limit.HasValue)
            {
                target.SetField(new Price(Convert.ToDecimal(source.OrderType.Limit.Value)));
            }
            if (source.OrderType.Stop.HasValue)
            {
                target.SetField(new StopPx(Convert.ToDecimal(source.OrderType.Stop.Value)));
            }

            if (source.TimeInForce != null)
            {
                target.SetField(new TimeInForce(source.TimeInForce.Type));
                if (source.TimeInForce.Expiration.HasValue)
                {
                    if (source.TimeInForce.Expiration.Value.Kind == DateTimeKind.Unspecified)
                    {
                        target.SetField(new ExpireDate(Tools.FormatLocalMktDate(source.TimeInForce.Expiration.Value)));
                    }
                    else
                    {
                        target.SetField(new ExpireTime(source.TimeInForce.Expiration.Value));
                    }
                }
            }

            AssignOrderContract(target, source.OrderContract);

            if (!string.IsNullOrEmpty(source.TradingSession))
            {
                var session = new Group(Tags.NoTradingSessions, Tags.TradingSessionID);
                session.SetField(new TradingSessionID(source.TradingSession));
                addGroup(session);
            }

            if (source.AllocationBlock != null)
                AssignPreAllocationBlock(target, source.AllocationBlock, addGroup);

            if (source.OrderType.TrailingStop != null)
            {
                target.SetField(new ExecInst(ExecInst.TRAILING_STOP_PEG));

                if (source.OrderType.TrailingStop.Amount.HasValue)
                {
                    target.SetField(new PegOffsetValue(Convert.ToDecimal(source.OrderType.TrailingStop.Amount.Value)));
                }

                if (source.OrderType.TrailingStop.TriggerType.HasValue)
                {
                    target.SetField(new TrailingTriggerType(source.OrderType.TrailingStop.TriggerType.Value));
                    target.SetField(new TrailingAmountInPercents(source.OrderType.TrailingStop.AmountInPercents));
                }
            }
        }

        protected virtual void AssignOrderContract(FieldMap message, OrderContract orderContract)
        {
            FixContract contract = GetFixContract(orderContract);

            if (!string.IsNullOrEmpty(contract.Symbol))
                message.SetField(new Symbol(contract.Symbol));

            if (!string.IsNullOrEmpty(contract.CFICode))
                message.SetField(new CFICode(contract.CFICode));

            if (contract.MonthYear.HasValue)
                message.SetField(new QuickFix.Fields.MaturityMonthYear(Tools.FormatMonthYear(contract.MonthYear.Value)));

            if (contract.Strike.HasValue)
                message.SetField(new StrikePrice(Convert.ToDecimal(contract.Strike.Value)));
        }

        private void AssignPreAllocationBlock(FieldMap message, AllocationBlock<PreAllocationBlockItem> block, Action<Group> addGroup)
        {
            message.SetField(new AllocText(block.Name));
            message.SetField(new AllocType((int)block.Rule));

            foreach (PreAllocationBlockItem item in block.Items)
            {
                var group = new NewOrderSingle.NoAllocsGroup();
                group.AllocAccount = new AllocAccount(item.Account);
                group.AllocQty = new AllocQty(Convert.ToDecimal(item.Weight));
                addGroup(group);
            }
        }

        private void AssignPostAllocationBlock(Message message, AllocationBlock<PostAllocationBlockItem> block)
        {
            message.SetField(new AllocType((int)block.Rule));

            foreach (PostAllocationBlockItem item in block.Items)
            {
                var group = new AllocationInstruction.NoAllocsGroup();

                group.AllocAccount = new AllocAccount(item.Account.Spec);

                if (!string.IsNullOrEmpty(item.Account.Firm))
                    group.SetField(new ClearingFirmID(item.Account.Firm));
                else if (!string.IsNullOrEmpty(item.Account.ClearingHouse))
                    group.SetField(new ClearingFirm(item.Account.ClearingHouse));

                group.AllocPrice = new AllocPrice(Convert.ToDecimal(item.Price));
                group.AllocQty = new AllocQty(Convert.ToDecimal(item.Weight));

                message.AddGroup(group);
            }
        }

        private FixContract GetFixContract(OrderContract contract)
        {
            var result = new FixContract { Symbol = contract.Symbol.Name };

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
                            result.CFICode = contract.Put.HasValue && contract.Put.Value ? Code.FutureOptionsPut : Code.FutureOptionsCall;
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
            //TODO: VP it is not good solution
            foreach (var tag in fields.Where(source.IsSetField))
                target.SetField(new StringField(tag, source.GetField(tag)));
        }

        public string GetAccountFor(ContractAsset asset)
        {
            switch (asset)
            {
                case ContractAsset.Forex:
                    return (string)_properties[Prop.ForexAccount].Value;

                case ContractAsset.Future:
                    return (string)_properties[Prop.FutureAccount].Value;

                default:
                    throw new ExecutionException("Invalid ContractAsset.");
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
        public override string BeginString => FixVersion.FIX44;

        public FixProtocol44(Props properties)
            : base(properties)
        {
        }

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
        public override string BeginString => FixVersion.FIX42;

        public FixProtocol42(Props properties)
            : base(properties)
        {
        }

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

            if (target.IsSetField(Tags.CFICode))
            {
                string cfiCode = target.GetString(Tags.CFICode);
                target.RemoveField(Tags.CFICode);

                if (cfiCode == Code.Futures)
                    target.SetField(new SecurityType(SecurityType.FUTURE));
                else if (cfiCode == Code.FuturesMultileg || cfiCode == Code.FutureOptionsMultileg)
                    target.SetField(new SecurityType(SecurityType.MULTI_LEG_INSTRUMENT));
                else if (cfiCode == Code.FutureOptionsCall)
                {
                    target.SetField(new SecurityType(SecurityType.OPTIONS_ON_FUTURES));
                    target.SetField(new PutOrCall(PutOrCall.CALL));
                }
                else if (cfiCode == Code.FutureOptionsPut)
                {
                    target.SetField(new SecurityType(SecurityType.OPTIONS_ON_FUTURES));
                    target.SetField(new PutOrCall(PutOrCall.PUT));
                }
                else if (cfiCode == Code.Forex)
                    target.SetField(new SecurityType(SecurityType.FOREIGN_EXCHANGE_CONTRACT));
                else
                    throw new ExecutionException("Unsupported CFICode '{0}'", cfiCode);
            }
        }
    }
}