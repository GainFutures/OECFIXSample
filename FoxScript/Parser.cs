using System.Collections.Generic;
using OEC.FIX.Sample.FoxScript.AllocationBlocks;



using System;

namespace OEC.FIX.Sample.FoxScript {



public class Parser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _messageField = 2;
	public const int _fixConst = 3;
	public const int _globalProp = 4;
	public const int _endOfClause = 5;
	public const int _comma = 6;
	public const int _actionNew = 7;
	public const int _actionModify = 8;
	public const int _actionCancel = 9;
	public const int _actionOrderStatus = 10;
	public const int _actionOrderMassStatus = 11;
	public const int _actionDelete = 12;
	public const int _actionWait = 13;
	public const int _actionEnsure = 14;
	public const int _actionPrint = 15;
	public const int _actionPrintf = 16;
	public const int _actionReset = 17;
	public const int _actionSet = 18;
	public const int _actionGet = 19;
	public const int _actionPing = 20;
	public const int _actionBegin = 21;
	public const int _actionEnd = 22;
	public const int _actionPositions = 23;
	public const int _actionBalance = 24;
	public const int _actionQuit = 25;
	public const int _actionExit = 26;
	public const int _actionStop = 27;
	public const int _actionConnect = 28;
	public const int _actionAuth = 29;
	public const int _actionDisconnect = 30;
	public const int _actionExec = 31;
	public const int _actionTest = 32;
	public const int _actionTestStat = 33;
	public const int _actionEnsureOrderStatus = 34;
	public const int _actionEnsurePureOrderStatus = 35;
	public const int _actionEnsureModifyAccepted = 36;
	public const int _actionEnsureTrade = 37;
	public const int _actionSleep = 38;
	public const int _actionAnyKey = 39;
	public const int _actionContract = 40;
	public const int _actionPostAllocation = 41;
	public const int _actionPost = 42;
	public const int _actionPAlloc = 43;
	public const int _actionUserRequest = 44;
	public const int _actionMarginCalc = 45;
	public const int _actionHeartbeat = 46;
	public const int _actionConnectFast = 47;
	public const int _actionDisconnectFast = 48;
	public const int _actionCancelSubscribe = 49;
	public const int _actionSubscribeQuotes = 50;
	public const int _actionSubscribeDOM = 51;
	public const int _actionSubscribeHistogram = 52;
	public const int _actionSubscribeTicks = 53;
	public const int _actionLoadTicks = 54;
	public const int _assignOp = 55;
	public const int _equalOp = 56;
	public const int _notEqualOp = 57;
	public const int _lessOp = 58;
	public const int _lessOrEqualOp = 59;
	public const int _greaterOp = 60;
	public const int _greaterOrEqualOp = 61;
	public const int _orOp = 62;
	public const int _andOp = 63;
	public const int _integer = 64;
	public const int _float = 65;
	public const int _string = 66;
	public const int _true = 67;
	public const int _false = 68;
	public const int _on = 69;
	public const int _off = 70;
	public const int _null = 71;
	public const int _timespan = 72;
	public const int _timestamp = 73;
	public const int _date = 74;
	public const int _uuid = 75;
	public const int _buy = 76;
	public const int _sell = 77;
	public const int _put = 78;
	public const int _call = 79;
	public const int _open = 80;
	public const int _close = 81;
	public const int maxT = 233;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

internal Parser(Scanner scanner, ExecEngine execEngine, Errors errors)
		: this(scanner)
	{
		this.ExecEngine = execEngine;
		if (errors != null) {
			this.errors = errors;
		}
	}

	internal readonly ExecEngine ExecEngine;

	private Token Peek()
	{
		return scanner.Peek();
	}

	private void ResetPeek()
	{
		scanner.ResetPeek();
	}

	private void SemanticAction(System.Action action)
	{
		if (action == null) {
			return;
		}
		
		if (SemanticActionEnabled())
		{ 
			try
			{
				action();
			}
			catch (System.Exception e)
			{
				SemErr(e.Message ?? "Semantic action failed.");
			}
		}
	}

	private bool IsMsgVarNameFormatArg()
	{
		ResetPeek();
		var next = Peek();
		return next.kind == _comma;
	}
	
	private bool IsRValueFormatArg()
	{
		ResetPeek();
		var next = Peek();
		return next.kind == _comma;
	}
	
	private bool IsSenderCompID()
	{
		ResetPeek();
		var next = Peek();
		return next.kind == _comma;
	}

	private void PushBranchingPredicate(Func<bool> condition)
	{
		branchingPredicates.Push(new BranchingPredicate(condition));
	}

	private BranchingPredicate PopBranchingPredicate()
	{
		if (branchingPredicates.Empty()) {
			throw new InvalidOperationException("No BranchingPredicates assigned.");
		}
		return branchingPredicates.Pop();
	}
	
	private bool SemanticActionEnabled()
	{
		if (branchingPredicates.Empty()) {
			return true;
		}
		return CurrentBranchingPredicate.Accepted;
	}
	
	private BranchingPredicate CurrentBranchingPredicate
	{
		get
		{
			if (branchingPredicates.Empty()) {
				throw new InvalidOperationException("No BranchingPredicates assigned.");
			}
			return branchingPredicates.Peek(); 
		}
	}

	class BranchingPredicate
	{
		public bool Positive = true;
	
		public BranchingPredicate(Func<bool> condition)
		{
			if (condition == null) {
				throw new ArgumentException("Condition for BranchingPredicate not specified.");
			}
			this.condition = condition;
		}
		
		public bool Accepted
		{
			get { return Positive ? condition() : !condition(); }
		}
		
		private readonly Func<bool> condition;
	}
	
	private readonly Stack<BranchingPredicate> branchingPredicates = new Stack<BranchingPredicate>();
	


	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void FoxScript() {
		while (StartOf(1)) {
			if (StartOf(2)) {
				Command();
			} else {
				Statement();
			}
		}
	}

	void Command() {
		if (StartOf(3)) {
			SimpleCommand();
		} else if (StartOf(4)) {
			MsgProducingCommand();
		} else SynErr(234);
	}

	void Statement() {
		IfStatement();
	}

	void IfStatement() {
		object expr = null; bool actionEnabled = SemanticActionEnabled(); 
		Expect(82);
		Expect(83);
		LogicalExpr(ref expr);
		Expect(84);
		if (actionEnabled)
		{
			var predicate = ExecEngine.BuildPredicate(expr); 
			PushBranchingPredicate(predicate);
			CurrentBranchingPredicate.Positive = true;
		}
		
		Expect(85);
		while (StartOf(1)) {
			if (StartOf(2)) {
				Command();
			} else {
				Statement();
			}
		}
		Expect(86);
		if (la.kind == 87) {
			Get();
			if (actionEnabled) {
			CurrentBranchingPredicate.Positive = false; 
			}
			
			Expect(85);
			while (StartOf(1)) {
				if (StartOf(2)) {
					Command();
				} else {
					Statement();
				}
			}
			Expect(86);
		}
		Expect(5);
		if (actionEnabled) {
		PopBranchingPredicate(); 
		}
		
	}

	void LogicalExpr(ref object expr) {
		OrExpr(ref expr);
	}

	void SimpleCommand() {
		switch (la.kind) {
		case 19: {
			GetPropCommand();
			break;
		}
		case 18: {
			SetPropCommand();
			break;
		}
		case 20: {
			PingCommand();
			break;
		}
		case 15: {
			PrintCommand();
			break;
		}
		case 16: {
			PrintfCommand();
			break;
		}
		case 25: case 26: case 27: {
			QuitCommand();
			break;
		}
		case 31: {
			ExecCommand();
			break;
		}
		case 32: {
			TestCommand();
			break;
		}
		case 33: {
			TestStatCommand();
			break;
		}
		case 14: {
			EnsureCommand();
			break;
		}
		case 34: {
			EnsureOrderStatusCommand();
			break;
		}
		case 35: {
			EnsurePureOrderStatusCommand();
			break;
		}
		case 36: {
			EnsureModifyAcceptedCommand();
			break;
		}
		case 37: {
			EnsureTradeCommand();
			break;
		}
		case 38: {
			SleepCommand();
			break;
		}
		case 39: {
			AnyKeyCommand();
			break;
		}
		case 28: {
			ConnectCommand();
			break;
		}
		case 30: {
			DisconnectCommand();
			break;
		}
		case 17: {
			ResetSeqnumbers();
			break;
		}
		case 29: {
			AuthorizationCommand();
			break;
		}
		case 47: {
			ConnectFastCommand();
			break;
		}
		case 48: {
			DisconnectFastCommand();
			break;
		}
		case 46: {
			HeartbeatCommand();
			break;
		}
		default: SynErr(235); break;
		}
		Expect(5);
	}

