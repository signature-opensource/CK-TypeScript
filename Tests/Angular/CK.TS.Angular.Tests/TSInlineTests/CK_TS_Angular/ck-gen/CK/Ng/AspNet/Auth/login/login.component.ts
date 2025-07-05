import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SomeAuthService } from '../SomeAuthService';

@Component({
    selector: 'ck-login',
    standalone: true,
    imports: [RouterOutlet],
    templateUrl: './login.component.html',
    styleUrl: './login.component.less'
})
export class LoginComponent {

    readonly authService = inject(SomeAuthService);

}

