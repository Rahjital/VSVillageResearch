using System;
using System.IO;
using System.Collections;
using System.Text;

namespace TCParser
{
    public class MathParser : Parser<float>
    {        
        private MathTokenizer mathTokenizer;
        private IVariableContext variableContext;

        public MathParser(TextReader textReader, IVariableContext variableContext = null) : base(new MathTokenizer(textReader), textReader) 
        {
            this.variableContext = variableContext;

            mathTokenizer = tokenizer as MathTokenizer;
        }

        public override float Parse()
        {
            mathTokenizer.TokenizeNext();
            float result = ParseConditional();

            if (!mathTokenizer.finished)
            {
                throw new Exception("Unparsed character at end of expression!");
            }

            return result;
        }

        private float ParseConditional()
        {
            float result = ParseAddSubtract(); // left hand side of the equation

            while(true)
            {
                switch (mathTokenizer.lastToken)
                {
                    case MathToken.Equal:
                        mathTokenizer.TokenizeNext();
                        result = result == ParseAddSubtract() ? 1f : 0f;
                        break;

                    case MathToken.NotEqual:
                        mathTokenizer.TokenizeNext();
                        result = result != ParseAddSubtract() ? 1f : 0f;
                        break;

                    case MathToken.MoreThan:
                        mathTokenizer.TokenizeNext();
                        result = result > ParseAddSubtract() ? 1f : 0f;
                        break;

                    case MathToken.MoreOrEqual:
                        mathTokenizer.TokenizeNext();
                        result = result >= ParseAddSubtract() ? 1f : 0f;
                        break;

                    case MathToken.LessThan:
                        mathTokenizer.TokenizeNext();
                        result = result < ParseAddSubtract() ? 1f : 0f;
                        break;

                    case MathToken.LessOrEqual:
                        mathTokenizer.TokenizeNext();
                        result = result <= ParseAddSubtract() ? 1f : 0f;
                        break;

                    case MathToken.And:
                        mathTokenizer.TokenizeNext();
                        result = ParseAddSubtract() > 0f && result > 0f ? 1f : 0f;
                        break;

                    case MathToken.Or:
                        mathTokenizer.TokenizeNext();
                        result = ParseAddSubtract() > 0f && result > 0f ? 1f : 0f;
                        break;

                    default:
                        return result;
                }
            }
        }

        private float ParseAddSubtract()
        {
            float result = ParseMultiplyDivideModulo(); // left hand side of the equation

            while(true)
            {
                bool isAdd = false;

                if (mathTokenizer.lastToken == MathToken.Add)
                {
                    isAdd = true;
                } 
                else if (mathTokenizer.lastToken != MathToken.Subtract)
                {
                    return result; // if there's nothing more to add/subtract, return the result 
                }

                mathTokenizer.TokenizeNext();

                result += isAdd ? ParseMultiplyDivideModulo() : -ParseMultiplyDivideModulo(); // add (or subtract) the right-hand side of the equation
            }
        }

        private float ParseMultiplyDivideModulo()
        {
            float result = ParseConditionalNot(); // left hand side of the equation

            while(true)
            {
                switch (mathTokenizer.lastToken)
                {
                    case MathToken.Multiply:
                        mathTokenizer.TokenizeNext();
                        result = result * ParseConditionalNot();
                        break;

                    case MathToken.Divide:
                        mathTokenizer.TokenizeNext();
                        result = result / ParseConditionalNot();
                        break;

                    case MathToken.Modulo:
                        mathTokenizer.TokenizeNext();
                        result = result % ParseConditionalNot();
                        break;

                    default:
                        return result; // if there's nothing more to multiply/divide, return the result
                }
            }
        }

        private float ParseConditionalNot()
        {
            if (mathTokenizer.lastToken == MathToken.Not)
            {
                mathTokenizer.TokenizeNext();
                float number = ParseConditionalNot(); // it's possible to chain nots

                return number > 0f ? 0f : 1f;
            }
            else
            {
                return ParseBrackets();
            }
        }

        private float ParseBrackets()
        {
            if (mathTokenizer.lastToken == MathToken.BracketOpen)
            {
                int bracketStartPos = mathTokenizer.position;

                mathTokenizer.TokenizeNext();
                float result = ParseConditional(); // start the whole process from conditionals again

                if (mathTokenizer.lastToken != MathToken.BracketClose)
                {
                    throw new Exception($"Brackets at position {bracketStartPos} were not closed!");
                }

                mathTokenizer.TokenizeNext();
                return result;
            }
            else
            {
                return ParseNumber();
            }
        }

        private float ParseNumber()
        {
            bool negative = false;

            // a plus before a number does nothing, but is allowed; a minus negates it, and multiple minuses are allowed, cancelling each other
            while (mathTokenizer.lastToken == MathToken.Subtract || mathTokenizer.lastToken == MathToken.Add)
            {
                negative = !negative && mathTokenizer.lastToken == MathToken.Subtract;
                mathTokenizer.TokenizeNext();
            }

            if (mathTokenizer.lastToken == MathToken.Number)
            {
                float result = negative ? -mathTokenizer.lastNumber : mathTokenizer.lastNumber;
                mathTokenizer.TokenizeNext();
                return result;
            }
            else if (mathTokenizer.lastToken == MathToken.Word)
            {
                if (variableContext == null)
                {
                    throw new Exception("Expression contains a word but no variable context is assigned!");
                }

                float result = negative ? -variableContext.ResolveVariable(mathTokenizer.lastString) : variableContext.ResolveVariable(mathTokenizer.lastString);
                mathTokenizer.TokenizeNext();
                return result;
            }

            throw new Exception($"Unexpected symbol at position {mathTokenizer.position}");
        }

        // Convenience method to parse a string directly
        public static float ParseString(string input, IVariableContext variableContext = null)
        {
            MathParser parser = new MathParser(new StringReader(input), variableContext);

            return parser.Parse();
        }

        public static bool ParseStringAsBool(string input, IVariableContext variableContext = null)
        {
            MathParser parser = new MathParser(new StringReader(input), variableContext);

            return parser.Parse() > 0f;
        }
    }
}
