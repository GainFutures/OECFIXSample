
using System;
using System.IO;
using System.Collections;

namespace OEC.FIX.Sample.FoxScript {

public class Token {
	public int kind;    // token kind
	public int pos;     // token position in bytes in the source text (starting at 0)
	public int charPos;  // token position in characters in the source text (starting at 0)
	public int col;     // token column (starting at 1)
	public int line;    // token line (starting at 1)
	public string val;  // token value
	public Token next;  // ML 2005-03-11 Tokens are kept in linked list
}

//-----------------------------------------------------------------------------------
// Buffer
//-----------------------------------------------------------------------------------
public class Buffer {
	// This Buffer supports the following cases:
	// 1) seekable stream (file)
	//    a) whole stream in buffer
	//    b) part of stream in buffer
	// 2) non seekable stream (network, console)

	public const int EOF = char.MaxValue + 1;
	const int MIN_BUFFER_LENGTH = 1024; // 1KB
	const int MAX_BUFFER_LENGTH = MIN_BUFFER_LENGTH * 64; // 64KB
	byte[] buf;         // input buffer
	int bufStart;       // position of first byte in buffer relative to input stream
	int bufLen;         // length of buffer
	int fileLen;        // length of input stream (may change if the stream is no file)
	int bufPos;         // current position in buffer
	Stream stream;      // input stream (seekable)
	bool isUserStream;  // was the stream opened by the user?
	
	public Buffer (Stream s, bool isUserStream) {
		stream = s; this.isUserStream = isUserStream;
		
		if (stream.CanSeek) {
			fileLen = (int) stream.Length;
			bufLen = Math.Min(fileLen, MAX_BUFFER_LENGTH);
			bufStart = Int32.MaxValue; // nothing in the buffer so far
		} else {
			fileLen = bufLen = bufStart = 0;
		}

		buf = new byte[(bufLen>0) ? bufLen : MIN_BUFFER_LENGTH];
		if (fileLen > 0) Pos = 0; // setup buffer to position 0 (start)
		else bufPos = 0; // index 0 is already after the file, thus Pos = 0 is invalid
		if (bufLen == fileLen && stream.CanSeek) Close();
	}
	
	protected Buffer(Buffer b) { // called in UTF8Buffer constructor
		buf = b.buf;
		bufStart = b.bufStart;
		bufLen = b.bufLen;
		fileLen = b.fileLen;
		bufPos = b.bufPos;
		stream = b.stream;
		// keep destructor from closing the stream
		b.stream = null;
		isUserStream = b.isUserStream;
	}

	~Buffer() { Close(); }
	
	protected void Close() {
		if (!isUserStream && stream != null) {
			stream.Close();
			stream = null;
		}
	}
	
	public virtual int Read () {
		if (bufPos < bufLen) {
			return buf[bufPos++];
		} else if (Pos < fileLen) {
			Pos = Pos; // shift buffer start to Pos
			return buf[bufPos++];
		} else if (stream != null && !stream.CanSeek && ReadNextStreamChunk() > 0) {
			return buf[bufPos++];
		} else {
			return EOF;
		}
	}

	public int Peek () {
		int curPos = Pos;
		int ch = Read();
		Pos = curPos;
		return ch;
	}
	
	// beg .. begin, zero-based, inclusive, in byte
	// end .. end, zero-based, exclusive, in byte
	public string GetString (int beg, int end) {
		int len = 0;
		char[] buf = new char[end - beg];
		int oldPos = Pos;
		Pos = beg;
		while (Pos < end) buf[len++] = (char) Read();
		Pos = oldPos;
		return new String(buf, 0, len);
	}

	public int Pos {
		get { return bufPos + bufStart; }
		set {
			if (value >= fileLen && stream != null && !stream.CanSeek) {
				// Wanted position is after buffer and the stream
				// is not seek-able e.g. network or console,
				// thus we have to read the stream manually till
				// the wanted position is in sight.
				while (value >= fileLen && ReadNextStreamChunk() > 0);
			}

			if (value < 0 || value > fileLen) {
				throw new FatalError("buffer out of bounds access, position: " + value);
			}

			if (value >= bufStart && value < bufStart + bufLen) { // already in buffer
				bufPos = value - bufStart;
			} else if (stream != null) { // must be swapped in
				stream.Seek(value, SeekOrigin.Begin);
				bufLen = stream.Read(buf, 0, buf.Length);
				bufStart = value; bufPos = 0;
			} else {
				// set the position to the end of the file, Pos will return fileLen.
				bufPos = fileLen - bufStart;
			}
		}
	}
	
