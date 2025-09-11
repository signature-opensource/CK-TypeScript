import { CTSType } from './CTSType';

    /**
    * Mere encapsulation of the culture name..
    **/
    export class ExtendedCultureInfo {

    constructor(public readonly name: string)
    {CTSType["ExtendedCultureInfo"].set( this );
}
    toString() { return this.name; }
    toJSON() { return this.name; }
}
