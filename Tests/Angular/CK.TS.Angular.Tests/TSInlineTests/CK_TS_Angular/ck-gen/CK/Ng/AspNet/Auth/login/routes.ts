// import { Route } from '@angular/router';

export default [
{ path: "password-lost", loadComponent: () => import( "../password-lost/password-lost" ).then( c => c.PasswordLostComponent ) }
]; // as Route[];