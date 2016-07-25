using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using OEC.FIX.Sample.FAST;
using OEC.FIX.Sample.FIX;
using QuickFix;

namespace OEC.FIX.Sample.FoxScript
{
    partial class ExecEngine
    {
        private readonly FastClient _fastClient;
        private readonly FixEngine _fixEngine;
        private readonly StreamWriter _input;
        private readonly Dictionary<string, MsgVar> _msgVars = new Dictionary<string, MsgVar>();
        private readonly TestStatistics _testStat = new TestStatistics();
        private bool _running = true;
        private FixProtocol _fixProtocol;

        private FixProtocol FixProtocol
        {
            get
            {
                if (_fixProtocol == null || _fixProtocol.BeginString != FixVersion.Current(_properties))
                    _fixProtocol = FixVersion.Create(_properties);
                return _fixProtocol;
            }
        }

        public ExecEngine(Props properties)
        {
            _fixProtocol = FixVersion.Create(properties);
        }

        private void AssignFields(MessageWrapper target, FixFields fields)
        {
            if (fields == null)
            {
                return;
            }
            foreach (FixField field in fields)
            {
                object value = GetObjectValue(field.Value, null);
                MessageWrapper.SetFieldValue(target, field.Name, value);
            }
        }

        private void ExecuteOutgoingMsgCommand(string msgVarName, OutgoingMsgCommand command)
        {
            MessageWrapper msg = CreateMessage(command);

            AssignFields(msg, command.Fields);

            SetMsgVar(msgVarName, msg);

            if (msg.IsQuickFix)
                _fixEngine.SendMessage(msg.QFMessage);
            else
            {
                var output = command as IOutputFile;
                string outputFileName = null;
                if (output != null)
                    outputFileName = output.OutputFileName;

                _fastClient.SendMessage(msg.OFMessage, outputFileName, command is CancelSubscribeCommand);
            }
        }

        private void ExecuteIncomingMsgCommand(string msgVarName, IncomingMsgCommand command)
        {
            MessageWrapper msg;

            var messageCommand = command as WaitMessageCommand;
            if (messageCommand != null)
                msg = ExecuteWaitMessageCommand(messageCommand);
            else
                throw new ExecutionException("Unknown IncomingMsgCommand.");

            SetMsgVar(msgVarName, msg);
        }

        private MessageWrapper ExecuteWaitMessageCommand(WaitMessageCommand command)
        {
            bool isQF = false;

            string msgType = IsFastAvailable ? _fastClient.GetMsgTypeValue(command.MsgTypeName) : null;
            if (msgType == null)
            {
                msgType = QFReflector.GetMsgTypeValue(command.MsgTypeName);
                isQF = true;
            }

            var timeout = (TimeSpan)_properties[Prop.ResponseTimeout].Value;
            if (command.Timeout.HasValue)
            {
                timeout = command.Timeout.Value;
            }
            if (isQF)
            {
                Predicate<Message> predicate = null;
                if (command.LogicalExpr != null)
                {
                    predicate = BuildMsgCtxPredicate(command.LogicalExpr);
                }

                Message msg = _fixEngine.WaitMessage(msgType, timeout, predicate);
                if (msg == null)
                {
                    FixEngine.WriteLine("	No messages received of type '{0}'", command.MsgTypeName);
                }
                return MessageWrapper.Create(msg);
            }
            else
            {
                Predicate<OpenFAST.Message> predicate = null;
                if (command.LogicalExpr != null)
                {
                    predicate = BuildMsgCtxPredicateOF(command.LogicalExpr);
                }

                OpenFAST.Message msg = _fastClient.WaitMessage(msgType, timeout, predicate);
                if (msg == null)
                {
                    FixEngine.WriteLine("	No messages received of type '{0}'", command.MsgTypeName);
                }
                return MessageWrapper.Create(msg);
            }
        }

        private object GetSyntaxConstructionValue(object constr)
        {
            if (constr == null)
            {
                throw new SyntaxErrorException("Syntax construction not specified.");
            }

