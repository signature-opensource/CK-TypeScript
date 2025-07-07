import { UserMessageLevel } from './UserMessageLevel';
import { CTSType } from './CTSType';

/**
* Immutable simple info, warn or error message with an optional indentation.
**/
export class SimpleUserMessage
{

static #invalid: SimpleUserMessage;

/**
* Gets the default, invalid, message.
**/
static get invalid() : SimpleUserMessage { return SimpleUserMessage.#invalid ??= new SimpleUserMessage(UserMessageLevel.None, "", 0); }

/**
* Initializes a new SimpleUserMessage.
* @param level Message level (info, warn or error). 
* @param message Message text. 
* @param depth Optional indentation. 
**/
    constructor(
        public readonly level: UserMessageLevel,
        public readonly message: string,
        public readonly depth: number = 0
    )
    {
CTSType["SimpleUserMessage"].set( this );
    }

    toString() { return '['+UserMessageLevel[this.level]+'] ' + this.message; }
    toJSON() { return this.level !== UserMessageLevel.None
                        ? [this.level,this.message,this.depth]
                        : [0]; }
    static parse( o: {} ) : SimpleUserMessage
    {
        if( o instanceof Array )
        {
            if( o.length === 1 )
            {
                if( o[0] === 0 ) return SimpleUserMessage.invalid;
            }
            else if( o.length === 3 || o.length === 8 )
            {
                const level = o[0];
                if( level === UserMessageLevel.Info || level === UserMessageLevel.Warn || level === UserMessageLevel.Error )
                {
                    let msg = o[1];
                    let d = o[2];
                    if( typeof msg === "number" )
                    {
                        // This is a UserMessage (o.length === 8). 
                        msg = d;
                        d = o[1];
                    }
                    if( typeof d === "number" && d >= 0 && d <= 255 )
                    {
                        return new SimpleUserMessage( level, msg, d );
                    }
                }
            }
        }
        throw new Error( `Unable to parse '{{o}}' as a SimpleUserMessage.` );
    }
}
