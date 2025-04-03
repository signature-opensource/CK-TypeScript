using CK.BinarySerialization;
using CK.EmbeddedResources;
using CK.TypeScript.CodeGen;
using System;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    static TypeScriptContext()
    {
        BinarySerializer.DefaultSharedContext.AddSerializationDriver( typeof( TypeScriptRoot ), new AsCodeContainer() );
    }

    sealed class AsCodeContainer : ReferenceTypeSerializer<TypeScriptRoot>, ISerializationDriverTypeRewriter
    {
        public override string DriverName => "VersionedBinarySerializable";

        public override int SerializationVersion => 0;

        public Type GetTypeToWrite( Type type ) => typeof( CodeGenResourceContainer );

        protected override void Write( IBinarySerializer s, in TypeScriptRoot o )
        {
        }
    }
}
