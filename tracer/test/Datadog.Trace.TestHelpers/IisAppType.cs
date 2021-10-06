namespace Datadog.Trace.TestHelpers
{
    public enum IisAppType
    {
        /// <summary>
        /// ASP.NET app using the Clr4IntegratedAppPool app pool
        /// </summary>
        AspNetIntegrated,

        /// <summary>
        /// ASP.NET app using the Clr4ClassicAppPool app pool
        /// </summary>
        AspNetClassic,

        /// <summary>
        /// ASP.NET Core using in-process hosting model and the UnmanagedClassicAppPool app pool
        /// </summary>
        AspNetCoreInProcess,

        /// <summary>
        /// ASP.NET Core using out-of-process hosting model and the UnmanagedClassicAppPool app pool
        /// </summary>
        AspNetCoreOutOfProcess,
    }
}
