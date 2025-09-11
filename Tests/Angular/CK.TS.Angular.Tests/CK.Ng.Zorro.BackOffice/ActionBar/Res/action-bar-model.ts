import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

export interface ActionBarContent<T> {
    left: Array<ActionBarAction<T>>;
    right: Array<ActionBarAction<T>>;
}

export interface ActionBarAction<T> {
    name: string;
    displayName: string;
    isDanger: boolean;
    icon?: IconDefinition;
    class?: string;
    shouldBeDisplayed: (selectedItems?: Array<T>) => boolean;
    execute: (selectedItems?: Array<T>) => void;
    shouldBeDisabled?: (selectedItems?: Array<T>) => boolean;
}
