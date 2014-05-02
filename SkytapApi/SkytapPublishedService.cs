using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Inedo.BuildMasterExtensions.Skytap.SkytapApi
{
    internal sealed class SkytapPublishedService
    {
        public SkytapPublishedService(XElement service)
        {
            this.Id = (string)service.Element("id");
            this.InternalPort = (int)service.Element("internal_port");
            this.ExternalIPAddress = (string)service.Element("external_ip");
            this.ExternalPort = (int)service.Element("external_port");
        }

        public string Id { get; private set; }
        public int InternalPort { get; private set; }
        public string ExternalIPAddress { get; private set; }
        public int ExternalPort { get; private set; }
    }
}
