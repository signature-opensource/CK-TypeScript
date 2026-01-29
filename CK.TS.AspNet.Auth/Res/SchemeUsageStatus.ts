/** Defines the status of the user schemes (see @see IUserSchemeInfo). */
export enum SchemeUsageStatus {

    /** The scheme has not been used by the user and is available. */
    Unused,

    /** The scheme has been used and is available. */
    Active,

    /**
     * The scheme has been used by the user but is currently not available: user should
     * be invited to login through another scheme.
     * */
    Deprecated
}