            if (constr.IsObject())
            {
                return GetObjectValue(constr as Object, null);
            }
            if (constr.IsLogicalExpr())
            {
                Func<bool> predicate = BuildPredicate(constr as LogicalExpr);
                return predicate();
            }
            throw new SyntaxErrorException("Unknown syntax construction type '{0}'", constr.GetType().FullName);
        }

        private string ApplyFormatArgs(FormatArgs fargs)
        {
            if (fargs == null)
            {
                return null;
            }

            object[] args = fargs
                .Args
                .Select(GetSyntaxConstructionValue)
                .ToArray();

            return string.Format(fargs.Format, args);
        }

        public Func<bool> BuildPredicate(object logicalExpr)
        {
            Expression body;
            BuildLogicalExpression(logicalExpr, out body);

            Expression<Func<bool>> expr = Expression.Lambda<Func<bool>>(body);
            return expr.Compile();
        }

        private Predicate<Message> BuildMsgCtxPredicate(object logicalExpr)
        {
            ParameterExpression context = Expression.Parameter(typeof(Message), "context");

            Expression body;
            BuildMsgCtxLogicalExpression(logicalExpr, context, out body);

            Expression<Predicate<Message>> expr = Expression.Lambda<Predicate<Message>>(body, context);
            return expr.Compile();
        }

        private Predicate<OpenFAST.Message> BuildMsgCtxPredicateOF(object logicalExpr)
        {
            ParameterExpression context = Expression.Parameter(typeof(OpenFAST.Message), "context");

            Expression body;
            BuildMsgCtxLogicalExpression(logicalExpr, context, out body);

            Expression<Predicate<OpenFAST.Message>> expr = Expression.Lambda<Predicate<OpenFAST.Message>>(body, context);
            return expr.Compile();
        }

        private static Expression BuildLogicalOperation(LogicalOp op, Expression left, Expression right)
        {
            switch (op)
            {
                case LogicalOp.Or:
                    return Expression.OrElse(left, right);

                case LogicalOp.And:
                    return Expression.AndAlso(left, right);

                case LogicalOp.Equal:
                    return Expression.Call(
                        left,
                        left.Type.GetEqualsMethodInfo(),
                        Expression.Convert(right, typeof(object)));

                case LogicalOp.NotEqual:
                    MethodCallExpression expr = Expression.Call(
                        left,
                        left.Type.GetEqualsMethodInfo(),
                        Expression.Convert(right, typeof(object)));

                    return Expression.IsFalse(expr);

                case LogicalOp.Less:
                    return Expression.LessThan(left, right);

                case LogicalOp.LessOrEqual:
                    return Expression.LessThanOrEqual(left, right);

                case LogicalOp.Greater:
                    return Expression.GreaterThan(left, right);

                case LogicalOp.GreaterOrEqual:
                    return Expression.GreaterThanOrEqual(left, right);

                default:
                    throw new ExecutionException("Unknown logical operation: {0}", op);
            }
        }

        private void BuildLogicalExpression(object source, out Expression target)
        {
            var logicalExpr = source as LogicalExpr;
            if (logicalExpr != null)
            {
                Expression left;
                BuildLogicalExpression(logicalExpr.Left, out left);

                Expression right;
                BuildLogicalExpression(logicalExpr.Right, out right);

                target = BuildLogicalOperation(logicalExpr.Operation, left, right);
            }
            else
            {
                var obj = source as Object;
                if (obj != null)
                {
                    target = Expression.Constant(GetObjectValue(obj, null));
                }
                else
                {
                    throw new ExecutionException("Unknown expression type {0}", source);
                }
            }
        }

