using BMTP3.Common.MessageFormatterParser.Nodes;
using System;
using System.Collections.Generic;

namespace BMTP3.Common.MessageFormatterParser;

public class Parser2 {
    private readonly Lexer2 _lexer;
    private Token _currentToken;

    public Parser2(Lexer2 lexer) {
        _lexer = lexer;
        _currentToken = _lexer.NextToken();
    }

    private void Eat(TokenType type) {
        if (_currentToken.Type != type) {
            throw new InvalidOperationException($"Expected token type {type}, but got {_currentToken.Type} ('{_currentToken.Value}') at column {_currentToken.Column}");
        }
        _currentToken = _lexer.NextToken();
    }

    public RootNode Parse() {
        var rootNode = new RootNode();
        while (_currentToken.Type != TokenType.EOF) {
            if (_currentToken.Type == TokenType.DollarBraceOpen || _currentToken.Type == TokenType.IndexBraceOpen || _currentToken.Type == TokenType.HashBraceOpen) {
                rootNode.Children.Add(ParsePlaceholderExpression());
            } else if (_currentToken.Type == TokenType.LiteralString) {
                rootNode.Children.Add(ParseLiteral());
            } else {
                // If we hit something unexpected at root level (like a random brace), treat it as text or fail.
                // Since lexer treats unknown things as text usually, this might be a logic error.
                throw new InvalidOperationException($"Unexpected token {_currentToken.Type} at root");
            }
        }
        return rootNode;
    }

    private PlaceholderNode ParsePlaceholderExpression() {
        var token = _currentToken;
        bool isNamed = (token.Type == TokenType.DollarBraceOpen);
        Eat(token.Type); // Eat ${ or #{

        string nameOrIndex;
        if (isNamed) {
            nameOrIndex = _currentToken.Value;
            Eat(TokenType.Identifier);
        } else {
            nameOrIndex = _currentToken.Value;
            Eat(TokenType.LiteralInteger);
        }

        // Create the node based on type (though Evaluator seems to treat them similarly via base class)
        // Note: We are constructing the base class directly or a concrete one. 
        // Since NamedPlaceholderNode logic seems broken/incomplete in the file dump, 
        // we will use a concrete GenericPlaceholder implementation or just rely on fixing the classes later.
        // For now, let's assume we have a concrete way to instantiate.
        // I'll create a 'ConcretePlaceholderNode' internally or just use NamedPlaceholderNode and fix it later.
        
        var functions = new List<FunctionCallNode>();
        while (_currentToken.Type == TokenType.Dot) {
            functions.Add(ParseFunctionCall());
        }

        // Pattern (e.g. :yyyy)
        List<AstNode>? pattern = null;
        if (_currentToken.Type == TokenType.Colon) {
            Eat(TokenType.Colon);
            pattern = ParsePattern();
        }

        // Eval/Condition (e.g. §if,...)
        IfConditionNode? condition = null;
        if (_currentToken.Type == TokenType.Section) {
             // Parse condition logic if implemented
             // condition = ParseIfCondition(); 
             // Skipping complex condition parsing for now to ensure basic functionality first
             Eat(TokenType.Section);
             // Eat until close brace for now if we don't support it fully yet
             while (_currentToken.Type != TokenType.BraceClose && _currentToken.Type != TokenType.EOF) {
                 _currentToken = _lexer.NextToken();
             }
        }

        Eat(TokenType.BraceClose);

        // We need a concrete instance. I will fix Nodes later, but for now let's instantiate a helper class 
        // or assume we can use NamedPlaceholderNode if I fix it.
        // Let's try to use a new simple class to avoid the inheritance mess I saw.
        return new ConcretePlaceholderNode(nameOrIndex, functions, pattern, condition);
    }

    private List<AstNode> ParsePattern() {
        var parts = new List<AstNode>();
        // Pattern can be a mix of literals and other placeholders if the lexer supports it.
        // The lexer state 'InsidePatternLiteral' returns LiteralPattern tokens.
        
        if (_currentToken.Type == TokenType.LiteralPattern) {
            parts.Add(new LiteralNode(_currentToken.Value));
            Eat(TokenType.LiteralPattern);
        }
        return parts;
    }

    private FunctionCallNode ParseFunctionCall() {
        Eat(TokenType.Dot);
        string name = _currentToken.Value;
        Eat(TokenType.Identifier);
        
        var args = new List<AstNode>();
        if (_currentToken.Type == TokenType.ParenOpen) {
            Eat(TokenType.ParenOpen);
            while (_currentToken.Type != TokenType.ParenClose) {
                args.Add(ParseLiteral()); 
                if (_currentToken.Type == TokenType.Comma) {
                    Eat(TokenType.Comma);
                }
            }
            Eat(TokenType.ParenClose);
        }
        
        return new FunctionCallNode(name, args);
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
    private AstNode ParseLiteral() {
        var token = _currentToken;
        if (token.Type == TokenType.LiteralString) {
            Eat(TokenType.LiteralString);
			// TODO: Differentiate between TextNode and LiteralNode if needed
			return new TextNode(token.Value); // Using TextNode for root strings
        } else if (token.Type == TokenType.LiteralInteger) {
            Eat(TokenType.LiteralInteger);
            return new LiteralNode(token.Value); // Helper, maybe separate TextNode and LiteralNode logic
        }
        throw new InvalidOperationException($"Expected literal, got {token.Type}");
    }
}