	// Read the next chunk of bytes from the stream, increases the buffer
	// if needed and updates the fields fileLen and bufLen.
	// Returns the number of bytes read.
	private int ReadNextStreamChunk() {
		int free = buf.Length - bufLen;
		if (free == 0) {
			// in the case of a growing input stream
			// we can neither seek in the stream, nor can we
			// foresee the maximum length, thus we must adapt
			// the buffer size on demand.
			byte[] newBuf = new byte[bufLen * 2];
			Array.Copy(buf, newBuf, bufLen);
			buf = newBuf;
			free = bufLen;
		}
		int read = stream.Read(buf, bufLen, free);
		if (read > 0) {
			fileLen = bufLen = (bufLen + read);
			return read;
		}
		// end of stream reached
		return 0;
	}
}

//-----------------------------------------------------------------------------------
// UTF8Buffer
//-----------------------------------------------------------------------------------
public class UTF8Buffer: Buffer {
	public UTF8Buffer(Buffer b): base(b) {}

	public override int Read() {
		int ch;
		do {
			ch = base.Read();
			// until we find a utf8 start (0xxxxxxx or 11xxxxxx)
		} while ((ch >= 128) && ((ch & 0xC0) != 0xC0) && (ch != EOF));
		if (ch < 128 || ch == EOF) {
			// nothing to do, first 127 chars are the same in ascii and utf8
			// 0xxxxxxx or end of file character
		} else if ((ch & 0xF0) == 0xF0) {
			// 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
			int c1 = ch & 0x07; ch = base.Read();
			int c2 = ch & 0x3F; ch = base.Read();
			int c3 = ch & 0x3F; ch = base.Read();
			int c4 = ch & 0x3F;
			ch = (((((c1 << 6) | c2) << 6) | c3) << 6) | c4;
		} else if ((ch & 0xE0) == 0xE0) {
			// 1110xxxx 10xxxxxx 10xxxxxx
			int c1 = ch & 0x0F; ch = base.Read();
			int c2 = ch & 0x3F; ch = base.Read();
			int c3 = ch & 0x3F;
			ch = (((c1 << 6) | c2) << 6) | c3;
		} else if ((ch & 0xC0) == 0xC0) {
			// 110xxxxx 10xxxxxx
			int c1 = ch & 0x1F; ch = base.Read();
			int c2 = ch & 0x3F;
			ch = (c1 << 6) | c2;
		}
		return ch;
	}
}

//-----------------------------------------------------------------------------------
// Scanner
//-----------------------------------------------------------------------------------
public class Scanner {
	const char EOL = '\n';
	const int eofSym = 0; /* pdt */
	const int maxT = 233;
	const int noSym = 233;
	char valCh;       // current input character (for token.val)

	public Buffer buffer; // scanner buffer
	
	Token t;          // current token
	int ch;           // current input character
	int pos;          // byte position of current character
	int charPos;      // position by unicode characters starting with 0
	int col;          // column number of current character
	int line;         // line number of current character
	int oldEols;      // EOLs that appeared in a comment;
	static readonly Hashtable start; // maps first token character to start state

	Token tokens;     // list of tokens already peeked (first token is a dummy)
	Token pt;         // current peek token
	
	char[] tval = new char[128]; // text of current token
	int tlen;         // length of current token
	
	static Scanner() {
		start = new Hashtable(128);
		for (int i = 95; i <= 95; ++i) start[i] = 96;
		for (int i = 103; i <= 111; ++i) start[i] = 96;
		for (int i = 113; i <= 122; ++i) start[i] = 96;
		for (int i = 48; i <= 57; ++i) start[i] = 97;
		for (int i = 97; i <= 101; ++i) start[i] = 98;
		start[102] = 99; 
		start[112] = 100; 
		start[59] = 8; 
		start[44] = 9; 
		start[61] = 101; 
		start[33] = 11; 
		start[60] = 102; 
		start[62] = 103; 
		start[124] = 15; 
		start[38] = 17; 
		start[45] = 104; 
		start[39] = 23; 
		start[91] = 105; 
		start[40] = 140; 
		start[41] = 141; 
		start[123] = 142; 
		start[125] = 143; 
		start[42] = 144; 
		start[58] = 147; 
		start[37] = 146; 
		start[Buffer.EOF] = -1;

	}
	
	public Scanner (string fileName) {
		try {
			Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			buffer = new Buffer(stream, false);
			Init();
		} catch (IOException) {
			throw new FatalError("Cannot open file " + fileName);
		}
	}
	
