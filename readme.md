OEC FIX Sample is a console application, which communicates to OEC FIX and FAST servers and allows to send order-manipulating commands such as create an order, replace/cancel orders, request balance/positions, subscribe to data.

OEC FIX Sample provides a script language called FOXScript (FIX OEC eXchange Script) for creating commands/scripts, as well as for test scenarios. 

# FOXScript Language 

- All language tokens are case-insensitive. 
- In interactive console mode, typing the most trailing ";" end-of-clause symbol is not required. 

## Data types

Data type defines what values an object can store and what operations can be performed on the object. FOXScript provides the following data types: 

- **Integer**. Positive, zero or negative integer values.  
  Examples: `-1`, `0`, `133`. 
- **Float**. Float-point numbers.  
  Examples: `-12.25`, `3.14159`, `1.1134e3`. 
- **String**. Sequence of characters, included into single-quotes.  
  Examples: `'This is a string.'`
- **Boolean**. Logical values True or False.  
  Examples: `True`, `False`, `On`, `Off`. 
- **Timespan**. Time interval.  
  Format: [D.HH:MM:SS]  
  Examples: `[00:05]`, `[1.00:00:01]`
- **Timestamp**. Date/time moment.  
  Format: [YYYYMMDD-HH:MM:SS]  
  Examples: `[20110229-15:30]`
- **Date**. Date without time.  
  Format: [YYYYMMDD]  
  Examples: `[20110131]`

## Objects 

Objects store some values. The following object types are defined:

- Literals of data types.  
  Examples: `1`, `'This is a text'`, `[00:00:15]`.   
- NULL value. This value means that an object is not assigned any value. Any object can have null value, so any object can be checked to equal/not equal null.  
  Examples: `if (report1 == null) ...`
- FIX messages. FIX messages are produced by dedicated commands.  
  Examples: `order1 = new buy 1 ESH1 ... ; print order1.ClOrdID;`
- Fields of FIX messages. Fields of a FIX message are specified by a FIX message and field name. Value of a field has corresponding data type: Integer, Float, String, Boolean, Timespan, Timestamp, or Date.  
  Examples: `order1.ClOrdID`, `order2.11`, `report2.OrdStatus`, `report2.54`.  
- FIX constants. FIX constant is a named value of some FIX field according FIX standard. The data type of a constant is the same as the one of corresponding FIX field.  
  Examples: `FIX.Side.Sell`, `FIX.54.BUY`, `FIX.PosReqType.POSITIONS`, `FIX.35.ExecutionReport`. 

## Global Properties 

There is a set of global properties for OEC FIX Sample application. Any property has name and value of some data type. The following properties are defined: 

- **Host (String)**. The host where OEC FIX server is running and where OEC FIX Sample connects to. 
- **Port (Integer)**. TCP port on which OEC FIX server is listening for client connections. 
- **ReconnectInterval (Integer)**. Interval (in seconds) between attempts to connect to OEC FIX server. 
- **HeartbeatInterval (Integer)**. Interval (in seconds) for FIX heartbeat messages.  
- **BeginString (String)**. FIX version "FIX.4.4". 
- **SenderCompID (String)**. FIX sender company ID. 
- **TargetCompID (String)**. FIX target company ID. 
- **SessionStart (Timespan)**. Start of FIX session in local time. 
- **SessionEnd (Timespan)**. End of FIX session in local time. 
- **ResponseTimeout (Timespan)**. Timeout for receiving a FIX message from OEC FIX server. 
- **ConnectTimeout (Timespan)**. Timeout for connecting to OEC FIX server. 
- **ConnectTimeout (Timespan)**. Timeout for connecting to OEC FIX server. 
- **FutureAccount (String)**. Default account for using in commands with futures context
- **ForexAccount (String)**. Default account for using in commands with forex context
- **FastHashCode (String)**. Hashcode for authentication in OEC FAST server
- **SSL (Boolean)**. Indicates if the next CONNECT command should use SSL encryption

## Statements 

### IF/ELSE Statement 

The IF/ELSE statement has the following format: 

```
if (LogicalExpr) { commands or statements }  
else { commands or statements };
```

or

`if (LogicalExpr) { commands or statements };`

In logical expression the following operations are allowed (in order from higher to lower precedence): 

1. <, <=, >, >=
2. ==, != 
3. && (AND operation) 
4. || (OR operation) 

Operands in a logical expression must be objects. 

## Commands

- Parts of command formats inside square brackets are optional and can be omitted. Parts of commands inside braces can be repeated zero 
or more times. "|" character means OR choice of specified items. 
- To simplify typing, some commands can accept arguments as identifiers or string literals. Identifier is a token that starts with letter 
or underscore character and followed by letter, digit or underscore characters. If a value conforms to identifier naming, it may be written 
as identifier, otherwise the value must be written as a string literal inside single quotes. 

