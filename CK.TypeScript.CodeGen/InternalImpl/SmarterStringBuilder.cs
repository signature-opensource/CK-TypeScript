using System;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Internal class that handles new lines and unifies the output to a StringBuilder
    /// or an Action{string}.
    /// </summary>
    class SmarterStringBuilder
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

        public override string ToString() => Builder.ToString();
    }
}