	void MsgProducingCommand() {
		string msgVarName = null; MsgCommand command = null; 
		if (la.kind == 1 || la.kind == 88) {
			if (la.kind == 88) {
				Get();
			}
			MsgVarName(ref msgVarName);
			Expect(55);
		}
		switch (la.kind) {
		case 7: {
			NewOrderCommand(ref command);
			break;
		}
		case 8: {
			ModifyOrderCommand(ref command);
			break;
		}
		case 9: {
			CancelOrderCommand(ref command);
			break;
		}
		case 13: {
			WaitMessageCommand(ref command);
			break;
		}
		case 24: {
			BalanceCommand(ref command);
			break;
		}
		case 10: {
			OrderStatusCommand(ref command);
			break;
		}
		case 11: {
			OrderMassStatusCommand(ref command);
			break;
		}
		case 23: {
			PositionsCommand(ref command);
			break;
		}
		case 40: {
			ContractCommand(ref command);
			break;
		}
		case 41: case 42: case 43: {
			PostAllocationCommand(ref command);
			break;
		}
		case 44: {
			UserRequestCommand(ref command);
			break;
		}
		case 45: {
			MarginCalcCommand(ref command);
			break;
		}
		case 49: {
			CancelSubscribeCommand(ref command);
			break;
		}
		case 50: {
			SubscribeQuotesCommand(ref command);
			break;
		}
		case 51: {
			SubscribeDOMCommand(ref command);
			break;
		}
		case 52: {
			SubscribeHistogramCommand(ref command);
			break;
		}
		case 53: {
			SubscribeTicksCommand(ref command);
			break;
		}
		case 54: {
			LoadTicksCommand(ref command);
			break;
		}
		default: SynErr(236); break;
		}
		Expect(5);
		ExecEngine.MessageCommand(msgVarName, command); 
	}

	void MsgVarName(ref string msgVarName) {
		Expect(1);
		msgVarName = t.val; 
	}

	void GetPropCommand() {
		string name = null; 
		Expect(19);
		if (la.kind == 1) {
			Get();
			name = t.val; 
		}
		SemanticAction(() => ExecEngine.GetPropsValue(name)); 
	}

	void SetPropCommand() {
		Expect(18);
		if (la.kind == 196) {
			SetSeqNumPropCommand();
		} else if (la.kind == 1) {
			SetCommonPropCommand();
		} else SynErr(237);
	}

	void PingCommand() {
		Expect(20);
		SemanticAction(() => ExecEngine.Ping()); 
	}

	void PrintCommand() {
		object arg = null; var args = new List<object>(); 
		Expect(15);
		FormatArg(ref arg);
		args.Add(arg); 
		while (la.kind == 6) {
			Get();
			arg = null; 
			FormatArg(ref arg);
			args.Add(arg); 
		}
		SemanticAction(() => ExecEngine.Print(args)); 
	}

	void PrintfCommand() {
		FormatArgs fargs = null; 
		Expect(16);
		FormatArgs(ref fargs);
		SemanticAction(() => ExecEngine.Printf(fargs)); 
	}

	void QuitCommand() {
		if (la.kind == 25) {
			Get();
		} else if (la.kind == 26) {
			Get();
		} else if (la.kind == 27) {
			Get();
		} else SynErr(238);
		SemanticAction(() => ExecEngine.Exit()); 
	}

	void ExecCommand() {
		string filename = null; string scriptName = null; 
		Expect(31);
		IdentOrString(ref filename);
		if (la.kind == 6) {
			Get();
			Expect(66);
			scriptName = LiteralParser.ParseString(t.val); 
		}
		SemanticAction(() => ExecEngine.Exec(filename, scriptName)); 
	}

	void TestCommand() {
		string filename = null; 
		Expect(32);
		IdentOrString(ref filename);
		SemanticAction(() => ExecEngine.Test(filename)); 
	}

	void TestStatCommand() {
		bool reset = false; 
		Expect(33);
		if (la.kind == 17) {
			Get();
			reset = true; 
		}
		SemanticAction(() => ExecEngine.TestStat(reset)); 
	}

	void EnsureCommand() {
		FormatArgs fargs = null; object expr = null; 
		Expect(14);
		LogicalExpr(ref expr);
		if (la.kind == 6) {
			Get();
			FormatArgs(ref fargs);
		}
		SemanticAction(() => ExecEngine.Ensure(expr, fargs)); 
	}

	void EnsureOrderStatusCommand() {
		string msgVarName = null; Object ordStatus = null; 
		Expect(34);
		MsgVarName(ref msgVarName);
		RValue(ref ordStatus);
		SemanticAction(() => ExecEngine.EnsureOrderStatus(msgVarName, ordStatus)); 
	}

	void EnsurePureOrderStatusCommand() {
		string msgVarName = null; Object ordStatus = null; 
		Expect(35);
		MsgVarName(ref msgVarName);
		RValue(ref ordStatus);
		SemanticAction(() => ExecEngine.EnsurePureOrderStatus(msgVarName, ordStatus)); 
	}

	void EnsureModifyAcceptedCommand() {
		string msgVarName = null; Object ordStatus = null; 
		Expect(36);
		MsgVarName(ref msgVarName);
		RValue(ref ordStatus);
		SemanticAction(() => ExecEngine.EnsureModifyAccepted(msgVarName, ordStatus)); 
	}

	void EnsureTradeCommand() {
		string msgVarName = null; Object ordStatus = null; 
		Expect(37);
		MsgVarName(ref msgVarName);
		RValue(ref ordStatus);
		SemanticAction(() => ExecEngine.EnsureTrade(msgVarName, ordStatus)); 
	}

	void SleepCommand() {
		TimeSpan timeout; 
		Expect(38);
		if (la.kind == 72) {
			Get();
			timeout = LiteralParser.ParseTimespan(t.val); 
			SemanticAction(() => ExecEngine.Sleep(timeout)); 
		}
	}

	void AnyKeyCommand() {
		Expect(39);
		SemanticAction(() => ExecEngine.AnyKey()); 
	}

	void ConnectCommand() {
		string senderCompID = null; string password = null; string uuid = null; 
		Expect(28);
		if (IsSenderCompID()) {
			IdentOrString(ref senderCompID);
			Expect(6);
		}
		IdentOrString(ref password);
		if (la.kind == 75) {
			Get();
			uuid = t.val; 
		}
		SemanticAction(() => ExecEngine.Connect(senderCompID, password, uuid)); 
	}

	void DisconnectCommand() {
		Expect(30);
		SemanticAction(() => ExecEngine.Disconnect()); 
	}

	void ResetSeqnumbers() {
		Expect(17);
		SemanticAction(() => ExecEngine.ResetSeqnums()); 
		Expect(196);
	}

	void AuthorizationCommand() {
		string senderCompID = string.Empty; 
		Expect(29);
		IdentOrString(ref senderCompID);
		SemanticAction(() => ExecEngine.Auth(senderCompID)); 
	}

	void ConnectFastCommand() {
		string username = null; 
		Expect(47);
		if (la.kind == 1 || la.kind == 66) {
			IdentOrString(ref username);
		}
		SemanticAction(() => ExecEngine.ConnectFast(username)); 
	}

	void DisconnectFastCommand() {
		Expect(48);
		SemanticAction(() => ExecEngine.DisconnectFast()); 
	}

	void HeartbeatCommand() {
		Expect(46);
		SemanticAction(() => ExecEngine.FASTHeartbeat()); 
	}

	void NewOrderCommand(ref MsgCommand command) {
		var cmd = new NewOrderCommand(); 
		Expect(7);
		OrderBody(cmd);
		if (la.kind == 6) {
			Get();
			MessageFieldAssignments(cmd.Fields);
		}
		command = cmd; 
	}

	void ModifyOrderCommand(ref MsgCommand command) {
		var cmd = new ModifyOrderCommand(); 
		Expect(8);
		OrigMsgVarName(ref cmd.OrigMsgVarName);
		OrderBody(cmd);
		if (la.kind == 6) {
			Get();
			MessageFieldAssignments(cmd.Fields);
		}
		command = cmd; 
	}

	void CancelOrderCommand(ref MsgCommand command) {
		var cmd = new CancelOrderCommand(); 
		Expect(9);
		OrigMsgVarName(ref cmd.OrigMsgVarName);
		if (la.kind == 6) {
			Get();
			MessageFieldAssignments(cmd.Fields);
		}
		command = cmd; 
	}

	void WaitMessageCommand(ref MsgCommand command) {
		var cmd = new WaitMessageCommand(); 
		Expect(13);
		if (la.kind == 72) {
			Timeout();
			cmd.Timeout = LiteralParser.ParseTimespan(t.val); 
		}
		IdentOrString(ref cmd.MsgTypeName);
		if (la.kind == 6) {
			Get();
			MsgCtxLogicalExpr(ref cmd.LogicalExpr);
		}
		command = cmd; 
	}

	void BalanceCommand(ref MsgCommand command) {
		var cmd = new BalanceCommand(); 
		Expect(24);
		Account(ref cmd.Account);
		if (la.kind == 6) {
			Get();
			MessageFieldAssignments(cmd.Fields);
		}
		command = cmd; 
	}

	void OrderStatusCommand(ref MsgCommand command) {
		var cmd = new OrderStatusCommand(); 
		Expect(10);
		OrigMsgVarName(ref cmd.OrigMsgVarName);
		command = cmd; 
	}

