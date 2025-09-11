import { IPoco } from '../Core/IPoco';
import { SimpleUserMessage } from '../Core/SimpleUserMessage';
import { CTSType } from '../Core/CTSType';

/**
 * Describes the final result of a command.
 * 
 * The result's type of a command is not constrained (the TResult in ICommand`1 can be anything) or
 * a IAspNetCrisResultError.
 * 
 * This is for "API adaptation" of ASPNet endpoint that has no available back channel and can be called by agnostic
 * process.
 **/
export class AspNetResult implements IPoco {
/**
 * The error or result object (if any).
 * A IAspNetCrisResultError on error.
 * null for a successful a ICommand.
 * The result of a ICommand`1.
 **/
public result?: {};
/**
 * An optional list of UserMessageLevel.info, 
 * UserMessageLevel.warn or 
 * UserMessageLevel.error messages issued by the validation of the command.
 * Validation error messages also appear in the IAspNetCrisResultError.errors.
 **/
public validationMessages?: Array<SimpleUserMessage>;
/**
 * An optional correlation identifier.
 **/
public correlationId?: string;
public constructor()
public constructor(
result?: {},
validationMessages?: Array<SimpleUserMessage>,
correlationId?: string)
constructor(
result?: {},
validationMessages?: Array<SimpleUserMessage>,
correlationId?: string)
{
this.result = result;
this.validationMessages = validationMessages;
this.correlationId = correlationId;
CTSType["AspNetResult"].set( this );
}
readonly _brand!: IPoco["_brand"] & {"0":any};
}
