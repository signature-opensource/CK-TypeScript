import { Component, HostBinding, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActionBarAction, ActionBarContent } from './action-bar.model';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';

@Component({
    selector: 'ck-backoffice-action-bar',
    templateUrl: './action-bar.component.html',
    imports: [CommonModule, FormsModule, FontAwesomeModule, NzButtonModule, NzToolTipModule]
})
export class ActionBarComponent<T> {
    @HostBinding('class') class = 'ck-backoffice-action-bar';

    actions = input<ActionBarContent<T>>({ left: [], right: [] });
    selectedItems = input<Array<T>>([]);

    shouldShow(action: ActionBarAction<T>): boolean {
        return action.shouldBeDisplayed(this.selectedItems());
    }

    buttonClicked(action: ActionBarAction<T>): void {
        action.execute(this.selectedItems());
    }

    getButtonClass(action: ActionBarAction<T>): string {
        if (action.class) {
            return `ck-backoffice-action-bar-button ${action.class}`;
        }
        return 'ck-backoffice-action-bar-button';
    }

    isDisabled(action: ActionBarAction<T>): boolean {
        if (action.shouldBeDisabled) {
            return action.shouldBeDisabled();
        }

        return false;
    }
}
