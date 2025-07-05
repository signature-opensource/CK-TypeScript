export const SymCTS = Symbol.for("CK.CTSType");
/**
 * CTSType is currently &lt;any&gt;. Strongly typing it involves to handle null
 * (detect and raise error) in depth.
 * This is not a validator (the backend is up to date by design) and null handling
 * is a (basic) part of validation.
 */
export const CTSType : any = {
    get typeFilterName(): string {return "TypeScript"; },
    toTypedJson( o: any ) : [string,unknown]|null {
        if( o == null ) return null;
        const t = o[SymCTS];
        if( !t ) throw new Error( "Untyped object. A type must be specified with CTSType." );
        return [t.name, t.json( o )];
    },
    fromTypedJson( o: any ) : unknown {
        if( o == null ) return undefined;
        if( !(o instanceof Array && o.length === 2) ) throw new Error( "Expected 2-cells array." );
        const t = (<any>CTSType)[o[0]];
        if( !t ) throw new Error( `Invalid type name: ${o[0]}.` );
        if( !t.set ) throw new Error( `Type name '${o[0]}' is not serializable.` );
        const j = t.nosj( o[1] );
        return j !== null && typeof j === 'object' ? t.set( j ) : j;
   },
   stringify( o: any, withType: boolean = true ) : string {
       const t = CTSType.toTypedJson( o );
       return JSON.stringify( withType ? t : t[1] );
   },
   parse( s: string ) : unknown {
       return CTSType.fromTypedJson( JSON.parse( s ) );
   },
}
