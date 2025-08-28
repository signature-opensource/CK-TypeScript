import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'ck-logout-result',
    standalone: true,
    imports: [RouterLink],
    templateUrl: './logout-result.html',
    styleUrl: './logout-result.less'
})
export class LogoutResultComponent {

}
