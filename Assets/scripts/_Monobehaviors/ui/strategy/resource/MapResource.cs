namespace _Monobehaviors.resource
{
    public class MapResource : CommonResourceTab
    {
        public static MapResource instance;
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