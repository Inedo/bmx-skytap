using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace Inedo.BuildMasterExtensions.Skytap.SkytapApi
{
    internal sealed class SkytapVirtualMachine : SkytapResource
    {
        public SkytapVirtualMachine(XElement vm)
            : base((string)vm.Element("id"), (string)vm.Element("name"))
        {
            this.Runstate = (string)vm.Element("runstate");

            this.NetworkInterfaces = vm
                .Elements("interfaces")
                .SelectMany(e => e.Elements("interface").Select(i => new SkytapNetworkInterface(i)))
                .ToList()
                .AsReadOnly();

            this.Credentials = vm
                .Elements("credentials")
                .SelectMany(e => e.Elements("credential").Select(c => new SkytapCredential(c)))
                .Where(c => !string.IsNullOrEmpty(c.UserName))
                .ToList()
                .AsReadOnly();
        }

        public string Runstate { get; private set; }
        public ReadOnlyCollection<SkytapNetworkInterface> NetworkInterfaces { get; private set; }
        public ReadOnlyCollection<SkytapCredential> Credentials { get; private set; }
    }
}