	void OrderMassStatusCommand(ref MsgCommand command) {
		var cmd = new OrderMassStatusCommand(); 
		Expect(11);
		if (la.kind == 76 || la.kind == 77) {
			OrderSide(ref cmd.OrderSide);
		}
		if (la.kind == 1 || la.kind == 66) {
			OrderContract(ref cmd.OrderContract);
		}
		if (la.kind == 1 || la.kind == 66 || la.kind == 197) {
			if (la.kind == 1 || la.kind == 66) {
				AllocationBlock(out cmd.AllocationBlock);
			}
			Expect(197);
			Account(ref cmd.Account);
		}
		command = cmd; 
	}

	void PositionsCommand(ref MsgCommand command) {
		var cmd = new PositionsCommand(); 
		Expect(23);
		Account(ref cmd.Account);
		if (la.kind == 6) {
			Get();
			MessageFieldAssignments(cmd.Fields);
		}
		command = cmd; 
	}

	void ContractCommand(ref MsgCommand command) {
		Expect(40);
		if (la.kind == 90 || la.kind == 91 || la.kind == 92) {
			ContractRequest(ref command);
		} else if (la.kind == 95 || la.kind == 96 || la.kind == 97) {
			ContractLookup(ref command);
		} else SynErr(239);
	}

	void PostAllocationCommand(ref MsgCommand command) {
		var cmd = new PostAllocationCommand(); 
		if (la.kind == 41) {
			Get();
		} else if (la.kind == 42) {
			Get();
		} else if (la.kind == 43) {
			Get();
		} else SynErr(240);
		OrigMsgVarName(ref cmd.OrigMsgVarName);
		if (la.kind == 1 || la.kind == 66) {
			OrderContract(ref cmd.Contract);
		}
		PostAllocationBlock(out cmd.AllocationBlock);
		command = cmd; 
	}

	void UserRequestCommand(ref MsgCommand command) {
		var userCommand = new UserRequestCommand(); 
		Expect(44);
		if (la.kind == 89) {
			Get();
			Expect(55);
			IdentOrString(ref userCommand.UUID);
		}
		IdentOrString(ref userCommand.Name);
		command = userCommand; 
	}

	void MarginCalcCommand(ref MsgCommand command) {
		var cmd = new MarginCalcCommand(); 
		Expect(45);
		Account(ref cmd.Account);
		MarginCalcPositions(ref cmd.Positions);
		if (la.kind == 6) {
			Get();
			MessageFieldAssignments(cmd.Fields);
		}
		command = cmd; 
	}

	void CancelSubscribeCommand(ref MsgCommand command) {
		var cmd = new CancelSubscribeCommand(); 
		Expect(49);
		OrigMsgVarName(ref cmd.MDMessageVar);
		command = cmd; 
	}