### GET Command

`GET [PropertyName];`

Prints a value of the specified global property, as well as its data type. If the property name is not specified, prints value/data type for all global properties. 

### SET Command 

`SET PropertyName NewValue;`

Sets a value for the specified global property. The new value must be of proper data type. 

### PRINT Command 

`PRINT Object {, Object or Expression};`

Prints the specified object values, one value per a line. 

### PRINTF Command

`PRINTF 'FormatStr' {, Object or Expression};`

Prints formatted string and object values. Format placeholders specified as {0}, {1} and so on. 

### SLEEP Command 

`SLEEP timespan;`

Suspends the current thread for the specified amount of time. 

### ANYKEY Command

`ANYKEY`;

Suspends the current thread until user presses any key

### EXIT Command 

`EXIT;`

Exits OEC FIX Sample application. 

### CONNECT Command 

`CONNECT [SenderCompID,] Password [uuid];`

- SenderCompID can be an identifier or string literal. 
- Password can be identifier or string literal. 

Connects OEC FIX Sample application to OEC FIX server. If SenderCompID parameter not specified, the value is retrieved from the global property with the same name. Else, the value is stored in the global property.  
Duration of connecting is limited by ConnectTimeout global property. 

### DISCONNECT Command 

`DISCONNECT;`

Disconnects OEC FIX Sample application from OEC FIX server. 

### PING Command 

`PING;`

Pings OEC FIX server sending FIX TestRequest message. The command prints sequence numbers from both test request and test response FIX messages. It's recommended to execute PING command after CONNECT command to synchronize sequence numbers on both sides. 

### EXEC Command 

`EXEC FileName [, 'ScriptName'];`

- FileName can be an identifier or string literal. 

Executes (or calls) a script in the specified file. If script name is specified, such script is treated as a named script.  
For named script beginning and ending messages are printed.  
Executing scripts can call other scripts with arbitrary deep. If a syntax/semantic exception occurs on some level of script calls, the exception rolls a "call stack" up to console input level, aborting all calling scripts. 

### ENSURE Command 

`ENSURE LogicalExpr [, 'FormatStr' {, Object or Expression}];`

Checks the logical expression and if it is false, throws an exception. If there is a call stack of EXEC commands, all scripts are aborted. If FormatStr is specified, this value is set as the thrown exception message value. 

### NEW ORDER Command 

```
[[MSG] MsgName =] NEW (BUY | SELL) [OPEN | CLOSE] OrderQty  
(FutureSymbol [(CALL |PUT) Strike])  
(MKT | MOO | MOC | LMT Limit | STP Stop [LMT Limit] | ICE ShowVol Limit | MIT Level)  
[DAY | GTC | GTD (Timestamp | Date) | FOK | IOC]  
FOR Account  
{, (FieldName | Tag) : Object};
```

-	MsgName must be an identifier and specifies FIX message variable. 
-	FutureSymbol can be an identifier or string literal containing symbol and contract month/year like ESZ4. 
-	Account can be an identifier, integer or string literal. 

Creates an order and sends it to OEC FIX server. If MsgName is specified, stores the FIX order message as a variable so the order can be accessed/modified/cancelled later by commands.  
If a FIX message with the same name already exists, it will be overwritten by new FIX message. 

### MODIFY ORDER Command 

```
[[MSG] MsgName =] MODIFY OrigMsg
(BUY | SELL) [OPEN | CLOSE]
OrderQty FutureSymbol [(CALL |PUT) Strike]
(MKT | MOO | MOC | LMT Limit | STP Stop [LMT Limit] | ICE ShowVol Limit | MIT Level)
[DAY | GTC | GTD (Timestamp | Date) | FOK | IOC] FOR Account
{, (FieldName | Tag) : Object};
```

-	MsgName must be identifier and specifies FIX message variable. 
-	OrigMsg must specify FIX message variable of original order. 
-	FutureSymbol can be identifier or string literal containing symbol and contract month/year like ESH1. 
-	Account can be an identifier, integer or string literal. 

Creates a cancel/replace request and sends it to OEC FIX server. If MsgName is specified, stores the FIX cancel/replace request message as a variable so it can be accessed/modified/cancelled later by commands.  
If a FIX message with the same name already exists, it will be overwritten by new FIX message. 

### CANCEL ORDER Command 

`[[MSG] MsgName =] CANCEL OrigMsg {, (FieldName | Tag) : Object};`

-	MsgName must be an identifier and specifies FIX message variable. 
-	OrigMsg must specify FIX message variable of original order/modify request. 

Creates a cancel request and sends it to OEC FIX server. If MsgName is specified, stores the FIX cancel request message as a variable so it can be accessed later by commands.  
If a FIX message with the same name already exists, it will be overwritten by new FIX message. 

