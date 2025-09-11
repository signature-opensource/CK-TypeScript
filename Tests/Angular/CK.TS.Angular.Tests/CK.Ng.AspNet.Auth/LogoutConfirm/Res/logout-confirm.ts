import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'ck-logout-confirm',
    standalone: true,
    imports: [RouterOutlet],
    templateUrl: './logout-confirm.html',
    styleUrl: './logout-confirm.less'
})
export class LogoutConfirmComponent {

}
