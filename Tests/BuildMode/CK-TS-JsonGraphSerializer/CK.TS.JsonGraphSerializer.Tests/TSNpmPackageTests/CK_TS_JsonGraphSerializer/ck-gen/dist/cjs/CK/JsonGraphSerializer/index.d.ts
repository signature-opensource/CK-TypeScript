/**
 * Defines options of the {@link serialize}.
 */
export interface ISerializeOptions {
    /**
     *  Defaults to "~$£€".
     */
    prefix?: string;
    /**
     * Optional function that can substitute an object before serialization.
     * Can return the "v" object or a falsy value when no substitution must be made.
     * The "rT" parameter is .
     * @param v The object about to be serialized.
     * @param rT The type indicator (depends on the prefix).
     * @returns A substituted object or the "v" object or a falsy value when no substitution must be made.
     */
    substitor?: (v: any, rT: string) => any;
}
/**
 * Defines options of the {@link deserialize}.
 */
export interface IDeserializeOptions {
    /**
     *  Defaults to "~$£€".
     */
    prefix?: string;
    /**
     * Optional function that can change the deserialized instance.
     * @param v The deserilized instance.
     * @param rT The type indicator (depends on the prefix).
     * @returns The final object.
     */
    activator?: (v: any, rT: string) => any;
}
/**
 * Serializes an object in JSON that can contain internal relationships.
 * Serialized objects contain a "<prefix>i" field that is the index
 * in the breadth-first traversal of the graph done by JSON.stringify().
 * References to already serialized objects are exposed as single-property
 * objects like { "<prefix>r": idx }.
 * The deserialize function uses the index to restore the complete graph.
 * Parameter prefix is optional. It defaults to "~$£€".
 * @param {object} o - The object to serialize
 * @param {object} options - Serialization options: prefix (default '~$£€'), substitor
 * @return {string} The serialized value.
 */
export declare function serialize(o: object, options?: ISerializeOptions): string;
/**
 * Deserializes a previously-serialized object graph.
 * Parameter prefix is optional and defaults to "~$£€": it must, of course,
 * be the same as the prefix used to serialize the graph.
 * @param {(string|object)} o - The serialized string, or parsed object.
 * @param {object} options - Deserialization options.
 * @return {object} The deserialized value.
 */
export declare function deserialize(s: string | object, options?: IDeserializeOptions): any;
//# sourceMappingURL=index.d.ts.map