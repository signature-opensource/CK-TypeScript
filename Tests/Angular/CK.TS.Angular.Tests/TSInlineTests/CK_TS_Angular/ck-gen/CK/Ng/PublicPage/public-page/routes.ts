import { LoginComponent } from '../../AspNet/Auth/login/login.component';
import rLoginComponent from '../../AspNet/Auth/login/routes';

// import { Route } from '@angular/router';

export default [
{ path: "login", component: LoginComponent, children: rLoginComponent

 }
]; // as Route[];