### STATUS Command 

`[[MSG] MsgName =] STATUS OrigMsg;`

-   MsgName must be an identifier and specifies FIX message variable. 
-	OrigMsg must specify FIX message variable of original order/modify request. 

Creates a order status request and sends it to OEC FIX server. If MsgName is specified, stores the FIX order status request message as a variable so it can be accessed later by commands.  
If a FIX message with the same name already exists, it will be overwritten by new FIX message. 

### MASS STATUS Command 

`[[MSG] MsgName =] MASSSTATUS ["BUY" | "SELL"] [FutureSymbol [(CALL |PUT) Strike]] [FOR Account];`

Creates a mass order status request and sends it to OEC FIX server. Optionally, request can specify a filter by order side, contract and account. 

### WAIT Command 

`[[MSG] MsgName =] WAIT [Timeout] MsgType [, LogicalExpr];`

-	MsgName must be an identifier and specifies FIX message variable. 
-	Timeout must be of Timespan data type. If the timeout not specified, the ResponseTimeout global property is used. 
-	MsgType is a name of FIX message type according FIX standard like ExecutionReport or OrderCancelRequest. 
-	LogicalExpr can contain only field names in the context of the receiving message like ClOrdID == '...'. 

Blocks executing until FIX message of specified type and conforming logical expression is received or timeout elapsed. If MsgName is specified and timeout occurred, the command returns null.  
If a FIX message with the same name already exists, it will be overwritten by new FIX message. 

### POSTALLOCATION Command

`[[MSG] MsgName =] POSTALLOCATION OrigMsg [(FutureSymbol [(CALL |PUT) Strike])] ("POST" | "APS") "{" Account1 Price1 Qty1, Account2 Price2 Qty2, ...  "}"`

Sends a request to allocate filled quantity. OrigMsg should refer to a filled order.

### BALANCE Command 

`[[MSG] MsgName =] BALANCE Account {, (FieldName | Tag) : Object};`

-	MsgName must be an identifier and specifies FIX message variable. 
-	Account can be an identifier, integer or string literal. 

Creates CollateralInquiry request and sends it to OEC FIX server. If MsgName is specified, stores the FIX collateral inquiry request message as a variable so it can be accessed later by commands.  
If a FIX message with the same name already exists, it will be overwritten by new FIX message. 

### POSITIONS Command

`[[MSG] MsgName =] POSITIONS Account {, (FieldName | Tag) : Object};`

Creates RequestForPositions request and sends it to OEC FIX server. If MsgName is specified, stores the FIX request message as a variable so it can be accessed later by commands.  
If a FIX message with the same name already exists, it will be overwritten by new FIX message. 

### MARGINCALC Command

`MARGINCALC Account "{" FutureSymbol [(CALL |PUT) Strike] MinHypoNet MaxHypoNet "," ... "}"`

Sends a request to calculate margin requements for a list of hypothetical positions specified with a range of possible net quantity.

### CONTRACT Command
 
`CONTRACT (REQUEST | REQ | R) BaseContractSymbol`

-    BaseContractSymbol can be an identifier or string literal. 

Creates ContractRequest command and sends it to OEC FIX server. 

`CONTRACT (LOOKUP | LKP | L) SearchPattern, MODE=(ANY_INCLUSION | SYMBOL_STARTS_WITH | DESCRIPTION_STARTS_WITH | ANY_STARTS_WITH | EXACT_MATCH), MAX_RECORDS=MaxRecords, { , OptionalLookupParameter }`

-	SearchPattern can be an identifier or string literal.
-	MaxRecords is an integer

Optional Lookup Parameter:

| Name | Value | Description |
|----|-----|-----------|
| KIND | FUTURE |
|      | OPTION |
|      | FOREX |
|      | FUTURE\_COMPOUND |
| EXCH  | Identifier or string literal | OEC exchange name |
| CGROUP | Identifier or string literal | OEC contract group |
| TYPE | ELECTRONIC |
|     | PIT |	
| OPT\_TYPE | PUT |
|          | CALL |
| BY\_BASE\_CONTRACT | TRUE | Match base contract only |
|                    | FALSE |
| OPT\_REQUIRED | TRUE | Options Required |
|               | FALSE |
| BASE\_CONTRACT | Identifier or string literal | Base contract symbol |
| COMPOUND\_TYPE | GENERIC |
|                | STRADDLE |
|                | STRANGLE |
|                | FUTURE\_TIME\_SPREAD |
|                | FUTURE\_BUTTERFLY |
|                | FUTURE\_CONDOR |
|                | STRIP |
|                | PACK |
|                | BUNDLE |
|                | FUTURE\_PACK\_SPREAD |
|                | FUTURE\_PACK\_BUTTERFLY |
| UNDERLYING | Contract Description |

