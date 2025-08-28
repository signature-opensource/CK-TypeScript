import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'ck-password-lost',
    standalone: true,
    imports: [RouterLink],
    templateUrl: './password-lost.html',
    styleUrl: './password-lost.less'
})
export class PasswordLostComponent {

}
