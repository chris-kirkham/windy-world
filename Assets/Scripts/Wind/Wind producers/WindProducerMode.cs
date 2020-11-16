/** 
 * <summary>Enum to specify whether a wind producer is "static" or "dynamic" during runtime.</summary>
 * 
 * Dynamic:
 * the wind field will update its position, necessitating re-hashing etc.
 * Use this if the object can move during gameplay. (MOST EXPENSIVE) 
 *
 * [UNUSED]
 * Position Static:
 * the wind field will not try to update its position, but will update its wind information.
 * Use this if the object will not move during gameplay, but its wind information may change.
 *
 * Static:
 * The wind field will add this object at the start of gameplay, but not update it after that.
 * Use this if the object and its wind info will not change during gameplay (e.g. a static wind area)   
 **/
public enum WindProducerMode { Static, Dynamic };

