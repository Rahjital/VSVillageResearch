using System;
using System.IO;
using System.Collections;
using System.Text;

namespace TCParser
{
    public abstract class Parser<T>
    {        
        protected Tokenizer tokenizer;

        public Parser(Tokenizer tokenizer, TextReader textReader)
        {
            this.tokenizer = tokenizer;
        }

        public void Error(string errorMessage)
        {
            throw new Exception($"Error: '{errorMessage}' at position {tokenizer.position}, line {tokenizer.lineNumber}");
        }

        public abstract T Parse();
    }
}
