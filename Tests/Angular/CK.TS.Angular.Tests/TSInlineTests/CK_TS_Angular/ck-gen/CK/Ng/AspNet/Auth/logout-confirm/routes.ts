// import { Route } from '@angular/router';

export default [
{ path: "logout-result", loadComponent: () => import( "../logout-result/logout-result" ).then( c => c.LogoutResultComponent ) }
]; // as Route[];