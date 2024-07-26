export const CTSType = {
    toTypedJson(o) {
        if (o == null)
            return null;
        const t = o[SymCTS];
        if (!t)
            throw new Error("Untyped object. A type must be specified with CTSType.");
        return [t.name, t.json(o)];
    },
    fromTypedJson(o) {
        if (o == null)
            return undefined;
        if (!(o instanceof Array && o.length === 2))
            throw new Error("Expected 2-cells array.");
        var t = CTSType[o[0]];
        if (!t)
            throw new Error(`Invalid type name: ${o[0]}.`);
        if (!t.set)
            throw new Error(`Type name '${o[0]}' is not serializable.`);
        const j = t.nosj(o[1]);
        return j !== null && typeof j === 'object' ? t.set(j) : j;
    },
    stringify(o, withType = true) {
        var t = CTSType.toTypedJson(o);
        return JSON.stringify(withType ? t : t[1]);
    },
    parse(s) {
        return CTSType.fromTypedJson(JSON.parse(s));
    },
};
export const SymCTS = Symbol.for("CK.CTSType");
//# sourceMappingURL=CTSType.js.map