using System;
using System.IO;
using System.Collections;
using System.Text;

namespace TCParser
{
    public abstract class Tokenizer
    {        
        private TextReader textReader;
        protected StringBuilder stringBuilder = new StringBuilder();

        public bool finished = false;

        protected char lastCharacter;

        public float lastNumber;
        public string lastString;

        public int position = 0;
        public int lineNumber = 0;

        public Tokenizer(TextReader textReader)
        {
            this.textReader = textReader;
            NextCharacter();
        }

        public void NextCharacter()
        {
            int num = textReader.Read();

            if (num < 0)
            {
                finished = true;
                lastCharacter = '\0';
            }
            else if (lastCharacter == '\n')
            {
                lineNumber++;
            }

            lastCharacter = (char)num;
        }

        public bool TokenizeNext()
        {
            if (finished)
            {
                return false;
            }

            position++;

            if (DoSpecialRules())
            {
                return true;
            }

            while (char.IsWhiteSpace(lastCharacter))
            {
                NextCharacter();
            }

            // Check for being finished again, in case special rules or whitespaces got us to the end
            if (finished)
            {
                return false;
            }

            // number
            if (char.IsDigit(lastCharacter) || lastCharacter == '.')
            {
                stringBuilder.Clear();

                bool hasDecimal = false;

                while (char.IsDigit(lastCharacter) || (hasDecimal == false && lastCharacter == '.'))
                {
                    hasDecimal = lastCharacter == '.';
                    stringBuilder.Append(lastCharacter);
                    NextCharacter();
                }

                if (hasDecimal == true && lastCharacter == '.')
                {
                    throw new Exception("Tokenizer: number has multiple decimal points!");
                }

                lastNumber = float.Parse(stringBuilder.ToString());
                OnNumber();
                return true;
            }

            // word - can start with either a letter or an underscore, and can contain letters, underscores, and numbers
            if (char.IsLetter(lastCharacter) || lastCharacter == '_')
            {
                stringBuilder.Clear();

                while (char.IsLetterOrDigit(lastCharacter) || lastCharacter == '_')
                {
                    stringBuilder.Append(lastCharacter);
                    NextCharacter();
                }

                lastString = stringBuilder.ToString();
                OnWord();
                return true;
            }

            // special characters - everything else; usually single characters, but can be overriden to allow multi-character tokens (such as !=)
            stringBuilder.Clear();

            stringBuilder.Append(lastCharacter);

            char previousCharacter = lastCharacter;
            NextCharacter();

            while (CanSpecialTokenContinue(previousCharacter, lastCharacter))
            {
                stringBuilder.Append(lastCharacter);
                previousCharacter = lastCharacter;
                NextCharacter();
            }

            lastString = stringBuilder.ToString();
            OnSpecialToken();
            return true;
        }

        protected abstract void OnNumber();
        protected abstract void OnWord();
        protected abstract void OnSpecialToken();

        protected abstract bool CanSpecialTokenContinue(char previousCharacter, char currentCharacter);

        protected virtual bool DoSpecialRules()
        {
            return false;
        }
    }
}
