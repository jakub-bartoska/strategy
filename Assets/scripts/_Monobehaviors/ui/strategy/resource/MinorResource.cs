namespace _Monobehaviors.resource
{
    public class MinorResource : CommonResourceTab
    {
        public static MinorResource instance;

        private void Awake()
        {
            instance = this;
        }
    }
}