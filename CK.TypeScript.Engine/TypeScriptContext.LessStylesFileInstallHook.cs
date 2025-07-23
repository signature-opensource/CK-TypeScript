using CK.BinarySerialization;
using CK.Core;
using CK.Less.Transform;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CK.Setup;

public sealed partial class TypeScriptContext // Less styles support.
{
    // Collector for [AppStyleImportAttribute].
    // This may be better hosted in an independent "AppStyleCollector" provided
    // to AppStyleImportAttributeImpl.OnResPackageAvailable once CK-ReaDI exists
    // but for the moment, this internal hook is fine.
    PriorityQueue<EnsureImportLine, int>? _appStylesImport;

    /// <summary>
    /// Called by AppStyleImportAttributeImpl.
    /// </summary>
    internal void AddAppStyle( int order, string importPath )
    {
        _appStylesImport ??= new PriorityQueue<EnsureImportLine, int>();
        _appStylesImport.Enqueue( new EnsureImportLine( ImportKeyword.None, ImportKeyword.None, importPath ), order );
    }

    static bool IsPrimaryLessFile( ITransformInstallableItem item )
    {
        Throw.DebugAssert( "styles".Length == 6 );
        var fName = Path.GetFileNameWithoutExtension( item.TargetPath.Path.AsSpan() );
        return fName.Equals( "styles", StringComparison.Ordinal )
               || IsFolderBasedName( fName, item.Resources.Package.DefaultTargetPath.LastPart );

        static bool IsFolderBasedName( ReadOnlySpan<char> fName, string folderName )
        {
            return fName.StartsWith( folderName, StringComparison.Ordinal )
                   && (fName.Length == folderName.Length || fName[folderName.Length] == '.');
        }
    }

    static bool ErrorAmbiguousPrimaryFile( IActivityMonitor monitor, ITransformInstallableItem item, ITransformInstallableItem fItem )
    {
        monitor.Error( $"""
                            Ambiguous primary less file for '{item.Resources}':
                            - file '{item.TargetPath.LastPart}'
                            - and '{fItem.TargetPath.LastPart}'
                            Both names match a primary less file, only one can exist. Alternate sylesheets can exist:
                            the file name must not start with the folder name.
                            Here, renaming the first one (for instance) 'dark.{item.TargetPath.LastPart}' or 'alt-{item.TargetPath.LastPart}' is possible.
                            """ );
        return false;
    }

    sealed class LessStylesFileInstallHook : TransformableFileInstallHook
    {
        readonly NormalizedPath _srcFolderPath;
        readonly PriorityQueue<EnsureImportLine, int>? _appStylesImport;
        TransformerHost.Language? _lessLanguage;
        // SpaceData.AllPackageResources.Length array of the ITransformInstallableItem that is the primary
        // less file of the IResPackageResources.
        // If there are more than one file that match the IsPackagePrimaryLessFile pattern, HandleInstall
        // signals an error.
        ITransformInstallableItem?[]? _primaryLessFiles;

        /// <summary>
        /// Initializes a new LessStylesFileInstallHook.
        /// </summary>
        /// <param name="srcFolderPath">
        /// The application /src folder.
        /// When <see cref="NormalizedPath.IsEmptyPath"/> the src/styles.less file is not updated with the imports of the
        /// variables and [AppStyleImport] stylesheets.
        /// </param>
        /// <param name="appStylesImport">The collected [AppStyleImport].</param>
        public LessStylesFileInstallHook( NormalizedPath srcFolderPath, PriorityQueue<EnsureImportLine, int>? appStylesImport )
        {
            _srcFolderPath = srcFolderPath;
            _appStylesImport = appStylesImport;
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
            _primaryLessFiles = new ITransformInstallableItem?[SpaceData.AllPackageResources.Length];
        }

