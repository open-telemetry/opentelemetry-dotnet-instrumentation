using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GeneratePackageVersions
{
    public class XUnitFileGenerator : FileGenerator
    {
        private const string HeaderConst =
@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the GeneratePackageVersions tool. To safely
//     modify this file, edit PackageVersionsGeneratorDefinitions.json and
//     re-run the GeneratePackageVersions project in Visual Studio. See the
//     launchSettings.json for the project if you would like to run the tool
//     with the correct arguments outside of Visual Studio.

//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated. 
// </auto-generated>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{{
    [SuppressMessage(""StyleCop.CSharp.LayoutRules"", ""SA1516:Elements must be separated by blank line"", Justification = ""This is an auto-generated file."")]
    public class {0}
    {{";

        private const string FooterConst =
@"    }
}";

        private const string EntryFormat =
@"                new object[] {{ ""{0}"" }},";

        private const string BodyFormat =
@"{0}        public static IEnumerable<object[]> {1} =>

            new List<object[]>
            {{
#if DEFAULT_SAMPLES
                new object[] {{ string.Empty }},
#else{2}
#endif
            }};{3}
";

        private const string IfNetFrameworkDirectiveConst =
@"
#if NETFRAMEWORK";

        private const string EndIfDirectiveConst =
@"
#endif";

        private readonly string _className;

        public XUnitFileGenerator(string filename, string className)
            : base(filename)
        {
            _className = className;
        }

        protected override string Header
        {
            get
            {
                return string.Format(HeaderConst, _className);
            }
        }

        protected override string Footer
        {
            get
            {
                return FooterConst;
            }
        }

        public override void Write(PackageVersionEntry packageVersionEntry, IEnumerable<string> netFrameworkPackageVersions, IEnumerable<string> netCorePackageVersions)
        {
            Debug.Assert(Started, "Cannot call Write() before calling Start()");
            Debug.Assert(!Finished, "Cannot call Write() after calling Finish()");

            var bodyStringBuilder = new StringBuilder();

            bodyStringBuilder.Append(IfNetFrameworkDirectiveConst);
            foreach (var packageVersion in netFrameworkPackageVersions)
            {
                bodyStringBuilder.AppendLine();
                bodyStringBuilder.Append(string.Format(EntryFormat, packageVersion));
            }

            bodyStringBuilder.Append(EndIfDirectiveConst);

            foreach (var packageVersion in netCorePackageVersions)
            {
                bodyStringBuilder.AppendLine();
                bodyStringBuilder.Append(string.Format(EntryFormat, packageVersion));
            }

            string ifDirective = string.IsNullOrEmpty(packageVersionEntry.SampleTargetFramework) ? string.Empty : $"#if {packageVersionEntry.SampleTargetFramework.ToUpper().Replace('.', '_')}{Environment.NewLine}";
            string endifDirective = string.IsNullOrEmpty(packageVersionEntry.SampleTargetFramework) ? string.Empty : EndIfDirectiveConst;
            FileStringBuilder.AppendLine(string.Format(BodyFormat, ifDirective, packageVersionEntry.IntegrationName, bodyStringBuilder.ToString(), endifDirective));
        }
    }
}
