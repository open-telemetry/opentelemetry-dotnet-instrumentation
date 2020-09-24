using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenTelemetry.Util;

namespace OpenTelemetry.DynamicActivityBinding
{
    public struct ActivityStub : IDisposable
    {
        private static class NoOpSingeltons
        {
            internal static readonly ActivityStub ActivityStub = new ActivityStub(null);
            internal static readonly IEnumerable<KeyValuePair<string, string>> KvpEnumerable = new KeyValuePair<string, string>[0];
            internal static readonly TimeSpan TimeSpan = TimeSpan.Zero;
            internal static readonly DateTime DateTimeUtc = default(DateTime).ToUniversalTime();
            internal static readonly ActivityIdFormatStub ActivityIdFormat = ActivityIdFormatStub.Unknown;
            internal static readonly ActivityKindStub ActivityKind = ActivityKindStub.Internal;
        }

        private static readonly ConditionalWeakTable<object, SupplementalActivityData> s_supplementalActivityData = new ConditionalWeakTable<object, SupplementalActivityData>();

        public static SupportedFeatures SupportedFeatures { get { return SupportedFeatures.SingeltonInstance; } }

        private static string FormatNotSupportedErrorMessage(string apiName, string minRequiredFeatureSet)
        {
            string errMsg = $"{nameof(ActivityStub)}.{apiName} is not supported."
                                  + $" Status: {{{nameof(DynamicLoader)}.{nameof(DynamicLoader.InitializationState)}={DynamicLoader.InitializationState.ToString()};"
                                  + $" MinRequiredFeatureSet={minRequiredFeatureSet};"
                                  + $" SupportedFeatureSets={SupportedFeatures.FormatFeatureSetSupportList()}}}";
            return errMsg;
        }

        private readonly object _activityInstance;

        private ActivityStub(object activityInstance)
        {
            if (activityInstance == null)
            {
                _activityInstance = null;
            }
            else
            {
                DynamicLoader.Invoker.ValidateIsActivity(activityInstance);
                _activityInstance = activityInstance;
            }
        }

        public object ActivityInstance { get { return _activityInstance; } }

        public bool IsNoOpStub { get { return _activityInstance == null; } }

        public static ActivityStub Wrap(object activity)
        {
            if (activity == null)
            {
                return NoOpSingeltons.ActivityStub;
            }
            else
            {
                return new ActivityStub(activity);
            }
        }

        private bool TryGetSupplementalData(out SupplementalActivityData supplementalData)
        {
            if (_activityInstance == null)
            {
                supplementalData = null;
                return false;
            }

            ConditionalWeakTable<object, SupplementalActivityData> supplementalActivityData = s_supplementalActivityData;
            return supplementalActivityData.TryGetValue(_activityInstance, out supplementalData);
        }

        private SupplementalActivityData GetOrCreateSupplementalData()
        {
            if (_activityInstance == null)
            {
                return null;
            }

            ConditionalWeakTable<object, SupplementalActivityData> supplementalActivityData = s_supplementalActivityData;
            return supplementalActivityData.GetValue(_activityInstance, (_) => new SupplementalActivityData());
        }

        #region Operations backed by all supported DiagnostigSource.dll versions

        public static object CurrentActivity
        {
            get
            {
                if (!DynamicLoader.EnsureInitialized())
                {
                    return null;
                }

                return null;
            }
        }

        public static object CurrentStub
        {
            get
            {
                if (!DynamicLoader.EnsureInitialized())
                {
                    return NoOpSingeltons.ActivityStub;
                }

                object currentActivity = CurrentActivity;
                return Wrap(CurrentActivity);
            }
        }

        public static ActivityStub StartNewActivity(string operationName)
        {
            if (!DynamicLoader.EnsureInitialized())
            {
                return NoOpSingeltons.ActivityStub;
            }

            return default(ActivityStub);
        }

        public void AddBaggage(string key, string value)
        {
            if (_activityInstance == null)
            {
                return;
            }

            if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
            {
                // ...
            }
            else
            {
                string errMsg = FormatNotSupportedErrorMessage($"{nameof(AddBaggage)}(..)", "4020");
                throw new NotSupportedException(errMsg);
            }
        }

