using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Inedo.BuildMasterExtensions.Skytap.SkytapApi
{
    internal sealed class SkytapConfiguration : SkytapResource
    {
        public SkytapConfiguration(XElement configuration)
            : base((string)configuration.Element("id"), (string)configuration.Element("name"))
        {
            this.Runstate = (string)configuration.Element("runstate");
            this.VirtualMachines = configuration
                .Elements("vms")
                .SelectMany(e => e.Elements("vm").Select(v => new SkytapVirtualMachine(v)))
                .ToList()
                .AsReadOnly();
        }

        public string Runstate { get; private set; }
        public ReadOnlyCollection<SkytapVirtualMachine> VirtualMachines { get; private set; }
    }
}
