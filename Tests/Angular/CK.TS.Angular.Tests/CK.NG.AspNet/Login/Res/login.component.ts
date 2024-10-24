import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'ckgen-login',
    standalone: true,
    imports: [RouterLink],
    templateUrl: './login.component.html',
    styleUrl: './login.component.less'
})
export class LoginComponent {

}
