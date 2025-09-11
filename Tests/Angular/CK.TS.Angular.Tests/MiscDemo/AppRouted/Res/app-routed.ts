import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'app-routed',
    standalone: true,
    imports: [RouterOutlet],
    templateUrl: './app-routed.html',
    styleUrl: './app-routed.less'
})
export class AppRoutedComponent {

}
