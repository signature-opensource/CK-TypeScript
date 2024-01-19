import {IPoco, 
        ICrisPoco, 
        IAbstractCommand, ICommand, ICommandPart, 
        ICommandAuthUnsafe, ICommandAuthNormal, ICommandAuthCritical, ICommandAuthDeviceId } from "@local/ck-gen";
import {SomeCommand, 
        WithObjectCommand, Result } from "@local/ck-gen";

it("Cannot be tested because it is a compile time feature...",() => {
    expect("Brands have no runtime existence, they only exist at compile time.").toBeTruthy();
  });

type Pretty<T> = { [K in keyof T]: T[K] };
type B<T> = T extends {"_brand":any} ? T["_brand"] : never; 
type P<T> = Pretty<B<T>>; 

// SomeCommand implements ICommand<Number>, ICommandAuthCritical
var someCommand = new SomeCommand();

// When this compiles:
const v: ICommandAuthCritical = someCommand;

type BPoco = P<IPoco>;
type BAbstractCommand = P<IAbstractCommand>;
type BAuthNormal = P<ICommandAuthNormal>;
type BAuthCritical = P<ICommandAuthCritical>;
type BSomeCommmand = P<SomeCommand>;

{
    let good: ICommandAuthNormal = {} as ICommandAuthCritical; 
    //let bad: ICommandAuthCritical = {} as ICommandAuthNormal; 
    let goodB: BAuthNormal = {} as BAuthCritical; 
    //let badB: BAuthCritical = {} as BAuthNormal; 
}
{
    let goodB: BAuthCritical = {} as BSomeCommmand; 
    //let badB: BSomeCommmand = {} as BAuthCritical; 
}
{
    let goodB: BPoco = {} as BSomeCommmand; 
    //let badB: BSomeCommmand = {} as BPoco; 
}

// WithObjectCommand implements ICommandAuthDeviceId, ICommand<Result|undefined>
type BWithObjectCommand = P<WithObjectCommand>;
var withObjectCommand = new WithObjectCommand();

// Branding prevents a WithObjectCommand that is ICommandAuthDeviceId : ICommandAuthUnsafe
// to be considered a AuthNormal (or AuthCritical) command.
{
    //let bad: ICommandAuthNormal = {} as WithObjectCommand; 
    //let badB: BAuthNormal = {} as BWithObjectCommand; 
}

// Command result is covariant.
{
    let exact: ICommand<Result|undefined> = withObjectCommand;  
    let above1: ICommand<IPoco|undefined> = withObjectCommand;  
    let above2: ICommand<object|undefined> = withObjectCommand;
    //let bad: ICommand<string|undefined> = withObjectCommand;
}