Contract Description:

`(CFICode, Symbol, MaturityMonthYear [, StrikePrice])`

- CFICode, Symbol can be Identifier or string literal
- MaturityMonthYear format is YYYYMM[w|q]
- Strike price is double value

Creates ContractLookupRequest command and sends it to OEC FIX server. 

### USERREQUEST Command

`USERREQUEST ["UUID" "=" uuid]`

Sends a user request with optional UUID. Successful response will fill out FastHashCode with up-to-date value

### CONNECTFAST Command 

`CONNECTFAST [Username];`

- Username can be an identifier or string literal. 

Connects OEC FIX Sample application to OEC FAST server. If Username parameter not specified, the value is retrieved from SenderCompID 
global property. Required password is retrieved from FastHashCode global property. This property is populated automatically from 
successful FIX Logon response initiated by CONNECT command.

### DISCONNECTFAST Command 

`DISCONNECTFAST;`

Disconnects OEC FIX Sample application from OEC FAST server. 

### SUBSCRIBE Commands

`[[MSG] MsgName =] SubscribeQuotes ["FULL"] ((("SPREAD" | "FOREX") Symbol) | (FuturesBaseSymbol ExpirationMonth [("CALL" | "PUT") StrikePrice])) [">" OutputFilename]`
`[[MSG] MsgName =] SubscribeDOM ["FULL"] (("SPREAD" FuturesSpreadSymbol) | (FuturesBaseSymbol ExpirationMonth [("CALL" | "PUT") StrikePrice]))  [">" OutputFilename]`
`[[MSG] MsgName =] SubscribeHistogram ["FULL"] (("SPREAD" FuturesSpreadSymbol) | (FuturesBaseSymbol ExpirationMonth [("CALL" | "PUT") StrikePrice]))  [">" OutputFilename]`
`[[MSG] MsgName =] SubscribeTicks ["FULL"] ((("SPREAD" | "FOREX") Symbol) | (FuturesBaseSymbol ExpirationMonth [("CALL" | "PUT") StrikePrice])) [("FROM" timestamp) | ("LAST" timespan] [">" OutputFilename]`

These commands subscribe to quotes, DOM, histogram and ticks for specified futures, options, forex or futures spreads. Incoming message flow can be directed to OutputFilename.  
Tick subscription can specify start date/time for loading historical data.

### LOAD TICKS Command

`[[MSG] MsgName =] LoadTicks ["FULL"] ((("SPREAD" | "FOREX") Symbol) | (FuturesBaseSymbol ExpirationMonth [("CALL" | "PUT") StrikePrice])) [("FROM" timestamp "TO" timestamp) | ("LAST" timespan] [">" OutputFilename]`

Similar to SubscribeTicks, but loads only historical ticks without real-time updates.

### CANCEL SUBSCRIPTION Command

`CancelSubscribe OrigMsg`

Cancels previous subscription

# Basic example

	get host

will return you the host where you connect to
	
	connect vitaly, vitaly

connects to FIX engine with user name vitaly and password vitaly

	connectfast

connects to FIX Price engine

	disconnectfast
	disconnect

----

Project requires third party libraries:

## Coco/R compiler generator

Wiki: [http://en.wikipedia.org/wiki/Coco/R](http://en.wikipedia.org/wiki/Coco/R)  
Home page: [http://www.ssw.uni-linz.ac.at/Coco/](http://www.ssw.uni-linz.ac.at/Coco/)  
User manual: [http://www.ssw.uni-linz.ac.at/Coco/Doc/UserManual.pdf](http://www.ssw.uni-linz.ac.at/Coco/Doc/UserManual.pdf)  
Please [download the coco.exe](http://www.ssw.uni-linz.ac.at/Coco/CS/Coco.exe) and place it into ThirdParty\CocoR folder

## QuickFIX

Wiki: [http://en.wikipedia.org/wiki/QuickFIX](http://en.wikipedia.org/wiki/QuickFIX)  
Home page: [http://www.quickfixengine.org/](http://www.quickfixengine.org/)  
Intallation: [http://www.quickfixengine.org/quickfix/doc/html/install.html](http://www.quickfixengine.org/quickfix/doc/html/install.html)  
Place quickfix_net.dll and quickfix_net_messages.dll wrappers into ThirdParty\QuickFix folder

## OpenFAST

Wiki: [http://en.wikipedia.org/wiki/FAST_protocol](http://en.wikipedia.org/wiki/FAST_protocol)  
Home page: [http://www.fixprotocol.org/fast](http://www.fixprotocol.org/fast)  
C# Implementation: [http://sourceforge.net/projects/openfastdotnet/](http://sourceforge.net/projects/openfastdotnet/)  
Place openfast.dll into ThirdParty\OpenFAST folder
