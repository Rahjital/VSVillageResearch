using System;
using System.IO;
using System.Collections;
using System.Text;

namespace TCParser
{
    public enum MathToken
    {
        Number,
        Word,
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        BracketOpen,
        BracketClose,
        // Conditional operators
        Equal,
        NotEqual,
        And,
        Or,
        Not,
        MoreThan,
        MoreOrEqual,
        LessThan,
        LessOrEqual
    }

    public class MathTokenizer : Tokenizer
    {        
        public MathToken lastToken;

        public MathTokenizer(TextReader textReader) : base(textReader) {}

        protected override void OnNumber()
        {
            lastToken = MathToken.Number;
        }

        protected override void OnWord()
        {
            lastToken = MathToken.Word;
        }

        protected override void OnSpecialToken()
        {
            switch(lastString)
            {
                case "+": lastToken = MathToken.Add; break;
                case "-": lastToken = MathToken.Subtract; break;
                case "*": lastToken = MathToken.Multiply; break;
                case "/": lastToken = MathToken.Divide; break;
                case "%": lastToken = MathToken.Modulo; break;
                case "(": lastToken = MathToken.BracketOpen; break;
                case ")": lastToken = MathToken.BracketClose; break;
                // Conditional operators
                case "!": lastToken = MathToken.Not; break;
                case "==": lastToken = MathToken.Equal; break;
                case "!=": lastToken = MathToken.NotEqual; break;
                case "&&": lastToken = MathToken.And; break;
                case "||": lastToken = MathToken.Or; break;
                case ">": lastToken = MathToken.MoreThan; break;
                case ">=": lastToken = MathToken.MoreOrEqual; break;
                case "<": lastToken = MathToken.LessThan; break;
                case "<=": lastToken = MathToken.LessOrEqual; break;
                default: throw new Exception($"Unknown symbol {lastString} in math expression");
            }
        }

        protected override bool CanSpecialTokenContinue(char previousCharacter, char currentCharacter)
        {
            switch(previousCharacter)
            {
                case '=': return currentCharacter == '=';
                case '!': return currentCharacter == '=';
                case '&': return currentCharacter == '&';
                case '|': return currentCharacter == '|';
                case '>': return currentCharacter == '=';
                case '<': return currentCharacter == '=';
                default: return false;
            }
        }
    }
}
