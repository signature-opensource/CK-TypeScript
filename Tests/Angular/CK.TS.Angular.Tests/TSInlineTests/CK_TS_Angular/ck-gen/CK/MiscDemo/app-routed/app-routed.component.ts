import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'app-routed',
    standalone: true,
    imports: [RouterOutlet],
    templateUrl: './app-routed.component.html',
    styleUrl: './app-routed.component.less'
})
export class AppRoutedComponent {

}
