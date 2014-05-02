using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Inedo.BuildMasterExtensions.Skytap.SkytapApi
{
    internal sealed class SkytapNetworkInterface
    {
        public SkytapNetworkInterface(XElement networkInterface)
        {
            this.Id = (string)networkInterface.Element("id");
            this.IPAddress = (string)networkInterface.Element("ip");
            this.HostName = (string)networkInterface.Element("hostname");
            this.Services = networkInterface
                .Elements("services")
                .SelectMany(e => e.Elements("service").Select(s => new SkytapPublishedService(s)))
                .ToList()
                .AsReadOnly();
        }

        public string Id { get; private set; }
        public string IPAddress { get; private set; }
        public string HostName { get; private set; }
        public ReadOnlyCollection<SkytapPublishedService> Services { get; private set; }
    }
}
