import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'ck-public-page',
    standalone: true,
    imports: [RouterOutlet],
    templateUrl: './public-page.component.html',
    styleUrl: './public-page.component.less'
})
export class PublicPageComponent {
}
