using System.Collections.Generic;
using System.Linq;

namespace Inedo.BuildMasterExtensions.Skytap.SkytapApi
{
    internal sealed class SkytapPublishedSet : SkytapResource
    {
        public SkytapPublishedSet(string id, string name, string desktopsUrl, IEnumerable<SkytapPublishedVmRef> vms)
            : base(id, name)
        {
            this.DesktopsUrl = desktopsUrl;
            this.Vms = (vms ?? Enumerable.Empty<SkytapPublishedVmRef>()).ToList();
        }

        public string DesktopsUrl { get; private set; }
        public List<SkytapPublishedVmRef> Vms { get; private set; }
    }
}
