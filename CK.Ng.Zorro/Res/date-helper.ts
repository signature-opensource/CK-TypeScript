import { DateTime } from "luxon";

export function utcDateToLocal( date: string ) {
    if ( date.startsWith( '0001-01-01T00:00:00' ) ) {
        return '-';
    }
    const dateTime = DateTime.fromISO( date, { zone: 'utc' } ).setZone( Intl.DateTimeFormat().resolvedOptions().timeZone );
    return dateTime.toLocaleString( DateTime.DATETIME_SHORT );
}
