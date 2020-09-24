using OpenTelemetry.Util;
using System;
using System.Reflection;

namespace OpenTelemetry.DynamicActivityBinding
{
    internal class DynamicInvoker
    {
        private readonly Type _activityType;

        internal DynamicInvoker(Type activityType)
        {
            Validate.NotNull(activityType, nameof(activityType));
            _activityType = activityType;
        }

        public void ValidateIsActivity(object activity)
        {
            Validate.NotNull(activity, nameof(activity));

            Type actualType = activity.GetType();
            if (!_activityType.Equals(actualType))
            {
                if (_activityType.IsAssignableFrom(actualType))
                {
                    throw new ArgumentException($"The specified object is expected to be of type {_activityType.FullName},"
                                              + $" but the actual runtime type is {actualType.FullName}."
                                              + $" Notably, the expected type {_activityType.Name} is assignable from the actual runtime type {actualType.Name}."
                                              + $" However, an exact match is required.");
                }
                else
                {
                    throw new ArgumentException($"The specified object is expected to be of type {_activityType.FullName},"
                                              + $" but the actual runtime type is {actualType.FullName}.");
                }
            }
        }

        //public object Current()
        //{
        //    const string propertyName = "Current";
        //    const bool isStatic = true;
        //    const bool setMethod = false;

        //    BindingFlags staticOrInstanceFlag = (isStatic ? BindingFlags.Static : BindingFlags.Instance);

        //    PropertyInfo propInfo = _activityType.GetProperty(propertyName, BindingFlags.Public | staticOrInstanceFlag);
        //    if (propInfo == null)
        //    {
        //        throw new InvalidOperationException($"Cannot reflect over the property \"{propertyName}\" with the BindingFlags Public and {staticOrInstanceFlag.ToString()}."
        //                                          + $" The type being reflected is {_activityType.AssemblyQualifiedName}");
        //    }

        //    MethodInfo methodInfo = setMethod ? propInfo.SetMethod : propInfo.GetMethod;
        //    if (methodInfo == null)
        //    {
        //        throw new InvalidOperationException($"Cannot obtain the {(setMethod ? "SetMethod" : "GetMethod")} for property \"{propertyName}\" via reflection."
        //                                          + $" The type being reflected is {_activityType.AssemblyQualifiedName}");
        //    }

        //    //MulticastDelegate proxy = Delegate.CreateDelegate(typeof(Func<T, R>), methodInfo, throwOnBindFailure: true);
        //    //proxy.I
        //}

        public void AddBaggage()
        {
            const string methodName = "AddBaggage";
            const bool isStatic = true;

            BindingFlags staticOrInstanceFlag = (isStatic ? BindingFlags.Static : BindingFlags.Instance);

            MethodInfo methodInfo = _activityType.GetMethod(methodName, BindingFlags.Public | staticOrInstanceFlag);
            if (methodInfo == null)
            {
                throw new InvalidOperationException($"Cannot reflect over the method \"{methodName}\" with the BindingFlags Public and {staticOrInstanceFlag.ToString()}."
                                                  + $" The type being reflected is {_activityType.AssemblyQualifiedName}");
            }
            Type funcType = typeof(Func<,,>).MakeGenericType(_activityType, typeof(string), typeof(string));
        }
    }
}
