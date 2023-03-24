using System;
using System.IO;
using System.Collections;
using System.Text;

namespace VillageResearch
{
	public enum ScriptToken
	{
		Word,
		StatementListOpen,
		StatementListClose,
		VectorOpen,
		VectorClose,
		Separator,
		Condition,
		Set,
		SpecialString,
		SpecialStringBoundary,
		Newline,
		Comment,
		//EOF
	}

	public class ScriptTokenizer : TCParser.Tokenizer
	{
		private enum SpecialExpression
		{
			None,
			OneLine,
			MultiLine,
			VectorComponent,
			Exhausted
		}

		public ScriptToken lastToken;

		private bool inComment = false;
		private SpecialExpression currentSpecialExpression = SpecialExpression.None;

		public ScriptTokenizer(TextReader textReader) : base(textReader) {}

		protected override void OnNumber()
		{
			throw new Exception("Non-expression numbers are not supported by the script parser");
		}

		protected override void OnWord()
		{
			lastToken = ScriptToken.Word;
		}

		protected override void OnSpecialToken()
		{
			switch(lastString)
			{
				case "{": lastToken = ScriptToken.StatementListOpen; break;
				case "}": lastToken = ScriptToken.StatementListClose; break;
				case "[": lastToken = ScriptToken.VectorOpen; currentSpecialExpression = SpecialExpression.VectorComponent; break;
				case "]": lastToken = ScriptToken.VectorClose; break;
				case ",": lastToken = ScriptToken.Separator; currentSpecialExpression = SpecialExpression.VectorComponent; break;
				case ":": lastToken = ScriptToken.Condition; currentSpecialExpression = SpecialExpression.OneLine; break;
				case "=": lastToken = ScriptToken.Set; currentSpecialExpression = SpecialExpression.OneLine; break;
				case "\"": 
					lastToken = ScriptToken.SpecialStringBoundary; 
					currentSpecialExpression = currentSpecialExpression == SpecialExpression.Exhausted ? SpecialExpression.None : SpecialExpression.MultiLine;
					break;
				default: throw new Exception($"Unknown symbol {lastString} in script");
			}
		}

		protected override bool CanSpecialTokenContinue(char previousCharacter, char currentCharacter)
		{
			return false;
		}

		protected override bool DoSpecialRules()
		{
			if (currentSpecialExpression != SpecialExpression.None && currentSpecialExpression != SpecialExpression.Exhausted)
			{
				// Skip all whitespace that may be at the start of the special string
				while (char.IsWhiteSpace(lastCharacter))
				{
					// Check if string is not terminated before it even begins
					if (currentSpecialExpression == SpecialExpression.OneLine && lastCharacter == '\n')
					{
						throw new Exception("Expected expression string but no characters were found!");
					}

					NextCharacter();
				}

				stringBuilder.Clear();

				while (!finished)
				{
					// End the string if a terminator is reached
					if ((currentSpecialExpression == SpecialExpression.OneLine && lastCharacter == '\n')
						|| (currentSpecialExpression == SpecialExpression.VectorComponent && (lastCharacter == ',' || lastCharacter == ']'))
						|| (currentSpecialExpression == SpecialExpression.MultiLine && (lastCharacter == '"')))
					{
						break;
					}

					// End if the expression is followed by a comment
					if (lastCharacter == '/' && currentSpecialExpression == SpecialExpression.OneLine)
					{
						NextCharacter();
						if (lastCharacter == '/')
						{
							NextCharacter();
							inComment = true;
							break;
						}
						else
						{
							stringBuilder.Append('/');
						}
					}

					stringBuilder.Append(lastCharacter);
					NextCharacter();
				}

				lastToken = ScriptToken.SpecialString;
				lastString = stringBuilder.ToString();

				if (lastString.Length == 0)
				{
					throw new Exception("Expected expression string but no characters were found!");
				}

				currentSpecialExpression = currentSpecialExpression == SpecialExpression.MultiLine ? SpecialExpression.Exhausted : SpecialExpression.None;
				return true;
			}

			// Ignore comments, and search for newlines to separate statements
			while (!finished)
			{
				if (lastCharacter == '/' && !inComment)
				{
					NextCharacter();
					if (lastCharacter == '/')
					{
						inComment = true;
					}
					else
					{
						throw new Exception("Unfinished comment line");
					}
				}

				if (lastCharacter == '\n')
				{  
					NextCharacter();
					lastToken = ScriptToken.Newline;
					inComment = false;
					return true;
				}

				if (char.IsWhiteSpace(lastCharacter) || inComment)
				{
					NextCharacter();
				}
				else
				{
					break;
				}
			}

			return false;
		}
	}
}
