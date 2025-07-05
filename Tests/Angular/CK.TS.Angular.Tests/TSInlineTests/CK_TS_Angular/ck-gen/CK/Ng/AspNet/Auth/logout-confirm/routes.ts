export default [
{ path: "logout-result", loadComponent: () => import( "../logout-result/logout-result.component" ).then( c => c.LogoutResultComponent ) }
];