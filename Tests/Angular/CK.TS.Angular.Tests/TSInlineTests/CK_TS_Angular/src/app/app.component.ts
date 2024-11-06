import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CKGenAppModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.less'
})
export class AppComponent {
  title = 'Demo';
}