        public override bool HandleInstall( IActivityMonitor monitor,
                                            ITransformInstallableItem item,
                                            ref string finalText,
                                            IResourceSpaceItemInstaller installer,
                                            out bool handled )
        {
            Throw.DebugAssert( IsInitialized );
            // We never declare to handle a .less file (that is not a variables.less) because
            // we want it to be installed. Only variable files are truly hooked.
            handled = false;
            if( _lessLanguage != null
                && item.LanguageIndex == _lessLanguage.Index
                && IsPrimaryLessFile( item ) )
            {
                Throw.DebugAssert( _primaryLessFiles != null );
                Throw.DebugAssert( "Hook for variables must be BEFORE hook for less files.", !IsVariablesFile( item ) );
                ref var fItem = ref _primaryLessFiles[item.Resources.Index];
                if( fItem == null )
                {
                    fItem = item;
                }
                else
                {
                    return ErrorAmbiguousPrimaryFile( monitor, item, fItem );
                }
            }
            return true;
        }

        public override void StopInstall( IActivityMonitor monitor, bool success, IResourceSpaceItemInstaller installer )
        {
            Throw.DebugAssert( IsInitialized );

            if( !_srcFolderPath.IsEmptyPath )
            {
                TransformAppStyles( monitor, _srcFolderPath.Combine( "styles.less" ), _appStylesImport );
            }
            if( _lessLanguage != null )
            {
                Throw.DebugAssert( _primaryLessFiles != null );

                var stableStyles = new StringBuilder( """
                                            @import './stable.variables.less';


                                            """ );
                var localStyles = SpaceData.HasLiveState
                                    ? new StringBuilder( """
                                            @import './local.variables.less';


                                            """ )
                                    : null;
                foreach( var item in _primaryLessFiles )
                {
                    if( item != null )
                    {
                        var b = localStyles != null && item.IsLocalItem
                                   ? localStyles
                                   : stableStyles;
                        b.Append( "@import '../" ).Append( item.TargetPath ).Append( "';" ).AppendLine();
                    }
                }
                if( localStyles != null )
                {
                    installer.Write( "styles/stable.styles.less", stableStyles.ToString() );
                    installer.Write( "styles/local.styles.less", localStyles.ToString() );
                    installer.Write( "styles/styles.less", """
                        @import './stable.styles.less';
                        @import './local.styles.less';

                        """ );
                }
                else
                {
                    installer.Write( "styles/styles.less", stableStyles.ToString() );
                }
            }

            static void TransformAppStyles( IActivityMonitor monitor,
                                            NormalizedPath stylesFilePath,
                                            PriorityQueue<EnsureImportLine, int>? appStylesImport )
            {
                var ckGenStyles = new EnsureImportLine( ImportKeyword.None,
                                                        ImportKeyword.None,
                                                        "../ck-gen/styles/styles.less" );
                IEnumerable<EnsureImportLine> imports;
                if( appStylesImport != null )
                {
                    var toImport = new List<EnsureImportLine>( appStylesImport.Count + 1 );
                    while( appStylesImport.TryDequeue( out var imp, out _ ) )
                    {
                        toImport.Add( imp );
                    }
                    toImport.Add( ckGenStyles );
                    imports = toImport;
                }
                else
                {
                    imports = [ckGenStyles];
                }
                string text;
                if( File.Exists( stylesFilePath ) )
                {
                    text = File.ReadAllText( stylesFilePath );
                }
                else
                {
                    var srcDirectory = Path.GetDirectoryName( stylesFilePath );
                    Throw.CheckState( srcDirectory != null );
                    if( !Directory.Exists( srcDirectory ) )
                    {
                        monitor.Info( $"Creating '/src' directory: '{srcDirectory}'." );
                        Directory.CreateDirectory( srcDirectory );
                    }
                    text = "";
                }
                var result = new LessAnalyzer().TryParse( monitor, text );
                if( result != null )
                {
                    using( var editor = new SourceCodeEditor( monitor, result.SourceCode ) )
                    {
                        EnsureImportStatement.EnsureOrderedImports( editor, imports );
                        if( !editor.HasError )
                        {
                            text = result.SourceCode.ToString();
                        }
                    }
                    File.WriteAllText( stylesFilePath, text );
                }
            }

        }

