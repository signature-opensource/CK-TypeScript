import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'ckgen-password-lost',
    standalone: true,
    imports: [RouterLink],
    templateUrl: './password-lost.component.html',
    styleUrl: './password-lost.component.less'
})
export class PasswordLostComponent {

}
