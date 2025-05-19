using CK.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    sealed class TypeScriptLocalesResourceHandler : LocalesResourceHandler
    {
        public TypeScriptLocalesResourceHandler( IResourceSpaceItemInstaller? installer,
                                                 ISpaceDataCache packageDataCache,
                                                 ActiveCultureSet activeCultures )
            : base( installer,
                    packageDataCache,
                    "ts-locales",
                    activeCultures,
                    InstallOption.Full )
        {
        }

        sealed record class TSCultureName
        {
            public TSCultureName( NormalizedCultureInfo culture )
            {
                Culture = culture;
                VarName = culture.Name.Replace( '-', '$' );
                StrName = $"\"{culture.Name}\"";
                IdName = $"\"{culture.Id}\"";
                Props = $"{StrName}: {VarName}, {IdName}: {VarName}";
            }

            public string VarName { get; }
            public string StrName { get; }
            public string IdName { get; }
            public string Props { get; }
            public NormalizedCultureInfo Culture { get; }
        }

        protected override bool Install( IActivityMonitor monitor )
        {
            if( !base.Install( monitor ) ) return false;
            if( Installer != null )
            {
                using( Installer.PushSubPath( RootFolderName ) )
                {
                    var tsC = ActiveCultures.AllActiveCultures.Select( c => new TSCultureName( c.Culture ) ).ToList();

                    var allImports = new StringBuilder();
                    foreach( var c in tsC )
                    {
                        var import = $"import * as {c.VarName} from './{c.Culture.Name}.json';";
                        allImports.Append( import ).AppendLine();

                        Installer.Write( $"locales.{c.Culture.Name}.ts", $$"""
                            {{import}}
                            const locales = { {{c.Props}} };
                            export default locales;
                            """ );
                    }
                    Installer.Write( $"locales.ts", $$"""
                            {{allImports}}
                            const locales = { {{tsC.Select( c => c.Props ).Concatenate()}} }; 
                            export default locales;
                            """ );
                }
            }
            return true;
        }
    }
}
