//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;

namespace Datadog.Trace.Vendors.StatsdClient
{
    #pragma warning disable CS1591
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "See ObsoleteAttribute.")]
    [ObsoleteAttribute("This interface will become private in a future release.")]
    internal interface IStopwatch
    {
        void Start();

        void Stop();

        int ElapsedMilliseconds();
    }
}