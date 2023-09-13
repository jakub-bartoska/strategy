namespace component.soldier.behavior.behaviors
{
    public enum BehaviorType
    {
        NONE, //not set
        IDLE, //do nothing
        FOLLOW_CLOSEST_ENEMY,
        FIGHT,
        SHOOT_ARROW,
        MAKE_LINE_FORMATION,
        PROCESS_FORMATION_COMMAND
    }
}