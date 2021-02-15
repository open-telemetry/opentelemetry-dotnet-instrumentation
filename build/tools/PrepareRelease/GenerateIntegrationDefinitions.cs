using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Datadog.Trace.ClrProfiler;
using Newtonsoft.Json;

namespace PrepareRelease
{
    public static class GenerateIntegrationDefinitions
    {
        public static void Run(params string[] outputDirectories)
        {
            Console.WriteLine("Updating the integrations definitions");

            var assemblies = new List<Assembly>();
            assemblies.Add(typeof(Instrumentation).Assembly);

            // Extract all InstrumentMethodAttribute at assembly scope level
            var assemblyInstrumentMethodAttributes = from assembly in assemblies
                                                     let attributes = assembly.GetCustomAttributes(inherit: false)
                                                            .Select(a => a as InstrumentMethodAttribute)
                                                            .Where(a => a != null).ToList()
                                                     from attribute in attributes
                                                     let callTargetClassCheck = attribute.CallTargetType
                                                        ?? throw new NullReferenceException($"The usage of InstrumentMethodAttribute[Type={attribute.TypeName}, Method={attribute.MethodName}] in assembly scope must define the CallTargetType property.")
                                                     select attribute;

            // Extract all InstrumentMethodAttribute from the classes
            var classesInstrumentMethodAttributes = from assembly in assemblies
                                                    from wrapperType in assembly.GetTypes()
                                                    let attributes = wrapperType.GetCustomAttributes(inherit: false)
                                                            .Select(a => a as InstrumentMethodAttribute)
                                                            .Where(a => a != null)
                                                            .Select(a =>
                                                            {
                                                                a.CallTargetType = wrapperType;
                                                                return a;
                                                            }).ToList()
                                                    from attribute in attributes
                                                    select attribute;

            // combine all InstrumentMethodAttributes
            // and create objects that will generate correct JSON schema
            var callTargetIntegrations = from attribute in assemblyInstrumentMethodAttributes.Concat(classesInstrumentMethodAttributes)
                                         let integrationName = attribute.IntegrationName
                                         let assembly = attribute.CallTargetType.Assembly
                                         let wrapperType = attribute.CallTargetType
                                         orderby integrationName
                                         group new
                                         {
                                             assembly,
                                             wrapperType,
                                             attribute
                                         }
                                         by integrationName into g
                                         select new
                                         {
                                             name = g.Key,
                                             method_replacements = from item in g
                                                                   from assembly in item.attribute.AssemblyNames
                                                                   select new
                                                                   {
                                                                       caller = new
                                                                       {
                                                                           assembly = (string)null,
                                                                           type = (string)null,
                                                                           method = (string)null
                                                                       },
                                                                       target = new
                                                                       {
                                                                           assembly = assembly,
                                                                           type = item.attribute.TypeName,
                                                                           method = item.attribute.MethodName,
                                                                           signature = (string)null,
                                                                           signature_types = new string[] { item.attribute.ReturnTypeName }.Concat(item.attribute.ParameterTypeNames ?? Enumerable.Empty<string>()).ToArray(),
                                                                           minimum_major = item.attribute.VersionRange.MinimumMajor,
                                                                           minimum_minor = item.attribute.VersionRange.MinimumMinor,
                                                                           minimum_patch = item.attribute.VersionRange.MinimumPatch,
                                                                           maximum_major = item.attribute.VersionRange.MaximumMajor,
                                                                           maximum_minor = item.attribute.VersionRange.MaximumMinor,
                                                                           maximum_patch = item.attribute.VersionRange.MaximumPatch
                                                                       },
                                                                       wrapper = new
                                                                       {
                                                                           assembly = item.assembly.FullName,
                                                                           type = item.wrapperType.FullName,
                                                                           method = (string)null,
                                                                           signature = (string)null,
                                                                           action = MethodReplacementActionType.CallTargetModification.ToString()
                                                                       }
                                                                   }
                                         };

            // find all methods in Datadog.Trace.ClrProfiler.Managed.dll with [InterceptMethod]
            // and create objects that will generate correct JSON schema
            var integrations = from assembly in assemblies
                               from wrapperType in assembly.GetTypes()
                               from wrapperMethod in wrapperType.GetRuntimeMethods()
                               let attributes = wrapperMethod.GetCustomAttributes<InterceptMethodAttribute>(inherit: false)
                               where attributes.Any()
                               from attribute in attributes
                               let integrationName = attribute.Integration ?? GetIntegrationName(wrapperType)
                               orderby integrationName
                               group new
                               {
                                   assembly,
                                   wrapperType,
                                   wrapperMethod,
                                   attribute
                               }
                                   by integrationName into g
                               select new
                               {
                                   name = g.Key,
                                   method_replacements = from item in g
                                                         from targetAssembly in item.attribute.TargetAssemblies
                                                         select new
                                                         {
                                                             caller = new
                                                             {
                                                                 assembly = item.attribute.CallerAssembly,
                                                                 type = item.attribute.CallerType,
                                                                 method = item.attribute.CallerMethod
                                                             },
                                                             target = new
                                                             {
                                                                 assembly = targetAssembly,
                                                                 type = item.attribute.TargetType,
                                                                 method = item.attribute.TargetMethod ?? item.wrapperMethod.Name,
                                                                 signature = item.attribute.TargetSignature,
                                                                 signature_types = item.attribute.TargetSignatureTypes,
                                                                 minimum_major = item.attribute.TargetVersionRange.MinimumMajor,
                                                                 minimum_minor = item.attribute.TargetVersionRange.MinimumMinor,
                                                                 minimum_patch = item.attribute.TargetVersionRange.MinimumPatch,
                                                                 maximum_major = item.attribute.TargetVersionRange.MaximumMajor,
                                                                 maximum_minor = item.attribute.TargetVersionRange.MaximumMinor,
                                                                 maximum_patch = item.attribute.TargetVersionRange.MaximumPatch
                                                             },
                                                             wrapper = new
                                                             {
                                                                 assembly = item.assembly.FullName,
                                                                 type = item.wrapperType.FullName,
                                                                 method = item.wrapperMethod.Name,
                                                                 signature = GetMethodSignature(item.wrapperMethod, item.attribute),
                                                                 action = item.attribute.MethodReplacementAction.ToString()
                                                             }
                                                         }
                               };

            var serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            var json = JsonConvert.SerializeObject(callTargetIntegrations.Concat(integrations), serializerSettings);
            Console.WriteLine(json);

            foreach (var outputDirectory in outputDirectories)
            {
                var filename = Path.Combine(outputDirectory, "integrations.json");
                var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                File.WriteAllText(filename, json, utf8NoBom);
            }
        }

