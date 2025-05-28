import { DateTime } from "luxon";

export function utcDateToLocal( date: string ) {
    if ( date.startsWith( '0001-01-01T00:00:00' ) ) {
        return '-';
    }
    const dateTime = DateTime.fromISO( date, { zone: 'utc' } ).setZone( Intl.DateTimeFormat().resolvedOptions().timeZone );
    return dateTime.toLocaleString( DateTime.DATETIME_SHORT );
}

export function dateToLocalFRWithoutHours( date: string ) {
    if ( date.startsWith( '0001-01-01T00:00:00' ) ) {
        return '-';
    }
    const dateTime = DateTime.fromISO( date, { zone: 'utc' } ).setZone( Intl.DateTimeFormat().resolvedOptions().timeZone );
    return dateTime.toLocaleString();
}

export function isDefaultDate( date?: String ) {
    return date === null || date?.startsWith( '0001-01-01T00:00:00' );
}

export function getDefaultDateTime() {
    return DateTime.fromISO( '0001-01-01T00:00:00' );
}

export function getCalendarDaysUntilNow( current: Date ): number {
    const now = new Date();
    const startOfDayRight = now.setHours( 0, 0, 0, 0 );

    const timestampLeft = +current - DateTime.fromJSDate( current ).offset;
    const timestampRight = +startOfDayRight - DateTime.fromMillis( startOfDayRight ).offset;

    const millisInADay = 24 * 60 * 60 * 1000;
    return Math.round( ( timestampLeft - timestampRight ) / millisInADay );
}
