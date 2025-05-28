import { Pipe, PipeTransform } from '@angular/core';
import { utcDateToLocal } from './date-helper';

@Pipe( {
    name: 'isoUtcToLocal',
    standalone: true
} )
export class DateFormatPipe implements PipeTransform {
    transform( value: string ) {
        return utcDateToLocal( value );
    }
}
