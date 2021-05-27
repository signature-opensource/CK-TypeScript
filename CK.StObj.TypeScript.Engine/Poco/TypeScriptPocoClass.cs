using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Mutable description of the Poco class part.
    /// </summary>
    public class TypeScriptPocoClass
    {
        internal TypeScriptPocoClass( string className,
                                      ITSCodePart p,
                                      IPocoRootInfo info,
                                      List<TypeScriptPocoPropertyInfo> props,
                                      int requiredParameterCount,
                                      int readOnlyPropertyCount )
        {
            TypeName = className;
            Part = p;
            PocoRootInfo = info;
            Properties = props;
            CreateMethodDocumentation = new DocumentationBuilder();
            ReadOnlyPropertyCount = readOnlyPropertyCount;
            RequiredParameterCount = requiredParameterCount;
        }

        /// <summary>
        /// Gets the class name.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the number of required parameters in create method.
        /// Required parameters are the first in the CreateParameters list.
        /// </summary>
        public int RequiredParameterCount { get; }

        /// <summary>
        /// Gets the number of properties that are read only. They must be assigned in the constructor,
        /// either by being created (the <see cref="TypeScriptVarType.DefaultValue"/> of the <see cref="TypeScriptPocoPropertyInfo.Property"/>
        /// contains the new statement) or from the <see cref="TypeScriptPocoPropertyInfo.CreateMethodParameter"/>.
        /// <para>
        /// Note that these read only properties appear as optional in the create method.
        /// </para>
        /// </summary>
        public int ReadOnlyPropertyCount { get; }

        /// <summary>
        /// Gets the poco class part (the class appears at the top of the file, followed by interfaces).
        /// </summary>
        public ITSCodePart Part { get; }

        /// <summary>
        /// Gets the poco information.
        /// </summary>
        public IPocoRootInfo PocoRootInfo { get; }

        /// <summary>
        /// Gets a list of the properties that will be generated with their <see cref="TypeScriptPocoPropertyInfo.CreateMethodParameter"/>.
        /// First comes the properties that requires a parameter in the create method, then the simple optional properties and then the
        /// read only properties (that are optionals since we can automatically instantiate them).
        /// </summary>
        public IReadOnlyList<TypeScriptPocoPropertyInfo> Properties { get; }

        /// <summary>
        /// Gets the create method documentation.
        /// @param documentations from <see cref="CreateParameters"/> will be appended to it.
        /// </summary>
        public DocumentationBuilder CreateMethodDocumentation { get; }

        internal void AppendProperties( ITSCodePart b )
        {
            foreach( var p in Properties )
            {
                if( !String.IsNullOrWhiteSpace( p.Property.Comment ) )
                {
                    b.AppendDocumentation( p.Property.Comment );
                }
                if( p.PocoProperty.IsReadOnly ) b.Append( "readonly " );
                b.Append( p.Property.Name )
                 .Append( p.Property.Optional ? "?: " : ": " )
                 .Append( p.Property.Type )
                 .Append( ";" ).NewLine();
            }
        }

        internal void AppendCreateMethod( ITSCodePart b )
        {
            static void AppendCreateWithConfigSignature( ITSCodePart b, string typeName, bool withUndefined )
            {
                b.Append( "static create( config" ).Append( withUndefined ? "?" : "" ).Append( ": (c: " ).Append( typeName ).Append( ") => void ) : " ).Append( typeName ).NewLine();
            }

            // First comes the create overload with all the parameters (it's the default one).
            if( CreateMethodDocumentation.IsEmpty )
            {
                CreateMethodDocumentation.Append( "Factory method that exposes all the properties as parameters.", endWithNewline: true );
            }
            int createParams = 0;
            foreach( var prop in Properties )
            {
                var p = prop.CreateMethodParameter;
                if( p == null ) continue;

                var auto = prop.PocoProperty.IsReadOnly
                            ? " (Optional, automatically instantiated.) "
                            : prop.Property.Optional
                                ? " (Optional, defaults to undefined) "
                                : " ";
                bool hasComment = !String.IsNullOrEmpty( p.Comment );
                ++createParams;
                if( auto.Length > 1 || hasComment )
                {
                    CreateMethodDocumentation.Append( "@param " ).Append( p.Name ).Append( auto, endWithNewline: !hasComment );
                    if( hasComment )
                    {
                        CreateMethodDocumentation.AppendText( p.Comment!, trimFirstLine: true, trimLastLines: true, startNewLine: false, endWithNewline: true );
                    }
                }
            }
            string firstParameterName = "config";
            bool paramsOnOneLine = createParams <= 2;
            b.Append( CreateMethodDocumentation.GetFinalText() )
             .Append( "static create( " );
            if( !paramsOnOneLine ) b.NewLine();

            bool atLeastOne = false;
            foreach( var prop in Properties )
            {
                var p = prop.CreateMethodParameter;
                if( p == null ) continue;
                
                if( atLeastOne )
                {
                    b.Append( "," );
                    if( !paramsOnOneLine ) b.NewLine();
                }
                else
                {
                    firstParameterName = p.Name;
                    atLeastOne = true;
                }
                b.Append( p.Name )
                 .Append( p.Optional ? "?: " : ": " )
                 .Append( p.Type );
            }
            b.Append( " ) : " ).Append( TypeName ).NewLine();

            // Then the overload with config function.
            b.AppendDocumentation(
@"Creates a new command and calls a configurator for it.
@param config A function that configures the new command." );
            AppendCreateWithConfigSignature( b, TypeName, false );

            // And the implementation.
            b.Append( "// Implementation." ).NewLine();
            if( atLeastOne )
            {
                b.Append( "static create( " );
                if( !paramsOnOneLine ) b.NewLine();
                atLeastOne = false;
                foreach( var prop in Properties )
                {
                    var p = prop.CreateMethodParameter;
                    if( p == null ) continue;

                    if( atLeastOne )
                    {
                        b.Append( "," );
                        if( !paramsOnOneLine ) b.NewLine();
                    }
                    b.Append( p.Name )
                     .Append( atLeastOne || p.Optional ? "?: " : ": " )
                     .Append( p.Type );
                    if( !atLeastOne )
                    {
                        b.Append( "|((c:" ).Append( TypeName ).Append( ") => void)" );
                        atLeastOne = true;
                    }
                }
                b.Append( " )" ).OpenBlock()
                 .Append( "const c = new " ).Append( TypeName ).Append( "(" ).CreatePart( out var ctorParamsPart ).Append( ");" ).NewLine()
                 .Append( "if( typeof " ).Append( firstParameterName ).Append( " === 'function' ) " ).Append( firstParameterName ).Append( "(c);" ).NewLine()
                 .Append( "else" ).OpenBlock();

                bool atLeastOneReadOnly = false;
                atLeastOne = false;
                foreach( var prop in Properties )
                {
                    if( prop.PocoProperty.IsReadOnly )
                    {
                        if( atLeastOneReadOnly ) ctorParamsPart.Append( ", " );
                        else atLeastOneReadOnly = true;
                        ctorParamsPart.Append( prop.CreateMethodParameter?.Name ?? "undefined" );
                    }
                    if( prop.OverriddenAssignmentCreateMethodCode != null )
                    {
                        if( atLeastOne ) b.NewLine();
                        else atLeastOne = true;
                        b.Append( prop.OverriddenAssignmentCreateMethodCode );
                    }
                    else
                    {
                        if( prop.CreateMethodParameter != null && !prop.PocoProperty.IsReadOnly )
                        {
                            if( atLeastOne ) b.NewLine();
                            else atLeastOne = true;
                            b.Append( "c." ).Append( prop.Property.Name ).Append( " = " ).Append( prop.CreateMethodParameter.Name ).Append( ";" );
                        }
                    }
                }
                b.CloseBlock()
                 .Append( "return c;" )
                 .CloseBlock();
            }
            else
            {
                AppendCreateWithConfigSignature( b, TypeName, true );
                b.OpenBlock()
                 .Append( "const c = new " ).Append( TypeName ).Append( "();" ).NewLine()
                 .Append( "if( config ) config(c);" ).NewLine()
                 .Append( "return c;" )
                 .CloseBlock();
            }
        }

    }
}
