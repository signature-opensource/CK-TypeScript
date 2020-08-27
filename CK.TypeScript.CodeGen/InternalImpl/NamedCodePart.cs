namespace CK.TypeScript.CodeGen
{
    internal class NamedCodePart : RawCodePart, ITSNamedCodePart
    {
        internal NamedCodePart( string name, string closer )
            : base( closer )
        {
            Name = name;
        }

        public string Name { get; }
    }
}
