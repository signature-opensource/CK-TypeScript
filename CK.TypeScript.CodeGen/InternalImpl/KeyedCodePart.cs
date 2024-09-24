namespace CK.TypeScript.CodeGen;

internal class KeyedCodePart : RawCodePart, ITSKeyedCodePart
{
    internal KeyedCodePart( TypeScriptFile f, object key, string closer )
        : base( f, closer )
    {
        Key = key;
    }

    public object Key { get; }
}