        protected override bool ShouldWriteLiveState => _lessLanguage != null;

        protected override void WriteLiveState( IBinarySerializer s )
        {
            Throw.DebugAssert( IsInitialized
                               && _lessLanguage != null
                               && _primaryLessFiles != null );
            s.Writer.Write( _lessLanguage.Index );
            // Saves the LocalItem's primary variables files.
            foreach( var res in SpaceData.LocalPackageResources )
            {
                s.WriteNullableObject( _primaryLessFiles[res.Index] );
            }
        }

        public static ILiveTransformableFileInstallHook? ReadLiveState( IActivityMonitor monitor,
                                                                        ResSpaceData spaceData,
                                                                        IBinaryDeserializer d )
        {
            int lessLanguageIndex = d.Reader.ReadInt32();
            var primaryLessFiles = new ITransformInstallableItem?[spaceData.LocalPackageResources.Length];
            for( int i = 0; i < primaryLessFiles.Length; i++ )
            {
                var item = d.ReadNullableObject<ITransformInstallableItem>();
                if( item != null )
                {
                    Throw.DebugAssert( i == item.Resources.LocalIndex );
                    primaryLessFiles[i] = item;
                }
            }
            return new LiveHook( spaceData, lessLanguageIndex, primaryLessFiles );
        }

        sealed class LiveHook : ILiveTransformableFileInstallHook
        {
            readonly int _lessLanguageIndex;
            readonly ITransformInstallableItem?[] _primaryLessFiles;
            bool _rebuildAll;

            public LiveHook( ResSpaceData spaceData,
                             int lessLanguageIndex,
                             ITransformInstallableItem?[] primaryLessFiles )
            {
                _lessLanguageIndex = lessLanguageIndex;
                _primaryLessFiles = primaryLessFiles;
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
                // We don't declare that we handle the file because we always want it to be eventually deleted.
                handled = false;
                // No need to test the file name here.
                if( item.LanguageIndex == _lessLanguageIndex )
                {
                    // If we handled this item, clears our memory and signals
                    // a required rebuild.
                    ref var fItem = ref _primaryLessFiles[item.Resources.LocalIndex];
                    if( fItem == item )
                    {
                        fItem = null;
                        _rebuildAll = true;
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
                if( item.LanguageIndex == _lessLanguageIndex
                    && IsPrimaryLessFile( item ) )
                {
                    Throw.DebugAssert( "Hook for variables must be BEFORE hook for less files.",
                                        !IsVariablesFile( item ) );
                    // Variables files are not installed.
                    handled = true;
                    // Check new (rebuild all) or clash (ErrorAmbiguousVariablesFile).
                    ref var fItem = ref _primaryLessFiles[item.Resources.LocalIndex];
                    if( fItem == null || fItem == item )
                    {
                        fItem = item;
                    }
                    else
                    {
                        return ErrorAmbiguousVariablesFile( monitor, item, fItem );
                    }
                    _rebuildAll = true;
                }
                return true;
            }

            public void StopInstall( IActivityMonitor monitor, bool success, IResourceSpaceItemInstaller installer )
            {
                if( _rebuildAll )
                {
                    var localStyles = new StringBuilder( """
                                            @import './local.variables.less';


                                            """ );
                    foreach( var item in _primaryLessFiles )
                    {
                        if( item != null )
                        {
                            localStyles.Append( "@import '../" ).Append( item.TargetPath ).Append( "';" ).AppendLine();
                        }
                    }
                    monitor.Info( $"Updated 'styles/local.styles.less' file." );
                    installer.Write( "styles/local.styles.less", localStyles.ToString() );
                }
            }
        }

    }
}
