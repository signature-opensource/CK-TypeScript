import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'ck-private-page',
    standalone: true,
    imports: [RouterOutlet],
    templateUrl: './private-page.component.html',
    styleUrl: './private-page.component.less'
})
export class PrivatePageComponent {
}
