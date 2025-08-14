import { NgModule, Provider, EnvironmentProviders } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { DemoNgModule } from '../MiscDemo/demo-ng/demo-ng.module';
import { SomeAuthService } from '@local/ck-gen/CK/Ng/AspNet/Auth/SomeAuthService';


export type SourcedProvider = (EnvironmentProviders | Provider) & {source: string};

/**
 * Array-like of Provider or EnvironmentProviders that supports {@link exclude}
 * to remove some of them: they can be manually reinjected if nominal configuration
 * must be changed.
 */
export class SourcedProviders extends Array<SourcedProvider> {
    /**
     * Excludes all the providers issued by the given source.
     * At least one such provider must exist otherwise this throws.
     */
    exclude( sourceName: string ): SourcedProviders
    {
        let idx = this.findIndex( s => s.source === sourceName );
        if( idx < 0 ) throw new Error( `No provider from source '${sourceName}' found.` );
        do
        {
            this.splice( idx, 1 );
        }
        while( (idx = this.findIndex( s => s.source === sourceName )) >= 0 );
        return this;
    }
    
    static createFrom(o: SourcedProvider[]) {
        const sp = new SourcedProviders();
        sp.push(...o);
        return sp;
    }
}

@NgModule({
    imports: [
DemoNgModule
    ],
    exports: [
DemoNgModule
    ] })
export class CKGenAppModule {
    private static s( p: EnvironmentProviders | Provider, source: string ) : SourcedProvider
    {
        const s = <SourcedProvider>p;
        s.source = source; 
        return s;
    }

    static Providers : SourcedProviders = SourcedProviders.createFrom( [
CKGenAppModule.s( {provide: SomeAuthService, useValue: new SomeAuthService( 'Some explicit parameter' )}, "CK.Ng.AspNet.Auth.SomeAuthPackage" ),
CKGenAppModule.s( provideAnimationsAsync(), "CK.Ng.Zorro.ZorroPackage" ),

    ] );
}