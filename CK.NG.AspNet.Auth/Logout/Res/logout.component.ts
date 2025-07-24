import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@local/ck-gen';
import { TranslateModule } from '@ngx-translate/core';

import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzResultModule } from 'ng-zorro-antd/result';

@Component( {
  selector: 'ck-logout',
  imports: [NzButtonModule, NzResultModule, TranslateModule],
  templateUrl: './logout.component.html'
} )
export class LogoutComponent {
  readonly #authService = inject( AuthService );
  readonly #router = inject( Router );

  async ngOnInit(): Promise<void> {
    // <PreLogout />
    await this.#authService.logout();
    // <PostLogout />
  }

  return(): void {
    // <PreNavigation />
    this.#router.navigate( ['auth'] );
    // <PostRedirection />
  }
}
