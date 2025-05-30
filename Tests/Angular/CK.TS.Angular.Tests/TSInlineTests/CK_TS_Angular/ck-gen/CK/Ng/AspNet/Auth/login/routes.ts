export default [
{ path: "password-lost", loadComponent: () => import( "../password-lost/password-lost.component" ).then( c => c.PasswordLostComponent ) }
];