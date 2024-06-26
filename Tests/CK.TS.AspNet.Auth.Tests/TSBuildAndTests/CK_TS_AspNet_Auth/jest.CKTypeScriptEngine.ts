// This will run once before each test file and before the testing framework is installed.
// This is used by TestHelper.CreateTypeScriptTestRunner to duplicate environment variables settings
// in a "persistent" way: these environment variables will be available until the Runner
// returned by TestHelper.CreateTypeScriptTestRunner is disposed.
//
// This part fixes a bug in testEnvionment: 'jsdom':
// <fix href="https://stackoverflow.com/questions/68468203/why-am-i-getting-textencoder-is-not-defined-in-jest">
import { TextDecoder as ImportedTextDecoder, TextEncoder as ImportedTextEncoder, } from "util";
Object.assign(global, { TextDecoder: ImportedTextDecoder, TextEncoder: ImportedTextEncoder, })
// </fix>
// And this is another one:
// <fix href="https://stackoverflow.com/questions/42677387/jest-returns-network-error-when-doing-an-authenticated-request-with-axios/43020260#43020260">
//Object.assign(global, { XMLHttpRequest: undefined });
// </fix>

Object.assign( process.env, {"SERVER_ADDRESS":"http://[::1]:7494","CK_TYPESCRIPT_ENGINE":"true"});
