import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';

@Component({
    selector: 'ck-public-page',
    imports: [RouterOutlet, NzButtonModule, RouterLink],
    templateUrl: './public-page.component.html'
})
export class PublicPageComponent {
}
