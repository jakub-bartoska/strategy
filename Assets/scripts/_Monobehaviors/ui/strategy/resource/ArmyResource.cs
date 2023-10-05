namespace _Monobehaviors.resource
{
    public class ArmyResource : CommonResourceTab
    {
        public static ArmyResource instance;
        private bool active = false;

        private void Awake()
        {
            instance = this;
        }

        public void changeActive(bool active)
        {
            if (this.active == active) return;
            gameObject.SetActive(active);
            this.active = active;
        }
    }
}