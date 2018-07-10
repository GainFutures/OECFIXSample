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
	public const int _actionBracket = 46;
	public const int _actionHeartbeat = 47;
	public const int _actionConnectFast = 48;
	public const int _actionDisconnectFast = 49;
	public const int _actionCancelSubscribe = 50;
	public const int _actionSubscribeQuotes = 51;
	public const int _actionSubscribeDOM = 52;
	public const int _actionSubscribeHistogram = 53;
	public const int _actionSubscribeTicks = 54;
	public const int _actionLoadTicks = 55;
	public const int _assignOp = 56;
	public const int _equalOp = 57;
	public const int _notEqualOp = 58;
	public const int _lessOp = 59;
	public const int _lessOrEqualOp = 60;
	public const int _greaterOp = 61;
	public const int _greaterOrEqualOp = 62;
	public const int _orOp = 63;
	public const int _andOp = 64;
	public const int _integer = 65;
	public const int _float = 66;
	public const int _string = 67;
	public const int _true = 68;
	public const int _false = 69;
	public const int _on = 70;
	public const int _off = 71;
	public const int _null = 72;
	public const int _timespan = 73;
	public const int _timestamp = 74;
	public const int _date = 75;
	public const int _uuid = 76;
	public const int _buy = 77;
	public const int _sell = 78;
	public const int _put = 79;
	public const int _call = 80;
	public const int _open = 81;
	public const int _close = 82;
	public const int maxT = 250;

	const bool _T = true;
	const bool _x = false;
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
		} else SynErr(251);
	}

	void Statement() {
		IfStatement();
	}

	void IfStatement() {
		object expr = null; bool actionEnabled = SemanticActionEnabled(); 
		Expect(83);
		Expect(84);
		LogicalExpr(ref expr);
		Expect(85);
		if (actionEnabled)
		{
		var predicate = ExecEngine.BuildPredicate(expr); 
		PushBranchingPredicate(predicate);
		CurrentBranchingPredicate.Positive = true;
		}
		
		Expect(86);
		while (StartOf(1)) {
			if (StartOf(2)) {
				Command();
			} else {
				Statement();
			}
		}
		Expect(87);
		if (la.kind == 88) {
			Get();
			if (actionEnabled) {
			CurrentBranchingPredicate.Positive = false; 
			}
			
			Expect(86);
			while (StartOf(1)) {
				if (StartOf(2)) {
					Command();
				} else {
					Statement();
				}
			}
			Expect(87);
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
		case 48: {
			ConnectFastCommand();
			break;
		}
		case 49: {
			DisconnectFastCommand();
			break;
		}
		case 47: {
			HeartbeatCommand();
			break;
		}
		default: SynErr(252); break;
		}
		Expect(5);
	}

	void MsgProducingCommand() {
		string msgVarName = null; MsgCommand command = null; 
		if (la.kind == 1 || la.kind == 89) {
			if (la.kind == 89) {
				Get();
			}
			MsgVarName(ref msgVarName);
			Expect(56);
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
		case 46: {
			BracketOrderCommand(ref command);
			break;
		}
		case 50: {
			CancelSubscribeCommand(ref command);
			break;
		}
		case 51: {
			SubscribeQuotesCommand(ref command);
			break;
		}
		case 52: {
			SubscribeDOMCommand(ref command);
			break;
		}
		case 53: {
			SubscribeHistogramCommand(ref command);
			break;
		}
		case 54: {
			SubscribeTicksCommand(ref command);
			break;
		}
		case 55: {
			LoadTicksCommand(ref command);
			break;
		}
		default: SynErr(253); break;
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
		if (la.kind == 197) {
			SetSeqNumPropCommand();
		} else if (la.kind == 1) {
			SetCommonPropCommand();
		} else SynErr(254);
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
		} else SynErr(255);
		SemanticAction(() => ExecEngine.Exit()); 
	}

	void ExecCommand() {
		string filename = null; string scriptName = null; 
		Expect(31);
		IdentOrString(ref filename);
		if (la.kind == 6) {
			Get();
			Expect(67);
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
		if (la.kind == 73) {
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
		if (la.kind == 1 || la.kind == 67) {
			IdentOrString(ref password);
		}
		if (la.kind == 76) {
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
		Expect(197);
	}

	void AuthorizationCommand() {
		string senderCompID = string.Empty; 
		Expect(29);
		IdentOrString(ref senderCompID);
		SemanticAction(() => ExecEngine.Auth(senderCompID)); 
	}

	void ConnectFastCommand() {
		string username = null; 
		Expect(48);
		if (la.kind == 1 || la.kind == 67) {
			IdentOrString(ref username);
		}
		SemanticAction(() => ExecEngine.ConnectFast(username)); 
	}

	void DisconnectFastCommand() {
		Expect(49);
		SemanticAction(() => ExecEngine.DisconnectFast()); 
	}

	void HeartbeatCommand() {
		Expect(47);
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
		if (la.kind == 73) {
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
		if (la.kind == 77 || la.kind == 78) {
			OrderSide(ref cmd.OrderSide);
		}
		if (la.kind == 1 || la.kind == 67) {
			OrderContract(ref cmd.OrderContract);
		}
		if (la.kind == 1 || la.kind == 67 || la.kind == 200) {
			if (la.kind == 1 || la.kind == 67) {
				AllocationBlock(out cmd.AllocationBlock);
			}
			Expect(200);
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
		if (la.kind == 91 || la.kind == 92 || la.kind == 93) {
			ContractRequest(ref command);
		} else if (la.kind == 96 || la.kind == 97 || la.kind == 98) {
			ContractLookup(ref command);
		} else SynErr(256);
	}

	void PostAllocationCommand(ref MsgCommand command) {
		var cmd = new PostAllocationCommand(); 
		if (la.kind == 41) {
			Get();
		} else if (la.kind == 42) {
			Get();
		} else if (la.kind == 43) {
			Get();
		} else SynErr(257);
		OrigMsgVarName(ref cmd.OrigMsgVarName);
		if (la.kind == 1 || la.kind == 67) {
			OrderContract(ref cmd.Contract);
		}
		PostAllocationBlock(out cmd.AllocationBlock);
		command = cmd; 
	}

	void UserRequestCommand(ref MsgCommand command) {
		var userCommand = new UserRequestCommand(); 
		Expect(44);
		if (la.kind == 90) {
			Get();
			Expect(56);
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

	void BracketOrderCommand(ref MsgCommand command) {
		var cmd = new BracketOrderCommand(); cmd.BracketCommands = new List<BracketCommandItem>(); 
		Expect(46);
		BracketType(ref cmd.Type);
		if (la.kind == 235 || la.kind == 236 || la.kind == 237) {
			OSOGroupingMethod(ref cmd.OSOGroupingMethod);
		}
		Expect(86);
		var item = new BracketCommandItem(); 
		if (la.kind == 198) {
			Get();
			OrigMsgVarName(ref item.MsgVarName);
			Expect(199);
		}
		OrderBody(item);
		cmd.BracketCommands.Add(item); 
		while (la.kind == 6) {
			Get();
			item = new BracketCommandItem(); 
			if (la.kind == 198) {
				Get();
				OrigMsgVarName(ref item.MsgVarName);
				Expect(199);
			}
			OrderBody(item);
			cmd.BracketCommands.Add(item); 
		}
		Expect(87);
		command = cmd; 
	}

	void CancelSubscribeCommand(ref MsgCommand command) {
		var cmd = new CancelSubscribeCommand(); 
		Expect(50);
		OrigMsgVarName(ref cmd.MDMessageVar);
		command = cmd; 
	}

	void SubscribeQuotesCommand(ref MsgCommand command) {
		var cmd = new SubscribeQuotesCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(51);
		FASTUpdateType(mdCmd);
		if (la.kind == 239) {
			FASTMDEntries(mdCmd);
		}
		FASTContract(mdCmd);
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void SubscribeDOMCommand(ref MsgCommand command) {
		var cmd = new SubscribeDOMCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(52);
		FASTUpdateType(mdCmd);
		FASTContract(mdCmd);
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void SubscribeHistogramCommand(ref MsgCommand command) {
		var cmd = new SubscribeHistogramCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(53);
		FASTUpdateType(mdCmd);
		FASTContract(mdCmd);
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void SubscribeTicksCommand(ref MsgCommand command) {
		var cmd = new SubscribeTicksCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(54);
		FASTUpdateType(mdCmd);
		FASTContract(mdCmd);
		if (la.kind == 229 || la.kind == 248) {
			if (la.kind == 248) {
				Get();
				Expect(74);
				cmd.SetStartTime(LiteralParser.ParseTimestamp(t.val)); 
			} else {
				Get();
				Expect(73);
				cmd.SetStartTime(LiteralParser.ParseTimespan(t.val)); 
			}
		}
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void LoadTicksCommand(ref MsgCommand command) {
		var cmd = new LoadTicksCommand(); var mdCmd = cmd as MDMessageCommand; 
		Expect(55);
		FASTUpdateType(mdCmd);
		FASTContract(mdCmd);
		if (la.kind == 248) {
			Get();
			Expect(74);
			cmd.SetStartTime(LiteralParser.ParseTimestamp(t.val)); 
			Expect(249);
			Expect(74);
			cmd.SetEndTime(LiteralParser.ParseTimestamp(t.val)); 
		} else if (la.kind == 229) {
			Get();
			Expect(73);
			cmd.SetStartTime(LiteralParser.ParseTimespan(t.val)); 
		} else SynErr(258);
		SubscribeMarketDataToFile(ref mdCmd);
		command = cmd; 
	}

	void MessageFieldAssignments(FixFields fields) {
		string name = null; Object value = null; 
		if (la.kind == 1) {
			Get();
			name = t.val; 
		} else if (la.kind == 65) {
			Get();
			name = t.val; 
		} else SynErr(259);
		Expect(56);
		RValue(ref value);
		fields.Add(name, value); 
		while (la.kind == 6) {
			name = null; value = null; 
			Get();
			if (la.kind == 1) {
				Get();
				name = t.val; 
			} else if (la.kind == 65) {
				Get();
				name = t.val; 
			} else SynErr(260);
			Expect(56);
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
		} else SynErr(261);
	}

	void IdentOrString(ref string str) {
		if (la.kind == 1) {
			Get();
			str = t.val; 
		} else if (la.kind == 67) {
			Get();
			str = LiteralParser.ParseString(t.val); 
		} else SynErr(262);
	}

	void MarginCalcPosition(out MarginCalcCommand.Position position) {
		position = new MarginCalcCommand.Position(); 
		OrderContract(ref position.Contract);
		Expect(65);
		position.MinQty = int.Parse(t.val); 
		Expect(65);
		position.MaxQty = int.Parse(t.val); 
	}

	void OrderContract(ref OrderContract contract) {
		contract = new OrderContract(); 
		Symbol(ref contract.Symbol);
		if (la.kind == 79 || la.kind == 80) {
			StrikeSide(contract);
		}
	}

	void MarginCalcPositions(ref List<MarginCalcCommand.Position> positions) {
		positions = new List<MarginCalcCommand.Position>();
		MarginCalcCommand.Position item;
		
		Expect(86);
		MarginCalcPosition(out item);
		positions.Add(item); 
		while (la.kind == 6) {
			Get();
			MarginCalcPosition(out item);
			positions.Add(item); 
		}
		Expect(87);
	}

	void Account(ref string account) {
		if (la.kind == 1) {
			Get();
			account = t.val; 
		} else if (la.kind == 67) {
			Get();
			account = LiteralParser.ParseString(t.val); 
		} else if (la.kind == 65) {
			Get();
			account = t.val; 
		} else if (la.kind == 4) {
			Get();
			var prop = new Object(ObjectType.GlobalProp, t.val); account = (string)ExecEngine.GetObjectValue(prop, null); 
		} else SynErr(263);
	}

	void ContractRequest(ref MsgCommand command) {
		if (la.kind == 91) {
			Get();
		} else if (la.kind == 92) {
			Get();
		} else if (la.kind == 93) {
			Get();
		} else SynErr(264);
		if (la.kind == 1 || la.kind == 67) {
			ContractRequestCommand temp; 
			ByBaseContractRequest(out temp);
			command = temp; 
		} else if (la.kind == 94 || la.kind == 95) {
			BaseContractRequestCommand temp; 
			BaseContractRequest(out temp);
			command = temp; 
		} else SynErr(265);
	}

	void ContractLookup(ref MsgCommand command) {
		var lcommand = new SymbolLookupCommand(); 
		if (la.kind == 96) {
			Get();
		} else if (la.kind == 97) {
			Get();
		} else if (la.kind == 98) {
			Get();
		} else SynErr(266);
		IdentOrString(ref lcommand.Name);
		Expect(6);
		Expect(99);
		Expect(56);
		ContractLookupMode(ref lcommand.Mode);
		Expect(6);
		Expect(100);
		Expect(56);
		Expect(65);
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
		if (la.kind == 74) {
			Get();
			command.UpdatesSinceTimestamp = LiteralParser.ParseTimestamp(t.val); 
		}
	}

	void BaseContractRequest(out BaseContractRequestCommand command) {
		command = new BaseContractRequestCommand(); 
		if (la.kind == 94) {
			Get();
		} else if (la.kind == 95) {
			Get();
		} else SynErr(267);
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
		if (la.kind == 101) {
			Get();
			symbolRequestType = '0'; 
		} else if (la.kind == 102) {
			Get();
			symbolRequestType = '1'; 
		} else if (la.kind == 103) {
			Get();
			symbolRequestType = '2'; 
		} else if (la.kind == 104) {
			Get();
			symbolRequestType = 'U'; 
		} else SynErr(268);
	}

	void BaseContractParam(ref BaseContractRequestCommand command) {
		if (la.kind == 112) {
			Get();
			Expect(56);
			IdentOrString(ref command.Exchange);
		} else if (la.kind == 113) {
			Get();
			Expect(56);
			IdentOrString(ref command.ContractGroup);
		} else if (la.kind == 114) {
			Get();
			Expect(56);
			CompoundType(ref command.CompoundType);
		} else SynErr(269);
	}

	void ContractLookupMode(ref int mode) {
		if (la.kind == 192) {
			Get();
			mode = 0; 
		} else if (la.kind == 193) {
			Get();
			mode = 1; 
		} else if (la.kind == 194) {
			Get();
			mode = 2; 
		} else if (la.kind == 195) {
			Get();
			mode = 3; 
		} else if (la.kind == 196) {
			Get();
			mode = 4; 
		} else SynErr(270);
	}

	void ContractLookupParam(ref SymbolLookupCommand command) {
		switch (la.kind) {
		case 105: {
			Get();
			Expect(56);
			ContractKindList(ref command.ContractKinds);
			break;
		}
		case 106: {
			Get();
			Expect(56);
			ContractType(ref command.ContractType);
			break;
		}
		case 107: {
			Get();
			Expect(56);
			OptionType(ref command.OptionType);
			break;
		}
		case 108: {
			Get();
			Expect(56);
			BoolOptional(ref command.ByBaseContractsOnly);
			break;
		}
		case 109: {
			Get();
			Expect(56);
			BoolOptional(ref command.OptionsRequired);
			break;
		}
		case 110: {
			Get();
			Expect(56);
			IdentOrString(ref command.BaseContract);
			break;
		}
		case 111: {
			Get();
			Expect(56);
			ContractDescription(ref command.ParentContract);
			break;
		}
		case 112: case 113: case 114: {
			BaseContractRequestCommand bcommand = command as BaseContractRequestCommand; 
			BaseContractParam(ref bcommand);
			break;
		}
		default: SynErr(271); break;
		}
	}

	void ContractKindList(ref List<ContractKind> list) {
		ContractKind kind; 
		if (StartOf(7)) {
			ContractKind(out kind);
			list.Add(kind); 
		} else if (la.kind == 84) {
			Get();
			ContractKind(out kind);
			list.Add(kind); 
			while (la.kind == 6) {
				Get();
				ContractKind(out kind);
				list.Add(kind); 
			}
			Expect(85);
		} else SynErr(272);
	}

	void ContractType(ref ContractType? ctype) {
		if (la.kind == 185) {
			Get();
			ctype = Sample.FoxScript.ContractType.ELECTRONIC; 
		} else if (la.kind == 186) {
			Get();
			ctype = Sample.FoxScript.ContractType.PIT; 
		} else SynErr(273);
	}

	void OptionType(ref OptionType optionType) {
		optionType = Sample.FoxScript.OptionType.ALL; 
		if (la.kind == 79) {
			Get();
			optionType = Sample.FoxScript.OptionType.PUT; 
		} else if (la.kind == 80) {
			Get();
			optionType = Sample.FoxScript.OptionType.CALL; 
		} else SynErr(274);
	}

	void BoolOptional(ref bool? value) {
		if (la.kind == 68) {
			Get();
			value = true; 
		} else if (la.kind == 69) {
			Get();
			value = false; 
		} else SynErr(275);
	}

	void ContractDescription(ref Contract contract) {
		var stringValue = string.Empty; 
		contract = new Contract();
		
		Expect(84);
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
		Expect(85);
	}

	void CompoundType(ref CompoundType compoundType) {
		switch (la.kind) {
		case 115: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.UNKNOWN; 
			break;
		}
		case 116: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.GENERIC; 
			break;
		}
		case 117: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PERFORMANCE_INDEX_BASKET; 
			break;
		}
		case 118: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.NON_PERFORMANCE_INDEX_BASKET; 
			break;
		}
		case 119: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE; 
			break;
		}
		case 120: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRANGLE; 
			break;
		}
		case 121: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_TIME_SPREAD; 
			break;
		}
		case 122: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.OPTION_TIME_SPREAD; 
			break;
		}
		case 123: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PRICE_SPREAD; 
			break;
		}
		case 124: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.SYNTHETIC_UNDERLYING; 
			break;
		}
		case 125: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE_TIME_SPREAD; 
			break;
		}
		case 126: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_SPREAD; 
			break;
		}
		case 127: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_FUTURE_TIME_SPREAD; 
			break;
		}
		case 128: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_OPTION_TIME_SPREAD; 
			break;
		}
		case 129: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PUT_CALL_SPREAD; 
			break;
		}
		case 130: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_PUT_CALL_SPREAD; 
			break;
		}
		case 131: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.LADDER; 
			break;
		}
		case 132: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BOX; 
			break;
		}
		case 133: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BUTTERFLY; 
			break;
		}
		case 134: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.CONDOR; 
			break;
		}
		case 135: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.IRON_BUTTERFLY; 
			break;
		}
		case 136: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.DIAGONAL_SPREAD; 
			break;
		}
		case 137: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_DIAGONAL_SPREAD; 
			break;
		}
		case 138: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE_DIAGONAL_SPREAD; 
			break;
		}
		case 139: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.CONVERSION_REVERSAL; 
			break;
		}
		case 140: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.COVERED_OPTION; 
			break;
		}
		case 141: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RESERVED1; 
			break;
		}
		case 142: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RESERVED2; 
			break;
		}
		case 143: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.CURRENCY_FUTURE_SPREAD; 
			break;
		}
		case 144: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATE_FUTURE_SPREAD; 
			break;
		}
		case 145: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.INDEX_FUTURE_SPREAD; 
			break;
		}
		case 146: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_BUTTERFLY; 
			break;
		}
		case 147: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_CONDOR; 
			break;
		}
		case 148: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRIP; 
			break;
		}
		case 149: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PACK; 
			break;
		}
		case 150: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BUNDLE; 
			break;
		}
		case 151: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BOND_DELIVERABLE_BASKET; 
			break;
		}
		case 152: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STOCK_BASKET; 
			break;
		}
		case 153: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PRICE_SPREAD_VS_OPTION; 
			break;
		}
		case 154: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE_VS_OPTION; 
			break;
		}
		case 155: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BOND_SPREAD; 
			break;
		}
		case 156: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.EXCHANGE_SPREAD; 
			break;
		}
		case 157: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_PACK_SPREAD; 
			break;
		}
		case 158: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURE_PACK_BUTTERFLY; 
			break;
		}
		case 159: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.WHOLE_SALE; 
			break;
		}
		case 160: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.COMMODITY_SPREAD; 
			break;
		}
		case 161: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.JELLY_ROLL; 
			break;
		}
		case 162: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.IRON_CONDOR; 
			break;
		}
		case 163: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.OPTIONS_STRIP; 
			break;
		}
		case 164: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.CONTINGENT_ORDERS; 
			break;
		}
		case 165: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.INTERPRODUCT_SPREAD; 
			break;
		}
		case 166: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PSEUDO_STRADDLE; 
			break;
		}
		case 167: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.TAILOR_MADE; 
			break;
		}
		case 168: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURES_GENERIC; 
			break;
		}
		case 169: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.OPTIONS_GENERIC; 
			break;
		}
		case 170: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.BASIS_TRADE; 
			break;
		}
		case 171: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.FUTURETIME_SPREAD_REDUCED_TICK_SIZE; 
			break;
		}
		case 172: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.GENERIC_VOLA_STRATEGY_VS; 
			break;
		}
		case 173: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRADDLE_VOLA_STRATEGY_VS; 
			break;
		}
		case 174: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.STRANGLE_VS; 
			break;
		}
		case 175: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.OPTION_TIME_SPREAD_VS; 
			break;
		}
		case 176: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PRICE_SPREAD_VS; 
			break;
		}
		case 177: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.RATIO_SPREAD_VS; 
			break;
		}
		case 178: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PUT_CALL_SPREADVS; 
			break;
		}
		case 179: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.LADDER_VS; 
			break;
		}
		case 180: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PRICE_SPREAD_VS_OPTION_VS; 
			break;
		}
		case 181: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.COLLAR; 
			break;
		}
		case 182: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.COMBO; 
			break;
		}
		case 183: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.PROTECTIVE_PUT; 
			break;
		}
		case 184: {
			Get();
			compoundType = Sample.FoxScript.CompoundType.SPREAD; 
			break;
		}
		default: SynErr(276); break;
		}
	}

	void DoubleOptional(ref double? value) {
		if (la.kind == 65) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else if (la.kind == 66) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else SynErr(277);
	}

	void ContractKind(out ContractKind kind) {
		kind = Sample.FoxScript.ContractKind.UNKNOWN; 
		if (la.kind == 187) {
			Get();
			kind = Sample.FoxScript.ContractKind.FUTURE; 
		} else if (la.kind == 188) {
			Get();
			kind = Sample.FoxScript.ContractKind.OPTION; 
		} else if (la.kind == 189) {
			Get();
			kind = Sample.FoxScript.ContractKind.FOREX; 
		} else if (la.kind == 190) {
			Get();
			kind = Sample.FoxScript.ContractKind.FUTURE_COMPOUND; 
		} else if (la.kind == 191) {
			Get();
			kind = Sample.FoxScript.ContractKind.OPTIONS_COMPOUND; 
		} else SynErr(278);
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
		if (la.kind == 1 || la.kind == 67) {
			AllocationBlock(out command.AllocationBlock);
		}
		if (la.kind == 200) {
			Get();
			Account(ref command.Account);
		}
	}

	void BracketType(ref BracketType btype) {
		if (la.kind == 233) {
			Get();
			btype = Sample.FoxScript.BracketType.OCO; 
		} else if (la.kind == 234) {
			Get();
			btype = Sample.FoxScript.BracketType.OSO; 
		} else SynErr(279);
	}

	void OSOGroupingMethod(ref OSOGroupingMethod btype) {
		if (la.kind == 235) {
			Get();
			btype = Sample.FoxScript.OSOGroupingMethod.ByFirstPrice; 
		} else if (la.kind == 236) {
			Get();
			btype = Sample.FoxScript.OSOGroupingMethod.ByPrice; 
		} else if (la.kind == 237) {
			Get();
			btype = Sample.FoxScript.OSOGroupingMethod.ByFill; 
		} else SynErr(280);
	}

	void OrigMsgVarName(ref string origMsgVarName) {
		Expect(1);
		origMsgVarName = t.val; 
	}

	void OrderSide(ref OrderSide orderSide) {
		orderSide = new OrderSide(); 
		if (la.kind == 77) {
			Get();
			orderSide.Side = QuickFix.Fields.Side.BUY; 
		} else if (la.kind == 78) {
			Get();
			orderSide.Side = QuickFix.Fields.Side.SELL; 
		} else SynErr(281);
		if (la.kind == 81 || la.kind == 82) {
			if (la.kind == 81) {
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
		Expect(86);
		AllocationItem(out item);
		ab.Add(item); 
		while (la.kind == 6) {
			Get();
			AllocationItem(out item);
			ab.Add(item); 
		}
		Expect(87);
	}

	void PostAllocationBlock(out AllocationBlock<PostAllocationBlockItem> ab) {
		ab = new AllocationBlock<PostAllocationBlockItem>(); 
		PostAllocationBlockItem item;
		
		PostAllocationRule(ref ab.Rule);
		Expect(86);
		PostAllocationItem(out item);
		ab.Add(item); 
		while (la.kind == 6) {
			Get();
			PostAllocationItem(out item);
			ab.Add(item); 
		}
		Expect(87);
	}

	void FormatArgs(ref FormatArgs fargs) {
		Expect(67);
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
		} else SynErr(282);
	}

	void SetSeqNumPropCommand() {
		Expect(197);
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
		if (la.kind == 201) {
			Get();
			result = -1; 
		} else if (la.kind == 65) {
			Get();
			result = LiteralParser.ParseInteger(t.val); 
		} else SynErr(283);
	}

	void Literal(ref Object value) {
		switch (la.kind) {
		case 65: {
			Get();
			value = new Object(ObjectType.Integer, t.val); 
			break;
		}
		case 66: {
			Get();
			value = new Object(ObjectType.Float, t.val); 
			break;
		}
		case 67: {
			Get();
			value = new Object(ObjectType.String, t.val); 
			break;
		}
		case 68: {
			Get();
			value = new Object(ObjectType.Bool, t.val); 
			break;
		}
		case 70: {
			Get();
			value = new Object(ObjectType.Bool, "TRUE"); 
			break;
		}
		case 69: {
			Get();
			value = new Object(ObjectType.Bool, t.val); 
			break;
		}
		case 71: {
			Get();
			value = new Object(ObjectType.Bool, "FALSE"); 
			break;
		}
		case 73: {
			Get();
			value = new Object(ObjectType.Timespan, t.val); 
			break;
		}
		case 74: {
			Get();
			value = new Object(ObjectType.Timestamp, t.val); 
			break;
		}
		case 75: {
			Get();
			value = new Object(ObjectType.Date, t.val); 
			break;
		}
		default: SynErr(284); break;
		}
	}

	void OrderQty(ref int orderQty) {
		Expect(65);
		orderQty = LiteralParser.ParseInteger(t.val); 
	}

	void OrderType(ref OrderType orderType) {
		double stop = 0, limit = 0; orderType = new OrderType(); 
		switch (la.kind) {
		case 221: {
			Get();
			orderType.Type = QuickFix.Fields.OrdType.MARKET; 
			break;
		}
		case 222: {
			Get();
			orderType.Type = Sample.FoxScript.OrderType.MARKET_ON_OPEN; 
			break;
		}
		case 223: {
			Get();
			orderType.Type = Sample.FoxScript.OrderType.MARKET_ON_CLOSE; 
			break;
		}
		case 224: {
			Get();
			orderType.Type = QuickFix.Fields.OrdType.LIMIT; 
			Price(ref limit);
			orderType.Limit = limit; 
			break;
		}
		case 225: {
			Get();
			orderType.Type = QuickFix.Fields.OrdType.STOP; 
			Price(ref stop);
			orderType.Stop = stop; 
			if (la.kind == 224) {
				Get();
				orderType.Type = QuickFix.Fields.OrdType.STOP_LIMIT; 
				Price(ref limit);
				orderType.Limit = limit; 
			}
			if (la.kind == 228) {
				TrailingStop(ref orderType.TrailingStop);
			}
			break;
		}
		case 226: {
			Get();
			orderType.Type = Sample.FoxScript.OrderType.ICEBERG; 
			Expect(65);
			orderType.MaxFloor = LiteralParser.ParseInteger(t.val); 
			Expect(224);
			Price(ref limit);
			orderType.Limit = limit; 
			break;
		}
		case 227: {
			Get();
			orderType.Type = QuickFix.Fields.OrdType	.MARKET_IF_TOUCHED; 
			Price(ref limit);
			orderType.Limit = limit; 
			break;
		}
		default: SynErr(285); break;
		}
	}

	void TimeInForce(ref TimeInForce tif) {
		tif = new TimeInForce(); 
		if (la.kind == 216) {
			Get();
			tif.Type = QuickFix.Fields.TimeInForce.DAY; 
		} else if (la.kind == 217) {
			Get();
			tif.Type = QuickFix.Fields.TimeInForce.GOOD_TILL_CANCEL; 
		} else if (la.kind == 218) {
			Get();
			tif.Type = QuickFix.Fields.TimeInForce.GOOD_TILL_DATE; 
			if (la.kind == 74) {
				Get();
				tif.Expiration = LiteralParser.ParseTimestamp(t.val); 
			} else if (la.kind == 75) {
				Get();
				tif.Expiration = LiteralParser.ParseDate(t.val); 
			} else SynErr(286);
		} else if (la.kind == 219) {
			Get();
			tif.Type = QuickFix.Fields.TimeInForce.FILL_OR_KILL; 
		} else if (la.kind == 220) {
			Get();
			tif.Type = QuickFix.Fields.TimeInForce.IMMEDIATE_OR_CANCEL; 
		} else SynErr(287);
	}

	void TradingSession(ref string session) {
		if (la.kind == 209) {
			Get();
			Expect(202);
		}
		switch (la.kind) {
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
		case 213: {
			Get();
			session = t.val; 
			break;
		}
		case 214: {
			Get();
			session = t.val; 
			break;
		}
		case 215: {
			Get();
			session = t.val; 
			break;
		}
		default: SynErr(288); break;
		}
	}

	void AllocationRule(ref AllocationRule rule) {
		if (la.kind == 204) {
			Get();
			rule = Sample.FoxScript.AllocationRule.LowAcctLowPrice; 
		} else if (la.kind == 205) {
			Get();
			rule = Sample.FoxScript.AllocationRule.LowAcctHighPrice; 
		} else if (la.kind == 206) {
			Get();
			rule = Sample.FoxScript.AllocationRule.HighAcctLowPrice; 
		} else if (la.kind == 207) {
			Get();
			rule = Sample.FoxScript.AllocationRule.HighAcctHighPrice; 
		} else if (la.kind == 208) {
			Get();
			rule = Sample.FoxScript.AllocationRule.APS; 
		} else SynErr(289);
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
		} else if (la.kind == 208) {
			Get();
			rule = Sample.FoxScript.AllocationRule.PostAllocationAPS; 
		} else SynErr(290);
	}

	void PostAllocationItem(out PostAllocationBlockItem item) {
		item = new PostAllocationBlockItem(); 
		AccountInfo(ref item.Account);
		Double(ref item.Price);
		Double(ref item.Weight);
	}

	void Double(ref double value) {
		if (la.kind == 65) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else if (la.kind == 66) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else SynErr(291);
	}

	void AccountInfo(ref ExtendedAccount account) {
		IdentOrString(ref account.Spec);
		if (la.kind == 202 || la.kind == 203) {
			if (la.kind == 202) {
				Get();
				IdentOrString(ref account.Firm);
			} else {
				Get();
				IdentOrString(ref account.ClearingHouse);
			}
		}
	}

	void Timeout() {
		Expect(73);
	}

	void MsgCtxLogicalExpr(ref object expr) {
		MsgCtxOrExpr(ref expr);
	}

	void OrExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		AndExpr(ref left);
		expr = left; 
		if (la.kind == 63) {
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
		if (la.kind == 64) {
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
		} else if (la.kind == 72) {
			Get();
			expr = new Object(ObjectType.Null, null); 
		} else if (la.kind == 1) {
			MsgVarName(ref msgVarName);
			expr = new Object(ObjectType.FixMsgVar, msgVarName); 
		} else SynErr(292);
		if (la.kind == 57 || la.kind == 58) {
			msgVarName = null; 
			if (la.kind == 57) {
				Get();
				op = LogicalOp.Equal; 
			} else {
				Get();
				op = LogicalOp.NotEqual; 
			}
			if (StartOf(11)) {
				RelExpr(ref right);
				expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = right }; 
			} else if (la.kind == 72) {
				Get();
				expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = new Object(ObjectType.Null, null) }; 
			} else if (la.kind == 1) {
				MsgVarName(ref msgVarName);
				expr = new LogicalExpr() { Operation = op.Value, Left = expr, Right = new Object(ObjectType.FixMsgVar, msgVarName) }; 
			} else SynErr(293);
		}
	}

	void RelExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		RelExprArg(ref left);
		expr = left; 
		if (StartOf(12)) {
			if (la.kind == 59) {
				Get();
				op = LogicalOp.Less; 
			} else if (la.kind == 61) {
				Get();
				op = LogicalOp.Greater; 
			} else if (la.kind == 60) {
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
		} else if (la.kind == 84) {
			Get();
			LogicalExpr(ref arg);
			Expect(85);
		} else SynErr(294);
	}

	void MsgCtxOrExpr(ref object expr) {
		object left = null; object right = null; LogicalOp? op = null; 
		MsgCtxAndExpr(ref left);
		expr = left; 
		if (la.kind == 63) {
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
		if (la.kind == 64) {
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
		if (la.kind == 57 || la.kind == 58) {
			if (la.kind == 57) {
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
			if (la.kind == 59) {
				Get();
				op = LogicalOp.Less; 
			} else if (la.kind == 61) {
				Get();
				op = LogicalOp.Greater; 
			} else if (la.kind == 60) {
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
		} else if (la.kind == 84) {
			Get();
			MsgCtxLogicalExpr(ref arg);
			Expect(85);
		} else SynErr(295);
	}

	void FieldName(ref string fieldName) {
		Expect(1);
		fieldName = t.val; 
	}

	void Price(ref double value) {
		if (la.kind == 65) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else if (la.kind == 66) {
			Get();
			value = LiteralParser.ParseFloat(t.val); 
		} else SynErr(296);
	}

	void TrailingStop(ref TrailingStop trailing) {
		trailing = new TrailingStop(); 
		Expect(228);
		if (la.kind == 229 || la.kind == 230 || la.kind == 231) {
			if (la.kind == 229) {
				Get();
				trailing.TriggerType = TokenParser.ParseTrailingTriggerType(t.val); 
			} else if (la.kind == 230) {
				Get();
				trailing.TriggerType = TokenParser.ParseTrailingTriggerType(t.val); 
			} else {
				Get();
				trailing.TriggerType = TokenParser.ParseTrailingTriggerType(t.val); 
			}
			if (la.kind == 65) {
				Get();
				trailing.Amount = LiteralParser.ParseInteger(t.val); 
			} else if (la.kind == 66) {
				Get();
				trailing.Amount = LiteralParser.ParseFloat(t.val); 
			} else SynErr(297);
			if (la.kind == 232) {
				Get();
				trailing.AmountInPercents = true; 
			}
		} else if (la.kind == 65 || la.kind == 66) {
			if (la.kind == 65) {
				Get();
				trailing.Amount = LiteralParser.ParseInteger(t.val); 
			} else {
				Get();
				trailing.Amount = LiteralParser.ParseFloat(t.val); 
			}
		} else SynErr(298);
	}

	void Symbol(ref OrderSymbol symbol) {
		if (la.kind == 1) {
			Get();
			symbol = TokenParser.ParseOrderSymbol(t.val); 
		} else if (la.kind == 67) {
			Get();
			symbol = TokenParser.ParseOrderSymbol(LiteralParser.ParseString(t.val)); 
		} else SynErr(299);
	}

	void StrikeSide(OrderContract contract) {
		double strike = 0; 
		if (la.kind == 80) {
			Get();
			contract.Put = false; 
		} else if (la.kind == 79) {
			Get();
			contract.Put = true; 
		} else SynErr(300);
		Price(ref strike);
		contract.Strike = strike; 
	}

	void FASTUpdateType(MDMessageCommand command) {
		if (la.kind == 238) {
			Get();
			command.UpdateType = 0; 
		}
	}

	void FASTMDEntries(MDMessageCommand command) {
		command.ResetMDEntries(); 
		Expect(239);
		Expect(198);
		if (StartOf(14)) {
			FAST.MDEntryType type; 
			FASTMDEntryType(out type);
			command.Add(type); 
			while (la.kind == 6) {
				Get();
				FASTMDEntryType(out type);
				command.Add(type); 
			}
		}
		Expect(199);
	}

	void FASTMDEntryType(out FAST.MDEntryType type) {
		type = FAST.MDEntryType.BID; 
		switch (la.kind) {
		case 230: {
			Get();
			type = FAST.MDEntryType.BID; 
			break;
		}
		case 240: {
			Get();
			type = FAST.MDEntryType.OFFER; 
			break;
		}
		case 241: {
			Get();
			type = FAST.MDEntryType.TRADE; 
			break;
		}
		case 242: {
			Get();
			type = FAST.MDEntryType.OPENING_PRICE; 
			break;
		}
		case 243: {
			Get();
			type = FAST.MDEntryType.SETTLEMENT_PRICE; 
			break;
		}
		case 244: {
			Get();
			type = FAST.MDEntryType.TRADE_VOLUME; 
			break;
		}
		case 245: {
			Get();
			type = FAST.MDEntryType.OPEN_INTEREST; 
			break;
		}
		case 246: {
			Get();
			type = FAST.MDEntryType.WORKUP_TRADE; 
			break;
		}
		case 247: {
			Get();
			type = FAST.MDEntryType.EMPTY_BOOK; 
			break;
		}
		default: SynErr(301); break;
		}
	}

	void FASTContract(MDMessageCommand command) {
		if (la.kind == 184 || la.kind == 189) {
			FASTSymbolBasedContract(command);
		} else if (la.kind == 1 || la.kind == 67) {
			FASTFuturesBasedContract(command);
		} else SynErr(302);
	}

	void FASTSymbolBasedContract(MDMessageCommand command) {
		if (la.kind == 184) {
			Get();
			command.ContractKind = Sample.FoxScript.ContractKind.FUTURE_COMPOUND; 
		} else if (la.kind == 189) {
			Get();
			command.ContractKind = Sample.FoxScript.ContractKind.FOREX; 
		} else SynErr(303);
		IdentOrString(ref command.BaseSymbol);
		if (la.kind == 79 || la.kind == 80) {
			FASTStrikeSide(command);
			command.ContractKind = Sample.FoxScript.ContractKind.OPTIONS_COMPOUND; 
		}
	}

	void FASTFuturesBasedContract(MDMessageCommand command) {
		command.ContractKind = Sample.FoxScript.ContractKind.FUTURE; 
		IdentOrString(ref command.BaseSymbol);
		if (la.kind == 65) {
			Get();
			command.ExpirationMonth = LiteralParser.ParseInteger(t.val);   
		}
		if (la.kind == 79 || la.kind == 80) {
			FASTStrikeSide(command);
			command.ContractKind = Sample.FoxScript.ContractKind.OPTION; 
		}
	}

	void FASTStrikeSide(MDMessageCommand command) {
		command.StrikeSide = new FASTStrikeSide(); double strike = 0; 
		if (la.kind == 80) {
			Get();
			command.StrikeSide.Put = false; 
		} else if (la.kind == 79) {
			Get();
			command.StrikeSide.Put = true; 
		} else SynErr(304);
		Price(ref strike);
		command.StrikeSide.Strike = strike; 
	}

	void SubscribeMarketDataToFile(ref MDMessageCommand command) {
		if (la.kind == 61) {
			Get();
			Expect(67);
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
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_x,_x,_x, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_T, _T,_T,_T,_x, _x,_x,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_T,_T,_T, _x,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_T,_T,_T, _x,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_T,_T,_T, _x,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x}

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
			case 46: s = "actionBracket expected"; break;
			case 47: s = "actionHeartbeat expected"; break;
			case 48: s = "actionConnectFast expected"; break;
			case 49: s = "actionDisconnectFast expected"; break;
			case 50: s = "actionCancelSubscribe expected"; break;
			case 51: s = "actionSubscribeQuotes expected"; break;
			case 52: s = "actionSubscribeDOM expected"; break;
			case 53: s = "actionSubscribeHistogram expected"; break;
			case 54: s = "actionSubscribeTicks expected"; break;
			case 55: s = "actionLoadTicks expected"; break;
			case 56: s = "assignOp expected"; break;
			case 57: s = "equalOp expected"; break;
			case 58: s = "notEqualOp expected"; break;
			case 59: s = "lessOp expected"; break;
			case 60: s = "lessOrEqualOp expected"; break;
			case 61: s = "greaterOp expected"; break;
			case 62: s = "greaterOrEqualOp expected"; break;
			case 63: s = "orOp expected"; break;
			case 64: s = "andOp expected"; break;
			case 65: s = "integer expected"; break;
			case 66: s = "float expected"; break;
			case 67: s = "string expected"; break;
			case 68: s = "true expected"; break;
			case 69: s = "false expected"; break;
			case 70: s = "on expected"; break;
			case 71: s = "off expected"; break;
			case 72: s = "null expected"; break;
			case 73: s = "timespan expected"; break;
			case 74: s = "timestamp expected"; break;
			case 75: s = "date expected"; break;
			case 76: s = "uuid expected"; break;
			case 77: s = "buy expected"; break;
			case 78: s = "sell expected"; break;
			case 79: s = "put expected"; break;
			case 80: s = "call expected"; break;
			case 81: s = "open expected"; break;
			case 82: s = "close expected"; break;
			case 83: s = "\"if\" expected"; break;
			case 84: s = "\"(\" expected"; break;
			case 85: s = "\")\" expected"; break;
			case 86: s = "\"{\" expected"; break;
			case 87: s = "\"}\" expected"; break;
			case 88: s = "\"else\" expected"; break;
			case 89: s = "\"msg\" expected"; break;
			case 90: s = "\"uuid\" expected"; break;
			case 91: s = "\"request\" expected"; break;
			case 92: s = "\"req\" expected"; break;
			case 93: s = "\"r\" expected"; break;
			case 94: s = "\"base\" expected"; break;
			case 95: s = "\"b\" expected"; break;
			case 96: s = "\"lookup\" expected"; break;
			case 97: s = "\"lkp\" expected"; break;
			case 98: s = "\"l\" expected"; break;
			case 99: s = "\"mode\" expected"; break;
			case 100: s = "\"max_records\" expected"; break;
			case 101: s = "\"snapshot\" expected"; break;
			case 102: s = "\"subscribe\" expected"; break;
			case 103: s = "\"unsubscribe\" expected"; break;
			case 104: s = "\"updates_only\" expected"; break;
			case 105: s = "\"kind\" expected"; break;
			case 106: s = "\"type\" expected"; break;
			case 107: s = "\"opt_type\" expected"; break;
			case 108: s = "\"by_base_contract\" expected"; break;
			case 109: s = "\"opt_required\" expected"; break;
			case 110: s = "\"base_contract\" expected"; break;
			case 111: s = "\"underlying\" expected"; break;
			case 112: s = "\"exch\" expected"; break;
			case 113: s = "\"cgroup\" expected"; break;
			case 114: s = "\"compound_type\" expected"; break;
			case 115: s = "\"unknown\" expected"; break;
			case 116: s = "\"generic\" expected"; break;
			case 117: s = "\"performance_index_basket\" expected"; break;
			case 118: s = "\"non_performance_index_basket\" expected"; break;
			case 119: s = "\"straddle\" expected"; break;
			case 120: s = "\"strangle\" expected"; break;
			case 121: s = "\"future_time_spread\" expected"; break;
			case 122: s = "\"option_time_spread\" expected"; break;
			case 123: s = "\"price_spread\" expected"; break;
			case 124: s = "\"synthetic_underlying\" expected"; break;
			case 125: s = "\"straddle_time_spread\" expected"; break;
			case 126: s = "\"ratio_spread\" expected"; break;
			case 127: s = "\"ratio_future_time_spread\" expected"; break;
			case 128: s = "\"ratio_option_time_spread\" expected"; break;
			case 129: s = "\"put_call_spread\" expected"; break;
			case 130: s = "\"ratio_put_call_spread\" expected"; break;
			case 131: s = "\"ladder\" expected"; break;
			case 132: s = "\"box\" expected"; break;
			case 133: s = "\"butterfly\" expected"; break;
			case 134: s = "\"condor\" expected"; break;
			case 135: s = "\"iron_butterfly\" expected"; break;
			case 136: s = "\"diagonal_spread\" expected"; break;
			case 137: s = "\"ratio_diagonal_spread\" expected"; break;
			case 138: s = "\"straddle_diagonal_spread\" expected"; break;
			case 139: s = "\"conversion_reversal\" expected"; break;
			case 140: s = "\"covered_option\" expected"; break;
			case 141: s = "\"reserved1\" expected"; break;
			case 142: s = "\"reserved2\" expected"; break;
			case 143: s = "\"currency_future_spread\" expected"; break;
			case 144: s = "\"rate_future_spread\" expected"; break;
			case 145: s = "\"index_future_spread\" expected"; break;
			case 146: s = "\"future_butterfly\" expected"; break;
			case 147: s = "\"future_condor\" expected"; break;
			case 148: s = "\"strip\" expected"; break;
			case 149: s = "\"pack\" expected"; break;
			case 150: s = "\"bundle\" expected"; break;
			case 151: s = "\"bond_deliverable_basket\" expected"; break;
			case 152: s = "\"stock_basket\" expected"; break;
			case 153: s = "\"price_spread_vs_option\" expected"; break;
			case 154: s = "\"straddle_vs_option\" expected"; break;
			case 155: s = "\"bond_spread\" expected"; break;
			case 156: s = "\"exchange_spread\" expected"; break;
			case 157: s = "\"future_pack_spread\" expected"; break;
			case 158: s = "\"future_pack_butterfly\" expected"; break;
			case 159: s = "\"whole_sale\" expected"; break;
			case 160: s = "\"commodity_spread\" expected"; break;
			case 161: s = "\"jelly_roll\" expected"; break;
			case 162: s = "\"iron_condor\" expected"; break;
			case 163: s = "\"options_strip\" expected"; break;
			case 164: s = "\"contingent_orders\" expected"; break;
			case 165: s = "\"interproduct_spread\" expected"; break;
			case 166: s = "\"pseudo_straddle\" expected"; break;
			case 167: s = "\"tailor_made\" expected"; break;
			case 168: s = "\"futures_generic\" expected"; break;
			case 169: s = "\"options_generic\" expected"; break;
			case 170: s = "\"basis_trade\" expected"; break;
			case 171: s = "\"futuretime_spread_reduced_tick_size\" expected"; break;
			case 172: s = "\"generic_vola_strategy_vs\" expected"; break;
			case 173: s = "\"straddle_vola_strategy_vs\" expected"; break;
			case 174: s = "\"strangle_vs\" expected"; break;
			case 175: s = "\"option_time_spread_vs\" expected"; break;
			case 176: s = "\"price_spread_vs\" expected"; break;
			case 177: s = "\"ratio_spread_vs\" expected"; break;
			case 178: s = "\"put_call_spreadvs\" expected"; break;
			case 179: s = "\"ladder_vs\" expected"; break;
			case 180: s = "\"price_spread_vs_option_vs\" expected"; break;
			case 181: s = "\"collar\" expected"; break;
			case 182: s = "\"combo\" expected"; break;
			case 183: s = "\"protective_put\" expected"; break;
			case 184: s = "\"spread\" expected"; break;
			case 185: s = "\"electronic\" expected"; break;
			case 186: s = "\"pit\" expected"; break;
			case 187: s = "\"future\" expected"; break;
			case 188: s = "\"option\" expected"; break;
			case 189: s = "\"forex\" expected"; break;
			case 190: s = "\"future_compound\" expected"; break;
			case 191: s = "\"options_compound\" expected"; break;
			case 192: s = "\"any_inclusion\" expected"; break;
			case 193: s = "\"symbol_starts_with\" expected"; break;
			case 194: s = "\"description_starts_with\" expected"; break;
			case 195: s = "\"any_starts_with\" expected"; break;
			case 196: s = "\"exact_match\" expected"; break;
			case 197: s = "\"seqnum\" expected"; break;
			case 198: s = "\"[\" expected"; break;
			case 199: s = "\"]\" expected"; break;
			case 200: s = "\"for\" expected"; break;
			case 201: s = "\"*\" expected"; break;
			case 202: s = "\":\" expected"; break;
			case 203: s = "\"::\" expected"; break;
			case 204: s = "\"low_acct_low_price\" expected"; break;
			case 205: s = "\"low_acct_high_price\" expected"; break;
			case 206: s = "\"high_acct_low_price\" expected"; break;
			case 207: s = "\"high_acct_high_price\" expected"; break;
			case 208: s = "\"aps\" expected"; break;
			case 209: s = "\"ts\" expected"; break;
			case 210: s = "\"pre\" expected"; break;
			case 211: s = "\"main\" expected"; break;
			case 212: s = "\"after\" expected"; break;
			case 213: s = "\"p1\" expected"; break;
			case 214: s = "\"p2\" expected"; break;
			case 215: s = "\"p3\" expected"; break;
			case 216: s = "\"day\" expected"; break;
			case 217: s = "\"gtc\" expected"; break;
			case 218: s = "\"gtd\" expected"; break;
			case 219: s = "\"fok\" expected"; break;
			case 220: s = "\"ioc\" expected"; break;
			case 221: s = "\"mkt\" expected"; break;
			case 222: s = "\"moo\" expected"; break;
			case 223: s = "\"moc\" expected"; break;
			case 224: s = "\"lmt\" expected"; break;
			case 225: s = "\"stp\" expected"; break;
			case 226: s = "\"ice\" expected"; break;
			case 227: s = "\"mit\" expected"; break;
			case 228: s = "\"trailing\" expected"; break;
			case 229: s = "\"last\" expected"; break;
			case 230: s = "\"bid\" expected"; break;
			case 231: s = "\"ask\" expected"; break;
			case 232: s = "\"%\" expected"; break;
			case 233: s = "\"oco\" expected"; break;
			case 234: s = "\"oso\" expected"; break;
			case 235: s = "\"byfirstprice\" expected"; break;
			case 236: s = "\"byprice\" expected"; break;
			case 237: s = "\"byfill\" expected"; break;
			case 238: s = "\"full\" expected"; break;
			case 239: s = "\"mdentries:\" expected"; break;
			case 240: s = "\"offer\" expected"; break;
			case 241: s = "\"trade\" expected"; break;
			case 242: s = "\"opening_price\" expected"; break;
			case 243: s = "\"settlement_price\" expected"; break;
			case 244: s = "\"trade_volume\" expected"; break;
			case 245: s = "\"open_interest\" expected"; break;
			case 246: s = "\"workup_trade\" expected"; break;
			case 247: s = "\"empty_book\" expected"; break;
			case 248: s = "\"from\" expected"; break;
			case 249: s = "\"to\" expected"; break;
			case 250: s = "??? expected"; break;
			case 251: s = "invalid Command"; break;
			case 252: s = "invalid SimpleCommand"; break;
			case 253: s = "invalid MsgProducingCommand"; break;
			case 254: s = "invalid SetPropCommand"; break;
			case 255: s = "invalid QuitCommand"; break;
			case 256: s = "invalid ContractCommand"; break;
			case 257: s = "invalid PostAllocationCommand"; break;
			case 258: s = "invalid LoadTicksCommand"; break;
			case 259: s = "invalid MessageFieldAssignments"; break;
			case 260: s = "invalid MessageFieldAssignments"; break;
			case 261: s = "invalid RValue"; break;
			case 262: s = "invalid IdentOrString"; break;
			case 263: s = "invalid Account"; break;
			case 264: s = "invalid ContractRequest"; break;
			case 265: s = "invalid ContractRequest"; break;
			case 266: s = "invalid ContractLookup"; break;
			case 267: s = "invalid BaseContractRequest"; break;
			case 268: s = "invalid SubscriptionType"; break;
			case 269: s = "invalid BaseContractParam"; break;
			case 270: s = "invalid ContractLookupMode"; break;
			case 271: s = "invalid ContractLookupParam"; break;
			case 272: s = "invalid ContractKindList"; break;
			case 273: s = "invalid ContractType"; break;
			case 274: s = "invalid OptionType"; break;
			case 275: s = "invalid BoolOptional"; break;
			case 276: s = "invalid CompoundType"; break;
			case 277: s = "invalid DoubleOptional"; break;
			case 278: s = "invalid ContractKind"; break;
			case 279: s = "invalid BracketType"; break;
			case 280: s = "invalid OSOGroupingMethod"; break;
			case 281: s = "invalid OrderSide"; break;
			case 282: s = "invalid FormatArg"; break;
			case 283: s = "invalid IntegerOrDefault"; break;
			case 284: s = "invalid Literal"; break;
			case 285: s = "invalid OrderType"; break;
			case 286: s = "invalid TimeInForce"; break;
			case 287: s = "invalid TimeInForce"; break;
			case 288: s = "invalid TradingSession"; break;
			case 289: s = "invalid AllocationRule"; break;
			case 290: s = "invalid PostAllocationRule"; break;
			case 291: s = "invalid Double"; break;
			case 292: s = "invalid EqlExpr"; break;
			case 293: s = "invalid EqlExpr"; break;
			case 294: s = "invalid RelExprArg"; break;
			case 295: s = "invalid MsgCtxRelExprArg"; break;
			case 296: s = "invalid Price"; break;
			case 297: s = "invalid TrailingStop"; break;
			case 298: s = "invalid TrailingStop"; break;
			case 299: s = "invalid Symbol"; break;
			case 300: s = "invalid StrikeSide"; break;
			case 301: s = "invalid FASTMDEntryType"; break;
			case 302: s = "invalid FASTContract"; break;
			case 303: s = "invalid FASTSymbolBasedContract"; break;
			case 304: s = "invalid FASTStrikeSide"; break;

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