﻿using System.Xml.Serialization;

namespace SmarterBalanced.SampleItems.Core.Diagnostics.Models
{
    public class AccessStatus : BaseDiagnostic
    {
        [XmlIgnore]
        public bool? Readable { get; set; }

        [XmlElement(ElementName = "db-readable")]
        public string ReadableStatus
        {
            get
            {
                string val = null;
                if (Readable.HasValue)
                {
                    val = Readable.ToString().ToLower();
                }
                return val;
            }
            set { }
        }

        [XmlIgnore]
        public bool? Writable { get; set; }

        [XmlElement(ElementName = "db-writable")]
        public string WritableStatus
        {
            get
            {
                string val = null;
                if (Writable.HasValue)
                {
                    val = Writable.ToString().ToLower();
                }
                return val;
            }
            set { }
        }
    }
}
