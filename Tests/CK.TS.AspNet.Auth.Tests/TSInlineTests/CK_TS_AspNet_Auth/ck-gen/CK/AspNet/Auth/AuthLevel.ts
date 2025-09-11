/**
 * Standard authentication levels.
 **/
export enum AuthLevel {
/**
 * No authentication: this is the default value.
 * The user is necessarily the anonymous.
 **/
None = 0,
/**
 * The authentication information is not safe: it is issued from a 
 * long lived cookie or other not very secure means.
 **/
Unsafe = 1,
/**
 * Normal authentication level.
 **/
Normal = 2,
/**
 * Critical level MUST be short term and rely on strong authentication 
 * mechanisms (re-authentication, two-factor authentication, etc.).
 **/
Critical = 3
}
