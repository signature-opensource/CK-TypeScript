using CK.Core;
using CK.CrisLike;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    // This static class is only here to trigger the global FakeTypeScriptCrisCommandGeneratorImpl ITSCodeGenerator.
    // This is the same as the static class TypeScriptCrisCommandGenerator in CK.Cris.TypeScript package.
    [ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CrisLike.FakeTypeScriptCrisCommandGeneratorImpl, CK.StObj.TypeScript.Tests" )]
    public static class FakeTypeScriptCrisCommandGenerator {}

    // Hard coded Cris-like TypeScriptCrisCommandGeneratorImpl.
    public class FakeTypeScriptCrisCommandGeneratorImpl : ITSCodeGenerator
    {
        TSType? _crisPoco;
        TSType? _abstractCommand;
        TSType? _commandPart;
        TSType? _command;

        // We don't add anything to the default IPocoType handling.
        public virtual bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, TSTypeRequiredEventArgs e ) => true;

        // We don't generate global code.
        public virtual bool GenerateCode( IActivityMonitor monitor, TypeScriptContext g ) => true;

        public virtual bool Initialize( IActivityMonitor monitor, TypeScriptContext context )
        {
            // This can be called IF multiple contexts must be generated:
            // we reset the cached instance here.
            _command = null;
            context.PrimaryPocoGenerating += OnPrimaryPocoGenerating;
            context.AbstractPocoGenerating += OnAbstractPocoGenerating;
            return true;
        }

        void OnAbstractPocoGenerating( object? sender, GeneratingAbstractPocoEventArgs e )
        {
            // Filtering out redundant ICommand, ICommand<T>: in TypeScript type name is
            // unique (both are handled by ICommand<TResult = void>).
            // On the TypeScript side, we have always a ICommand<T> where T can be void.

            // By filtering out the base interface it doesn't appear in the base interfaces
            // nor in the branded type. 

            // The ICommand<T> branding substitutes the "ICommand":any from the IAbstractCommand
            // with "ICommand":PocovariantBrand<TResult>" when TResult is not void but restores
            // "ICommand":any when the TResult is void.
            // This allows ICommand to be specialized in ICommand<Something> and enables a
            // ICommand (or IAbstractCommand or ICommandPart) to be assignable from any ICommand<T>.
            bool withoutResult = false;
            bool withResult = false;
            foreach( var i in e.ImplementedInterfaces )
            {
                if( !withResult && i.GenericTypeDefinition?.Type == typeof( ICommand<> ) )
                {
                    withResult = true;
                    if( withoutResult ) break;
                }
                if( !withoutResult && i.Type == typeof( ICommand ) )
                {
                    withoutResult = true;
                    if( withResult ) break;
                }
            }
            if( withResult && withoutResult )
            {
                e.ImplementedInterfaces = e.ImplementedInterfaces.Where( i => i.Type != typeof( ICommand ) );
            }
        }

        void OnPrimaryPocoGenerating( object? sender, GeneratingPrimaryPocoEventArgs e )
        {
            if( e.PrimaryPocoType.AbstractTypes.Any( a => a.Type == typeof(IAbstractCommand) ) )
            {
                e.PocoModelPart.NewLine()
                 .Append( "applyAmbientValues( command: any, a: any, o: any )" )
                 .OpenBlock()
                    .Append( "/*Apply HERE*/" )
                 .CloseBlock();
            }
        }

        public virtual bool OnResolveType( IActivityMonitor monitor,
                                           TypeScriptContext context,
                                           TypeBuilderRequiredEventArgs builder )
        {
            var t = builder.Type;
            // Hooks:
            //   - ICommand and ICommand<TResult>: they are both implemented by ICommand<TResult = void> in Model.ts.
            //   - IAbstractCommand, ICommandPart (alias on IAbstractCommand) and ICrisPoco.
            // 
            // Model.ts also implements ICommandModel, ExecutedCommand<T>, and CrisError.
            //
            if( t.Namespace == "CK.CrisLike" )
            {
                if( t.Name == "ICommand" || (t.IsGenericTypeDefinition && t.Name == "ICommand`1") )
                {
                    EnsureCrisCommandModel( monitor, context );
                    builder.ResolvedType = _command;
                }
                else if( t.Name == "ICommandPart" )
                {
                    EnsureCrisCommandModel( monitor, context );
                    builder.ResolvedType = _commandPart;
                }
                else if( t.Name == "IAbstractCommand" )
                {
                    EnsureCrisCommandModel( monitor, context );
                    builder.ResolvedType = _abstractCommand;
                }
                else if( t.Name == "ICrisPoco" )
                {
                    EnsureCrisCommandModel( monitor, context );
                    builder.ResolvedType = _crisPoco;
                }
            }
            return true;
        }

        [MemberNotNull(nameof(_command), nameof( _abstractCommand ), nameof( _commandPart ), nameof( _crisPoco ) )]
        void EnsureCrisCommandModel( IActivityMonitor monitor, TypeScriptContext context )
        {
            if( _command != null )
            {
                Throw.DebugAssert( _abstractCommand != null && _commandPart != null && _crisPoco != null );
                return;
            }

            var modelFile = context.Root.Root.FindOrCreateFile( "CK/Cris/Model.ts" );
            GenerateCrisModelFile( monitor, context, modelFile );
            //GenerateCrisEndpoint( monitor, modelFile.Folder.FindOrCreateFile( "CrisEndpoint.ts" ) );
            //GenerateCrisHttpEndpoint( monitor, modelFile.Folder.FindOrCreateFile( "HttpCrisEndpoint.ts" ) );
            _crisPoco = new TSType( "ICrisPoco", imports => imports.EnsureImport( modelFile, "ICrisPoco" ), null );
            _abstractCommand = new TSType( "IAbstractCommand", imports => imports.EnsureImport( modelFile, "IAbstractCommand" ), null );
            _commandPart = new TSType( "ICommandPart", imports => imports.EnsureImport( modelFile, "ICommandPart" ), null );
            _command = new TSType( "ICommand", imports => imports.EnsureImport( modelFile, "ICommand" ), null );

            static void GenerateCrisModelFile( IActivityMonitor monitor, TypeScriptContext context, TypeScriptFile fModel )
            {
                fModel.Imports.EnsureImport( monitor, typeof( SimpleUserMessage ) );
                fModel.Imports.EnsureImport( monitor, typeof( UserMessageLevel ) );
                var pocoType = context.GetTypeScriptPocoType( monitor );
                // Imports the IPoco itself...
                pocoType.EnsureRequiredImports( fModel.Imports );
                // ...and its IPocoModel and the PocovariantBrand type factory.
                fModel.Imports.EnsureImport( pocoType.File, "IPocoModel", "PocovariantBrand" );

                fModel.Body.Append( """
                                /**
                                 * Extends the Poco model. 
                                 **/
                                export interface ICommandModel extends IPocoModel {
                                    /**
                                     * This supports the CrisEdpoint implementation. This is not to be used directly.
                                     **/
                                    readonly applyAmbientValues: (command: any, a: any, o: any ) => void;
                                }

                                /** 
                                 * Abstraction of any Cris objects (currently only commands).
                                 **/
                                export interface ICrisPoco extends IPoco
                                {
                                    readonly _brand: IPoco["_brand"] & { "ICrisPoco": any };
                                }
                                
                                /** 
                                 * Command abstraction extends the Poco model.
                                 * The C# ICommand (without result) is the TypeScript ICommand<void>
                                 * and the ICommandPart is "erased" by being an alias of this IAbstractCommand.
                                 **/
                                export interface IAbstractCommand extends ICrisPoco
                                {
                                    readonly pocoModel: ICommandModel;
                                    readonly _brand: ICrisPoco["_brand"] & { "ICommand": any };
                                }
                                
                                /** 
                                 * The ICommandPart type is a pure alias on IAbstractCommand.
                                 **/
                                export type ICommandPart = IAbstractCommand;
                                                                                                
                                /** 
                                 * Command with or without a result.
                                 **/
                                export interface ICommand<TResult = void> extends IAbstractCommand {
                                    readonly _brand: Omit<IAbstractCommand["_brand"],"ICommand"> & { "ICommand": TResult extends void ? any : PocovariantBrand<TResult> };
                                }
                                
                                /** 
                                 * Captures the result of a command execution.
                                 **/
                                export type ExecutedCommand<T> = {
                                    /** The executed command. **/
                                    readonly command: ICommand<T>,
                                    /** The execution result. **/
                                    readonly result: CrisError | T,
                                    /** Optional correlation identifier. **/
                                    readonly correlationId?: string
                                };

                                /**
                                 * Captures communication, validation or execution error.
                                 **/
                                export class CrisError extends Error {
                                    /**
                                     * Get this error type.
                                     */
                                    public readonly errorType : "CommunicationError"|"ValidationError"|"ExecutionError";
                                    /**
                                     * Gets the messages. At least one message is guaranteed to exist.
                                     */
                                    public readonly messages: ReadonlyArray<SimpleUserMessage>; 
                                    /**
                                     * The Error.cause support is a mess. This replaces it at this level. 
                                     */
                                    public readonly innerError?: Error; 
                                    /**
                                     * When defined, enables to find the backend log entry.
                                     */
                                    public readonly logKey?: string; 
                                    /**
                                     * Gets the command that failed.
                                     */
                                    public readonly command: ICommand<unknown>;

                                    constructor( command: ICommand<unknown>, 
                                                 message: string, 
                                                 isValidationError: boolean,
                                                 innerError?: Error, 
                                                 messages?: ReadonlyArray<SimpleUserMessage>,
                                                 logKey?: string ) 
                                    {
                                        super( message );
                                        this.command = command;   
                                        this.errorType = isValidationError 
                                                            ? "ValidationError" 
                                                            : innerError ? "CommunicationError" : "ExecutionError";
                                        this.innerError = innerError;
                                        this.messages = messages && messages.length > 0 
                                                        ? messages
                                                        : [new SimpleUserMessage(UserMessageLevel.Error,message,0)];
                                        this.logKey = logKey;
                                    }
                                }
                                
                                """ );
            }
        }
    }
}