        private static string GetIntegrationName(Type wrapperType)
        {
            const string integrations = "Integration";
            var typeName = wrapperType.Name;

            if (typeName.EndsWith(integrations, StringComparison.OrdinalIgnoreCase))
            {
                return typeName.Substring(startIndex: 0, length: typeName.Length - integrations.Length);
            }

            return typeName;
        }

        private static string GetMethodSignature(MethodInfo method, InterceptMethodAttribute attribute)
        {
            var returnType = method.ReturnType;
            var parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();

            var requiredParameterTypes = new[] { typeof(int), typeof(int), typeof(long) };
            var lastParameterTypes = parameters.Skip(parameters.Length - requiredParameterTypes.Length);

            if (attribute.MethodReplacementAction == MethodReplacementActionType.ReplaceTargetMethod)
            {
                if (!lastParameterTypes.SequenceEqual(requiredParameterTypes))
                {
                    throw new Exception(
                        $"Method {method.DeclaringType.FullName}.{method.Name}() does not meet parameter requirements. " +
                        "Wrapper methods must have at least 3 parameters and the last 3 must be of types Int32 (opCode), Int32 (mdToken), and Int64 (moduleVersionPtr).");
                }
            }
            else if (attribute.MethodReplacementAction == MethodReplacementActionType.InsertFirst)
            {
                if (attribute.CallerAssembly == null || attribute.CallerType == null || attribute.CallerMethod == null)
                {
                    throw new Exception(
                        $"Method {method.DeclaringType.FullName}.{method.Name}() does not meet InterceptMethodAttribute requirements. " +
                        "Currently, InsertFirst methods must have CallerAssembly, CallerType, and CallerMethod defined. " +
                        $"Current values: CallerAssembly=\"{attribute.CallerAssembly}\", CallerType=\"{attribute.CallerType}\", CallerMethod=\"{attribute.CallerMethod}\"");
                }
                else if (parameters.Any())
                {
                    throw new Exception(
                        $"Method {method.DeclaringType.FullName}.{method.Name}() does not meet parameter requirements. " +
                        "Currently, InsertFirst methods must have zero parameters.");
                }
                else if (returnType != typeof(void))
                {
                    throw new Exception(
                        $"Method {method.DeclaringType.FullName}.{method.Name}() does not meet return type requirements. " +
                        "Currently, InsertFirst methods must have a void return type.");
                }
            }

            var signatureHelper = SignatureHelper.GetMethodSigHelper(method.CallingConvention, returnType);
            signatureHelper.AddArguments(parameters, requiredCustomModifiers: null, optionalCustomModifiers: null);
            var signatureBytes = signatureHelper.GetSignature();

            if (method.IsGenericMethod)
            {
                // if method is generic, fix first byte (calling convention)
                // and insert a second byte with generic parameter count
                const byte IMAGE_CEE_CS_CALLCONV_GENERIC = 0x10;
                var genericArguments = method.GetGenericArguments();

                var newSignatureBytes = new byte[signatureBytes.Length + 1];
                newSignatureBytes[0] = (byte)(signatureBytes[0] | IMAGE_CEE_CS_CALLCONV_GENERIC);
                newSignatureBytes[1] = (byte)genericArguments.Length;
                Array.Copy(signatureBytes, 1, newSignatureBytes, 2, signatureBytes.Length - 1);

                signatureBytes = newSignatureBytes;
            }

            return string.Join(" ", signatureBytes.Select(b => b.ToString("X2")));
        }
    }
}
