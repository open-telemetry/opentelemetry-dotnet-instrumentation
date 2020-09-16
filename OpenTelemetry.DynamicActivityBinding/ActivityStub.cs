using System;
using OpenTelemetry.Util;

namespace OpenTelemetry.DynamicActivityBinding
{
    public struct ActivityStub : IDisposable
    {
        private readonly object _activityInstance;

        public ActivityStub(object activityInstance)
        {
            _activityInstance = activityInstance;
        }

        public object ActivityInstance { get { return _activityInstance; } }  
        
        public bool IsValid { get { return _activityInstance != null; } }
        
        public void Dispose()
        {
            if (ActivityFactory.SupportedFeatures.ActivityIsDisposable)
            {
                IDisposable activity = _activityInstance as IDisposable;
                if (activity != null)
                {
                    activity.Dispose();
                }
            }
            
        }
    }
}