	void SubscribeQuotesCommand(ref MsgCommand command) {
		var cmd = new SubscribeQuotesCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(50);
		FASTUpdateType(mdCmd);
		FASTContract(mdCmd);
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void SubscribeDOMCommand(ref MsgCommand command) {
		var cmd = new SubscribeDOMCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(51);
		FASTUpdateType(mdCmd);
		FASTContract(mdCmd);
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void SubscribeHistogramCommand(ref MsgCommand command) {
		var cmd = new SubscribeHistogramCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(52);
		FASTUpdateType(mdCmd);
		FASTContract(mdCmd);
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void SubscribeTicksCommand(ref MsgCommand command) {
		var cmd = new SubscribeTicksCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(53);
		FASTUpdateType(mdCmd);
		FASTContract(mdCmd);
		if (la.kind == 226 || la.kind == 231) {
			if (la.kind == 231) {
				Get();
				Expect(73);
				cmd.SetStartTime(LiteralParser.ParseTimestamp(t.val)); 
			} else {
				Get();
				Expect(72);
				cmd.SetStartTime(LiteralParser.ParseTimespan(t.val)); 
			}
		}
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void LoadTicksCommand(ref MsgCommand command) {
		var cmd = new LoadTicksCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(54);
		FASTUpdateType(mdCmd);
		FASTContract(mdCmd);
		if (la.kind == 231) {
			Get();
			Expect(73);
			cmd.SetStartTime(LiteralParser.ParseTimestamp(t.val)); 
			Expect(232);
			Expect(73);
			cmd.SetEndTime(LiteralParser.ParseTimestamp(t.val)); 
		} else if (la.kind == 226) {
			Get();
			Expect(72);
			cmd.SetStartTime(LiteralParser.ParseTimespan(t.val)); 
		} else SynErr(241);
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void MessageFieldAssignments(FixFields fields) {
		string name = null; Object value = null; 
		if (la.kind == 1) {
			Get();
			name = t.val; 
		} else if (la.kind == 64) {
			Get();
			name = t.val; 
		} else SynErr(242);
		Expect(55);
		RValue(ref value);
		fields.Add(name, value); 
		while (la.kind == 6) {
			name = null; value = null; 
			Get();
			if (la.kind == 1) {
				Get();
				name = t.val; 
			} else if (la.kind == 64) {
				Get();
				name = t.val; 
			} else SynErr(243);
			Expect(55);
			RValue(ref value);
			fields.Add(name, value); 
		}
	}

	void RValue(ref Object value) {
		if (StartOf(5)) {
			Literal(ref value);
		} else if (la.kind == 2) {
			Get();
			value = new Object(ObjectType.FixMsgVarField, t.val); 
		} else if (la.kind == 3) {
			Get();
			value = new Object(ObjectType.FixConst, t.val); 
		} else if (la.kind == 4) {
			Get();
			value = new Object(ObjectType.GlobalProp, t.val); 
		} else SynErr(244);
	}

	void IdentOrString(ref string str) {
		if (la.kind == 1) {
			Get();
			str = t.val; 
		} else if (la.kind == 66) {
			Get();
			str = LiteralParser.ParseString(t.val); 
		} else SynErr(245);
	}

	void MarginCalcPosition(out MarginCalcCommand.Position position) {
		position = new MarginCalcCommand.Position(); 
		OrderContract(ref position.Contract);
		Expect(64);
		position.MinQty = int.Parse(t.val); 
		Expect(64);
		position.MaxQty = int.Parse(t.val); 
	}

	void OrderContract(ref OrderContract contract) {
		contract = new OrderContract(); 
		Symbol(ref contract.Symbol);
		if (la.kind == 78 || la.kind == 79) {
			StrikeSide(contract);
		}
	}

	void MarginCalcPositions(ref List<MarginCalcCommand.Position> positions) {
		positions = new List<MarginCalcCommand.Position>();
		MarginCalcCommand.Position item;
		
		Expect(85);
		MarginCalcPosition(out item);
		positions.Add(item); 
		while (la.kind == 6) {
			Get();
			MarginCalcPosition(out item);
			positions.Add(item); 
		}
		Expect(86);
	}

	void Account(ref string account) {
		if (la.kind == 1) {
			Get();
			account = t.val; 
		} else if (la.kind == 66) {
			Get();
			account = LiteralParser.ParseString(t.val); 
		} else if (la.kind == 64) {
			Get();
			account = t.val; 
		} else if (la.kind == 4) {
			Get();
			var prop = new Object(ObjectType.GlobalProp, t.val); account = (string)ExecEngine.GetObjectValue(prop, null); 
		} else SynErr(246);
	}

	void ContractRequest(ref MsgCommand command) {
		if (la.kind == 90) {
			Get();
		} else if (la.kind == 91) {
			Get();
		} else if (la.kind == 92) {
			Get();
		} else SynErr(247);
		if (la.kind == 1 || la.kind == 66) {
			ContractRequestCommand temp; 
			ByBaseContractRequest(out temp);
			command = temp; 
		} else if (la.kind == 93 || la.kind == 94) {
			BaseContractRequestCommand temp; 
			BaseContractRequest(out temp);
			command = temp; 
		} else SynErr(248);
	}

	void ContractLookup(ref MsgCommand command) {
		var lcommand = new SymbolLookupCommand(); 
		if (la.kind == 95) {
			Get();
		} else if (la.kind == 96) {
			Get();
		} else if (la.kind == 97) {
			Get();
		} else SynErr(249);
		IdentOrString(ref lcommand.Name);
		Expect(6);
		Expect(98);
		Expect(55);
		ContractLookupMode(ref lcommand.Mode);
		Expect(6);
		Expect(99);
		Expect(55);
		Expect(64);
		lcommand.MaxRecords = LiteralParser.ParseInteger(t.val); 
		while (la.kind == 6) {
			Get();
			ContractLookupParam(ref lcommand);
		}
		command = lcommand; 
	}

	void ByBaseContractRequest(out ContractRequestCommand command) {
		command = new ContractRequestCommand(); 
		IdentOrString(ref command.Name);
		if (StartOf(6)) {
			SubscriptionType(ref command.SubscriptionRequestType);
		}
		if (la.kind == 73) {
			Get();
			command.UpdatesSinceTimestamp = LiteralParser.ParseTimestamp(t.val); 
		}
	}

	void BaseContractRequest(out BaseContractRequestCommand command) {
		command = new BaseContractRequestCommand(); 
		if (la.kind == 93) {
			Get();
		} else if (la.kind == 94) {
			Get();
		} else SynErr(250);
		if (StartOf(6)) {
			SubscriptionType(ref command.SubscriptionRequestType);
			Expect(6);
		}
		BaseContractParam(ref command);
		while (la.kind == 6) {
			Get();
			BaseContractParam(ref command);
		}
	}

	void SubscriptionType(ref char symbolRequestType) {
		if (la.kind == 100) {
			Get();
			symbolRequestType = '0'; 
		} else if (la.kind == 101) {
			Get();
			symbolRequestType = '1'; 
		} else if (la.kind == 102) {
			Get();
			symbolRequestType = '2'; 
		} else if (la.kind == 103) {
			Get();
			symbolRequestType = 'U'; 
		} else SynErr(251);
	}

	void BaseContractParam(ref BaseContractRequestCommand command) {
		if (la.kind == 111) {
			Get();
			Expect(55);
			IdentOrString(ref command.Exchange);
		} else if (la.kind == 112) {
			Get();
			Expect(55);
			IdentOrString(ref command.ContractGroup);
		} else if (la.kind == 113) {
			Get();
			Expect(55);
			CompoundType(ref command.CompoundType);
		} else SynErr(252);
	}

	void ContractLookupMode(ref int mode) {
		if (la.kind == 191) {
			Get();
			mode = 0; 
		} else if (la.kind == 192) {
			Get();
			mode = 1; 
		} else if (la.kind == 193) {
			Get();
			mode = 2; 
		} else if (la.kind == 194) {
			Get();
			mode = 3; 
		} else if (la.kind == 195) {
			Get();
			mode = 4; 
		} else SynErr(253);
	}

	void ContractLookupParam(ref SymbolLookupCommand command) {
		switch (la.kind) {
		case 104: {
			Get();
			Expect(55);
			ContractKindList(ref command.ContractKinds);
			break;
		}
		case 105: {
			Get();
			Expect(55);
			ContractType(ref command.ContractType);
			break;
		}
		case 106: {
			Get();
			Expect(55);
			OptionType(ref command.OptionType);
			break;
		}
		case 107: {
			Get();
			Expect(55);
			BoolOptional(ref command.ByBaseContractsOnly);
			break;
		}
		case 108: {
			Get();
			Expect(55);
			BoolOptional(ref command.OptionsRequired);
			break;
		}
		case 109: {
			Get();
			Expect(55);
			IdentOrString(ref command.BaseContract);
			break;
		}
		case 110: {
			Get();
			Expect(55);
			ContractDescription(ref command.ParentContract);
			break;
		}
		case 111: case 112: case 113: {
			BaseContractRequestCommand bcommand = command as BaseContractRequestCommand; 
			BaseContractParam(ref bcommand);
			break;
		}
		default: SynErr(254); break;
		}
	}

	void ContractKindList(ref List<ContractKind> list) {
		ContractKind kind; 
		if (StartOf(7)) {
			ContractKind(out kind);
			list.Add(kind); 
		} else if (la.kind == 83) {
			Get();
			ContractKind(out kind);
			list.Add(kind); 
			while (la.kind == 6) {
				Get();
				ContractKind(out kind);
				list.Add(kind); 
			}
			Expect(84);
		} else SynErr(255);
	}

	void ContractType(ref ContractType? ctype) {
		if (la.kind == 184) {
			Get();
			ctype = Sample.FoxScript.ContractType.ELECTRONIC; 
		} else if (la.kind == 185) {
			Get();
			ctype = Sample.FoxScript.ContractType.PIT; 
		} else SynErr(256);
	}

	void OptionType(ref OptionType optionType) {
		optionType = Sample.FoxScript.OptionType.ALL; 
		if (la.kind == 78) {
			Get();
			optionType = Sample.FoxScript.OptionType.PUT; 
		} else if (la.kind == 79) {
			Get();
			optionType = Sample.FoxScript.OptionType.CALL; 
		} else SynErr(257);
	}

	void BoolOptional(ref bool? value) {
		if (la.kind == 67) {
			Get();
			value = true; 
		} else if (la.kind == 68) {
			Get();
			value = false; 
		} else SynErr(258);
	}

	void ContractDescription(ref Contract contract) {
		var stringValue = string.Empty; 
		contract = new Contract();
		
		Expect(83);
		IdentOrString(ref stringValue);
		contract.Code = new CFI.Code(stringValue); 
		Expect(6);
		IdentOrString(ref contract.Symbol);
		Expect(6);
		IdentOrString(ref stringValue);
		contract.MaturityMonthYear = new MaturityMonthYear(stringValue); 
		if (la.kind == 6) {
			Get();
			DoubleOptional(ref contract.Strike);
		}
		Expect(84);
	}

	void CompoundType(ref CompoundType compoundType) {
		switch (la.kind) {
		case 114: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.UNKNOWN; 
			break;
		}
		case 115: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.GENERIC; 
			break;
		}
		case 116: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PERFORMANCE_INDEX_BASKET; 
			break;
		}
		case 117: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.NON_PERFORMANCE_INDEX_BASKET; 
			break;
		}
		case 118: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE; 
			break;
		}
		case 119: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRANGLE; 
			break;
		}
		case 120: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_TIME_SPREAD; 
			break;
		}
		case 121: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.OPTION_TIME_SPREAD; 
			break;
		}
		case 122: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PRICE_SPREAD; 
			break;
		}
		case 123: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.SYNTHETIC_UNDERLYING; 
			break;
		}
		case 124: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE_TIME_SPREAD; 
			break;
		}
		case 125: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_SPREAD; 
			break;
		}
		case 126: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_FUTURE_TIME_SPREAD; 
			break;
		}
		case 127: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_OPTION_TIME_SPREAD; 
			break;
		}
		case 128: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PUT_CALL_SPREAD; 
			break;
		}
		case 129: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_PUT_CALL_SPREAD; 
			break;
		}
		case 130: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.LADDER; 
			break;
		}
		case 131: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BOX; 
			break;
		}
		case 132: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BUTTERFLY; 
			break;
		}
		case 133: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.CONDOR; 
			break;
		}
		case 134: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.IRON_BUTTERFLY; 
			break;
		}
		case 135: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.DIAGONAL_SPREAD; 
			break;
		}
		case 136: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_DIAGONAL_SPREAD; 
			break;
		}
		case 137: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE_DIAGONAL_SPREAD; 
			break;
		}
		case 138: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.CONVERSION_REVERSAL; 
			break;
		}
		case 139: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.COVERED_OPTION; 
			break;
		}
		case 140: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RESERVED1; 
			break;
		}
		case 141: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RESERVED2; 
			break;
		}
		case 142: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.CURRENCY_FUTURE_SPREAD; 
			break;
		}
		case 143: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATE_FUTURE_SPREAD; 
			break;
		}
		case 144: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.INDEX_FUTURE_SPREAD; 
			break;
		}
		case 145: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_BUTTERFLY; 
			break;
		}
		case 146: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_CONDOR; 
			break;
		}
		case 147: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRIP; 
			break;
		}
		case 148: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PACK; 
			break;
		}
		case 149: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BUNDLE; 
			break;
		}
		case 150: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BOND_DELIVERABLE_BASKET; 
			break;
		}
		case 151: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STOCK_BASKET; 
			break;
		}
		case 152: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PRICE_SPREAD_VS_OPTION; 
			break;
		}
		case 153: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE_VS_OPTION; 
			break;
		}
		case 154: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BOND_SPREAD; 
			break;
		}
		case 155: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.EXCHANGE_SPREAD; 
			break;
		}
		case 156: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_PACK_SPREAD; 
			break;
		}
		case 157: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_PACK_BUTTERFLY; 
			break;
		}
		case 158: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.WHOLE_SALE; 
			break;
		}
		case 159: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.COMMODITY_SPREAD; 
			break;
		}
		case 160: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.JELLY_ROLL; 
			break;
		}
		case 161: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.IRON_CONDOR; 
			break;
		}
		case 162: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.OPTIONS_STRIP; 
			break;
		}
		case 163: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.CONTINGENT_ORDERS; 
			break;
		}
		case 164: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.INTERPRODUCT_SPREAD; 
			break;
		}
		case 165: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PSEUDO_STRADDLE; 
			break;
		}
		case 166: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.TAILOR_MADE; 
			break;
		}
		case 167: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURES_GENERIC; 
			break;
		}
		case 168: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.OPTIONS_GENERIC; 
			break;
		}
		case 169: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BASIS_TRADE; 
			break;
		}
		case 170: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURETIME_SPREAD_REDUCED_TICK_SIZE; 
			break;
		}
		case 171: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.GENERIC_VOLA_STRATEGY_VS; 
			break;
		}
		case 172: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE_VOLA_STRATEGY_VS; 
			break;
		}
		case 173: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRANGLE_VS; 
			break;
		}
		case 174: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.OPTION_TIME_SPREAD_VS; 
			break;
		}
		case 175: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PRICE_SPREAD_VS; 
			break;
		}
		case 176: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_SPREAD_VS; 
			break;
		}
		case 177: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PUT_CALL_SPREADVS; 
			break;
		}
		case 178: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.LADDER_VS; 
			break;
		}
		case 179: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PRICE_SPREAD_VS_OPTION_VS; 
			break;
		}
		case 180: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.COLLAR; 
			break;
		}
		case 181: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.COMBO; 
			break;
		}
		case 182: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PROTECTIVE_PUT; 
			break;
		}
		case 183: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.SPREAD; 
			break;
		}
		default: SynErr(259); break;
		}
	}

	void DoubleOptional(ref double? value) {
		if (la.kind == 64) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else if (la.kind == 65) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else SynErr(260);
	}

	void ContractKind(out ContractKind kind) {
		kind = Sample.FoxScript.ContractKind.UNKNOWN; 
		if (la.kind == 186) {
			Get();
			kind = Sample.FoxScript.ContractKind.FUTURE; 
		} else if (la.kind == 187) {
			Get();
			kind = Sample.FoxScript.ContractKind.OPTION; 
		} else if (la.kind == 188) {
			Get();
			kind = Sample.FoxScript.ContractKind.FOREX; 
		} else if (la.kind == 189) {
			Get();
			kind = Sample.FoxScript.ContractKind.FUTURE_COMPOUND; 
		} else if (la.kind == 190) {
			Get();
			kind = Sample.FoxScript.ContractKind.OPTIONS_COMPOUND; 
		} else SynErr(261);
	}

	void OrderBody(OrderCommand command) {
		OrderSide(ref command.OrderSide);
		OrderQty(ref command.OrderQty);
		OrderContract(ref command.OrderContract);
		OrderType(ref command.OrderType);
		if (StartOf(8)) {
			TimeInForce(ref command.TimeInForce);
		}
		if (StartOf(9)) {
			TradingSession(ref command.TradingSession);
		}
		if (la.kind == 1 || la.kind == 66) {
			AllocationBlock(out command.AllocationBlock);
		}
		if (la.kind == 197) {
			Get();
			Account(ref command.Account);
		}
	}

	void OrigMsgVarName(ref string origMsgVarName) {
		Expect(1);
		origMsgVarName = t.val; 
	}

	void OrderSide(ref OrderSide orderSide) {
		orderSide = new OrderSide(); 
		if (la.kind == 76) {
			Get();
			orderSide.Side = QuickFix.Side.BUY; 
		} else if (la.kind == 77) {
			Get();
			orderSide.Side = QuickFix.Side.SELL; 
		} else SynErr(262);
		if (la.kind == 80 || la.kind == 81) {
			if (la.kind == 80) {
				Get();
				orderSide.Open = true; 
			} else {
				Get();
				orderSide.Open = false; 
			}
		}
	}

	void AllocationBlock(out AllocationBlock<PreAllocationBlockItem> ab) {
		string str = string.Empty; 
		ab = new AllocationBlock<PreAllocationBlockItem>(); 
		PreAllocationBlockItem item;
		
		IdentOrString(ref str);
		ab.Name = str; 
		AllocationRule(ref ab.Rule);
		Expect(85);
		AllocationItem(out item);
		ab.Add(item); 
		while (la.kind == 6) {
			Get();
			AllocationItem(out item);
			ab.Add(item); 
		}
		Expect(86);
	}

	void PostAllocationBlock(out AllocationBlock<PostAllocationBlockItem> ab) {
		ab = new AllocationBlock<PostAllocationBlockItem>(); 
		PostAllocationBlockItem item;
		
		PostAllocationRule(ref ab.Rule);
		Expect(85);
		PostAllocationItem(out item);
		ab.Add(item); 
		while (la.kind == 6) {
			Get();
			PostAllocationItem(out item);
			ab.Add(item); 
		}
		Expect(86);
	}

	void FormatArgs(ref FormatArgs fargs) {
		Expect(66);
		fargs = new FormatArgs(LiteralParser.ParseString(t.val)); 
		while (la.kind == 6) {
			Get();
			object arg = null; 
			FormatArg(ref arg);
			fargs.AddArg(arg);  
		}
	}

	void FormatArg(ref object arg) {
		string msgVarName = null; object expr = null; Object rvalue = null; 
		if (IsRValueFormatArg()) {
			RValue(ref rvalue);
			arg = rvalue; 
		} else if (IsMsgVarNameFormatArg()) {
			MsgVarName(ref msgVarName);
			arg = new Object(ObjectType.FixMsgVar, msgVarName); 
		} else if (StartOf(10)) {
			LogicalExpr(ref expr);
			arg = expr; 
		} else SynErr(263);
	}

	void SetSeqNumPropCommand() {
		Expect(196);
		int SenderSeqNum = -1; int TargetSeqNum = -1; 
		IntegerOrDefault(ref SenderSeqNum);
		Expect(6);
		IntegerOrDefault(ref TargetSeqNum);
		SemanticAction(() => ExecEngine.SetSeqNumbers(SenderSeqNum, TargetSeqNum)); 
	}

	void SetCommonPropCommand() {
		Object value = null; string name = null; 
		Expect(1);
		name = t.val; 
		Literal(ref value);
		SemanticAction(() => ExecEngine.SetPropValue(name, value)); 
	}

	void IntegerOrDefault(ref int result) {
		if (la.kind == 198) {
			Get();
			result = -1; 
		} else if (la.kind == 64) {
			Get();
			result = LiteralParser.ParseInteger(t.val); 
		} else SynErr(264);
	}

	void Literal(ref Object value) {
		switch (la.kind) {
		case 64: {
			Get();
			value = new Object(ObjectType.Integer, t.val); 
			break;
		}
		case 65: {
			Get();
			value = new Object(ObjectType.Float, t.val); 
			break;
		}
		case 66: {
			Get();
			value = new Object(ObjectType.String, t.val); 
			break;
		}
		case 67: {
			Get();
			value = new Object(ObjectType.Bool, t.val); 
			break;
		}
		case 69: {
			Get();
			value = new Object(ObjectType.Bool, "TRUE"); 
			break;
		}
		case 68: {
			Get();
			value = new Object(ObjectType.Bool, t.val); 
			break;
		}
		case 70: {
			Get();
			value = new Object(ObjectType.Bool, "FALSE"); 
			break;
		}
		case 72: {
			Get();
			value = new Object(ObjectType.Timespan, t.val); 
			break;
		}
		case 73: {
			Get();
			value = new Object(ObjectType.Timestamp, t.val); 
			break;
		}
		case 74: {
			Get();
			value = new Object(ObjectType.Date, t.val); 
			break;
		}
		default: SynErr(265); break;
		}
	}

	void OrderQty(ref int orderQty) {
		Expect(64);
		orderQty = LiteralParser.ParseInteger(t.val); 
	}

	void OrderType(ref OrderType orderType) {
		double stop = 0, limit = 0; orderType = new OrderType(); 
		switch (la.kind) {
		case 218: {
			Get();
			orderType.Type = QuickFix.OrdType.MARKET; 
			break;
		}
		case 219: {
			Get();
			orderType.Type = Sample.FoxScript.OrderType.MARKET_ON_OPEN; 
			break;
		}
		case 220: {
			Get();
			orderType.Type = Sample.FoxScript.OrderType.MARKET_ON_CLOSE; 
			break;
		}
		case 221: {
			Get();
			orderType.Type = QuickFix.OrdType.LIMIT; 
			Price(ref limit);
			orderType.Limit = limit; 
			break;
		}
		case 222: {
			Get();
			orderType.Type = QuickFix.OrdType.STOP; 
			Price(ref stop);
			orderType.Stop = stop; 
			if (la.kind == 221) {
				Get();
				orderType.Type = QuickFix.OrdType.STOP_LIMIT; 
				Price(ref limit);
				orderType.Limit = limit; 
			}
			if (la.kind == 225) {
				TrailingStop(ref orderType.TrailingStop);
			}
			break;
		}
		case 223: {
			Get();
			orderType.Type = Sample.FoxScript.OrderType.ICEBERG; 
			Expect(64);
			orderType.MaxFloor = LiteralParser.ParseInteger(t.val); 
			Expect(221);
			Price(ref limit);
			orderType.Limit = limit; 
			break;
		}
		case 224: {
			Get();
			orderType.Type = QuickFix.OrdType.MARKET_IF_TOUCHED; 
			Price(ref limit);
			orderType.Limit = limit; 
			break;
		}
		default: SynErr(266); break;
		}
	}

	void TimeInForce(ref TimeInForce tif) {
		tif = new TimeInForce(); 
		if (la.kind == 213) {
			Get();
			tif.Type = QuickFix.TimeInForce.DAY; 
		} else if (la.kind == 214) {
			Get();
			tif.Type = QuickFix.TimeInForce.GOOD_TILL_CANCEL; 
		} else if (la.kind == 215) {
			Get();
			tif.Type = QuickFix.TimeInForce.GOOD_TILL_DATE; 
			if (la.kind == 73) {
				Get();
				tif.Expiration = LiteralParser.ParseTimestamp(t.val); 
			} else if (la.kind == 74) {
				Get();
				tif.Expiration = LiteralParser.ParseDate(t.val); 
			} else SynErr(267);
		} else if (la.kind == 216) {
			Get();
			tif.Type = QuickFix.TimeInForce.FILL_OR_KILL; 
		} else if (la.kind == 217) {
			Get();
			tif.Type = QuickFix.TimeInForce.IMMEDIATE_OR_CANCEL; 
		} else SynErr(268);
	}

	void TradingSession(ref string session) {
		if (la.kind == 206) {
			Get();
			Expect(199);
		}
		switch (la.kind) {
		case 207: {
			Get();
			session = t.val; 
			break;
		}
		case 208: {
			Get();
			session = t.val; 
			break;
		}
		case 209: {
			Get();
			session = t.val; 
			break;
		}
		case 210: {
			Get();
			session = t.val; 
			break;
		}
		case 211: {
			Get();
			session = t.val; 
			break;
		}
		case 212: {
			Get();
			session = t.val; 
			break;
		}
		default: SynErr(269); break;
		}
	}

	void AllocationRule(ref AllocationRule rule) {
		if (la.kind == 201) {
			Get();
			rule = Sample.FoxScript.AllocationRule.LowAcctLowPrice; 
		} else if (la.kind == 202) {
			Get();
			rule = Sample.FoxScript.AllocationRule.LowAcctHighPrice; 
		} else if (la.kind == 203) {
			Get();
			rule = Sample.FoxScript.AllocationRule.HighAcctLowPrice; 
		} else if (la.kind == 204) {
			Get();
			rule = Sample.FoxScript.AllocationRule.HighAcctHighPrice; 
		} else if (la.kind == 205) {
			Get();
			rule = Sample.FoxScript.AllocationRule.APS; 
		} else SynErr(270);
	}

	void AllocationItem(out PreAllocationBlockItem item) {
		item = new PreAllocationBlockItem(); 
		IdentOrString(ref item.Account);
		Double(ref item.Weight);
	}

	void PostAllocationRule(ref AllocationRule rule) {
		if (la.kind == 42) {
			Get();
			rule = Sample.FoxScript.AllocationRule.PostAllocation; 
		} else if (la.kind == 205) {
			Get();
			rule = Sample.FoxScript.AllocationRule.PostAllocationAPS; 
		} else SynErr(271);
	}

	void PostAllocationItem(out PostAllocationBlockItem item) {
		item = new PostAllocationBlockItem(); 
		AccountInfo(ref item.Account);
		Double(ref item.Price);
		Double(ref item.Weight);
	}

	void Double(ref double value) {
		if (la.kind == 64) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else if (la.kind == 65) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else SynErr(272);
	}

	void AccountInfo(ref ExtendedAccount account) {
		IdentOrString(ref account.Spec);
		if (la.kind == 199 || la.kind == 200) {
			if (la.kind == 199) {
				Get();
				IdentOrString(ref account.Firm);
			} else {
				Get();
				IdentOrString(ref account.ClearingHouse);
			}
		}
	}

	void Timeout() {
		Expect(72);
	}

	void MsgCtxLogicalExpr(ref object expr) {
		MsgCtxOrExpr(ref expr);
	}

	void OrExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		AndExpr(ref left);
		expr = left; 
		if (la.kind == 62) {
			Get();
			op = LogicalOp.Or; 
			AndExpr(ref right);
			expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = right }; 
		}
	}

	void AndExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		EqlExpr(ref left);
		expr = left; 
		if (la.kind == 63) {
			Get();
			op = LogicalOp.And; 
			EqlExpr(ref right);
			expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = right }; 
		}
	}

	void EqlExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; string msgVarName = null; 
		if (StartOf(11)) {
			RelExpr(ref left);
			expr = left; 
		} else if (la.kind == 71) {
			Get();
			expr = new Object(ObjectType.Null, null); 
		} else if (la.kind == 1) {
			MsgVarName(ref msgVarName);
			expr = new Object(ObjectType.FixMsgVar, msgVarName); 
		} else SynErr(273);
		if (la.kind == 56 || la.kind == 57) {
			msgVarName = null; 
			if (la.kind == 56) {
				Get();
				op = LogicalOp.Equal; 
			} else {
				Get();
				op = LogicalOp.NotEqual; 
			}
			if (StartOf(11)) {
				RelExpr(ref right);
				expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = right }; 
			} else if (la.kind == 71) {
				Get();
				expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = new Object(ObjectType.Null, null) }; 
			} else if (la.kind == 1) {
				MsgVarName(ref msgVarName);
				expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = new Object(ObjectType.FixMsgVar, msgVarName) }; 
			} else SynErr(274);
		}
	}

	void RelExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		RelExprArg(ref left);
		expr = left; 
		if (StartOf(12)) {
			if (la.kind == 58) {
				Get();
				op = LogicalOp.Less; 
			} else if (la.kind == 60) {
				Get();
				op = LogicalOp.Greater; 
			} else if (la.kind == 59) {
				Get();
				op = LogicalOp.LessOrEqual; 
			} else {
				Get();
				op = LogicalOp.GreaterOrEqual; 
			}
			RelExprArg(ref right);
			expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = right }; 
		}
	}

	void RelExprArg(ref object arg) {
		Object rvalue = null; 
		if (StartOf(13)) {
			RValue(ref rvalue);
			arg = rvalue; 
		} else if (la.kind == 83) {
			Get();
			LogicalExpr(ref arg);
			Expect(84);
		} else SynErr(275);
	}

	void MsgCtxOrExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		MsgCtxAndExpr(ref left);
		expr = left; 
		if (la.kind == 62) {
			Get();
			op = LogicalOp.Or; 
			MsgCtxAndExpr(ref right);
			expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = right }; 
		}
	}

	void MsgCtxAndExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		MsgCtxEqlExpr(ref left);
		expr = left; 
		if (la.kind == 63) {
			Get();
			op = LogicalOp.And; 
			MsgCtxEqlExpr(ref right);
			expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = right }; 
		}
	}

	void MsgCtxEqlExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		MsgCtxRelExpr(ref left);
		expr = left; 
		if (la.kind == 56 || la.kind == 57) {
			if (la.kind == 56) {
				Get();
				op = LogicalOp.Equal; 
			} else {
				Get();
				op = LogicalOp.NotEqual; 
			}
			MsgCtxRelExpr(ref right);
			expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = right }; 
		}
	}

	void MsgCtxRelExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		MsgCtxRelExprArg(ref left);
		expr = left; 
		if (StartOf(12)) {
			if (la.kind == 58) {
				Get();
				op = LogicalOp.Less; 
			} else if (la.kind == 60) {
				Get();
				op = LogicalOp.Greater; 
			} else if (la.kind == 59) {
				Get();
				op = LogicalOp.LessOrEqual; 
			} else {
				Get();
				op = LogicalOp.GreaterOrEqual; 
			}
			MsgCtxRelExprArg(ref right);
			expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = right }; 
		}
	}

	void MsgCtxRelExprArg(ref object arg) {
		string fieldName = null; Object rvalue = null; 
		if (la.kind == 1) {
			FieldName(ref fieldName);
			arg = new Object(ObjectType.FixField, fieldName); 
		} else if (StartOf(13)) {
			RValue(ref rvalue);
			arg = rvalue; 
		} else if (la.kind == 83) {
			Get();
			MsgCtxLogicalExpr(ref arg);
			Expect(84);
		} else SynErr(276);
	}

	void FieldName(ref string fieldName) {
		Expect(1);
		fieldName = t.val; 
	}

	void Price(ref double value) {
		if (la.kind == 64) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else if (la.kind == 65) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else SynErr(277);
	}

	void TrailingStop(ref TrailingStop trailing) {
		trailing = new TrailingStop(); 
		Expect(225);
		if (la.kind == 226 || la.kind == 227 || la.kind == 228) {
			if (la.kind == 226) {
				Get();
				trailing.TriggerType = TokenParser.ParseTrailingTriggerType(t.val); 
			} else if (la.kind == 227) {
				Get();
				trailing.TriggerType = TokenParser.ParseTrailingTriggerType(t.val); 
			} else {
				Get();
				trailing.TriggerType = TokenParser.ParseTrailingTriggerType(t.val); 
			}
			if (la.kind == 64) {
				Get();
				trailing.Amount = LiteralParser.ParseInteger(t.val); 
			} else if (la.kind == 65) {
				Get();
				trailing.Amount = LiteralParser.ParseFloat(t.val); 
			} else SynErr(278);
			if (la.kind == 229) {
				Get();
				trailing.AmountInPercents = true; 
			}
		} else if (la.kind == 64 || la.kind == 65) {
			if (la.kind == 64) {
				Get();
				trailing.Amount = LiteralParser.ParseInteger(t.val); 
			} else {
				Get();
				trailing.Amount = LiteralParser.ParseFloat(t.val); 
			}
		} else SynErr(279);
	}

	void Symbol(ref OrderSymbol symbol) {
		if (la.kind == 1) {
			Get();
			symbol = TokenParser.ParseOrderSymbol(t.val); 
		} else if (la.kind == 66) {
			Get();
			symbol = TokenParser.ParseOrderSymbol(LiteralParser.ParseString(t.val)); 
		} else SynErr(280);
	}

	void StrikeSide(OrderContract contract) {
		double strike = 0; 
		if (la.kind == 79) {
			Get();
			contract.Put = false; 
		} else if (la.kind == 78) {
			Get();
			contract.Put = true; 
		} else SynErr(281);
		Price(ref strike);
		contract.Strike = strike; 
	}

	void FASTUpdateType(MDMessageCommand command) {
		if (la.kind == 230) {
			Get();
			command.UpdateType = 0; 
		}
	}

	void FASTContract(MDMessageCommand command) {
		if (la.kind == 183 || la.kind == 188) {
			FASTSymbolBasedContract(command);
		} else if (la.kind == 1 || la.kind == 66) {
			FASTFuturesBasedContract(command);
		} else SynErr(282);
	}

	void FASTSymbolBasedContract(MDMessageCommand command) {
		if (la.kind == 183) {
			Get();
			command.ContractKind = Sample.FoxScript.ContractKind.FUTURE_COMPOUND; 
		} else if (la.kind == 188) {
			Get();
			command.ContractKind = Sample.FoxScript.ContractKind.FOREX; 
		} else SynErr(283);
		IdentOrString(ref command.BaseSymbol);
		if (la.kind == 78 || la.kind == 79) {
			FASTStrikeSide(command);
			command.ContractKind = Sample.FoxScript.ContractKind.OPTIONS_COMPOUND; 
		}
	}

	void FASTFuturesBasedContract(MDMessageCommand command) {
		command.ContractKind = Sample.FoxScript.ContractKind.FUTURE; 
		IdentOrString(ref command.BaseSymbol);
		Expect(64);
		command.ExpirationMonth = LiteralParser.ParseInteger(t.val);   
		if (la.kind == 78 || la.kind == 79) {
			FASTStrikeSide(command);
			command.ContractKind = Sample.FoxScript.ContractKind.OPTION; 
		}
	}

	void FASTStrikeSide(MDMessageCommand command) {
		command.StrikeSide = new FASTStrikeSide(); double strike = 0; 
		if (la.kind == 79) {
			Get();
			command.StrikeSide.Put = false; 
		} else if (la.kind == 78) {
			Get();
			command.StrikeSide.Put = true; 
		} else SynErr(284);
		Price(ref strike);
		command.StrikeSide.Strike = strike; 
	}

	void SubscribeMarketDataToFile(ref MDMessageCommand command) {
		if (la.kind == 60) {
			Get();
			Expect(66);
			command.OutputFileName = LiteralParser.ParseString(t.val); 
		}
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		FoxScript();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,T,x,x, x,x,x,T, T,T,T,T, x,T,T,T, T,T,T,T, T,x,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,T,x,x, x,x,x,T, T,T,T,T, x,T,T,T, T,T,T,T, T,x,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,x,x,x, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x,x,x,x, x,x,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,T,x,x, x,x,x,T, T,T,T,T, x,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, T,T,x,x, x,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, T,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, T,T,T,x, T,T,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, T,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "messageField expected"; break;
			case 3: s = "fixConst expected"; break;
			case 4: s = "globalProp expected"; break;
			case 5: s = "endOfClause expected"; break;
			case 6: s = "comma expected"; break;
			case 7: s = "actionNew expected"; break;
			case 8: s = "actionModify expected"; break;
			case 9: s = "actionCancel expected"; break;
			case 10: s = "actionOrderStatus expected"; break;
			case 11: s = "actionOrderMassStatus expected"; break;
			case 12: s = "actionDelete expected"; break;
			case 13: s = "actionWait expected"; break;
			case 14: s = "actionEnsure expected"; break;
			case 15: s = "actionPrint expected"; break;
			case 16: s = "actionPrintf expected"; break;
			case 17: s = "actionReset expected"; break;
			case 18: s = "actionSet expected"; break;
			case 19: s = "actionGet expected"; break;
			case 20: s = "actionPing expected"; break;
			case 21: s = "actionBegin expected"; break;
			case 22: s = "actionEnd expected"; break;
			case 23: s = "actionPositions expected"; break;
			case 24: s = "actionBalance expected"; break;
			case 25: s = "actionQuit expected"; break;
			case 26: s = "actionExit expected"; break;
			case 27: s = "actionStop expected"; break;
			case 28: s = "actionConnect expected"; break;
			case 29: s = "actionAuth expected"; break;
			case 30: s = "actionDisconnect expected"; break;
			case 31: s = "actionExec expected"; break;
			case 32: s = "actionTest expected"; break;
			case 33: s = "actionTestStat expected"; break;
			case 34: s = "actionEnsureOrderStatus expected"; break;
			case 35: s = "actionEnsurePureOrderStatus expected"; break;
			case 36: s = "actionEnsureModifyAccepted expected"; break;
			case 37: s = "actionEnsureTrade expected"; break;
			case 38: s = "actionSleep expected"; break;
			case 39: s = "actionAnyKey expected"; break;
			case 40: s = "actionContract expected"; break;
			case 41: s = "actionPostAllocation expected"; break;
			case 42: s = "actionPost expected"; break;
			case 43: s = "actionPAlloc expected"; break;
			case 44: s = "actionUserRequest expected"; break;
			case 45: s = "actionMarginCalc expected"; break;
			case 46: s = "actionHeartbeat expected"; break;
			case 47: s = "actionConnectFast expected"; break;
			case 48: s = "actionDisconnectFast expected"; break;
			case 49: s = "actionCancelSubscribe expected"; break;
			case 50: s = "actionSubscribeQuotes expected"; break;
			case 51: s = "actionSubscribeDOM expected"; break;
			case 52: s = "actionSubscribeHistogram expected"; break;
			case 53: s = "actionSubscribeTicks expected"; break;
			case 54: s = "actionLoadTicks expected"; break;
			case 55: s = "assignOp expected"; break;
			case 56: s = "equalOp expected"; break;
			case 57: s = "notEqualOp expected"; break;
			case 58: s = "lessOp expected"; break;
			case 59: s = "lessOrEqualOp expected"; break;
			case 60: s = "greaterOp expected"; break;
			case 61: s = "greaterOrEqualOp expected"; break;
			case 62: s = "orOp expected"; break;
			case 63: s = "andOp expected"; break;
			case 64: s = "integer expected"; break;
			case 65: s = "float expected"; break;
			case 66: s = "string expected"; break;
			case 67: s = "true expected"; break;
			case 68: s = "false expected"; break;
			case 69: s = "on expected"; break;
			case 70: s = "off expected"; break;
			case 71: s = "null expected"; break;
			case 72: s = "timespan expected"; break;
			case 73: s = "timestamp expected"; break;
			case 74: s = "date expected"; break;
			case 75: s = "uuid expected"; break;
			case 76: s = "buy expected"; break;
			case 77: s = "sell expected"; break;
			case 78: s = "put expected"; break;
			case 79: s = "call expected"; break;
			case 80: s = "open expected"; break;
			case 81: s = "close expected"; break;
			case 82: s = "\"if\" expected"; break;
			case 83: s = "\"(\" expected"; break;
			case 84: s = "\")\" expected"; break;
			case 85: s = "\"{\" expected"; break;
			case 86: s = "\"}\" expected"; break;
			case 87: s = "\"else\" expected"; break;
			case 88: s = "\"msg\" expected"; break;
			case 89: s = "\"uuid\" expected"; break;
			case 90: s = "\"request\" expected"; break;
			case 91: s = "\"req\" expected"; break;
			case 92: s = "\"r\" expected"; break;
			case 93: s = "\"base\" expected"; break;
			case 94: s = "\"b\" expected"; break;
			case 95: s = "\"lookup\" expected"; break;
			case 96: s = "\"lkp\" expected"; break;
			case 97: s = "\"l\" expected"; break;
			case 98: s = "\"mode\" expected"; break;
			case 99: s = "\"max_records\" expected"; break;
			case 100: s = "\"snapshot\" expected"; break;
			case 101: s = "\"subscribe\" expected"; break;
			case 102: s = "\"unsubscribe\" expected"; break;
			case 103: s = "\"updates_only\" expected"; break;
			case 104: s = "\"kind\" expected"; break;
			case 105: s = "\"type\" expected"; break;
			case 106: s = "\"opt_type\" expected"; break;
			case 107: s = "\"by_base_contract\" expected"; break;
			case 108: s = "\"opt_required\" expected"; break;
			case 109: s = "\"base_contract\" expected"; break;
			case 110: s = "\"underlying\" expected"; break;
			case 111: s = "\"exch\" expected"; break;
			case 112: s = "\"cgroup\" expected"; break;
			case 113: s = "\"compound_type\" expected"; break;
			case 114: s = "\"unknown\" expected"; break;
			case 115: s = "\"generic\" expected"; break;
			case 116: s = "\"performance_index_basket\" expected"; break;
			case 117: s = "\"non_performance_index_basket\" expected"; break;
			case 118: s = "\"straddle\" expected"; break;
			case 119: s = "\"strangle\" expected"; break;
			case 120: s = "\"future_time_spread\" expected"; break;
			case 121: s = "\"option_time_spread\" expected"; break;
			case 122: s = "\"price_spread\" expected"; break;
			case 123: s = "\"synthetic_underlying\" expected"; break;
			case 124: s = "\"straddle_time_spread\" expected"; break;
			case 125: s = "\"ratio_spread\" expected"; break;
			case 126: s = "\"ratio_future_time_spread\" expected"; break;
			case 127: s = "\"ratio_option_time_spread\" expected"; break;
			case 128: s = "\"put_call_spread\" expected"; break;
			case 129: s = "\"ratio_put_call_spread\" expected"; break;
			case 130: s = "\"ladder\" expected"; break;
			case 131: s = "\"box\" expected"; break;
			case 132: s = "\"butterfly\" expected"; break;
			case 133: s = "\"condor\" expected"; break;
			case 134: s = "\"iron_butterfly\" expected"; break;
			case 135: s = "\"diagonal_spread\" expected"; break;
			case 136: s = "\"ratio_diagonal_spread\" expected"; break;
			case 137: s = "\"straddle_diagonal_spread\" expected"; break;
			case 138: s = "\"conversion_reversal\" expected"; break;
			case 139: s = "\"covered_option\" expected"; break;
			case 140: s = "\"reserved1\" expected"; break;
			case 141: s = "\"reserved2\" expected"; break;
			case 142: s = "\"currency_future_spread\" expected"; break;
			case 143: s = "\"rate_future_spread\" expected"; break;
			case 144: s = "\"index_future_spread\" expected"; break;
			case 145: s = "\"future_butterfly\" expected"; break;
			case 146: s = "\"future_condor\" expected"; break;
			case 147: s = "\"strip\" expected"; break;
			case 148: s = "\"pack\" expected"; break;
			case 149: s = "\"bundle\" expected"; break;
			case 150: s = "\"bond_deliverable_basket\" expected"; break;
			case 151: s = "\"stock_basket\" expected"; break;
			case 152: s = "\"price_spread_vs_option\" expected"; break;
			case 153: s = "\"straddle_vs_option\" expected"; break;
			case 154: s = "\"bond_spread\" expected"; break;
			case 155: s = "\"exchange_spread\" expected"; break;
			case 156: s = "\"future_pack_spread\" expected"; break;
			case 157: s = "\"future_pack_butterfly\" expected"; break;
			case 158: s = "\"whole_sale\" expected"; break;
			case 159: s = "\"commodity_spread\" expected"; break;
			case 160: s = "\"jelly_roll\" expected"; break;
			case 161: s = "\"iron_condor\" expected"; break;
			case 162: s = "\"options_strip\" expected"; break;
			case 163: s = "\"contingent_orders\" expected"; break;
			case 164: s = "\"interproduct_spread\" expected"; break;
			case 165: s = "\"pseudo_straddle\" expected"; break;
			case 166: s = "\"tailor_made\" expected"; break;
			case 167: s = "\"futures_generic\" expected"; break;
			case 168: s = "\"options_generic\" expected"; break;
			case 169: s = "\"basis_trade\" expected"; break;
			case 170: s = "\"futuretime_spread_reduced_tick_size\" expected"; break;
			case 171: s = "\"generic_vola_strategy_vs\" expected"; break;
			case 172: s = "\"straddle_vola_strategy_vs\" expected"; break;
			case 173: s = "\"strangle_vs\" expected"; break;
			case 174: s = "\"option_time_spread_vs\" expected"; break;
			case 175: s = "\"price_spread_vs\" expected"; break;
			case 176: s = "\"ratio_spread_vs\" expected"; break;
			case 177: s = "\"put_call_spreadvs\" expected"; break;
			case 178: s = "\"ladder_vs\" expected"; break;
			case 179: s = "\"price_spread_vs_option_vs\" expected"; break;
			case 180: s = "\"collar\" expected"; break;
			case 181: s = "\"combo\" expected"; break;
			case 182: s = "\"protective_put\" expected"; break;
			case 183: s = "\"spread\" expected"; break;
			case 184: s = "\"electronic\" expected"; break;
			case 185: s = "\"pit\" expected"; break;
			case 186: s = "\"future\" expected"; break;
			case 187: s = "\"option\" expected"; break;
			case 188: s = "\"forex\" expected"; break;
			case 189: s = "\"future_compound\" expected"; break;
			case 190: s = "\"options_compound\" expected"; break;
			case 191: s = "\"any_inclusion\" expected"; break;
			case 192: s = "\"symbol_starts_with\" expected"; break;
			case 193: s = "\"description_starts_with\" expected"; break;
			case 194: s = "\"any_starts_with\" expected"; break;
			case 195: s = "\"exact_match\" expected"; break;
			case 196: s = "\"seqnum\" expected"; break;
			case 197: s = "\"for\" expected"; break;
			case 198: s = "\"*\" expected"; break;
			case 199: s = "\":\" expected"; break;
			case 200: s = "\"::\" expected"; break;
			case 201: s = "\"low_acct_low_price\" expected"; break;
			case 202: s = "\"low_acct_high_price\" expected"; break;
			case 203: s = "\"high_acct_low_price\" expected"; break;
			case 204: s = "\"high_acct_high_price\" expected"; break;
			case 205: s = "\"aps\" expected"; break;
			case 206: s = "\"ts\" expected"; break;
			case 207: s = "\"pre\" expected"; break;
			case 208: s = "\"main\" expected"; break;
			case 209: s = "\"after\" expected"; break;
			case 210: s = "\"p1\" expected"; break;
			case 211: s = "\"p2\" expected"; break;
			case 212: s = "\"p3\" expected"; break;
			case 213: s = "\"day\" expected"; break;
			case 214: s = "\"gtc\" expected"; break;
			case 215: s = "\"gtd\" expected"; break;
			case 216: s = "\"fok\" expected"; break;
			case 217: s = "\"ioc\" expected"; break;
			case 218: s = "\"mkt\" expected"; break;
			case 219: s = "\"moo\" expected"; break;
			case 220: s = "\"moc\" expected"; break;
			case 221: s = "\"lmt\" expected"; break;
			case 222: s = "\"stp\" expected"; break;
			case 223: s = "\"ice\" expected"; break;
			case 224: s = "\"mit\" expected"; break;
			case 225: s = "\"trailing\" expected"; break;
			case 226: s = "\"last\" expected"; break;
			case 227: s = "\"bid\" expected"; break;
			case 228: s = "\"ask\" expected"; break;
			case 229: s = "\"%\" expected"; break;
			case 230: s = "\"full\" expected"; break;
			case 231: s = "\"from\" expected"; break;
			case 232: s = "\"to\" expected"; break;
			case 233: s = "??? expected"; break;
			case 234: s = "invalid Command"; break;
			case 235: s = "invalid SimpleCommand"; break;
			case 236: s = "invalid MsgProducingCommand"; break;
			case 237: s = "invalid SetPropCommand"; break;
			case 238: s = "invalid QuitCommand"; break;
			case 239: s = "invalid ContractCommand"; break;
			case 240: s = "invalid PostAllocationCommand"; break;
			case 241: s = "invalid LoadTicksCommand"; break;
			case 242: s = "invalid MessageFieldAssignments"; break;
			case 243: s = "invalid MessageFieldAssignments"; break;
			case 244: s = "invalid RValue"; break;
			case 245: s = "invalid IdentOrString"; break;
			case 246: s = "invalid Account"; break;
			case 247: s = "invalid ContractRequest"; break;
			case 248: s = "invalid ContractRequest"; break;
			case 249: s = "invalid ContractLookup"; break;
			case 250: s = "invalid BaseContractRequest"; break;
			case 251: s = "invalid SubscriptionType"; break;
			case 252: s = "invalid BaseContractParam"; break;
			case 253: s = "invalid ContractLookupMode"; break;
			case 254: s = "invalid ContractLookupParam"; break;
			case 255: s = "invalid ContractKindList"; break;
			case 256: s = "invalid ContractType"; break;
			case 257: s = "invalid OptionType"; break;
			case 258: s = "invalid BoolOptional"; break;
			case 259: s = "invalid CompoundType"; break;
			case 260: s = "invalid DoubleOptional"; break;
			case 261: s = "invalid ContractKind"; break;
			case 262: s = "invalid OrderSide"; break;
			case 263: s = "invalid FormatArg"; break;
			case 264: s = "invalid IntegerOrDefault"; break;
			case 265: s = "invalid Literal"; break;
			case 266: s = "invalid OrderType"; break;
			case 267: s = "invalid TimeInForce"; break;
			case 268: s = "invalid TimeInForce"; break;
			case 269: s = "invalid TradingSession"; break;
			case 270: s = "invalid AllocationRule"; break;
			case 271: s = "invalid PostAllocationRule"; break;
			case 272: s = "invalid Double"; break;
			case 273: s = "invalid EqlExpr"; break;
			case 274: s = "invalid EqlExpr"; break;
			case 275: s = "invalid RelExprArg"; break;
			case 276: s = "invalid MsgCtxRelExprArg"; break;
			case 277: s = "invalid Price"; break;
			case 278: s = "invalid TrailingStop"; break;
			case 279: s = "invalid TrailingStop"; break;
			case 280: s = "invalid Symbol"; break;
			case 281: s = "invalid StrikeSide"; break;
			case 282: s = "invalid FASTContract"; break;
			case 283: s = "invalid FASTSymbolBasedContract"; break;
			case 284: s = "invalid FASTStrikeSide"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public virtual void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public virtual void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public virtual void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public virtual void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}
}