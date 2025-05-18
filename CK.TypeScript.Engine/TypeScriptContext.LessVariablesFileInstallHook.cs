using CK.BinarySerialization;
using CK.Core;
using CK.Transform.Core;
using System;
using System.Linq;
using System.Text;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    sealed class LessVariablesFileInstallHook : TransformableFileInstallHook
    {
        TransformerHost.Language? _lessLanguage;
        StringBuilder? _stableVariables;

        // When ResSpaceData.HasLiveState is true.
        // The "ck-gen/styles/local.variables.less" content file.
        StringBuilder? _localVariables;
        // The index in the _localVariables of each IResPackageResources.LocalIndex.
        // Even positions contain the start of the resource package section, odd ones contain the end.
        int[]? _localVariablesResIndex;

        public LessVariablesFileInstallHook()
        {
        }

        public override void StartInstall( IActivityMonitor monitor )
        {
            Throw.DebugAssert( IsInitialized );
            _lessLanguage = TransformerHost.FindLanguage( "Less", withFileExtensions: false );
            if( _lessLanguage == null )
            {
                monitor.Warn( "Missing Less language registration in TransformerHost." );
                return;
            }
            const string commonHeader = """
                // This file contains the lifted content of all the "Res/variables.less", "Res/<package or group name>.variables.less",
                // "Res[After]/variables.less" or "Res[After]/<package or group name>.variables.less" (following their topological order)
                """;

            if( SpaceData.HasLiveState )
            {
                _stableVariables = new StringBuilder( $"""
                    {commonHeader}
                    // of the {SpaceData.Packages.Length - SpaceData.LocalPackages.Length} stable packages.


                    """ );
                _localVariables = new StringBuilder( $"""
                    {commonHeader}
                    // of the {SpaceData.LocalPackages.Length} local packages.


                    """ );
                _localVariablesResIndex = new int[2*SpaceData.LocalPackageResources.Length];
            }
            else
            {
                _stableVariables = new StringBuilder( $"""
                    {commonHeader}
                    // of the {SpaceData.Packages.Length} packages.


                    """ );
            }
        }

        public override bool HandleInstall( IActivityMonitor monitor, ITransformInstallableItem item, string finalText, IResourceSpaceItemInstaller installer )
        {
            Throw.DebugAssert( IsInitialized );
            if( _lessLanguage != null && item.LanguageIndex == _lessLanguage.Index )
            {
                Throw.DebugAssert( "variables".Length == 9 );
                var fName = System.IO.Path.GetFileNameWithoutExtension( item.TargetPath.Path.AsSpan() );
                if( fName.EndsWith( "variables" )
                    && (fName.Length == 9
                        || (fName[^9] == '.'
                            && fName[..^9].Equals( item.Resources.Package.DefaultTargetPath.LastPart, StringComparison.Ordinal ))) )
                {
                    bool isLocal = SpaceData.HasLiveState && item.IsLocalItem;
                    StringBuilder? b;
                    if( isLocal )
                    {
                        Throw.DebugAssert( _localVariables != null && _localVariablesResIndex != null );
                        b = _localVariables;
                        _localVariablesResIndex[2 * item.Resources.LocalIndex] = b.Length;
                    }
                    else
                    {
                        Throw.DebugAssert( _stableVariables != null );
                        b = _stableVariables;
                    }
                    b.Append( "// " ).Append( item.TargetPath ).AppendLine()
                     .Append( finalText ).AppendLine();
                    if( isLocal )
                    {
                        Throw.DebugAssert( _localVariablesResIndex != null );
                        _localVariablesResIndex[2 * item.Resources.LocalIndex + 1] = b.Length;
                    }
                    return true;
                }
            }
            return false;
        }

        public override void StopInstall( IActivityMonitor monitor, IResourceSpaceItemInstaller installer )
        {
            Throw.DebugAssert( IsInitialized );
            if( _lessLanguage != null )
            {
                if( SpaceData.HasLiveState )
                {
                    Throw.DebugAssert( _localVariables != null );
                    installer.Write( "styles/local.variables.less", _localVariables.ToString() );
                }
                Throw.DebugAssert( _stableVariables != null );
                installer.Write( "styles/stable.variables.less", _stableVariables.ToString() );
            }
        }

        protected override bool ShouldWriteLiveState => _lessLanguage != null; 

        protected override void WriteLiveState( IBinarySerializer s )
        {
            Throw.DebugAssert( _lessLanguage != null && _localVariables != null && _localVariablesResIndex != null );
            s.Writer.Write( _lessLanguage.Index );
            s.WriteObject( _localVariablesResIndex );
        }

        public static ITransformableFileInstallHook? ReadLiveState( IActivityMonitor monitor, IBinaryDeserializer d )
        {
            return null;
        }

    }
}
