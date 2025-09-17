using BMTP3.Common.MessageFormatterParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser;

// Represents a single token
public class Token {
	public TokenType Type { get; }
	public string Value { get; }
	public int Position { get; }

	public Token(TokenType type, string value, int position) {
		Type = type;
		Value = value;
		Position = position;
	}

	public Token(TokenType type, char value, int position) :this(type, value.ToString(), position) {

	}

	public override string ToString() => $"[{Type}: '{Value}' @ {Position}]";
}