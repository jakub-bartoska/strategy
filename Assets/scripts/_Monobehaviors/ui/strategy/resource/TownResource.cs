﻿namespace _Monobehaviors.resource
{
    public class TownResource : CommonResourceTab
    {
        public static TownResource instance;

        private void Awake()
        {
            instance = this;
        }
    }
}