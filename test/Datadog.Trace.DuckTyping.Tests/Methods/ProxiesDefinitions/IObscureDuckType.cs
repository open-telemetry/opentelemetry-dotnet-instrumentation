namespace Datadog.Trace.DuckTyping.Tests.Methods.ProxiesDefinitions
{
    public interface IObscureDuckType
    {
#if INTERFACE_DEFAULTS
        int Sum(int a, int b) => a + b;
#else
        int Sum(int a, int b);
#endif

        float Sum(float a, float b);

        double Sum(double a, double b);

        short Sum(short a, short b);

        TestEnum2 ShowEnum(TestEnum2 val);

        object InternalSum(int a, int b);

        [Duck(ParameterTypeNames = new string[] { "System.String", "Datadog.Trace.DuckTyping.Tests.ObscureObject+DummyFieldObject, Datadog.Trace.DuckTyping.Tests" })]
        void Add(string name, object obj);

        void Add(string name, int obj);

        void Add(string name, string obj = "none");

        void Pow2(ref int value);

        void GetOutput(out int value);

        [Duck(Name = "GetOutput")]
        void GetOutputObject(out object value);

        bool TryGetObscure(out IDummyFieldObject obj);

        [Duck(Name = "TryGetObscure")]
        bool TryGetObscureObject(out object obj);

        void GetReference(ref int value);

        [Duck(Name = "GetReference")]
        void GetReferenceObject(ref object value);

        bool TryGetReference(ref IDummyFieldObject obj);

        [Duck(Name = "TryGetReference")]
        bool TryGetReferenceObject(ref object obj);

        bool TryGetPrivateObscure(out IDummyFieldObject obj);

        [Duck(Name = "TryGetPrivateObscure")]
        bool TryGetPrivateObscureObject(out object obj);

        bool TryGetPrivateReference(ref IDummyFieldObject obj);

        [Duck(Name = "TryGetPrivateReference")]
        bool TryGetPrivateReferenceObject(ref object obj);

        IDummyFieldObject Bypass(IDummyFieldObject obj);
    }
}
