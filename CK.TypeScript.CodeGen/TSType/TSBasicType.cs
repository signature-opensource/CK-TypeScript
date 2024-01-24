using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Default implementation of <see cref="ITSType"/> for basic types.
    /// <para>
    /// This concrete class can be be used for TypeScript types that don't have an associated <see cref="File"/>: they have no code
    /// other than their TypeName and DefaultValueSource, they are "simple signatures".
    /// </para>
    /// <para>
    /// It is not linked to a specific C# type (as opposed to a <see cref="ITSFileCSharpType"/> that handles C# type
    /// in a <see cref="TypeScriptFile"/>).
    /// </para>
    /// <para>
    /// It can be instantiated directly thanks to the public <see cref="TSBasicType(string,Action{ITSFileImportSection},string)"/> constructor
    /// or a <see cref="ITSTypeSignatureBuilder"/> can be used for more complex cases.
    /// </para>
    /// </summary>
    public class TSBasicType : TSType
    {
        readonly Action<ITSFileImportSection>? _requiredImports;
        string? _defaultValueSource;

        /// <summary>
        /// Initializes a new <see cref="TSBasicType"/>.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="imports">The required imports. Null when using this type doesn't require imports.</param>
        /// <param name="defaultValueSource">The type default value if any.</param>
        public TSBasicType( string typeName, Action<ITSFileImportSection>? imports, string? defaultValueSource )
            : this( imports, typeName, defaultValueSource )
        {
        }

        /// <summary>
        /// Internal: only used by TSGeneratedType.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="imports">Optional required imports to use this type.</param>
        /// <param name="defaultValueSource">The default value as a non empty or whitespace string.</param>
        /// <param name="nullFactory">The <see cref="Nullable"/> factory.</param>
        internal TSBasicType( string typeName, Action<ITSFileImportSection>? imports, string? defaultValueSource, Func<TSBasicType, ITSType> nullFactory )
            : this( imports, typeName, defaultValueSource )
        {
        }

        TSBasicType( Action<ITSFileImportSection>? imports, string typeName, string? defaultValueSource )
            : base( typeName ) 
        {
            Throw.CheckArgument( defaultValueSource == null || !string.IsNullOrWhiteSpace( defaultValueSource ) );
            _requiredImports = imports;
            _defaultValueSource = defaultValueSource;
        }

        /// <inheritdoc />
        public override string? DefaultValueSource => _defaultValueSource;

        // Only set after type registration for TSGeneratedType from a deferred factory function if any.
        internal void SetDefaultValueSource( string? v ) => _defaultValueSource = v;

        /// <inheritdoc cref="ITSType.EnsureRequiredImports(ITSFileImportSection)" />
        public override void EnsureRequiredImports( ITSFileImportSection section )
        {
            Throw.CheckNotNullArgument( section );
            _requiredImports?.Invoke( section );
        }
    }
}

