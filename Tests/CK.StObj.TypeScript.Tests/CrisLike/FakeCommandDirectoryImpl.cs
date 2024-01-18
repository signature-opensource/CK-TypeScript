using CK.Core;
using CK.CrisLike;
using CK.Setup;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    public record MinimalCommandModel( IPocoType? ResultType );

    /// <summary>
    /// Triggers the <see cref="FakeCommandDirectoryImpl"/>.
    /// In actual code this is a ISingletonAutoService that exposes all the ICommandModel.
    /// </summary>
    [ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CrisLike.FakeCommandDirectoryImpl, CK.StObj.TypeScript.Tests" )]
    public static class FakeCommandDirectory
    {
        // This is pure fake of course...
        public static IDictionary<IPrimaryPocoType, MinimalCommandModel> Commands => FakeCommandDirectoryImpl._models;
    }


    // Hard coded fake CommandDirectoryImpl: resolves the hopefully unique TResult of ICommand<>
    // but doesn't generate the C# code of the FakeCommandDirectoryImpl that is
    // a static class here.
    public class FakeCommandDirectoryImpl : ICSCodeGenerator
    {
        public static readonly Dictionary<IPrimaryPocoType, MinimalCommandModel> _models = new();

        public CSCodeGenerationResult Implement( IActivityMonitor monitor, ICSCodeGenerationContext codeGenContext )
        {
            _models.Clear();
            bool success = true;
            var pocoTypeSystem = codeGenContext.Assembly.GetPocoTypeSystem();
            var allCommands = pocoTypeSystem.FindByType<IAbstractPocoType>( typeof( IAbstractCommand ) )?.PrimaryPocoTypes;
            if( allCommands == null || !allCommands.Any() )
            {
                monitor.Warn( $"No ICommand found." );
            }
            else
            {
                foreach( var c in allCommands )
                {
                    if( c.IsExchangeable )
                    {
                        var resultTypes = c.MinimalAbstractTypes.Where( a => a.GenericTypeDefinition?.Type == typeof( ICommand<> ) )
                                                                .Select( a => a.GenericArguments[0].Type )
                                                                .ToList();
                        IPocoType? resultType = null;
                        if( resultTypes.Count > 0 )
                        {
                            if( resultTypes.Count > 1 )
                            {
                                var conflicts = resultTypes.Select( r => r.ToString() ).Concatenate();
                                monitor.Error( $"Command Result Type conflict for '{c}': {conflicts}" );
                                success = false;
                            }
                            resultType = resultTypes[0];
                        }
                        if( success )
                        {
                            _models.Add( c, new MinimalCommandModel( resultType ) );
                        }
                    }
                }
            }
            return success ? CSCodeGenerationResult.Success : CSCodeGenerationResult.Failed;
        }
    }
}
