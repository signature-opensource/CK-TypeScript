namespace CK.TypeScript.CodeGen
{
    internal class NamedCodePart : RawCodePart, ITSNamedCodePart
    {
        internal NamedCodePart( TypeScriptFile f, string name, string closer )
            : base( f, closer )
        {
            Name = name;
        }

        public string Name { get; }
    }
}
