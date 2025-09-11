/**
 * Standard grant level: this is a simple (but often enough) way to secure a resource.
 * Among the different levels, depending on the actual resource, some (or most) of them 
 * are useless and can be ignored.
 * But for some kind of resources all of them make sense: a "service object" (a kind of executable process) for instance can
 * benefits of all these levels.
 **/
export enum GrantLevel {
/**
 * Actor doesn't even know that object exists.
 **/
Blind = 0,
/**
 * Actor can see the object names and may use services provided by the object 
 * but cannot see the object itself.
 **/
User = 8,
/**
 * Actor can view the object but cannot interact with it.
 **/
Viewer = 16,
/**
 * Actor can contribute to the object but cannot modify the object itself.
 **/
Contributor = 32,
/**
 * Actor can edit the standard properties of the object. He may not be able to 
 * change more sensitive aspects such as the different names of the object.
 **/
Editor = 64,
/**
 * Actor can edit the object, its names and any property, but can not change
 * the security settings.
 **/
SuperEditor = 80,
/**
 * Actor can edit all properties of the object and can 
 * change the security settings by choosing an Acl among defined security
 * contexts. The actor can not destroy the object.
 **/
SafeAdministrator = 112,
/**
 * Actor has full control on the object including its destruction. It may create 
 * and configure an independent Acl for the object.
 **/
Administrator = 127
}
