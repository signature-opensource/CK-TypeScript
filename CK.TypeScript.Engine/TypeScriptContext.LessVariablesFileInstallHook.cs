using CK.BinarySerialization;
using CK.Core;
using CK.Transform.Core;
using System;
using System.Text;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{

    static bool IsVariablesFile( ITransformInstallableItem item )
    {
        Throw.DebugAssert( "variables".Length == 9 );
        var fName = System.IO.Path.GetFileNameWithoutExtension( item.TargetPath.Path.AsSpan() );
        return fName.EndsWith( "variables" )
            && (fName.Length == 9
                || (fName[^10] == '.'
                    && IsFolderBasedName( fName, item.Resources.Package.DefaultTargetPath.LastPart )));

        static bool IsFolderBasedName( ReadOnlySpan<char> fName, string folderName )
        {
            return fName.Length >= folderName.Length + 10
                   && fName.StartsWith( folderName, StringComparison.Ordinal )
                   && fName[folderName.Length] == '.';
        }
    }


    static bool ErrorAmbiguousVariablesFile( IActivityMonitor monitor, ITransformInstallableItem item, ITransformInstallableItem fItem )
    {
        monitor.Error( $"""
                        Ambiguous variables less file for '{item.Resources}':
                        - file '{item.TargetPath.LastPart}'
                        - and '{fItem.TargetPath.LastPart}'
                        Both names match a variables file variables.less file, only one can exist.
                        Here, renaming the first one (for instance) 'alt-{item.TargetPath.LastPart}', 'alt.{item.TargetPath.LastPart}' is possible.
                        """ );
        return false;
    }

    const string _commonHeader = """
        // This file contains the lifted content of all the "Res/variables.less", "Res/<package or group name>.variables.less",
        // "Res[After]/variables.less" or "Res[After]/<package or group name>.variables.less" (following their topological order)
        """;

    sealed class LocalBuilder
    {
        readonly StringBuilder _builder;

        public LocalBuilder( ResCoreData spaceData )
        {
            _builder = new StringBuilder( $"""
                    {_commonHeader}
                    // of the {spaceData.LocalPackages.Length} local packages.


                    """ );
        }

        public void Add( ITransformInstallableItem item, string finalText )
        {
            _builder.Append( "// " ).Append( item.TargetPath ).AppendLine()
                    .Append( finalText )
                    .AppendLine();
        }

        public override string ToString() => _builder.ToString();
    }

    sealed class LessVariablesFileInstallHook : TransformableFileInstallHook
    {
        TransformerHost.Language? _lessLanguage;

        // SpaceData.AllPackageResources.Length array of the ITransformInstallableItem that is the primary
        // variables file of the IResPackageResources.
        // If there are more than one file that match the IsPackageVariablesFile pattern, HandleInstall
        // signals an error.
        ITransformInstallableItem?[]? _primaryVariableFiles;
        // Captures the text of each primary variables file.
        string?[]? _finalTexts;

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

            _primaryVariableFiles = new ITransformInstallableItem?[SpaceData.AllPackageResources.Length];
            _finalTexts = new string?[SpaceData.AllPackageResources.Length];
        }

        public override bool HandleInstall( IActivityMonitor monitor,
                                            ITransformInstallableItem item,
                                            ref string finalText,
                                            IResourceSpaceItemInstaller installer,
                                            out bool handled )
        {
            Throw.DebugAssert( IsInitialized );
            handled = false;
            if( _lessLanguage != null
                && item.LanguageIndex == _lessLanguage.Index
                && IsVariablesFile( item ) )
            {
                // Variables files are not installed.
                handled = true;
                Throw.DebugAssert( _primaryVariableFiles != null && _finalTexts != null );
                ref var fItem = ref _primaryVariableFiles[item.Resources.Index];
                if( fItem == null )
                {
                    fItem = item;
                    _finalTexts[item.Resources.Index] = finalText;
                }
                else
                {
                    return ErrorAmbiguousVariablesFile( monitor, item, fItem );
                }
            }
            return true;
        }

        public override void StopInstall( IActivityMonitor monitor, bool success, IResourceSpaceItemInstaller installer )
        {
            Throw.DebugAssert( IsInitialized );
            if( _lessLanguage != null )
            {
                Throw.DebugAssert( _primaryVariableFiles != null && _finalTexts != null );

                StringBuilder stableVariables = new StringBuilder( $"""
                    {_commonHeader}
                    // of the {(SpaceData.HasLiveState
                                ? SpaceData.Packages.Length - SpaceData.LocalPackages.Length
                                : SpaceData.Packages.Length)} stable packages.

                    """ );

                var localVariables = SpaceData.HasLiveState
                                         ? new LocalBuilder( SpaceData )
                                         : null;

                foreach( var item in _primaryVariableFiles )
                {
                    if( item == null ) continue;
                    var text = _finalTexts[item.Resources.Index];
                    Throw.DebugAssert( text != null );
                    if( localVariables != null && item.IsLocalItem )
                    {
                        localVariables.Add( item, text );
                    }
                    else
                    {
                        stableVariables.Append( "// " ).Append( item.TargetPath ).AppendLine()
                                       .Append( text )
                                       .AppendLine();
                    }
                }
                if( localVariables != null )
                {
                    installer.Write( "styles/stable.variables.less", stableVariables.ToString() );
                    installer.Write( "styles/local.variables.less", localVariables.ToString() );
                    installer.Write( "styles/variables.less", """
                        @import './stable.variables.less';
                        @import './local.variables.less';

                        """ );
                }
                else
                {
                    installer.Write( "styles/variables.less", stableVariables.ToString() );
                }
            }
        }

        protected override bool ShouldWriteLiveState => _lessLanguage != null; 

        protected override void WriteLiveState( IBinarySerializer s )
        {
            Throw.DebugAssert( IsInitialized
                               && _lessLanguage != null
                               && _primaryVariableFiles != null
                               && _finalTexts != null );
            s.Writer.Write( _lessLanguage.Index );
            // Saves the LocalItem's only primary variables files
            // (with their captured text).
            foreach( var res in SpaceData.LocalPackageResources )
            {
                var item = _primaryVariableFiles[res.Index];
                s.WriteNullableObject( item );
                if( item != null )
                {
                    var text = _finalTexts[res.Index];
                    Throw.DebugAssert( text != null );
                    s.Writer.Write( text );
                }
            }
        }

        public static ILiveTransformableFileInstallHook? ReadLiveState( IActivityMonitor monitor,
                                                                        ResCoreData spaceData,
                                                                        IBinaryDeserializer d )
        {
            int lessLanguageIndex = d.Reader.ReadInt32();
            var primaryVariableFiles = new ITransformInstallableItem?[spaceData.LocalPackageResources.Length];
            var finalTexts = new string?[spaceData.LocalPackageResources.Length];
            for( int i = 0; i < primaryVariableFiles.Length; i++ )
            {
                var item = d.ReadNullableObject<ITransformInstallableItem>();
                if( item != null )
                {
                    Throw.DebugAssert( i == item.Resources.LocalIndex );
                    primaryVariableFiles[i] = item;
                    finalTexts[i] = d.Reader.ReadString();
                }
            }
            return new LiveHook( spaceData, lessLanguageIndex, primaryVariableFiles, finalTexts );
        }

        sealed class LiveHook : ILiveTransformableFileInstallHook
        {
            readonly ResCoreData _spaceData;
            readonly int _lessLanguageIndex;
            readonly ITransformInstallableItem?[] _primaryVariableFiles;
            readonly string?[] _finalTexts;
            bool _rebuildAll;

            public LiveHook( ResCoreData spaceData,
                             int lessLanguageIndex,
                             ITransformInstallableItem?[] primaryVariableFiles,
                             string?[] finalTexts )
            {
                _spaceData = spaceData;
                _lessLanguageIndex = lessLanguageIndex;
                _primaryVariableFiles = primaryVariableFiles;
                _finalTexts = finalTexts;
            }

            public void StartInstall( IActivityMonitor monitor )
            {
                _rebuildAll = false;
            }

            public bool HandleRemove( IActivityMonitor monitor,
                                      ITransformInstallableItem item,
                                      ILiveResourceSpaceItemInstaller installer,
                                      out bool handled )
            {
                handled = false;
                // No need to test the file name here.
                if( item.LanguageIndex == _lessLanguageIndex )
                {
                    // If we handled this item, clears our memory, signals a
                    // required rebuild and declare its handling (there's nothing to delete).
                    ref var fItem = ref _primaryVariableFiles[item.Resources.LocalIndex];
                    if( fItem == item )
                    {
                        fItem = null;
                        _finalTexts[item.Resources.LocalIndex] = null;
                        _rebuildAll = true;
                        handled = true;
                    }
                }
                return true;
            }

            public bool HandleInstall( IActivityMonitor monitor,
                                       ITransformInstallableItem item,
                                       ref string finalText,
                                       IResourceSpaceItemInstaller installer,
                                       out bool handled )
            {
                handled = false;
                if( item.LanguageIndex == _lessLanguageIndex )
                {
                    if( IsVariablesFile( item ) )
                    {
                        // Variables files are not installed.
                        handled = true;
                        // Check new (rebuild all) or clash (ErrorAmbiguousVariablesFile).
                        ref var fItem = ref _primaryVariableFiles[item.Resources.LocalIndex];
                        if( fItem == null || fItem == item )
                        {
                            fItem = item;
                            _finalTexts[item.Resources.LocalIndex] = finalText;
                            _rebuildAll = true;
                        }
                        else
                        {
                            return ErrorAmbiguousVariablesFile( monitor, item, fItem );
                        }
                    }
                }
                return true;
            }

            public void StopInstall( IActivityMonitor monitor, bool success, IResourceSpaceItemInstaller installer )
            {
                if( _rebuildAll )
                {
                    var localVariables = new LocalBuilder( _spaceData );
                    foreach( var item in _primaryVariableFiles )
                    {
                        if( item == null ) continue;
                        var text = _finalTexts[item.Resources.LocalIndex];
                        Throw.DebugAssert( text != null );
                        localVariables.Add( item, text );
                    }
                    monitor.Info( $"Updated 'styles/local.variables.less' file." );
                    installer.Write( "styles/local.variables.less", localVariables.ToString() );
                }
            }
        }
    }
}