	public Scanner (Stream s) {
		buffer = new Buffer(s, true);
		Init();
	}
	
	void Init() {
		pos = -1; line = 1; col = 0; charPos = -1;
		oldEols = 0;
		NextCh();
		if (ch == 0xEF) { // check optional byte order mark for UTF-8
			NextCh(); int ch1 = ch;
			NextCh(); int ch2 = ch;
			if (ch1 != 0xBB || ch2 != 0xBF) {
				throw new FatalError(String.Format("illegal byte order mark: EF {0,2:X} {1,2:X}", ch1, ch2));
			}
			buffer = new UTF8Buffer(buffer); col = 0; charPos = -1;
			NextCh();
		}
		pt = tokens = new Token();  // first token is a dummy
	}
	
	void NextCh() {
		if (oldEols > 0) { ch = EOL; oldEols--; } 
		else {
			pos = buffer.Pos;
			// buffer reads unicode chars, if UTF8 has been detected
			ch = buffer.Read(); col++; charPos++;
			// replace isolated '\r' by '\n' in order to make
			// eol handling uniform across Windows, Unix and Mac
			if (ch == '\r' && buffer.Peek() != '\n') ch = EOL;
			if (ch == EOL) { line++; col = 0; }
		}
		if (ch != Buffer.EOF) {
			valCh = (char) ch;
			ch = char.ToLower((char) ch);
		}

	}

	void AddCh() {
		if (tlen >= tval.Length) {
			char[] newBuf = new char[2 * tval.Length];
			Array.Copy(tval, 0, newBuf, 0, tval.Length);
			tval = newBuf;
		}
		if (ch != Buffer.EOF) {
			tval[tlen++] = valCh;
			NextCh();
		}
	}



	bool Comment0() {
		int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
		NextCh();
		if (ch == '*') {
			NextCh();
			for(;;) {
				if (ch == '*') {
					NextCh();
					if (ch == '/') {
						level--;
						if (level == 0) { oldEols = line - line0; NextCh(); return true; }
						NextCh();
					}
				} else if (ch == '/') {
					NextCh();
					if (ch == '*') {
						level++; NextCh();
					}
				} else if (ch == Buffer.EOF) return false;
				else NextCh();
			}
		} else {
			buffer.Pos = pos0; NextCh(); line = line0; col = col0; charPos = charPos0;
		}
		return false;
	}

	bool Comment1() {
		int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
		NextCh();
		if (ch == '/') {
			NextCh();
			for(;;) {
				if (ch == 10) {
					level--;
					if (level == 0) { oldEols = line - line0; NextCh(); return true; }
					NextCh();
				} else if (ch == Buffer.EOF) return false;
				else NextCh();
			}
		} else {
			buffer.Pos = pos0; NextCh(); line = line0; col = col0; charPos = charPos0;
		}
		return false;
	}


