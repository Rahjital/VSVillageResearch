using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace VillageResearch
{
	public class ScriptParser : TCParser.Parser<bool>
	{        
		private ScriptTokenizer scriptTokenizer;
		private TCParser.IVariableContext variableContext;

		Grammar grammar;

		public ScriptParser(Grammar grammar, TextReader textReader, TCParser.IVariableContext variableContext = null) : base(new ScriptTokenizer(textReader), textReader) 
		{
			this.grammar = grammar;

			this.variableContext = variableContext;
			scriptTokenizer = tokenizer as ScriptTokenizer;
		}

		public override bool Parse()
		{
			scriptTokenizer.TokenizeNext();
			SkipNewlines();

			while (!scriptTokenizer.finished)
			{
				ParseModule();
				scriptTokenizer.TokenizeNext();
				SkipNewlines();
			}

			return true;
		}

		public void ParseModule()
		{
			string moduleName;

			if (scriptTokenizer.lastToken == ScriptToken.Word)
			{
				moduleName = scriptTokenizer.lastString;
			}
			else
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected module name"); return;
			}

			scriptTokenizer.TokenizeNext();

			string condition = null;

			if (scriptTokenizer.lastToken == ScriptToken.Condition)
			{
				scriptTokenizer.TokenizeNext();

				if (scriptTokenizer.lastToken == ScriptToken.SpecialString)
				{
					condition = scriptTokenizer.lastString;
					scriptTokenizer.TokenizeNext();
				}
				else
				{
					Error($"Unknown token {scriptTokenizer.lastToken}, expected module condition");
				}
			}

			StatementList statementList = ParseStatementList();

			Rule rule = new Rule(statementList);

			if (condition != null)
			{
				rule.AddCondition(condition);
			}

			grammar.AddModuleRule(moduleName, rule);
		}

		public StatementList ParseStatementList()
		{
			StatementList statementList = new StatementList();

			SkipNewlines();

			if (scriptTokenizer.lastToken != ScriptToken.StatementListOpen)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected statement list");
			}

			scriptTokenizer.TokenizeNext();
			SkipNewlines();

			while (scriptTokenizer.lastToken == ScriptToken.Word)
			{
				Statement newStatement = null;
				int startLineNumber = scriptTokenizer.lineNumber;

				switch (scriptTokenizer.lastString)
				{
					case "module": scriptTokenizer.TokenizeNext(); newStatement = ParseStatementModule(); break;
					case "set_block": scriptTokenizer.TokenizeNext(); newStatement = ParseStatementSetBlock(); break;
					case "move": scriptTokenizer.TokenizeNext(); newStatement = ParseStatementMove(); break;
					case "rotate": scriptTokenizer.TokenizeNext(); newStatement = ParseStatementRotate(); break;
					case "expand": scriptTokenizer.TokenizeNext(); newStatement = ParseStatementExpand(); break;
					case "resize_to": scriptTokenizer.TokenizeNext(); newStatement = ParseStatementResizeTo(); break;
					case "split": scriptTokenizer.TokenizeNext(); newStatement = ParseStatementSplit(); break;
					case "shape": scriptTokenizer.TokenizeNext(); newStatement = ParseStatementShape(); break;
					case "select": scriptTokenizer.TokenizeNext(); newStatement = ParseStatementSelect(); break;
					// Unrecognised strings are treated as variable assignment
					default: newStatement = ParseStatementSetVariable(); break;
				}

				if (newStatement is not null)
				{
					statementList.AddStatement(newStatement, startLineNumber);
				}

				SkipNewlines();
			}

			if (scriptTokenizer.lastToken != ScriptToken.StatementListClose)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected statement or end of statement list");
			}

			return statementList;
		}

		public StatementAddModule ParseStatementModule()
		{
			if (scriptTokenizer.lastToken != ScriptToken.Word)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected module name");
			}

			string argument = scriptTokenizer.lastString;
			scriptTokenizer.TokenizeNext();

			return new StatementAddModule(argument);
		}

		public StatementSetVariable ParseStatementSetVariable()
		{
			string varName = scriptTokenizer.lastString;
			scriptTokenizer.TokenizeNext();

			if (scriptTokenizer.lastToken != ScriptToken.Set)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected assignment operator");
			}

			scriptTokenizer.TokenizeNext();
			string argumentExpression = ParseExpressionString();

			return new StatementSetVariable(varName, argumentExpression);
		}

		public StatementSetBlock ParseStatementSetBlock()
		{
			string argumentExpression = ParseExpressionString();

			return new StatementSetBlock(argumentExpression);
		}

		public StatementMove ParseStatementMove()
		{
			string[] result = ParseVector(3);

			return new StatementMove(result[0], result[1], result[2]);
		}

		public StatementRotate ParseStatementRotate()
		{
			if (scriptTokenizer.lastToken != ScriptToken.Word)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected axis string");
			}

			string argumentAxis = scriptTokenizer.lastString.ToLowerInvariant();

			StatementRotate.Axis axis;

			switch (argumentAxis)
			{
				case "x" : axis = StatementRotate.Axis.X; break;
				case "y" : axis = StatementRotate.Axis.Y; break;
				case "z" : axis = StatementRotate.Axis.Z; break;
				default: Error($"Unknown rotation axis {argumentAxis}"); return null;
			}

			scriptTokenizer.TokenizeNext();

			string argumentExpression = ParseExpressionString();

			return new StatementRotate(axis, argumentExpression);
		}

		public StatementExpand ParseStatementExpand()
		{
			if (scriptTokenizer.lastToken != ScriptToken.Word)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected axis string");
			}

			StatementExpand.Axis axis;

			switch(scriptTokenizer.lastString.ToLowerInvariant())
			{
				case "x" : axis = StatementExpand.Axis.X; break;
				case "y" : axis = StatementExpand.Axis.Y; break;
				case "z" : axis = StatementExpand.Axis.Z; break;
				default: Error($"Unknown expand axis {scriptTokenizer.lastString}"); return null;
			}

			scriptTokenizer.TokenizeNext();

			StatementExpand.Direction direction = StatementExpand.Direction.Both;

			// Optional argument - positive/negative
			if (scriptTokenizer.lastToken == ScriptToken.Word)
			{
				switch(scriptTokenizer.lastString.ToLowerInvariant())
				{
					case "both": break;
					case "positive":
					case "pos":
						direction = StatementExpand.Direction.Positive; break;
					case "negative":
					case "neg":
						direction = StatementExpand.Direction.Negative; break;
					default: Error($"Unknown expand direction {scriptTokenizer.lastString}"); return null;
				}

				scriptTokenizer.TokenizeNext();
			}

			string argumentExpression = ParseExpressionString();

			return new StatementExpand(axis, direction, argumentExpression);
		}

		public StatementResizeTo ParseStatementResizeTo()
		{
			string[] result = ParseVector(3);

			return new StatementResizeTo(result[0], result[1], result[2]);
		}

		public StatementSplit ParseStatementSplit()
		{
			if (scriptTokenizer.lastToken != ScriptToken.Word)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected axis string");
			}

			string argumentAxis = scriptTokenizer.lastString.ToLowerInvariant();

			scriptTokenizer.TokenizeNext();

			List<string> splitSizes = new List<string>();
			List<StatementList> splitStatementLists = new List<StatementList>();

			while (!scriptTokenizer.finished)
			{
				SkipNewlines();

				if (scriptTokenizer.lastToken != ScriptToken.SpecialString && scriptTokenizer.lastToken != ScriptToken.SpecialStringBoundary)
				{
					if (splitSizes.Count == 0)
					{
						Error($"No arguments found for split statement"); 
					}

					break;
				}

				splitSizes.Add(ParseExpressionString());

				splitStatementLists.Add(ParseStatementList());
				scriptTokenizer.TokenizeNext();
			}

			StatementSplit.SliceType sliceType;

			switch (argumentAxis)
			{
				case "x" : sliceType = StatementSplit.SliceType.X; break;
				case "y" : sliceType = StatementSplit.SliceType.Y; break;
				case "z" : sliceType = StatementSplit.SliceType.Z; break;
				default: Error($"Unknown slice axis type {argumentAxis}"); return null;
			}

			StatementSplit.Slice[] slices = new StatementSplit.Slice[splitSizes.Count];

			for (int i = 0; i < splitSizes.Count; i++)
			{
				slices[i] = new StatementSplit.Slice(splitSizes[i], splitStatementLists[i]);
			}

			return new StatementSplit(sliceType, slices);
		}

	public StatementShape ParseStatementShape()
		{
			if (scriptTokenizer.lastToken != ScriptToken.Word)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected shape type");
			}

			Shape shape;

			switch(scriptTokenizer.lastString.ToLowerInvariant())
			{
				case "box" : shape = Shape.Box; break;
				case "prism" : shape = Shape.Prism; break;
				default: Error($"Unknown shape type {scriptTokenizer.lastString}"); return null;
			}

			scriptTokenizer.TokenizeNext();

			return new StatementShape(shape);
		}

		public StatementSelect ParseStatementSelect()
		{
			if (scriptTokenizer.lastToken != ScriptToken.Word)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected subshape type");
			}

			string subshapeType = scriptTokenizer.lastString;
			scriptTokenizer.TokenizeNext();

			SkipNewlines();

			StatementList statementList = ParseStatementList();
			scriptTokenizer.TokenizeNext();

			return new StatementSelect(subshapeType, statementList);
		}

		// Common functions
		public void SkipNewlines()
		{
			while (scriptTokenizer.lastToken == ScriptToken.Newline && !scriptTokenizer.finished)
			{
				scriptTokenizer.TokenizeNext();
			}
		}

		public string ParseExpressionString()
		{
			if (scriptTokenizer.lastToken == ScriptToken.SpecialStringBoundary)
			{
				scriptTokenizer.TokenizeNext();
			}

			if (scriptTokenizer.lastToken != ScriptToken.SpecialString)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected expression string");
			}

			string expression = scriptTokenizer.lastString;

			scriptTokenizer.TokenizeNext();

			if (scriptTokenizer.lastToken == ScriptToken.SpecialStringBoundary)
			{
				scriptTokenizer.TokenizeNext();
			}

			return expression;
		}

		public string[] ParseVector(int vectorSize)
		{
			if (scriptTokenizer.lastToken != ScriptToken.VectorOpen)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected vector start");
			}

			scriptTokenizer.TokenizeNext();

			string[] result = new string[vectorSize];

			for (int i = 0; i < vectorSize; i++)
			{
				if (scriptTokenizer.lastToken != ScriptToken.SpecialString)
				{
					Error($"Unknown token {scriptTokenizer.lastToken}, expected expression string in vector");
				}

				result[i] = scriptTokenizer.lastString;

				scriptTokenizer.TokenizeNext();

				if (i < vectorSize - 1)
				{
					if (scriptTokenizer.lastToken != ScriptToken.Separator)
					{
						Error($"Unknown token {scriptTokenizer.lastToken}, expected separator in vector");
					}

					scriptTokenizer.TokenizeNext();
				}
			}

			if (scriptTokenizer.lastToken != ScriptToken.VectorClose)
			{
				Error($"Unknown token {scriptTokenizer.lastToken}, expected vector end");
			}

			scriptTokenizer.TokenizeNext();

			return result;
		}

		// Convenience method to parse a string directly
		public static bool ParseString(Grammar grammar, string input)
		{
			ScriptParser parser = new ScriptParser(grammar, new StringReader(input));

			return parser.Parse();
		}
	}
}
