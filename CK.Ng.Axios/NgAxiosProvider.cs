using CK.TS.Angular;

namespace CK.Ng.Axios;

[NgProvider<TSPackage>( "{ provide: AXIOS, useValue: axios.create() }" )]
// [TypeScriptImport( "@local/ck-gen", "AXIOS" )]
// [TypeScriptImport( "axios", "default axios", "AxiosInstance" )]
public class NgAxiosProvider : NgProvider
{
}