	void CheckLiteral() {
		switch (t.val.ToLower()) {
			case "new": t.kind = 7; break;
			case "modify": t.kind = 8; break;
			case "cancel": t.kind = 9; break;
			case "status": t.kind = 10; break;
			case "massstatus": t.kind = 11; break;
			case "delete": t.kind = 12; break;
			case "wait": t.kind = 13; break;
			case "ensure": t.kind = 14; break;
			case "print": t.kind = 15; break;
			case "printf": t.kind = 16; break;
			case "reset": t.kind = 17; break;
			case "set": t.kind = 18; break;
			case "get": t.kind = 19; break;
			case "ping": t.kind = 20; break;
			case "begin": t.kind = 21; break;
			case "end": t.kind = 22; break;
			case "positions": t.kind = 23; break;
			case "balance": t.kind = 24; break;
			case "quit": t.kind = 25; break;
			case "exit": t.kind = 26; break;
			case "stop": t.kind = 27; break;
			case "connect": t.kind = 28; break;
			case "auth": t.kind = 29; break;
			case "disconnect": t.kind = 30; break;
			case "exec": t.kind = 31; break;
			case "test": t.kind = 32; break;
			case "teststat": t.kind = 33; break;
			case "ensureorderstatus": t.kind = 34; break;
			case "ensurepureorderstatus": t.kind = 35; break;
			case "ensuremodifyaccepted": t.kind = 36; break;
			case "ensuretrade": t.kind = 37; break;
			case "sleep": t.kind = 38; break;
			case "anykey": t.kind = 39; break;
			case "contract": t.kind = 40; break;
			case "postallocation": t.kind = 41; break;
			case "post": t.kind = 42; break;
			case "palloc": t.kind = 43; break;
			case "userrequest": t.kind = 44; break;
			case "margincalc": t.kind = 45; break;
			case "heartbeat": t.kind = 46; break;
			case "connectfast": t.kind = 47; break;
			case "disconnectfast": t.kind = 48; break;
			case "cancelsubscribe": t.kind = 49; break;
			case "subscribequotes": t.kind = 50; break;
			case "subscribedom": t.kind = 51; break;
			case "subscribehistogram": t.kind = 52; break;
			case "subscribeticks": t.kind = 53; break;
			case "loadticks": t.kind = 54; break;
			case "true": t.kind = 67; break;
			case "false": t.kind = 68; break;
			case "on": t.kind = 69; break;
			case "off": t.kind = 70; break;
			case "null": t.kind = 71; break;
			case "buy": t.kind = 76; break;
			case "sell": t.kind = 77; break;
			case "put": t.kind = 78; break;
			case "call": t.kind = 79; break;
			case "open": t.kind = 80; break;
			case "close": t.kind = 81; break;
			case "if": t.kind = 82; break;
			case "else": t.kind = 87; break;
			case "msg": t.kind = 88; break;
			case "uuid": t.kind = 89; break;
			case "request": t.kind = 90; break;
			case "req": t.kind = 91; break;
			case "r": t.kind = 92; break;
			case "base": t.kind = 93; break;
			case "b": t.kind = 94; break;
			case "lookup": t.kind = 95; break;
			case "lkp": t.kind = 96; break;
			case "l": t.kind = 97; break;
			case "mode": t.kind = 98; break;
			case "max_records": t.kind = 99; break;
			case "snapshot": t.kind = 100; break;
			case "subscribe": t.kind = 101; break;
			case "unsubscribe": t.kind = 102; break;
			case "updates_only": t.kind = 103; break;
			case "kind": t.kind = 104; break;
			case "type": t.kind = 105; break;
			case "opt_type": t.kind = 106; break;
			case "by_base_contract": t.kind = 107; break;
			case "opt_required": t.kind = 108; break;
			case "base_contract": t.kind = 109; break;
			case "underlying": t.kind = 110; break;
			case "exch": t.kind = 111; break;
			case "cgroup": t.kind = 112; break;
			case "compound_type": t.kind = 113; break;
			case "unknown": t.kind = 114; break;
			case "generic": t.kind = 115; break;
			case "performance_index_basket": t.kind = 116; break;
			case "non_performance_index_basket": t.kind = 117; break;
			case "straddle": t.kind = 118; break;
			case "strangle": t.kind = 119; break;
			case "future_time_spread": t.kind = 120; break;
			case "option_time_spread": t.kind = 121; break;
			case "price_spread": t.kind = 122; break;
			case "synthetic_underlying": t.kind = 123; break;
			case "straddle_time_spread": t.kind = 124; break;
			case "ratio_spread": t.kind = 125; break;
			case "ratio_future_time_spread": t.kind = 126; break;
			case "ratio_option_time_spread": t.kind = 127; break;
			case "put_call_spread": t.kind = 128; break;
			case "ratio_put_call_spread": t.kind = 129; break;
			case "ladder": t.kind = 130; break;
			case "box": t.kind = 131; break;
			case "butterfly": t.kind = 132; break;
			case "condor": t.kind = 133; break;
			case "iron_butterfly": t.kind = 134; break;
			case "diagonal_spread": t.kind = 135; break;
			case "ratio_diagonal_spread": t.kind = 136; break;
			case "straddle_diagonal_spread": t.kind = 137; break;
			case "conversion_reversal": t.kind = 138; break;
			case "covered_option": t.kind = 139; break;
			case "reserved1": t.kind = 140; break;
			case "reserved2": t.kind = 141; break;
			case "currency_future_spread": t.kind = 142; break;
			case "rate_future_spread": t.kind = 143; break;
			case "index_future_spread": t.kind = 144; break;
			case "future_butterfly": t.kind = 145; break;
			case "future_condor": t.kind = 146; break;
			case "strip": t.kind = 147; break;
			case "pack": t.kind = 148; break;
			case "bundle": t.kind = 149; break;
			case "bond_deliverable_basket": t.kind = 150; break;
			case "stock_basket": t.kind = 151; break;
			case "price_spread_vs_option": t.kind = 152; break;
			case "straddle_vs_option": t.kind = 153; break;
			case "bond_spread": t.kind = 154; break;
			case "exchange_spread": t.kind = 155; break;
			case "future_pack_spread": t.kind = 156; break;
			case "future_pack_butterfly": t.kind = 157; break;
			case "whole_sale": t.kind = 158; break;
			case "commodity_spread": t.kind = 159; break;
			case "jelly_roll": t.kind = 160; break;
			case "iron_condor": t.kind = 161; break;
			case "options_strip": t.kind = 162; break;
			case "contingent_orders": t.kind = 163; break;
			case "interproduct_spread": t.kind = 164; break;
			case "pseudo_straddle": t.kind = 165; break;
			case "tailor_made": t.kind = 166; break;
			case "futures_generic": t.kind = 167; break;
			case "options_generic": t.kind = 168; break;
			case "basis_trade": t.kind = 169; break;
			case "futuretime_spread_reduced_tick_size": t.kind = 170; break;
			case "generic_vola_strategy_vs": t.kind = 171; break;
			case "straddle_vola_strategy_vs": t.kind = 172; break;
			case "strangle_vs": t.kind = 173; break;
			case "option_time_spread_vs": t.kind = 174; break;
			case "price_spread_vs": t.kind = 175; break;
			case "ratio_spread_vs": t.kind = 176; break;
			case "put_call_spreadvs": t.kind = 177; break;
			case "ladder_vs": t.kind = 178; break;
			case "price_spread_vs_option_vs": t.kind = 179; break;
			case "collar": t.kind = 180; break;
			case "combo": t.kind = 181; break;
			case "protective_put": t.kind = 182; break;
			case "spread": t.kind = 183; break;
			case "electronic": t.kind = 184; break;
			case "pit": t.kind = 185; break;
			case "future": t.kind = 186; break;
			case "option": t.kind = 187; break;
			case "forex": t.kind = 188; break;
			case "future_compound": t.kind = 189; break;
			case "options_compound": t.kind = 190; break;
			case "any_inclusion": t.kind = 191; break;
			case "symbol_starts_with": t.kind = 192; break;
			case "description_starts_with": t.kind = 193; break;
			case "any_starts_with": t.kind = 194; break;
			case "exact_match": t.kind = 195; break;
			case "seqnum": t.kind = 196; break;
			case "for": t.kind = 197; break;
			case "low_acct_low_price": t.kind = 201; break;
			case "low_acct_high_price": t.kind = 202; break;
			case "high_acct_low_price": t.kind = 203; break;
			case "high_acct_high_price": t.kind = 204; break;
			case "aps": t.kind = 205; break;
			case "ts": t.kind = 206; break;
			case "pre": t.kind = 207; break;
			case "main": t.kind = 208; break;
			case "after": t.kind = 209; break;
			case "p1": t.kind = 210; break;
			case "p2": t.kind = 211; break;
			case "p3": t.kind = 212; break;
			case "day": t.kind = 213; break;
			case "gtc": t.kind = 214; break;
			case "gtd": t.kind = 215; break;
			case "fok": t.kind = 216; break;
			case "ioc": t.kind = 217; break;
			case "mkt": t.kind = 218; break;
			case "moo": t.kind = 219; break;
			case "moc": t.kind = 220; break;
			case "lmt": t.kind = 221; break;
			case "stp": t.kind = 222; break;
			case "ice": t.kind = 223; break;
			case "mit": t.kind = 224; break;
			case "trailing": t.kind = 225; break;
			case "last": t.kind = 226; break;
			case "bid": t.kind = 227; break;
			case "ask": t.kind = 228; break;
			case "full": t.kind = 230; break;
			case "from": t.kind = 231; break;
			case "to": t.kind = 232; break;
			default: break;
		}
	}

