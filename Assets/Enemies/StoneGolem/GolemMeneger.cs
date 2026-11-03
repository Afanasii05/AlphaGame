public class GolemMeneger 
{
    static GolemStonesSlots golem;

    public static void setGolem(GolemStonesSlots newGolem)
    {
        if (GolemMeneger.golem == null)
        {
            golem = newGolem;
            return;
        }
        golem.Deactivate();
        golem = newGolem;
    }

}
