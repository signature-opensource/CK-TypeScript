import { IPoco } from '../Core/IPoco';
import { CTSType } from '../Core/CTSType';

/**
 * Simplified ICrisResultError.
 **/
export class AspNetCrisResultError implements IPoco {
/**
 * Whether the command failed during validation or execution.
 **/
public isValidationError: boolean;
/**
 * One or more error messages.
 **/
public readonly errors: Array<string>;
/**
 * A LogKey that enables to locate the logs of the command execution.
 * It may not always be available.
 **/
public logKey?: string;
public constructor()
public constructor(
isValidationError?: boolean,
errors?: Array<string>,
logKey?: string)
constructor(
isValidationError?: boolean,
errors?: Array<string>,
logKey?: string)
{
this.isValidationError = isValidationError ?? false;
this.errors = errors ?? [];
this.logKey = logKey;
CTSType["AspNetCrisResultError"].set( this );
}
readonly _brand!: IPoco["_brand"] & {"1":any};
}