	Token NextToken() {
		while (ch == ' ' ||
			ch >= 9 && ch <= 10 || ch == 13
		) NextCh();
		if (ch == '/' && Comment0() ||ch == '/' && Comment1()) return NextToken();
		int recKind = noSym;
		int recEnd = pos;
		t = new Token();
		t.pos = pos; t.col = col; t.line = line; t.charPos = charPos;
		int state;
		if (start.ContainsKey(ch)) { state = (int) start[ch]; }
		else { state = 0; }
		tlen = 0; AddCh();
		
		switch (state) {
			case -1: { t.kind = eofSym; break; } // NextCh already done
			case 0: {
				if (recKind != noSym) {
					tlen = recEnd - t.pos;
					SetScannerBehindT();
				}
				t.kind = recKind; break;
			} // NextCh already done
			case 1:
				if (ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 2;}
				else if (ch >= '0' && ch <= '9') {AddCh(); goto case 3;}
				else {goto case 0;}
			case 2:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 2;}
				else {t.kind = 2; break;}
			case 3:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 3;}
				else {t.kind = 2; break;}
			case 4:
				if (ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 5;}
				else {goto case 0;}
			case 5:
				recEnd = pos; recKind = 3;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 5;}
				else {t.kind = 3; break;}
			case 6:
				if (ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 7;}
				else {goto case 0;}
			case 7:
				recEnd = pos; recKind = 4;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 7;}
				else {t.kind = 4; break;}
			case 8:
				{t.kind = 5; break;}
			case 9:
				{t.kind = 6; break;}
			case 10:
				{t.kind = 56; break;}
			case 11:
				if (ch == '=') {AddCh(); goto case 12;}
				else {goto case 0;}
			case 12:
				{t.kind = 57; break;}
			case 13:
				{t.kind = 59; break;}
			case 14:
				{t.kind = 61; break;}
			case 15:
				if (ch == '|') {AddCh(); goto case 16;}
				else {goto case 0;}
			case 16:
				{t.kind = 62; break;}
			case 17:
				if (ch == '&') {AddCh(); goto case 18;}
				else {goto case 0;}
			case 18:
				{t.kind = 63; break;}
			case 19:
				recEnd = pos; recKind = 65;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 19;}
				else if (ch == 'e') {AddCh(); goto case 20;}
				else {t.kind = 65; break;}
			case 20:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 22;}
				else if (ch == '+' || ch == '-') {AddCh(); goto case 21;}
				else {goto case 0;}
			case 21:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 22;}
				else {goto case 0;}
			case 22:
				recEnd = pos; recKind = 65;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 22;}
				else {t.kind = 65; break;}
			case 23:
				if (ch <= '&' || ch >= '(' && ch <= 65535) {AddCh(); goto case 23;}
				else if (ch == 39) {AddCh(); goto case 24;}
				else {goto case 0;}
			case 24:
				{t.kind = 66; break;}
			case 25:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 26;}
				else {goto case 0;}
			case 26:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 27;}
				else {goto case 0;}
			case 27:
				if (ch == ':') {AddCh(); goto case 28;}
				else {goto case 0;}
			case 28:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 29;}
				else {goto case 0;}
			case 29:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 30;}
				else {goto case 0;}
			case 30:
				if (ch == ']') {AddCh(); goto case 35;}
				else if (ch == ':') {AddCh(); goto case 32;}
				else {goto case 0;}
			case 31:
				if (ch == ']') {AddCh(); goto case 35;}
				else {goto case 0;}
			case 32:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 33;}
				else {goto case 0;}
			case 33:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 31;}
				else {goto case 0;}
			case 34:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 34;}
				else if (ch == '.') {AddCh(); goto case 25;}
				else {goto case 0;}
			case 35:
				{t.kind = 72; break;}
			case 36:
				if (ch == ']') {AddCh(); goto case 62;}
				else {goto case 0;}
			case 37:
				if (ch == 'n') {AddCh(); goto case 38;}
				else {goto case 0;}
			case 38:
				if (ch == 'o') {AddCh(); goto case 39;}
				else {goto case 0;}
			case 39:
				if (ch == 'w') {AddCh(); goto case 40;}
				else {goto case 0;}
			case 40:
				if (ch == ']') {AddCh(); goto case 62;}
				else if (ch == '+' || ch == '-') {AddCh(); goto case 41;}
				else {goto case 0;}
			case 41:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 42;}
				else {goto case 0;}
			case 42:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 43;}
				else {goto case 0;}
			case 43:
				if (ch == ':') {AddCh(); goto case 44;}
				else {goto case 0;}
			case 44:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 45;}
				else {goto case 0;}
			case 45:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 46;}
				else {goto case 0;}
			case 46:
				if (ch == ']') {AddCh(); goto case 62;}
				else if (ch == ':') {AddCh(); goto case 47;}
				else {goto case 0;}
			case 47:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 48;}
				else {goto case 0;}
			case 48:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 36;}
				else {goto case 0;}
			case 49:
				if (ch == 't') {AddCh(); goto case 50;}
				else {goto case 0;}
			case 50:
				if (ch == 'c') {AddCh(); goto case 37;}
				else {goto case 0;}
			case 51:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 52;}
				else {goto case 0;}
			case 52:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 53;}
				else {goto case 0;}
			case 53:
				if (ch == ':') {AddCh(); goto case 54;}
				else {goto case 0;}
			case 54:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 55;}
				else {goto case 0;}
			case 55:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 56;}
				else {goto case 0;}
			case 56:
				if (ch == ']') {AddCh(); goto case 62;}
				else if (ch == 'u') {AddCh(); goto case 58;}
				else if (ch == ':') {AddCh(); goto case 60;}
				else {goto case 0;}
			case 57:
				if (ch == ']') {AddCh(); goto case 62;}
				else if (ch == 'u') {AddCh(); goto case 58;}
				else {goto case 0;}
			case 58:
				if (ch == 't') {AddCh(); goto case 59;}
				else {goto case 0;}
			case 59:
				if (ch == 'c') {AddCh(); goto case 36;}
				else {goto case 0;}
			case 60:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 61;}
				else {goto case 0;}
			case 61:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 57;}
				else {goto case 0;}
			case 62:
				{t.kind = 73; break;}
			case 63:
				{t.kind = 74; break;}
			case 64:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 89;}
				else if (ch == '-') {AddCh(); goto case 65;}
				else {goto case 0;}
			case 65:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 86;}
				else if (ch == '-') {AddCh(); goto case 66;}
				else {goto case 0;}
			case 66:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 83;}
				else if (ch == '-') {AddCh(); goto case 67;}
				else {goto case 0;}
			case 67:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 80;}
				else if (ch == '-') {AddCh(); goto case 68;}
				else {goto case 0;}
			case 68:
				recEnd = pos; recKind = 75;
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 69;}
				else {t.kind = 75; break;}
			case 69:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 70;}
				else {goto case 0;}
			case 70:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 71;}
				else {goto case 0;}
			case 71:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 72;}
				else {goto case 0;}
			case 72:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 73;}
				else {goto case 0;}
			case 73:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 74;}
				else {goto case 0;}
			case 74:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 75;}
				else {goto case 0;}
			case 75:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 76;}
				else {goto case 0;}
			case 76:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 77;}
				else {goto case 0;}
			case 77:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 78;}
				else {goto case 0;}
			case 78:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 79;}
				else {goto case 0;}
			case 79:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 68;}
				else {goto case 0;}
			case 80:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 81;}
				else {goto case 0;}
			case 81:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 82;}
				else {goto case 0;}
			case 82:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 67;}
				else {goto case 0;}
			case 83:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 84;}
				else {goto case 0;}
			case 84:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 85;}
				else {goto case 0;}
			case 85:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 66;}
				else {goto case 0;}
			case 86:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 87;}
				else {goto case 0;}
			case 87:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 88;}
				else {goto case 0;}
			case 88:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 65;}
				else {goto case 0;}
			case 89:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 90;}
				else {goto case 0;}
			case 90:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 91;}
				else {goto case 0;}
			case 91:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 92;}
				else {goto case 0;}
			case 92:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 93;}
				else {goto case 0;}
			case 93:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 94;}
				else {goto case 0;}
			case 94:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 95;}
				else {goto case 0;}
			case 95:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 64;}
				else {goto case 0;}
			case 96:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 97:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 106;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 90;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 98:
				recEnd = pos; recKind = 1;
				if (ch == '_' || ch >= 'g' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 107;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 99:
				recEnd = pos; recKind = 1;
				if (ch == '_' || ch >= 'g' && ch <= 'h' || ch >= 'j' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 107;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else if (ch == 'i') {AddCh(); goto case 108;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 100:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'q' || ch >= 's' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else if (ch == 'r') {AddCh(); goto case 109;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 101:
				recEnd = pos; recKind = 55;
				if (ch == '=') {AddCh(); goto case 10;}
				else {t.kind = 55; break;}
			case 102:
				recEnd = pos; recKind = 58;
				if (ch == '=') {AddCh(); goto case 13;}
				else {t.kind = 58; break;}
			case 103:
				recEnd = pos; recKind = 60;
				if (ch == '=') {AddCh(); goto case 14;}
				else {t.kind = 60; break;}
			case 104:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 110;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 86;}
				else if (ch == '-') {AddCh(); goto case 66;}
				else {goto case 0;}
			case 105:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 111;}
				else if (ch == 'n') {AddCh(); goto case 38;}
				else if (ch == 'u') {AddCh(); goto case 49;}
				else {goto case 0;}
			case 106:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 112;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 91;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 107:
				recEnd = pos; recKind = 1;
				if (ch == '_' || ch >= 'g' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 113;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 108:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'w' || ch >= 'y' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else if (ch == 'x') {AddCh(); goto case 114;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 109:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'n' || ch >= 'p' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else if (ch == 'o') {AddCh(); goto case 115;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 110:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 116;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 87;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 111:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 117;}
				else if (ch == '.') {AddCh(); goto case 25;}
				else {goto case 0;}
			case 112:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 118;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 92;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 113:
				recEnd = pos; recKind = 1;
				if (ch == '_' || ch >= 'g' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 119;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 114:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch == '.') {AddCh(); goto case 120;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 115:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'o' || ch >= 'q' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else if (ch == 'p') {AddCh(); goto case 121;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 116:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 122;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 88;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 117:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 123;}
				else if (ch == ':') {AddCh(); goto case 28;}
				else if (ch == '.') {AddCh(); goto case 25;}
				else {goto case 0;}
			case 118:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 124;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 93;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 119:
				recEnd = pos; recKind = 1;
				if (ch == '_' || ch >= 'g' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 125;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 120:
				if (ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 126;}
				else if (ch >= '0' && ch <= '9') {AddCh(); goto case 127;}
				else {goto case 0;}
			case 121:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else if (ch == ':') {AddCh(); goto case 6;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 122:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 128;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 65;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 123:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 129;}
				else if (ch == '.') {AddCh(); goto case 25;}
				else {goto case 0;}
			case 124:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 130;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 94;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 125:
				recEnd = pos; recKind = 1;
				if (ch == '_' || ch >= 'g' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 131;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 126:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 126;}
				else if (ch == '.') {AddCh(); goto case 4;}
				else {t.kind = 2; break;}
			case 127:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 127;}
				else if (ch == '.') {AddCh(); goto case 4;}
				else {t.kind = 2; break;}
			case 128:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 110;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 86;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else if (ch == '-') {AddCh(); goto case 66;}
				else {t.kind = 64; break;}
			case 129:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 132;}
				else if (ch == '.') {AddCh(); goto case 25;}
				else {goto case 0;}
			case 130:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 133;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 95;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 131:
				recEnd = pos; recKind = 1;
				if (ch == '_' || ch >= 'g' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 134;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 132:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 135;}
				else if (ch == '.') {AddCh(); goto case 25;}
				else {goto case 0;}
			case 133:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 136;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 64;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else {t.kind = 64; break;}
			case 134:
				recEnd = pos; recKind = 1;
				if (ch == '_' || ch >= 'g' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 137;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 135:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 138;}
				else if (ch == '.') {AddCh(); goto case 25;}
				else {goto case 0;}
			case 136:
				recEnd = pos; recKind = 64;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 97;}
				else if (ch >= 'a' && ch <= 'f') {AddCh(); goto case 89;}
				else if (ch == '.') {AddCh(); goto case 19;}
				else if (ch == '-') {AddCh(); goto case 65;}
				else {t.kind = 64; break;}
			case 137:
				recEnd = pos; recKind = 1;
				if (ch == '_' || ch >= 'g' && ch <= 'z') {AddCh(); goto case 96;}
				else if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 98;}
				else if (ch == '.') {AddCh(); goto case 1;}
				else if (ch == '-') {AddCh(); goto case 65;}
				else {t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t;}
			case 138:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 139;}
				else if (ch == '.') {AddCh(); goto case 25;}
				else {goto case 0;}
			case 139:
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 34;}
				else if (ch == '.') {AddCh(); goto case 25;}
				else if (ch == '-') {AddCh(); goto case 51;}
				else if (ch == ']') {AddCh(); goto case 63;}
				else {goto case 0;}
			case 140:
				{t.kind = 83; break;}
			case 141:
				{t.kind = 84; break;}
			case 142:
				{t.kind = 85; break;}
			case 143:
				{t.kind = 86; break;}
			case 144:
				{t.kind = 198; break;}
			case 145:
				{t.kind = 200; break;}
			case 146:
				{t.kind = 229; break;}
			case 147:
				recEnd = pos; recKind = 199;
				if (ch == ':') {AddCh(); goto case 145;}
				else {t.kind = 199; break;}

		}
		t.val = new String(tval, 0, tlen);
		return t;
	}
	
	private void SetScannerBehindT() {
		buffer.Pos = t.pos;
		NextCh();
		line = t.line; col = t.col; charPos = t.charPos;
		for (int i = 0; i < tlen; i++) NextCh();
	}
	
	// get the next token (possibly a token already seen during peeking)
	public Token Scan () {
		if (tokens.next == null) {
			return NextToken();
		} else {
			pt = tokens = tokens.next;
			return tokens;
		}
	}

	// peek for the next token, ignore pragmas
	public Token Peek () {
		do {
			if (pt.next == null) {
				pt.next = NextToken();
			}
			pt = pt.next;
		} while (pt.kind > maxT); // skip pragmas
	
		return pt;
	}

	// make sure that peeking starts at the current scan position
	public void ResetPeek () { pt = tokens; }

} // end Scanner
}