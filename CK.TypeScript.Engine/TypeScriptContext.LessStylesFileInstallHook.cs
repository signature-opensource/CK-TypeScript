using CK.BinarySerialization;
using CK.Core;
using CK.Less.Transform;
using CK.Transform.Core;
using System.Collections.Generic;
using System.IO;

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

    sealed class LessStylesFileInstallHook : TransformableFileInstallHook
    {
        readonly NormalizedPath _srcFolderPath;
        readonly PriorityQueue<EnsureImportLine, int>? _appStylesImport;
        TransformerHost.Language? _lessLanguage;

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
        }

        public override bool HandleInstall( IActivityMonitor monitor,
                                            ITransformInstallableItem item,
                                            string finalText,
                                            IResourceSpaceItemInstaller installer )
        {
            Throw.DebugAssert( IsInitialized );
            if( _lessLanguage != null && item.LanguageIndex == _lessLanguage.Index )
            {

            }
            return false;
        }

        public override void StopInstall( IActivityMonitor monitor, IResourceSpaceItemInstaller installer )
        {
            Throw.DebugAssert( IsInitialized );

            if( !_srcFolderPath.IsEmptyPath )
            {
                TransformAppStyles( monitor, _srcFolderPath.Combine( "styles.less" ), _appStylesImport, SpaceData.HasLiveState );
            }

            static void TransformAppStyles( IActivityMonitor monitor,
                                            NormalizedPath stylesFilePath,
                                            PriorityQueue<EnsureImportLine, int>? appStylesImport,
                                            bool hasLiveState )
            {
                var ckGenLocalVariables = hasLiveState
                                            ? new EnsureImportLine( ImportKeyword.None,
                                                                    ImportKeyword.None,
                                                                    "../ck-gen/styles/local.variables.less" )
                                            : null;
                var ckGenStableVariables = new EnsureImportLine( ImportKeyword.None,
                                                                 ImportKeyword.None,
                                                                 "../ck-gen/styles/stable.variables.less" );
                IEnumerable<EnsureImportLine> imports;
                if( appStylesImport != null )
                {
                    var toImport = new List<EnsureImportLine>( appStylesImport.Count + 1 );
                    while( appStylesImport.TryDequeue( out var imp, out _ ) )
                    {
                        toImport.Add( imp );
                    }
                    if( ckGenLocalVariables != null ) toImport.Add( ckGenLocalVariables );
                    toImport.Add( ckGenStableVariables );
                    imports = toImport;
                }
                else
                {
                    imports = ckGenLocalVariables != null
                                ? [ckGenLocalVariables, ckGenStableVariables]
                                : [ckGenStableVariables];
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
        }
    }
}
