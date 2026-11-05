using CK.Core;
using CK.Setup;
using CK.Testing;
using CK.TypeScript.Engine;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public sealed class LocalesSampleAttribute : ContextBoundDelegationAttribute
{
    public LocalesSampleAttribute()
        : base( "CK.TypeScript.Tests.LocalesSampleAttributeImpl, CK.TypeScript.Tests" )
    {
    }
}

public sealed class LocalesSampleAttributeImpl : TypeScriptGroupOrPackageAttributeImplExtension
{
    public LocalesSampleAttributeImpl( LocalesSampleAttribute attr, Type type )
        : base( attr, type )
    {
    }

    protected internal override bool OnResPackageAvailable( IActivityMonitor monitor,
                                                            TypeScriptContext context,
                                                            TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                            ResSpaceData spaceData,
                                                            ResPackage package )
    {
        context.AddLocalesContributor( package, new SampleContributor() );
        return true;
    }

    sealed class SampleContributor : TSLocalesContributor
    {
        public override void AddImports( IActivityMonitor monitor, ActiveCultureSet activeCultures, List<string> imports )
        {
            imports.Add( "import { registerSample } from 'sample-lib';" );
        }

        public override bool WritePrelude( IActivityMonitor monitor, ActiveCultureSet activeCultures, NormalizedCultureInfo defaultCulture, StringBuilder code )
        {
            code.Append( "registerSample( '" ).Append( defaultCulture.Name ).Append( "' );" ).AppendLine();
            return true;
        }

        public override IReadOnlyList<TSLocaleInfoField> LocaleInfoFields { get; } =
            [new TSLocaleInfoField( "sample", "string", ( monitor, culture ) => $"'{culture.Name}-sample'" )];

        public override bool WriteEpilogue( IActivityMonitor monitor, ActiveCultureSet activeCultures, NormalizedCultureInfo defaultCulture, StringBuilder code )
        {
            code.AppendLine( "export const SAMPLE = true;" );
            return true;
        }
    }
}

[TestFixture]
public class LocalesContributorTests
{
    [TypeScriptPackage]
    [LocalesSample]
    public class LocalesSamplePackage : TypeScriptPackage
    {
    }

    [Test]
    public async Task a_package_contributes_to_the_generated_locales_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        engineConfig.FirstBinPath.Types.Add( typeof( LocalesSamplePackage ) );
        await engineConfig.RunSuccessfullyAsync();

        var locales = File.ReadAllText( targetProjectPath.Combine( "ck-gen/ts-locales/locales.ts" ) ).ReplaceLineEndings( "\n" );
        locales.ShouldBe( """
            import { registerSample } from 'sample-lib';
            registerSample( 'en' );
            export async function loadTranslations( lang: string ): Promise<{ [key: string]: string }> {
              switch( lang ) {
                default: return ( await import( './en.json' ) ).default;
              }
            }

            export type LocaleInfo = {
              name: string;
              nativeName: string;
              englishName: string;
              id: number;
              sample: string;
            };

            export type CKLocales = {
              [localeCode: string]: LocaleInfo;
            };

            export const locales: CKLocales = {
              "en": { name: 'en', "nativeName": 'English', "englishName": 'English', "id": 221272233, "sample": 'en-sample' },
            }

            export const DEFAULT_LOCALE_INFO = locales['en'];
            export const SAMPLE = true;

            """.ReplaceLineEndings( "\n" ) );
    }
}
