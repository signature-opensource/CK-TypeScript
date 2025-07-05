import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'ck-logout-result',
    standalone: true,
    imports: [RouterLink],
    templateUrl: './logout-result.component.html',
    styleUrl: './logout-result.component.less'
})
export class LogoutResultComponent {

}
