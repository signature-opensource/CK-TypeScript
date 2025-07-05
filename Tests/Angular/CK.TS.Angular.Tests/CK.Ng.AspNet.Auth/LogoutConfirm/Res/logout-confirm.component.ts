import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'ck-logout-confirm',
    standalone: true,
    imports: [RouterOutlet],
    templateUrl: './logout-confirm.component.html',
    styleUrl: './logout-confirm.component.less'
})
export class LogoutConfirmComponent {

}