        private void BuildMsgCtxLogicalExpression(object source, ParameterExpression context, out Expression target)
        {
            var logicalExpr = source as LogicalExpr;
            if (logicalExpr != null)
            {
                Expression left;
                BuildMsgCtxLogicalExpression(logicalExpr.Left, context, out left);

                Expression right;
                BuildMsgCtxLogicalExpression(logicalExpr.Right, context, out right);

                target = BuildLogicalOperation(logicalExpr.Operation, left, right);
            }
            else
            {
                var obj = source as Object;
                if (obj != null)
                {
                    target = obj.Type == ObjectType.FixField 
                        ? (Expression)Expression.Call(context.Type == typeof (Message) 
                            ? QFReflector.GetFieldValueMethodInfo 
                            : OFReflector.GetFieldValueMethodInfo, context, Expression.Constant(obj.Token)) 
                        : Expression.Constant(GetObjectValue(obj, null));
                }
                else
                {
                    throw new ExecutionException("Unknown expression type {0}", source);
                }
            }
        }

        private void SetMsgVar(string msgVarName, MessageWrapper msg)
        {
            if (string.IsNullOrEmpty(msgVarName))
            {
                return;
            }

            var varbl = new MsgVar(msgVarName) { Value = msg };
            _msgVars[msgVarName.ToUpperInvariant()] = varbl;
        }

        private MsgVar GetMsgVar(string msgVarName)
        {
            if (string.IsNullOrEmpty(msgVarName))
            {
                return null;
            }
            MsgVar varbl;
            if (_msgVars.TryGetValue(msgVarName.ToUpperInvariant(), out varbl))
            {
                return varbl;
            }
            throw new ExecutionException("Unknown message var '{0}'", msgVarName);
        }

        private MessageWrapper CreateMessage(OutgoingMsgCommand command)
        {
            MsgVar variable = null;
            var orderRefCommand = command as OrderRefOutgoingMsgCommand;
            if (orderRefCommand != null)
            {
                variable = GetMsgVar(orderRefCommand.OrigMsgVarName);
                variable.EnsureValueFix();
            }

            var newCommand = command as NewOrderCommand;
            if (newCommand != null)
                return MessageWrapper.Create(FixProtocol.NewOrderSingle(newCommand));

            var modifyCommand = command as ModifyOrderCommand;
            if (modifyCommand != null)
            {
                MsgVar varbl = GetMsgVar(modifyCommand.OrigMsgVarName);
                varbl.EnsureValueFix();

                return MessageWrapper.Create(FixProtocol.OrderCancelReplaceRequest(modifyCommand, varbl.Value.QFMessage));
            }

            var cancelCommand = command as CancelOrderCommand;
            if (cancelCommand != null)
                return MessageWrapper.Create(FixProtocol.OrderCancelRequest(cancelCommand, variable.Value.QFMessage));

            var statusCommand = command as OrderStatusCommand;
            if (statusCommand != null)
                return MessageWrapper.Create(FixProtocol.OrderStatusRequest(statusCommand, variable.Value.QFMessage));

            var massStatusCommand = command as OrderMassStatusCommand;
            if (massStatusCommand != null)
                return MessageWrapper.Create(FixProtocol.OrderMassStatusRequest(massStatusCommand));

            var balanceCommand = command as BalanceCommand;
            if (balanceCommand != null)
                return MessageWrapper.Create(FixProtocol.CollateralInquiry(balanceCommand));

            var positionsCommand = command as PositionsCommand;
            if (positionsCommand != null)
                return MessageWrapper.Create(FixProtocol.RequestForPositions(positionsCommand));

            var lookupCommand = command as SymbolLookupCommand;
            if (lookupCommand != null)
                return MessageWrapper.Create(FixProtocol.SymbolLookupRequest(lookupCommand));

            var baseContractCommand = command as BaseContractRequestCommand;
            if (baseContractCommand != null)
                return MessageWrapper.Create(FixProtocol.BaseContractRequest(baseContractCommand));

            var contractRequestCommand = command as ContractRequestCommand;
            if (contractRequestCommand != null)
                return MessageWrapper.Create(FixProtocol.ContractRequest(contractRequestCommand));

            var postAllocationCommand = command as PostAllocationCommand;
            if (postAllocationCommand != null)
                return MessageWrapper.Create(FixProtocol.AllocationInstruction(postAllocationCommand, variable.Value.QFMessage));

            var userRequestCommand = command as UserRequestCommand;
            if (userRequestCommand != null)
                return MessageWrapper.Create(FixProtocol.UserRequest(userRequestCommand));

            var marginCalcCommand = command as MarginCalcCommand;
            if (marginCalcCommand != null)
                return MessageWrapper.Create(FixProtocol.MarginCalc(marginCalcCommand));

            var mdMessageCommand = command as MDMessageCommand;
            if (mdMessageCommand != null && IsFastAvailable)
                return MessageWrapper.Create(_fastClient.MessageFactory.MarketDataRequest(mdMessageCommand));

            var cancelSubscribeCommand = command as CancelSubscribeCommand;
            if (cancelSubscribeCommand != null && IsFastAvailable)
            {
                MsgVar varCancel = GetMsgVar(cancelSubscribeCommand.MDMessageVar);
                varCancel.EnsureValueFAST();
                return MessageWrapper.Create(_fastClient.MessageFactory.CancelMDMessage(varCancel.Value.OFMessage));
            }

            var bracketOrderCommand = command as BracketOrderCommand;
            if (bracketOrderCommand != null)
                return MessageWrapper.Create(FixProtocol.NewOrderList(bracketOrderCommand, (msgVar, message) => SetMsgVar(msgVar, MessageWrapper.Create(message))));

            throw new ExecutionException("Unknown OutgoingMsgCommand.");
        }

