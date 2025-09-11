/**
* Simple immutable encapsulation of a string. No check is currently done on the 
* value format that must be in the '00000000-0000-0000-0000-000000000000' form.
*/
export class Guid {

    static #empty : Guid;   

    /**
    * The empty Guid '00000000-0000-0000-0000-000000000000' is the default.
    */
    public static get empty() { return Guid.#empty ??= new Guid('00000000-0000-0000-0000-000000000000'); }
    
    constructor( public readonly guid: string ) {
    }

    get value() {
        return this.guid;
      }

    toString() {
        return this.guid;
      }

    toJSON() {
        return this.guid;
      }
}