        public void AddTag(string key, string value)
        {
            if (_activityInstance == null)
            {
                return;
            }

            if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
            {
                // ...
            }
            else
            {
                string errMsg = FormatNotSupportedErrorMessage($"{nameof(AddTag)}(..)", "4020");
                throw new NotSupportedException(errMsg);
            }
        }

        public string GetBaggageItem(string key)
        {
            if (_activityInstance == null)
            {
                return null;
            }

            if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
            {
                return null;
            }
            else
            {
                string errMsg = FormatNotSupportedErrorMessage($"{nameof(GetBaggageItem)}(..)", "4020");
                throw new NotSupportedException(errMsg);
            }
        }

        public void SetParentId(string parentId)
        {
            if (_activityInstance == null)
            {
                return;
            }

            if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
            {
                // ...
            }
            else
            {
                string errMsg = FormatNotSupportedErrorMessage($"{nameof(SetParentId)}(..)", "4020");
                throw new NotSupportedException(errMsg);
            }
        }

        public void Stop()
        {
            if (_activityInstance == null)
            {
                return;
            }

            if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
            {
                // ...
            }
            else
            {
                string errMsg = FormatNotSupportedErrorMessage($"{nameof(Stop)}(..)", "4020");
                throw new NotSupportedException(errMsg);
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Baggage
        {
            get
            {
                if (_activityInstance == null)
                {
                    return NoOpSingeltons.KvpEnumerable;
                }

                if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
                {
                    return null;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Baggage), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public TimeSpan Duration {
            get
            {
                if (_activityInstance == null)
                {
                    return NoOpSingeltons.TimeSpan;
                }

                if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
                {
                    return default;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Duration), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public string Id
        {
            get
            {
                if (_activityInstance == null)
                {
                    return String.Empty;
                }

                if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
                {
                    return default;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Id), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public string OperationName
        {
            get
            {
                if (_activityInstance == null)
                {
                    return String.Empty;
                }

                if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
                {
                    return default;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(OperationName), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public ActivityStub ParentStub(out bool hasParent)
        {
            // A more appropriate shape for this API would be
            //   bool TryGetParentStub(out ActivityStub parentStub);
            // However, we want Intellisense to place this next to Parent.
            if (_activityInstance == null)
            {
                hasParent = false;
                return NoOpSingeltons.ActivityStub;
            }

            object parent = Parent;
            hasParent = (parent != null);
            return hasParent ? ActivityStub.Wrap(parent) : NoOpSingeltons.ActivityStub;
        }

        public object Parent
        {
            get
            {
                if (_activityInstance == null)
                {
                    return null;
                }

                if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
                {
                    return default;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Parent), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public string ParentId
        {
            get
            {
                if (_activityInstance == null)
                {
                    return null;
                }

                if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
                {
                    return default;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(ParentId), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public string RootId
        {
            get
            {
                if (_activityInstance == null)
                {
                    return null;
                }

                if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
                {
                    return default;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(RootId), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public DateTime StartTimeUtc
        {
            get
            {
                if (_activityInstance == null)
                {
                    return NoOpSingeltons.DateTimeUtc;
                }

                if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
                {
                    return default;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(StartTimeUtc), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Tags
        {
            get
            {
                if (_activityInstance == null)
                {
                    return NoOpSingeltons.KvpEnumerable;
                }

                if (SupportedFeatures.FeatureSet_5000 || SupportedFeatures.FeatureSet_4020)
                {
                    return default;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Tags), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        #endregion Operations backed by all supported DiagnostigSource.dll versions

        #region Operations backed by DiagnostigSource.dll versions 5.0 and newer

        public void Dispose()
        {
            if (_activityInstance != null)
            {
                IDisposable disposableActivity = _activityInstance as IDisposable;
                if (disposableActivity != null)
                {
                    disposableActivity.Dispose();
                }
                else
                {
                    Stop();
                }
            }
        }

        public static ActivityStub StartNewActivity(string operationName, ActivityKindStub kind)
        {
            if (!DynamicLoader.EnsureInitialized())
            {
                return NoOpSingeltons.ActivityStub;
            }

            if (SupportedFeatures.FeatureSet_5000)
            {
                return default(ActivityStub);
            }
            else if (SupportedFeatures.FeatureSet_4020)
            {
                ActivityStub activity = StartNewActivity(operationName);

                // Internal is the default. For internal, we avoid creating supplemantal data.
                if (kind != ActivityKindStub.Internal)
                {
                    SupplementalActivityData supplementalData = activity.GetOrCreateSupplementalData();
                    supplementalData.ActivityKind = kind;
                }

                return activity;
            }
            else
            {
                return NoOpSingeltons.ActivityStub;
            }
        }

        public object GetCustomProperty(string propertyName)
        {
            if (_activityInstance == null)
            {
                return null;
            }

            Validate.NotNull(propertyName, nameof(propertyName));

            if (SupportedFeatures.FeatureSet_5000)
            {
                return null; //...
            }
            else if (SupportedFeatures.FeatureSet_4020)
            {
                if (!TryGetSupplementalData(out SupplementalActivityData supplementalData))
                {
                    return null;
                }

                return supplementalData.GetCustomProperty(propertyName);
            }
            else
            {
                string errMsg = FormatNotSupportedErrorMessage(nameof(GetCustomProperty) + "(..)", "4020");
                throw new NotSupportedException(errMsg);
            }
        }

        public void SetCustomProperty(string propertyName, object propertyValue)
        {
            if (_activityInstance == null)
            {
                return;
            }

            Validate.NotNull(propertyName, nameof(propertyName));

            if (SupportedFeatures.FeatureSet_5000)
            {
                //...
            }
            else if (SupportedFeatures.FeatureSet_4020)
            {
                SupplementalActivityData supplementalData;

                if (propertyValue == null)
                {
                    if (TryGetSupplementalData(out supplementalData))
                    {
                        supplementalData.SetCustomProperty(propertyName, propertyValue);
                    }
                }
                else
                {
                    supplementalData = GetOrCreateSupplementalData();
                    supplementalData.SetCustomProperty(propertyName, propertyValue);
                }
            }
            else
            {
                string errMsg = FormatNotSupportedErrorMessage(nameof(SetCustomProperty) + "(..)", "4020");
                throw new NotSupportedException(errMsg);
            }
        }

        public static ActivityIdFormatStub DefaultIdFormat
        {
            get
            {
                if (SupportedFeatures.FeatureSet_5000)
                {
                    return default(ActivityIdFormatStub);
                }
                else if (SupportedFeatures.FeatureSet_4020)
                {
                    return ActivityIdFormatStub.Hierarchical;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(DefaultIdFormat), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }

            set
            {
                if (SupportedFeatures.FeatureSet_5000)
                {
                    //...
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Tags), "5000");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public static bool ForceDefaultIdFormat
        {
            get
            {
                if (SupportedFeatures.FeatureSet_5000)
                {
                    return true;
                }
                else if (SupportedFeatures.FeatureSet_4020)
                {
                    return true; // We only accept hierarchical
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Tags), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }

            set
            {
                if (SupportedFeatures.FeatureSet_5000)
                {
                    //...
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Tags), "5000");
                    throw new NotSupportedException(errMsg);
                }
            }
        }


        public ActivityIdFormatStub IdFormat
        {
            get
            {
                if (_activityInstance == null)
                {
                    return NoOpSingeltons.ActivityIdFormat;
                }

                if (SupportedFeatures.FeatureSet_5000)
                {
                    return default(ActivityIdFormatStub);
                }
                else if (SupportedFeatures.FeatureSet_4020)
                {
                    return ActivityIdFormatStub.Hierarchical;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Tags), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        public ActivityKindStub Kind
        {
            get
            {
                if (_activityInstance == null)
                {
                    return NoOpSingeltons.ActivityKind;
                }

                if (SupportedFeatures.FeatureSet_5000)
                {
                    return default(ActivityKindStub);
                }
                else if (SupportedFeatures.FeatureSet_4020)
                {
                    if (!TryGetSupplementalData(out SupplementalActivityData supplementalData))
                    {
                        return ActivityKindStub.Internal;
                    }

                    return supplementalData.ActivityKind;
                }
                else
                {
                    string errMsg = FormatNotSupportedErrorMessage(nameof(Tags), "4020");
                    throw new NotSupportedException(errMsg);
                }
            }
        }

        #endregion Operations backed by DiagnostigSource.dll versions 5.0 and newer

        #region Tracer-specific extensions

        #endregion Tracer-specific extensions
    }
}
