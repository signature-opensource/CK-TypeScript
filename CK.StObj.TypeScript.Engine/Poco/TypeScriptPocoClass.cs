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
                                      List<TypeScriptVarType> createParams )
        {
            TypeName = className;
            Part = p;
            PocoRootInfo = info;
            Properties = props;
            CreateMethodDocumentation = new DocumentationBuilder();
            CreateParameters = createParams;
        }

        /// <summary>
        /// Gets the class name.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the poco class part (the class appears at the top of the file, followed by interfaces).
        /// </summary>
        public ITSCodePart Part { get; }

        /// <summary>
        /// Gets the poco information.
        /// </summary>
        public IPocoRootInfo PocoRootInfo { get; }

        /// <summary>
        /// Gets a mutable list of properties that will be generated.
        /// </summary>
        public List<TypeScriptPocoPropertyInfo> Properties { get; }

        /// <summary>
        /// Gets the create method documentation.
        /// @param documentations from <see cref="CreateParameters"/> will be appended to it.
        /// </summary>
        public DocumentationBuilder CreateMethodDocumentation { get; }

        /// <summary>
        /// Gets a mutable list of the create method parameters.
        /// </summary>
        public List<TypeScriptVarType> CreateParameters { get; }

        internal void AppendProperties( ITSCodePart b )
        {
            foreach( var p in Properties )
            {
                b.Append( p.Property ).Append( ";" ).NewLine();
            }
        }

        internal void AppendCreateMethod( ITSCodePart b )
        {
            static void AppendCreateWithConfigSignature( ITSCodePart b, string typeName, bool withUndefined )
            {
                b.Append( "static create( config" ).Append( withUndefined ? "?" : "" ).Append( ": (c: " ).Append( typeName ).Append( ") => void ) : " ).Append( typeName ).NewLine();
            }

            // Sorts the parameters: first come the required ones.
            CreateParameters.Sort( ( x, y ) => x.Optional ? (y.Optional ? 0 : 1) : (y.Optional ? -1 : 0) );

            // First comes the create overload with all the parameters (it's the default one).
            if( CreateMethodDocumentation.IsEmpty )
            {
                CreateMethodDocumentation.Append( "Factory method that exposes all the properties as parameters.", endWithNewline: true );
            }
            foreach( var p in CreateParameters )
            {
                if( !String.IsNullOrEmpty( p.Comment ) )
                {
                    CreateMethodDocumentation.Append( "@param " ).Append( p.Name ).Append( " " );
                    CreateMethodDocumentation.AppendText( p.Comment, trimFirstLine: true, trimLastLines: true, startNewLine: false, endWithNewline: true );
                }
            }

            string firstParameterName = "config";
            bool paramsOnOneLine = CreateParameters.Count <= 2;
            b.Append( CreateMethodDocumentation.GetFinalText() )
             .Append( "static create( " );
            if( !paramsOnOneLine ) b.NewLine();

            bool atLeastOne = false;
            foreach( var p in CreateParameters )
            {
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
                foreach( var p in CreateParameters )
                {
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
                 .Append( "const c = new " ).Append( TypeName ).Append( "();" ).NewLine()
                 .Append( "if( typeof " ).Append( firstParameterName ).Append( " === 'function' ) " ).Append( firstParameterName ).Append( "(c);" ).NewLine()
                 .Append( "else" ).OpenBlock();

                atLeastOne = false;
                foreach( var p in Properties )
                {
                    if( atLeastOne ) b.NewLine();
                    else atLeastOne = true;
                    if( p.OverriddenAssignmentCreateMethodCode != null )
                    {
                        b.Append( p.OverriddenAssignmentCreateMethodCode );
                    }
                    else
                    {
                        if( CreateParameters.Any( a => a.Name == p.ParameterName ) )
                        {
                            b.Append( "c." ).Append( p.Property.Name ).Append( " = " ).Append( p.ParameterName ).Append( ";" );
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
