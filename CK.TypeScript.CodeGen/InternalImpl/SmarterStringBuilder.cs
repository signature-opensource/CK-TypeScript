using System;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Internal class that removes duplicated new lines on a StringBuilder.
    /// </summary>
    sealed class SmarterStringBuilder
    {
        public readonly StringBuilder Builder;

        public bool HasNewLine { get; set; }

        public SmarterStringBuilder( StringBuilder b )
        {
            Builder = b;
            HasNewLine = true;
        }

        public SmarterStringBuilder Append( string s )
        {
            Builder.Append( s );
            HasNewLine = s.EndsWith( Environment.NewLine, StringComparison.Ordinal );
            return this;
        }

        public SmarterStringBuilder AppendLine()
        {
            if( !HasNewLine )
            {
                Builder.Append( Environment.NewLine );
                HasNewLine = true;
            }
            return this;
        }

        public void Reset()
        {
            Builder.Clear();
            HasNewLine = true;
        }

        public override string ToString() => Builder.ToString();
    }
}
