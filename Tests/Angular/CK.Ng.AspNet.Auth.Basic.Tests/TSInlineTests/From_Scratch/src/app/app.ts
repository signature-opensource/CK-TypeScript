import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';

@Component({
  imports: [RouterOutlet, CKGenAppModule],
  selector: 'app-root',
  styleUrl: './app.less',
  templateUrl: './app.html',
})
export class App { }
