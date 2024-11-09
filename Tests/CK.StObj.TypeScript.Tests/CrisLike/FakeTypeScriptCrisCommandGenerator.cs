using CK.Core;
using CK.CrisLike;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.CrisLike;

// This static class is only here to trigger the global FakeTypeScriptCrisCommandGeneratorImpl ITSCodeGeneratorFactory.
// This is the same as the static class TypeScriptCrisCommandGenerator in CK.Cris.TypeScript package.
[ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CrisLike.FakeTypeScriptCrisCommandGeneratorImpl, CK.StObj.TypeScript.Tests" )]
public static class FakeTypeScriptCrisCommandGenerator { }

// Hard coded Cris-like TypeScriptCrisCommandGeneratorImpl.
public sealed class FakeTypeScriptCrisCommandGeneratorImpl : ITSCodeGeneratorFactory
{
    ITSCodeGenerator? ITSCodeGeneratorFactory.CreateTypeScriptGenerator( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        if( initializer.EnsureRegister( monitor, typeof( IAspNetCrisResult ), mustBePocoType: true )
            && initializer.EnsureRegister( monitor, typeof( IAspNetCrisResultError ), mustBePocoType: true )
            && initializer.EnsureRegister( monitor, typeof( IUbiquitousValues ), mustBePocoType: true ) )
        {
            return new CodeGen();
        }
        return null;
    }

    // This is public and not sealed as the FakeTypeScriptCrisCommandGeneratorWithFolders specializes it
    // (to test folder/file mapping) but in real life, there's only one such private sealed CodeGen.
    public class CodeGen : ITSCodeGenerator
    {
        TypeScriptFile? _modelFile;
        ITSDeclaredFileType? _crisPoco;
        ITSDeclaredFileType? _abstractCommand;
        ITSDeclaredFileType? _command;

        public virtual bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
        {
            context.PocoCodeGenerator.PrimaryPocoGenerating += OnPrimaryPocoGenerating;
            context.PocoCodeGenerator.AbstractPocoGenerating += OnAbstractPocoGenerating;
            return true;
        }

        // We don't add anything to the default IPocoType handling.
        public virtual bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

        void OnAbstractPocoGenerating( object? sender, GeneratingAbstractPocoEventArgs e )
        {
            // Filtering out redundant ICommand, ICommand<T>: in TypeScript type name is
            // unique (both are handled by ICommand<TResult = void>).
            // On the TypeScript side, we have always a ICommand<T> where T can be void.

            // By filtering out the base interface it doesn't appear in the base interfaces
            // nor in the branded type. 
            if( HasICommand( e.AbstractPocoType, e.ImplementedInterfaces, out var mustRemoveICommand ) && mustRemoveICommand )
            {
                e.ImplementedInterfaces = e.ImplementedInterfaces.Where( i => i.Type != typeof( ICommand ) );
            }
        }

        static bool HasICommand( IPocoType t, IEnumerable<IAbstractPocoType> implementedInterfaces, out bool mustRemoveICommand )
        {
            IPocoType? typedResult = null;
            bool hasICommand = false;
            foreach( var i in implementedInterfaces )
            {
                if( i.GenericTypeDefinition?.Type == typeof( ICommand<> ) )
                {
                    var tResult = i.GenericArguments[0].Type;
                    if( typedResult != null )
                    {
                        // This has been already checked.
                        throw new CKException( $"{t} returns both '{typedResult}' and '{tResult}'." );
                    }
                    typedResult = tResult;
                }
                if( i.Type == typeof( ICommand ) )
                {
                    hasICommand = true;
                }
            }
            mustRemoveICommand = hasICommand && typedResult != null;
            return hasICommand || typedResult != null;
        }

        void OnPrimaryPocoGenerating( object? sender, GeneratingPrimaryPocoEventArgs e )
        {
            if( HasICommand( e.PrimaryPocoType, e.ImplementedInterfaces, out var mustRemoveICommand ) )
            {
                if( mustRemoveICommand )
                {
                    e.ImplementedInterfaces = e.ImplementedInterfaces.Where( i => i.Type != typeof( ICommand ) );
                }
                e.PocoTypePart.File.Imports.ImportFromFile( EnsureCrisCommandModel( e.Monitor, e.TypeScriptContext ), "ICommandModel" );
                e.PocoTypePart.NewLine()
                    .Append( "get commandModel(): ICommandModel { return " ).Append( e.TSGeneratedType.TypeName ).Append( ".#m; }" ).NewLine()
                    .NewLine()
                    .Append( "static #m = " )
                    .OpenBlock()
                        .Append( "applyUbiquitousValues( command: any, a: any, o: any )" )
                        .OpenBlock()
                        .Append( "/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/" )
                        .CloseBlock()
                    .CloseBlock();
            }
        }

        public virtual bool OnResolveType( IActivityMonitor monitor,
                                           TypeScriptContext context,
                                           RequireTSFromTypeEventArgs builder )
        {
            var t = builder.Type;
            // Hooks:
            //   - ICommand and ICommand<TResult>: they are both implemented by ICommand<TResult = void> in Model.ts.
            //   - IAbstractCommand and ICrisPoco.
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

        [MemberNotNull( nameof( _command ), nameof( _abstractCommand ), nameof( _crisPoco ) )]
        TypeScriptFile EnsureCrisCommandModel( IActivityMonitor monitor, TypeScriptContext context )
        {
            if( _modelFile == null )
            {
                _modelFile = context.Root.Root.FindOrCreateTypeScriptFile( "CK/Cris/Model.ts" );
                GenerateCrisModelFile( monitor, context, _modelFile );

                var tA = (ITSFileType)context.Root.TSTypes.ResolveTSType( monitor, typeof( IUbiquitousValues ) );
                _crisPoco = _modelFile.DeclareType( "ICrisPoco" );
                _abstractCommand = _modelFile.DeclareType( "IAbstractCommand" );
                _command = _modelFile.DeclareType( "ICommand" );
            }
            Throw.DebugAssert( _command != null && _abstractCommand != null && _crisPoco != null );
            return _modelFile;

            static void GenerateCrisModelFile( IActivityMonitor monitor, TypeScriptContext context, TypeScriptFile fModel )
            {
                fModel.Imports.EnsureImport( monitor, typeof( SimpleUserMessage ) );
                fModel.Imports.EnsureImport( monitor, typeof( UserMessageLevel ) );
                var pocoType = context.Root.TSTypes.ResolveTSType( monitor, typeof( IPoco ) );
                // Imports the IPoco itself...
                pocoType.EnsureRequiredImports( fModel.Imports );

                fModel.Body.Append( """
                                /**
                                 * Describes a Command type. 
                                 **/
                                export interface ICommandModel {
                                    /**
                                     * This supports the CrisEndpoint implementation. This is not to be used directly.
                                     **/
                                    readonly applyUbiquitousValues: (command: any, a: any, o: any ) => void;
                                }

                                /** 
                                 * Abstraction of any Cris objects (currently only commands).
                                 **/
                                export interface ICrisPoco extends IPoco
                                {
                                    readonly _brand: IPoco["_brand"] & {"ICrisPoco": any};
                                }

                                /** 
                                 * Command abstraction.
                                 **/
                                export interface IAbstractCommand extends ICrisPoco
                                {
                                    /** 
                                     * Gets the command model.
                                     **/
                                    get commandModel(): ICommandModel;

                                    readonly _brand: ICrisPoco["_brand"] & {"ICommand": any};
                                }

                                /** 
                                 * Command with or without a result.
                                 * The C# ICommand (without result) is the TypeScript ICommand<void>.
                                 **/
                                export interface ICommand<out TResult = void> extends IAbstractCommand {
                                    readonly _brand: IAbstractCommand["_brand"] & {"ICommandResult": void extends TResult ? any : TResult};
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
