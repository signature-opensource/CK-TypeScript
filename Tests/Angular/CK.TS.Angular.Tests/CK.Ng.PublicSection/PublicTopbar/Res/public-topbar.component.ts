import { Component } from '@angular/core';
import { CKGenInjected } from '@local/ck-gen';

const ckGenInjected: CKGenInjected = [];

@Component({
    selector: 'ck-public-topbar',
    standalone: true,
    imports: [ ...ckGenInjected ],
    templateUrl: './public-topbar.component.html',
    styleUrl: './public-topbar.component.less'
})
export class PublicTopbarComponent {

}
