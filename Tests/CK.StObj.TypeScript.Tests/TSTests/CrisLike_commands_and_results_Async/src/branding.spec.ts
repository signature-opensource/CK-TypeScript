import {IPoco, 
        ICrisPoco, 
        IAbstractCommand, ICommand, ICommandPart, 
        ICommandAuthUnsafe, ICommandAuthNormal, ICommandAuthCritical, ICommandAuthDeviceId, 
        PocovariantBrand } from "@local/ck-gen";
import {SomeCommand, 
        WithObjectCommand, Result } from "@local/ck-gen";

it("Cannot be tested because it is a compile time feature...",() => {
    expect("Brand has no runtime existence, they only exist at compile time.").toBeTruthy();
  });

function C<T>(){ return {T: {} as PocovariantBrand<T>}; }

// SomeCommand implements ICommand<Number>, ICommandAuthCritical
var someCommand = new SomeCommand();

// When this compiles:
const v: ICommandAuthCritical = someCommand;
// The following check on the brand compiles (this avoids a variable declaration).
C<ICommandAuthCritical>().T = C<SomeCommand>().T;
// Since this command is AuthCritical it is necessarily also AuthNormal and AuthUnsafe.
C<ICommandAuthUnsafe>().T = C<SomeCommand>().T;

// Brand doesn't prevent structural typing "to the external world":
export interface IFakeTheCommandAuthUnsafe {
    actorId: Number;
}
const valid: IFakeTheCommandAuthUnsafe = someCommand;
// But only branded types can "enter the Poco world".
//C<ICommandAuthUnsafe>().T = C<IFakeTheCommandAuthUnsafe>().T;
// const invalid: ICommandAuthUnsafe = valid;

C<ICommand<Number>>().T = C<SomeCommand>().T;
// To base types:
C<ICommand>().T = C<SomeCommand>().T;
C<IAbstractCommand>().T = C<SomeCommand>().T;
C<ICrisPoco>().T = C<SomeCommand>().T;
C<IPoco>().T = C<SomeCommand>().T;
C<object>().T = C<SomeCommand>().T;
// Command result is covariant.
C<ICommand<object>>().T = C<SomeCommand>().T;
//C<ICommand<string>>().T = C<SomeCommand>().T;

type IsSubtypeOf<S, P> = S extends P ? true : false;

type t1 = IsSubtypeOf<SomeCommand, ICommand<object>>;
type f1 = IsSubtypeOf<ICommand<object>, SomeCommand>;

type t2 = IsSubtypeOf<SomeCommand, ICommand>;
type f2 = IsSubtypeOf<ICommand, SomeCommand>;

type f3 = IsSubtypeOf<SomeCommand,ICommand<string>>;
type f3bis = IsSubtypeOf<ICommand<string>, SomeCommand>;


// WithObjectCommand implements ICommandAuthDeviceId, ICommand<Result|undefined>


// Branding prevents a WithObjectCommand that is ICommandAuthDeviceId : ICommandAuthUnsafe
// to be considered a AuthNormal (or AuthCritical) command.
// C<ICommandAuthNormal>().T = C<WithObjectCommand>().T;

// Command result is covariant.
C<ICommand<Result|undefined>>().T = C<WithObjectCommand>().T;
C<ICommand<IPoco|undefined>>().T = C<WithObjectCommand>().T;
C<ICommand<object|undefined>>().T = C<WithObjectCommand>().T;
C<ICommand>().T = C<WithObjectCommand>().T;

// Specifically handled collections are Array, Set and Map
// with their readonly counterparts.
// Currently, we consider a ReadOnly to NOT be assignable from its mutable counterpart.
C<(object|undefined)[]>().T = C<object[]>().T;
C<(object|undefined)[]>().T = C<(number|undefined)[]>().T;
C<ReadonlyArray<object>>().T = C<number[]>().T;
// This is invalid.
// C<number[]>().T = C<ReadonlyArray<number>>().T;

// Collections are covariant. This does NOT match the way TS works... except for 
// arrays, because arrays are strange beasts (in TS as well as in C#).    
C<ICommand[]>().T = C<SomeCommand[]>().T;
let anArrayOfSomeCommand: SomeCommand[] = [];
let anArrayOfCommand: ICommand[] = anArrayOfSomeCommand;

C<Set<ICommand>>().T = C<Set<SomeCommand>>().T;
let aSetOfSomeCommand: Set<SomeCommand> = new Set<SomeCommand>();
let aSetOfCommand: Set<ICommand> = aSetOfSomeCommand;

C<Map<object,ICommand>>().T = C<Set<SomeCommand>>().T;

let aMapOfSomeCommand: Map<number,SomeCommand> = new Map<number,SomeCommand>();
let aMapOfCommand: Map<number,ICommand> = aMapOfSomeCommand;
