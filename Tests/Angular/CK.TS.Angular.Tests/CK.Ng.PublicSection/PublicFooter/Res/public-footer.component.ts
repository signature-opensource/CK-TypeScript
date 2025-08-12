import { Component } from '@angular/core';

// Even if we don't use these, this is to test the '@local/ck-gen' type resolution:
// This must be replaced with resolved from:
import { ActionBarContent, ActionBarAction } from '@local/ck-gen';     //==> @local/ck-gen/CK/Ng/Zorro/action-bar/action-bar.model
import { IFormControlConfig, FormControlConfig } from '@local/ck-gen'; //==> @local/ck-gen/CK/Ng/Zorro/generic-form/generic-form.model

// <TestPoint/>

@Component({
    selector: 'ck-public-footer',
    standalone: true,
    imports: [],
    templateUrl: './public-footer.component.html',
    styleUrl: './public-footer.component.less'
})
export class PublicFooterComponent {
    // Some change here.
}
