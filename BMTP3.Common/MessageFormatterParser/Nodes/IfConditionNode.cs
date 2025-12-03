namespace BMTP3.Common.MessageFormatterParser.Nodes
{
	// If condition node (e.g., if,eq0?no files:${other} files)
	public class IfConditionNode : AstNode
	{
		public AstNode ConditionOperator { get; }
		public List<AstNode> ConditionParameters { get; }
		public List<AstNode> TrueValue { get; }
		public List<AstNode> FalseValue { get; }

		public IfConditionNode(
			AstNode conditionOperator,
			List<AstNode> conditionParameters,
			List<AstNode> trueValue, List<AstNode> falseValue
			) : base(NodeType.IfCondition)
		{
			ConditionOperator = conditionOperator;
			ConditionParameters = conditionParameters;
			TrueValue = trueValue;
			FalseValue = falseValue;
		}
	}
}
