using System;
using System.Collections.Generic;
using BMTP3.Common.MessageFormatterParser;
using BMTP3.Common.MessageFormatterParser.Nodes;
using Xunit;

namespace BMTP3.Common.Tests.MessageFormatterParser
{
    public class ParserAndTypeCheckerTests
    {
        [Fact]
        public void Parser_ParsesSimplePlaceholder()
        {
            var lexer = new Lexer2("${filename}");
            var parser = new Parser2(lexer);
            var ast = parser.Parse();

            Assert.Single(ast.Children);
            var ph = Assert.IsType<ConcretePlaceholderNode>(ast.Children[0]);
            Assert.Equal("filename", ph.NameOrIndex);
        }

        [Fact]
        public void Parser_ParsesIfCondition()
        {
            // Use the section marker '§' which the lexer/parser expect for eval patterns
            var template = "${count§if,eq0?No files:One or more}";
            var lexer = new Lexer2(template);
            var parser = new Parser2(lexer);
            var ast = parser.Parse();

            Assert.Single(ast.Children);
            var ph = Assert.IsType<ConcretePlaceholderNode>(ast.Children[0]);
            Assert.NotNull(ph.Condition);
            var cond = ph.Condition;
            Assert.IsType<IfConditionNode>(cond);

            // True branch should contain the text "No files"
            Assert.NotEmpty(cond.TrueValue);
            var trueNode = Assert.IsType<TextNode>(cond.TrueValue[0]);
            Assert.Equal("No files", trueNode.Value);

            // False branch should contain the text "One or more"
            Assert.NotEmpty(cond.FalseValue);
            var falseNode = Assert.IsType<TextNode>(cond.FalseValue[0]);
            Assert.Equal("One or more", falseNode.Value);
        }

        [Fact]
        public void TypeChecker_ThrowsOnUnknownPlaceholder()
        {
            var lexer = new Lexer2("${unknown}");
            var parser = new Parser2(lexer);
            var ast = parser.Parse();

            var checker = new TypeChecker(new Dictionary<string, Type>());
            Assert.Throws<KeyNotFoundException>(() => checker.Validate(ast));
        }

        [Fact]
        public void MessageFormatter_EvaluatesIfCondition()
        {
            var template = "Files: ${count§if,eq0?no files:${other} files}";
            var values = new Dictionary<string, object>
            {
                { "count", 2.0 },
                { "other", "two" }
            };

            var result = new MessageFormatter().Format(template, values);

            Assert.Equal("Files: two files", result);
        }
    }
}
