import { Component } from '@angular/core';
import { CKGenInjected } from '@local/ck-gen';

import { TranslateModule } from '@ngx-translate/core';

const ckGenInjected: CKGenInjected = [];

@Component({
    selector: 'ck-public-footer',
    standalone: true,
    imports: [ ...ckGenInjected ],
    templateUrl: './public-footer.component.html',
    styleUrl: './public-footer.component.less'
})
export class PublicFooterComponent {

}
