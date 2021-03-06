using System;
using XRoadLib.Schema;
using XRoadLib.Serialization.Mapping;

namespace XRoadLib.Attributes
{
    /// <summary>
    /// Defines operation method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class XRoadServiceAttribute : Attribute
    {
        private static readonly Type serviceMapType = typeof(ServiceMap);

        internal uint? addedInVersion;
        internal uint? removedInVersion;

        /// <summary>
        /// Operation name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// ServiceMap type which implements operation definition.
        /// </summary>
        public virtual Type ServiceMapType => serviceMapType;

        /// <summary>
        /// Abstract operations do not define binding details.
        /// </summary>
        public virtual bool IsAbstract { get; set; }

        /// <summary>
        /// Hidden operations are not included in service description.
        /// </summary>
        public virtual bool IsHidden { get; set; }

        /// <summary>
        /// X-Road service version which first defined given operation.
        /// </summary>
        public virtual uint AddedInVersion { get => addedInVersion.GetValueOrDefault(1u); set => addedInVersion = value; }

        /// <summary>
        /// X-Road service version which removed given operation.
        /// </summary>
        public virtual uint RemovedInVersion { get => removedInVersion.GetValueOrDefault(uint.MaxValue); set => removedInVersion = value; }

        /// <summary>
        /// Provides extension specific customizations for the schema.
        /// </summary>
        public virtual ISchemaExporter SchemaExporter { get; } = null;

        /// <summary>
        /// Attachment serialization mode for service input. Available options are `Xml` (binary is serialized inside XML document
        /// using base64 encoding or MTOM optimization) or `Attachment` (binary is serialized as mime multipart attachment)
        /// </summary>
        public virtual BinaryMode InputBinaryMode { get; set; } = BinaryMode.Xml;

        /// <summary>
        /// Attachment serialization mode for service output. Available options are `Xml` (binary is serialized inside XML document
        /// using base64 encoding or MTOM optimization) or `Attachment` (binary is serialized as mime multipart attachment)
        /// </summary>
        public virtual BinaryMode OutputBinaryMode { get; set; }= BinaryMode.Xml;

        /// <summary>
        /// SOAPAction header value for this service.
        /// </summary>
        public virtual string SoapAction { get; set; }

        /// <summary>
        /// Initializes new operation definition.
        /// </summary>
        public XRoadServiceAttribute(string name)
        {
            Name = name;
        }
    }
}