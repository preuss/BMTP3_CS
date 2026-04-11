using System.Text;
using BMTP3.Common.MessageFormatterParser.Nodes;

namespace BMTP3.Common.MessageFormatterParser;

public class Parser2
{
	private readonly Lexer2 _lexer;
	private Token _currentToken;

	public Parser2(Lexer2 lexer)
	{
		_lexer = lexer;
		_currentToken = _lexer.NextToken();
	}

	private void Eat(TokenType type)
	{
		if (_currentToken.Type != type)
		{
			throw new InvalidOperationException(
				$"Expected token type {type}, but got {_currentToken.Type} ('{_currentToken.Value}') at column {_currentToken.Column}");
		}

		_currentToken = _lexer.NextToken();
	}

	public RootNode Parse()
	{
		RootNode rootNode = new();
		while (_currentToken.Type != TokenType.EOF)
		{
			if (_currentToken.Type == TokenType.DollarBraceOpen || _currentToken.Type == TokenType.HashBraceOpen)
			{
				rootNode.Children.Add(ParsePlaceholderExpression());
			}
			else if (_currentToken.Type == TokenType.LiteralString)
			{
				rootNode.Children.Add(ParseLiteral());
			}
			else
			{
				// If we hit something unexpected at root level (like a random brace), treat it as text or fail.
				// Since lexer treats unknown things as text usually, this might be a logic error.
				throw new InvalidOperationException($"Unexpected token {_currentToken.Type} at root");
			}
		}

		return rootNode;
	}

	private PlaceholderNode ParsePlaceholderExpression()
	{
		Token token = _currentToken;
		bool isNamed = token.Type == TokenType.DollarBraceOpen;
		Eat(token.Type); // Eat ${ or #{

		string nameOrIndex;
		if (isNamed)
		{
			nameOrIndex = _currentToken.Value;
			Eat(TokenType.Identifier);
		}
		else
		{
			nameOrIndex = _currentToken.Value;
			Eat(TokenType.LiteralInteger);
		}

		// Create the node based on type (though Evaluator seems to treat them similarly via base class)
		// Note: We are constructing the base class directly or a concrete one. 
		// Since NamedPlaceholderNode logic seems broken/incomplete in the file dump, 
		// we will use a concrete GenericPlaceholder implementation or just rely on fixing the classes later.
		// For now, let's assume we have a concrete way to instantiate.
		// I'll create a 'ConcretePlaceholderNode' internally or just use NamedPlaceholderNode and fix it later.

		List<FunctionCallNode> functions = new();
		while (_currentToken.Type == TokenType.Dot)
		{
			functions.Add(ParseFunctionCall());
		}

		// Pattern (e.g. :yyyy)
		List<AstNode>? pattern = null;
		if (_currentToken.Type == TokenType.Colon)
		{
			Eat(TokenType.Colon);
			pattern = ParsePattern();
		}

		// Eval/Condition (e.g. §if,...)
		IfConditionNode? condition = null;
		if (_currentToken.Type == TokenType.Section)
		{
			// Parse eval/if expression and attach to placeholder
			condition = ParseEvalExpression() as IfConditionNode;
		}

		// Be tolerant: some lexer/tokenization edge-cases may leave us at EOF instead of a BraceClose.
		if (_currentToken.Type == TokenType.BraceClose)
		{
			Eat(TokenType.BraceClose);
		}
		else if (_currentToken.Type == TokenType.EOF)
		{
			// Recover: treat as if we had a closing brace and continue.
		}
		else
		{
			Eat(TokenType.BraceClose); // will throw with proper message
		}

		// We need a concrete instance. I will fix Nodes later, but for now let's instantiate a helper class 
		// or assume we can use NamedPlaceholderNode if I fix it.
		// Let's try to use a new simple class to avoid the inheritance mess I saw.
		return new ConcretePlaceholderNode(nameOrIndex, functions, pattern, condition);
	}

	private List<AstNode> ParsePattern()
	{
		List<AstNode> parts = new();
		// Pattern can be a mix of literals and other placeholders if the lexer supports it.
		// The lexer state 'InsidePatternLiteral' returns LiteralPattern tokens.

		if (_currentToken.Type == TokenType.LiteralPattern)
		{
			parts.Add(new LiteralNode(_currentToken.Value));
			Eat(TokenType.LiteralPattern);
		}

		return parts;
	}

