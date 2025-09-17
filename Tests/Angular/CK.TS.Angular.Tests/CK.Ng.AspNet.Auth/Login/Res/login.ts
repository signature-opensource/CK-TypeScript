import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SomeAuthService } from '../SomeAuthService';

@Component({
    selector: 'ck-login',
    standalone: true,
    imports: [RouterOutlet],
    templateUrl: './login.html',
    styleUrl: './login.less'
})
export class Login {

    readonly authService = inject(SomeAuthService);

}

