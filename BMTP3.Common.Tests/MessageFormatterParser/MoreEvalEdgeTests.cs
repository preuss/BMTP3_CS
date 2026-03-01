using System.Collections.Generic;
using BMTP3.Common.MessageFormatterParser;
using Xunit;

namespace BMTP3.Common.Tests.MessageFormatterParser
{
    public class MoreEvalEdgeTests
    {
        [Fact]
        public void Eval_IfBranch_WithNestedPlaceholderAndSpacing()
        {
            var template = "Items: ${count§if,eq0?no items:${name} items}";
            var values = new Dictionary<string, object>
            {
                { "count", 2.0 },
                { "name", "several" }
            };

            var result = new MessageFormatter().Format(template, values);
            Assert.Equal("Items: several items", result);
        }

        [Fact]
        public void Eval_IfBranch_ZeroCase_ResolvesZero()
        {
            var template = "Count: ${count§if,eq0?none:some}";
            var values = new Dictionary<string, object>
            {
                { "count", 0.0 }
            };

            var result = new MessageFormatter().Format(template, values);
            Assert.Equal("Count: none", result);
        }
    }
}
