import { CTSType } from '../../CK/Core/CTSType';

/**
 * Simple data record. Compatible with a IPoco field (no mutable reference).
 **/
export class NamedRecord {
public constructor(
/**
 * The data value.
 **/
public value: number = 0,
/**
 * The data name.
 **/
public name: string = "")
{
CTSType["CK.TypeScript.Tests.CrisLike.NamedRecord"].set( this );
}
readonly _brand!: {"88":any};
}
