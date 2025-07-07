import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { ICommandAuthDeviceId } from '../../CK/CrisLike/ICommandAuthDeviceId';
import { Result } from '../WithObject/Result';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * This command requires authentication and is device dependent.
 * It returns an optional object as its result.
 * Secondary definition that makes IWithObjectCommand return a 
 * IResult and requires
 * the device identifier.
 * Secondary definition that makes IWithObjectSpecializedAsPocoCommand return a 
 * ISuperResult.
 **/
export class WithObjectCommand implements ICommandAuthDeviceId, ICommand<Result|undefined> {
/**
 * The power of this command.
 **/
public power?: number;
/**
 * The power of the Poco.
 **/
public powerPoco: number;
/**
 * The device identifier.
 **/
public deviceId: string;
/**
 * The actor identifier.
 * The default ~~!:CrisAuthenticationService~~ validates this field against the
 * current ~~!:IAuthenticationInfo.UnsafeUser~~.
 **/
public actorId: number;
public constructor()
public constructor(
power?: number,
powerPoco?: number,
deviceId?: string,
actorId?: number)
constructor(
power?: number,
powerPoco?: number,
deviceId?: string,
actorId?: number)
{
this.power = power;
this.powerPoco = powerPoco ?? 0;
this.deviceId = deviceId ?? "";
this.actorId = actorId ?? 0;
CTSType["CK.TypeScript.Tests.CrisLike.IWithObjectCommand"].set( this );
}

get commandModel(): ICommandModel { return WithObjectCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommandAuthDeviceId["_brand"] & ICommand<Result|undefined>["_brand"] & {"15":any};
}
