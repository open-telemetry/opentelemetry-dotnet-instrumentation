namespace Datadog.Trace.ClrProfiler.Emit
{
    internal enum OpCodeValue : short
    {
        /// <seealso cref="System.Reflection.Emit.OpCodes.Call"/>
        Call = 40,

        /// <seealso cref="System.Reflection.Emit.OpCodes.Callvirt"/>
        Callvirt = 111
    }
}