        public object GetObjectValue(Object obj, MessageWrapper context)
        {
            if (obj == null)
            {
                throw new ExecutionException("Object not specified.");
            }
            switch (obj.Type)
            {
                case ObjectType.GlobalProp:
                    {
                        string s = obj.Token.Substring(5); //	skip "PROP:" prefix
                        return _properties[s].Value;
                    }

                case ObjectType.FixConst:
                    {
                        string s = obj.Token.Substring(4); //	skip "FIX." prefix
                        int i = s.IndexOf('.');
                        return QFReflector.GetConstValue(s.Substring(0, i), s.Substring(i + 1));
                    }

                case ObjectType.FixField:
                    return MessageWrapper.GetFieldValue(context, obj.Token);

                case ObjectType.FixMsgVar:
                    return GetMsgVar(obj.Token);

                case ObjectType.FixMsgVarField:
                    {
                        int i = obj.Token.IndexOf('.');

                        MsgVar varbl = GetMsgVar(obj.Token.Substring(0, i));
                        varbl.EnsureValue();

                        return MessageWrapper.GetFieldValue(varbl.Value, obj.Token.Substring(i + 1));
                    }

                case ObjectType.Null:
                    return MsgVar.Null;

                case ObjectType.Bool:
                    return LiteralParser.ParseBool(obj.Token);

                case ObjectType.Float:
                    return LiteralParser.ParseFloat(obj.Token);

                case ObjectType.Integer:
                    return LiteralParser.ParseInteger(obj.Token);

                case ObjectType.String:
                    return LiteralParser.ParseString(obj.Token);

                case ObjectType.Timespan:
                    return LiteralParser.ParseTimespan(obj.Token);

                case ObjectType.Timestamp:
                    return LiteralParser.ParseTimestamp(obj.Token);

                case ObjectType.Date:
                    return LiteralParser.ParseDate(obj.Token);

                default:
                    throw new ExecutionException("Invalid ObjectType {0}", obj.Type);
            }
        }

        private Parser CreateParser(string script, string name)
        {
            _input.Flush();
            _input.BaseStream.SetLength(0);
            _input.WriteLine(script);

            var parser = new Parser(new Scanner(_input.BaseStream), this, new StrictErrors())
            {
                errors = { errMsgFormat = "	ERROR in [" + name + "]: {2}, Line: {0}, Col: {1}" }
            };

            return parser;
        }

        private string ReadLine()
        {
            string line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                return line;
            }
            return line.EndsWith(";") ? line : line + ";";
        }
    }
}