	private FunctionCallNode ParseFunctionCall()
	{
		Eat(TokenType.Dot);
		string name = _currentToken.Value;
		Eat(TokenType.Identifier);

		List<AstNode> args = new();
		if (_currentToken.Type == TokenType.ParenOpen)
		{
			Eat(TokenType.ParenOpen);
			while (_currentToken.Type != TokenType.ParenClose)
			{
				args.Add(ParseLiteral());
				if (_currentToken.Type == TokenType.Comma)
				{
					Eat(TokenType.Comma);
				}
			}

			Eat(TokenType.ParenClose);
		}

		return new FunctionCallNode(name, args);
	}

	private AstNode ParseEvalExpression()
	{
		// We expect we are at Section token (consumed by caller); next tokens: Identifier (eval type), Comma, then pattern
		Eat(TokenType.Section);
		string evalType;
		if (_currentToken.Type == TokenType.Identifier || _currentToken.Type == TokenType.LiteralString)
		{
			evalType = _currentToken.Value;
			_currentToken = _lexer.NextToken();
		}
		else
		{
			throw new InvalidOperationException($"Expected eval type identifier, but got {_currentToken.Type}");
		}

		// optional comma
		if (_currentToken.Type == TokenType.Comma)
		{
			Eat(TokenType.Comma);
		}

		if (evalType == "if")
		{
			// Delegate to the full if-expression parser which handles sequences and nested placeholders
			return ParseIfExpression();
		}

		throw new NotSupportedException($"Eval type '{evalType}' is not supported yet.");
	}

