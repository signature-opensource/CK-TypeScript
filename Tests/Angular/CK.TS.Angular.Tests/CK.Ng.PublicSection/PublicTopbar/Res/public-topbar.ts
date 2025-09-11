import { Component } from '@angular/core';

// Even if we don't use these, this is to test the '@local/ck-gen' type resolution:
// This must be replaced with resolved from:
import { ActionBarContent, ActionBarAction } from '@local/ck-gen';     //==> @local/ck-gen/CK/Ng/Zorro/action-bar/action-bar.model
// The PublicTopBarComponent requires the optional Zorro.ActionBar so it becomes no more optional.

@Component({
    selector: 'ck-public-topbar',
    standalone: true,
    imports: [],
    templateUrl: './public-topbar.html',
    styleUrl: './public-topbar.less'
})
export class PublicTopbarComponent {

    // <TestPoint/>
}
