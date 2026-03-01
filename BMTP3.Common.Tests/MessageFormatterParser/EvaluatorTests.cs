using System;
using System.Collections.Generic;
using BMTP3.Common.MessageFormatterParser;
using BMTP3.Common.MessageFormatterParser.Nodes;
using Xunit;

namespace BMTP3.Common.Tests.MessageFormatterParser
{
    public class EvaluatorTests
    {
        [Fact]
        public void Evaluate_SimpleTextAndPlaceholder_ReturnsCombined()
        {
            var ast = new RootNode();
            ast.Children.Add(new TextNode("Hello "));
            ast.Children.Add(new ConcretePlaceholderNode("name", new List<FunctionCallNode>(), null, null));

            var values = new Dictionary<string, object> { { "name", "World" } };
            var ev = new Evaluator(values);

            var result = ev.Evaluate(ast);

            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void Evaluate_DatePattern_ReturnsFormattedParts()
        {
            var pattern = new List<AstNode> { new TextNode("yyyy"), new TextNode("-"), new TextNode("MM"), new TextNode("-"), new TextNode("dd") };
            var placeholder = new ConcretePlaceholderNode("date", new List<FunctionCallNode>(), pattern, null);
            var ast = new RootNode();
            ast.Children.Add(placeholder);

            var dt = new DateTime(2021, 7, 9, 13, 5, 2);
            var ev = new Evaluator(new Dictionary<string, object> { { "date", dt } });

            var result = ev.Evaluate(ast);

            Assert.Equal("2021-07-09", result);
        }

        [Fact]
        public void Evaluate_ToUpperFunction_Works()
        {
            var func = new FunctionCallNode("toUpper", new List<AstNode>());
            var placeholder = new ConcretePlaceholderNode("name", new List<FunctionCallNode> { func }, null, null);
            var ast = new RootNode();
            ast.Children.Add(placeholder);

            var ev = new Evaluator(new Dictionary<string, object> { { "name", "abc" } });

            var result = ev.Evaluate(ast);

            Assert.Equal("ABC", result);
        }

        [Fact]
        public void Evaluate_FormatFunction_Works()
        {
            var func = new FunctionCallNode("format", new List<AstNode> { new TextNode("{0:0.00}") });
            var placeholder = new ConcretePlaceholderNode("num", new List<FunctionCallNode> { func }, null, null);
            var ast = new RootNode();
            ast.Children.Add(placeholder);

            var ev = new Evaluator(new Dictionary<string, object> { { "num", 3.14159 } });

            var result = ev.Evaluate(ast);

            Assert.Equal("3.14", result);
        }
    }
}