	private AstNode ParseIfExpression()
	{
		// Minimal implementation: parse an if expression into an IfConditionNode
		// Expect current token to be the operator identifier
		if (_currentToken.Type != TokenType.Identifier)
		{
			throw new InvalidOperationException($"Expected condition operator, got {_currentToken.Type}");
		}

		Token condOpToken = _currentToken;
		// condition operator may be provided as Identifier or as a combined token, accept both
		if (_currentToken.Type == TokenType.Identifier)
		{
			Eat(TokenType.Identifier);
		}
		else if (_currentToken.Type == TokenType.LiteralString)
		{
			// if the lexer returned the whole 'if,eq0' as LiteralString earlier, split it
			string[] parts = _currentToken.Value.Split(new[] { ',' }, 2);
			if (parts.Length > 1)
			{
				// advance token stream manually: treat the remainder as upcoming tokens by injecting via lexer is complex,
				// so we will create condOpToken from the split and continue; parser will rely on subsequent scanning for branches.
				condOpToken = new Token(TokenType.Identifier, parts[0], _currentToken.Position, _currentToken.Line,
					_currentToken.Column);
				// mutate current token to be the remainder so subsequent parsing sees it as LiteralString
				_currentToken = new Token(TokenType.LiteralString, parts[1],
					_currentToken.Position + parts[0].Length + 1, _currentToken.Line,
					_currentToken.Column + parts[0].Length + 1);
			}
			else
			{
				Eat(TokenType.LiteralString);
			}
		}
		else
		{
			throw new InvalidOperationException($"Expected condition operator, got {_currentToken.Type}");
		}

		List<AstNode> condParams = new();
		// optional integer parameter(s)
		while (_currentToken.Type == TokenType.LiteralInteger)
		{
			condParams.Add(ParseLiteral());
		}

		// question mark
		Eat(TokenType.QuestionMark);

		List<AstNode> trueParts = new();
		while (_currentToken.Type != TokenType.Colon && _currentToken.Type != TokenType.BraceClose &&
		       _currentToken.Type != TokenType.EOF)
		{
			if (_currentToken.Type == TokenType.LiteralString || _currentToken.Type == TokenType.LiteralInteger ||
			    _currentToken.Type == TokenType.Identifier)
			{
				// Consume a run of adjacent identifier/literal tokens and merge into a single TextNode to preserve spaces
				StringBuilder sb = new();
				bool first = true;
				while (_currentToken.Type == TokenType.LiteralString ||
				       _currentToken.Type == TokenType.LiteralInteger || _currentToken.Type == TokenType.Identifier)
				{
					if (!first)
					{
						sb.Append(' ');
					}

					first = false;
					if (_currentToken.Type == TokenType.Identifier || _currentToken.Type == TokenType.LiteralString)
					{
						sb.Append(_currentToken.Value);
						_currentToken = _lexer.NextToken();
					}
					else
					{
						sb.Append(_currentToken.Value);
						_currentToken = _lexer.NextToken();
					}
				}

				string textVal = sb.ToString();
				// If previous element is a placeholder and this text doesn't start with whitespace, insert a space
				if (trueParts.Count > 0 && trueParts[^1] is PlaceholderNode && textVal.Length > 0 &&
				    !char.IsWhiteSpace(textVal[0]))
				{
					textVal = " " + textVal;
				}

				trueParts.Add(new TextNode(textVal));
			}
			else if (_currentToken.Type == TokenType.DollarBraceOpen || _currentToken.Type == TokenType.HashBraceOpen)
			{
				trueParts.Add(ParsePlaceholderExpression());
			}
			else
			{
				break;
			}
		}

		Eat(TokenType.Colon);

		List<AstNode> falseParts = new();
		while (_currentToken.Type != TokenType.BraceClose && _currentToken.Type != TokenType.EOF)
		{
			if (_currentToken.Type == TokenType.LiteralString || _currentToken.Type == TokenType.LiteralInteger ||
			    _currentToken.Type == TokenType.Identifier)
			{
				// Merge adjacent tokens into one text node
				StringBuilder sb = new();
				bool first = true;
				while (_currentToken.Type == TokenType.LiteralString ||
				       _currentToken.Type == TokenType.LiteralInteger || _currentToken.Type == TokenType.Identifier)
				{
					if (!first)
					{
						sb.Append(' ');
					}

					first = false;
					sb.Append(_currentToken.Value);
					_currentToken = _lexer.NextToken();
				}

				string textValF = sb.ToString();
				if (falseParts.Count > 0 && falseParts[^1] is PlaceholderNode && textValF.Length > 0 &&
				    !char.IsWhiteSpace(textValF[0]))
				{
					textValF = " " + textValF;
				}

				falseParts.Add(new TextNode(textValF));
			}
			else if (_currentToken.Type == TokenType.DollarBraceOpen || _currentToken.Type == TokenType.HashBraceOpen)
			{
				falseParts.Add(ParsePlaceholderExpression());
			}
			else
			{
				break;
			}
		}

		TextNode condOpNode = new(condOpToken.Value);
		return new IfConditionNode(condOpNode, condParams, trueParts, falseParts);
	}

	// Parse a sequence of literal and placeholder nodes until a colon or closing brace.
	// This can be reused for eval/string branches that allow nested placeholders.
	private AstNode ParseEvalString()
	{
		// Parse a sequence of literal and placeholder nodes until a colon or closing brace
		RootNode root = new();
		while (_currentToken.Type != TokenType.Colon && _currentToken.Type != TokenType.BraceClose &&
		       _currentToken.Type != TokenType.EOF)
		{
			if (_currentToken.Type == TokenType.LiteralString || _currentToken.Type == TokenType.LiteralInteger)
			{
				root.Children.Add(ParseLiteral());
			}
			else if (_currentToken.Type == TokenType.DollarBraceOpen || _currentToken.Type == TokenType.HashBraceOpen)
			{
				root.Children.Add(ParsePlaceholderExpression());
			}
			else
			{
				throw new InvalidOperationException($"Unexpected token in eval string: {_currentToken.Type}");
			}
		}

		return root;
	}

	private AstNode ParseLiteral()
	{
		Token token = _currentToken;
		if (token.Type == TokenType.LiteralString)
		{
			Eat(TokenType.LiteralString);
			// TODO: Differentiate between TextNode and LiteralNode if needed
			return new TextNode(token.Value); // Using TextNode for root strings
		}

		if (token.Type == TokenType.LiteralInteger)
		{
			Eat(TokenType.LiteralInteger);
			return new LiteralNode(token.Value); // Helper, maybe separate TextNode and LiteralNode logic
		}

		throw new InvalidOperationException($"Expected literal, got {token.Type}");
	}
}