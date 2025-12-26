using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace XmlComparer.Core
{
    /// <summary>
    /// Factory for creating XmlSchemaSet instances from embedded resources or files.
    /// </summary>
    public static class XmlSchemaSetFactory
    {
        /// <summary>
        /// Lists all embedded XSD schema resources in the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to search for embedded resources.</param>
        /// <returns>An array of embedded resource names.</returns>
        /// <exception cref="ArgumentNullException">Thrown when assembly is null.</exception>
        public static string[] ListEmbeddedSchemas(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            return assembly.GetManifestResourceNames();
        }

        /// <summary>
        /// Creates an XmlSchemaSet from a single embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded schema.</param>
        /// <param name="resourceName">The name of the embedded resource.</param>
        /// <returns>A compiled XmlSchemaSet containing the schema.</returns>
        public static XmlSchemaSet FromEmbeddedResource(Assembly assembly, string resourceName)
        {
            return FromEmbeddedResources(assembly, new[] { resourceName });
        }

        /// <summary>
        /// Creates an XmlSchemaSet from multiple embedded resources.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded schemas.</param>
        /// <param name="resourceNames">The names of the embedded resources.</param>
        /// <returns>A compiled XmlSchemaSet containing all schemas.</returns>
        /// <exception cref="ArgumentNullException">Thrown when assembly or resourceNames is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when a resource is not found.</exception>
        public static XmlSchemaSet FromEmbeddedResources(Assembly assembly, IEnumerable<string> resourceNames)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (resourceNames == null) throw new ArgumentNullException(nameof(resourceNames));

            var set = new XmlSchemaSet();

            // Secure settings for reading schemas from embedded resources
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            foreach (var resourceName in resourceNames)
            {
                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new FileNotFoundException($"Embedded resource not found: {resourceName}", resourceName);
                }

                using var reader = XmlReader.Create(stream, settings);
                var schema = XmlSchema.Read(reader, ValidationWarningHandler);
                if (schema != null)
                {
                    set.Add(schema);
                }
            }

            set.Compile();
            return set;
        }

        /// <summary>
        /// Creates an XmlSchemaSet from XSD file paths.
        /// </summary>
        /// <param name="xsdPaths">Paths to XSD schema files.</param>
        /// <returns>A compiled XmlSchemaSet containing all schemas.</returns>
        /// <exception cref="ArgumentNullException">Thrown when xsdPaths is null.</exception>
        /// <exception cref="ArgumentException">Thrown when paths contain traversal sequences.</exception>
        public static XmlSchemaSet FromFiles(IEnumerable<string> xsdPaths)
        {
            if (xsdPaths == null) throw new ArgumentNullException(nameof(xsdPaths));

            var set = new XmlSchemaSet();

            // Secure settings for reading schemas from files
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            foreach (var path in xsdPaths)
            {
                XmlSecuritySettings.ValidatePath(path, nameof(xsdPaths));

                using var reader = XmlReader.Create(path, settings);
                var schema = XmlSchema.Read(reader, ValidationWarningHandler);
                if (schema != null)
                {
                    set.Add(schema);
                }
            }

            set.Compile();
            return set;
        }

        /// <summary>
        /// Handles validation warnings when reading schemas.
        /// </summary>
        private static void ValidationWarningHandler(object? sender, ValidationEventArgs e)
        {
            // Log or handle schema validation warnings if needed
            // Currently silently ignoring warnings as they don't prevent schema compilation
            if (e.Severity == XmlSeverityType.Warning)
            {
                // Could log here: System.Diagnostics.Debug.WriteLine($"Schema warning: {e.Message}");
            }
        }
    }
}
