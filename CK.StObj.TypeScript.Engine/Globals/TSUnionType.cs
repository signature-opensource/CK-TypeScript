using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// TypeScript for <see cref="IUnionPocoType"/>.
    /// </summary>
    public sealed class TSUnionType : TSBasicType
    {
        readonly IReadOnlyList<(IPocoType,ITSType)> _types;

        internal TSUnionType( TSTypeManager typeManager, string typeName, Action<ITSFileImportSection>? imports, IReadOnlyList<(IPocoType, ITSType)> types )
            : base( typeManager, typeName, imports, null )
        {
            _types = types;
        }

        /// <summary>
        /// Gets the <see cref="IOneOfPocoType.AllowedTypes"/> and their TypeScript type.
        /// </summary>
        public IReadOnlyList<(IPocoType PocoType, ITSType TSType)> Types => _types;

        /// <summary>
        /// Tests the <paramref name="value"/>'s type against the <see cref="Types"/>.
        /// </summary>
        /// <param name="writer">The target writer.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if the value has been written, false otherwise.</returns>
        protected override bool DoTryWriteValue( ITSCodeWriter writer, object value )
        {
            foreach( var (_,ts) in _types )
            {
                if( ts.TryWriteValue( writer, value ) ) return true;
            }
            return false;
        }
    }
}
