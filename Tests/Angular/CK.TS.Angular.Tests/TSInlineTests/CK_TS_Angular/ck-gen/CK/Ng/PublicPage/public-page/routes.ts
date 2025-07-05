import { LoginComponent } from '../../AspNet/Auth/login/login.component';
import rLoginComponent from '../../AspNet/Auth/login/routes';

export default [
{ path: "login", component: LoginComponent, children: rLoginComponent

 }
];