using BMTP3.Common.MessageFormatterParser.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	public class Parser2 {
		private readonly Lexer2 _lexer;
		private Token _currentToken;

		public Parser2(Lexer2 lexer) {
			_lexer = lexer;
			_currentToken = _lexer.NextToken(); // Henter den første token
		}

		private void Eat(TokenType type) {
			if(_currentToken.Type != type) {
				throw new InvalidOperationException($"Expected token type {type}, but got {_currentToken.Type}");
			}
			_currentToken = _lexer.NextToken(); // Forbruger tokenet og henter det næste
		}

		public AstNode Parse() {
			/*
			var rootNode = new MessageExpressionNode();
			while(_currentToken.Type != TokenType.EOF) {
				if(_currentToken.Type == TokenType.DollarBraceOpen || _currentToken.Type == TokenType.HashBraceOpen) {
					rootNode.Parts.Add(ParsePlaceholderExpression());
				} else if(_currentToken.Type == TokenType.LiteralString) {
					rootNode.Parts.Add(ParseLiteral());
				} else {
					// Håndterer andre tokens, der ikke er udtryk, som f.eks. uventede tegn
					throw new InvalidOperationException($"Unexpected token {_currentToken.Type}");
				}
			}
			return rootNode;
			*/
			throw new NotImplementedException();
		}

		private AstNode ParsePlaceholderExpression() {
			/*
			// Håndterer både ${...} og #{...}
			var token = _currentToken;
			Eat(token.Type);

			var placeholder = new PlaceholderNode();
			if(token.Type == TokenType.DollarBraceOpen) {
				placeholder.Name = _currentToken.Value;
				Eat(TokenType.Identifier);
			} else {
				placeholder.Index = int.Parse(_currentToken.Value);
				Eat(TokenType.LiteralInteger);
			}

			// Håndterer kædede funktionskald (.func1().func2())
			while(_currentToken.Type == TokenType.Dot) {
				placeholder.Functions.Add(ParseFunctionCall());
			}

			// Håndterer evalueringsudtryk (§ if, ...)
			if(_currentToken.Type == TokenType.Section) {
				placeholder.EvalExpression = ParseEvalExpression();
			}

			Eat(TokenType.BraceClose);
			return placeholder;
			*/
			throw new NotImplementedException();
		}

		private FunctionCallNode ParseFunctionCall() {
			/*
			Eat(TokenType.Dot);
			var function = new FunctionCallNode { Name = _currentToken.Value };
			Eat(TokenType.Identifier);
			Eat(TokenType.ParenOpen);

			// Håndterer argumenter til funktionen
			while(_currentToken.Type != TokenType.ParenClose) {
				function.Arguments.Add(ParseLiteral()); // Simplificeret til kun at parse literaler
				if(_currentToken.Type == TokenType.Comma) {
					Eat(TokenType.Comma);
				}
			}
			Eat(TokenType.ParenClose);

			return function;
			*/
			throw new NotImplementedException();
		}

		private AstNode ParseEvalExpression() {
			Eat(TokenType.Section);
			var evalType = _currentToken.Value;
			Eat(TokenType.Identifier); // For evalType (f.eks. "if")
			Eat(TokenType.Comma);

			if(evalType == "if") {
				return ParseIfExpression();
			}
			// TODO: Implementere logik for `plural` og `select`
			throw new NotSupportedException($"Eval type '{evalType}' is not supported yet.");
		}

		private AstNode ParseIfExpression() {
			/*
			var ifNode = new IfExpressionNode();

			// Håndterer 'condition' i 'if,condition?true:false'
			ifNode.ConditionOperator = _currentToken.Value;
			Eat(TokenType.Identifier);
			ifNode.ConditionValue = ParseLiteral();

			Eat(TokenType.QuestionMark);
			ifNode.TrueValue = ParseEvalString(); // Parse en streng, der potentielt indeholder indlejrede pladsholdere
			Eat(TokenType.Colon);
			ifNode.FalseValue = ParseEvalString();

			return ifNode;
			*/
			throw new NotImplementedException();
		}

		// Denne metode kan genbruges til at parse string-literaler, der kan indeholde indlejrede pladsholdere
		private AstNode ParseEvalString() {
			/*
			var stringNode = new MessageExpressionNode();
			while(_currentToken.Type != TokenType.Colon && _currentToken.Type != TokenType.BraceClose) {
				if(_currentToken.Type == TokenType.LiteralString) {
					stringNode.Parts.Add(ParseLiteral());
				} else if(_currentToken.Type == TokenType.DollarBraceOpen || _currentToken.Type == TokenType.HashBraceOpen) {
					stringNode.Parts.Add(ParsePlaceholderExpression());
				} else {
					throw new InvalidOperationException($"Unexpected token in eval string: {_currentToken.Type}");
				}
			}
			return stringNode;
			*/
			throw new NotImplementedException();
		}

		private LiteralNode ParseLiteral() {
			/*
			var token = _currentToken;
			if(token.Type == TokenType.LiteralString) {
				Eat(TokenType.LiteralString);
				return new LiteralNode { Value = token.Value };
			} else if(token.Type == TokenType.LiteralInteger) {
				Eat(TokenType.LiteralInteger);
				return new LiteralNode { Value = token.Value };
			}
			// Tilføj håndtering for andre literaler (f.eks. doubles) her.
			throw new InvalidOperationException($"Expected a literal, but got {_currentToken.Type}");
			*/
			throw new NotImplementedException();
		}
	}
}
