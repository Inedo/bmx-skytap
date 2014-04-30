using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Publish Skytap Resource Links",
        "Creates a Skytap Publish Set for an entire configuration or some of its virtual machines.")]
    [Tag("skytap")]
    [CustomEditor(typeof(PublishResourceLinksActionEditor))]
    public sealed class PublishResourceLinksAction : SkytapConfigurationActionBase
    {
        [Persistent]
        public string PublishedSetName { get; set; }

        [Persistent]
        public string DefaultAccess { get; set; }
        [Persistent(CustomSerializer = typeof(VmRefAdapter), CustomVariableReplacer = typeof(VmRefAdapter))]
        public PublishedVmRef[] VirtualMachines { get; set; }

        [Persistent]
        public bool OneUrlPerVm { get; set; }

        [Persistent]
        public string Password { get; set; }

        [Persistent]
        public string RuntimeLimitHours { get; set; }

        [Persistent]
        public string ExpirationHours { get; set; }

        [Persistent]
        public string DailyAccessStart { get; set; }
        [Persistent]
        public string DailyAccessEnd { get; set; }

        [Persistent]
        public bool ExportVariables { get; set; }

        public override ActionDescription GetActionDescription()
        {
            var shortDesc = new ShortActionDescription("Publish ", this.ConfigurationName, " Skytap Configuration");
            if (!string.IsNullOrEmpty(this.PublishedSetName))
                shortDesc.AppendContent(" to ", new Hilite(this.PublishedSetName));

            var longDesc = new LongActionDescription("with ");
            if (this.VirtualMachines == null || this.VirtualMachines.Length == 0)
            {
                longDesc.AppendContent(
                    new Hilite("all"),
                    " virtual machines at ",
                    new Hilite(this.DefaultAccess)
                );
            }
            else
            {
                longDesc.AppendContent(
                    new ListHilite(this.VirtualMachines.Select(v => string.Format("{0} ({1})", v.Name, v.Access)))
                );
                longDesc.AppendContent(" virtual machines");
            }

            var extraInfo = new List<object>();

            if (!string.IsNullOrEmpty(this.RuntimeLimitHours))
                extraInfo.Add(new object[] { "runtime limit of ", new Hilite(this.RuntimeLimitHours + " hours") });

            if (!string.IsNullOrEmpty(this.ExpirationHours))
                extraInfo.Add(new object[] { "expiring after ", new Hilite(this.ExpirationHours + " hours") });

            if (!string.IsNullOrEmpty(this.DailyAccessStart) && !string.IsNullOrEmpty(this.DailyAccessEnd))
            {
                var start = TimeSpan.Parse(this.DailyAccessStart);
                var end = TimeSpan.Parse(this.DailyAccessEnd);

                var today = DateTime.Today;
                extraInfo.Add(
                    new object[]
                    {
                        "daily access from ",
                        new Hilite((today + start).ToShortTimeString()),
                        " to ",
                        new Hilite((today + end).ToShortTimeString())
                    }
                );
            }

            if (extraInfo.Count > 0)
            {
                foreach (var info in extraInfo.Take(extraInfo.Count - 1))
                    longDesc.AppendContent(", ", info);

                if (extraInfo.Count > 1)
                    longDesc.AppendContent(", and ", extraInfo.Last());
            }

            return new ActionDescription(shortDesc, longDesc);
        }

        internal override void Execute(SkytapClient client, SkytapResource configuration)
        {
            if (string.IsNullOrWhiteSpace(this.DefaultAccess) && (this.VirtualMachines == null || this.VirtualMachines.Length == 0))
            {
                this.LogError("Virtual machine access level is not specified.");
                return;
            }

            this.LogInformation("Getting list of virtual machines for {0} configuration...", configuration.Name);
            var configVms = client.ListVms(configuration.Id).ToList();

            this.LogDebug("Received {0} virtual machines.", configVms.Count);

            var publishedVmRefs = new List<SkytapPublishedVmRef>();

            if (this.VirtualMachines != null && this.VirtualMachines.Length > 0)
            {
                var vms = this.VirtualMachines
                    .GroupJoin(
                        configVms,
                        p => p.Name,
                        v => v.Name,
                        (p, v) => new { p.Name, p.Access, Matches = v.ToList() },
                        StringComparer.OrdinalIgnoreCase);

                bool errors = false;
                foreach (var vm in vms)
                {
                    if (vm.Matches.Count == 1)
                    {
                        this.LogDebug("{0} with {1} access", vm.Name, vm.Access);
                        publishedVmRefs.Add(new SkytapPublishedVmRef(vm.Matches[0].Id, vm.Access));
                    }
                    else if (vm.Matches.Count == 0)
                    {
                        this.LogError("Could not resolve virtual machine named {0} in {1} configuration.", vm.Name, configuration.Name);
                        errors = true;
                    }
                    else
                    {
                        this.LogError("Ambiguous virtual machine names ({0}) in {1} configuration.", vm.Name, configuration.Name);
                        errors = true;
                    }
                }

                if (errors)
                    return;
            }
            else
            {
                foreach (var vm in configVms)
                {
                    this.LogDebug("{0} with {1} access", vm.Name, this.DefaultAccess);
                    publishedVmRefs.Add(new SkytapPublishedVmRef(vm.Id, this.DefaultAccess));
                }
            }

            TimeSpan? runtimeLimit = null;
            if (!string.IsNullOrEmpty(this.RuntimeLimitHours))
            {
                runtimeLimit = TimeSpan.FromHours(double.Parse(this.RuntimeLimitHours));
                this.LogDebug("Runtime limit: " + runtimeLimit);
            }

            DateTime? expirationDate = null;
            if (!string.IsNullOrEmpty(this.ExpirationHours))
            {
                var timeUntilExipiration = TimeSpan.FromHours(double.Parse(this.ExpirationHours));
                expirationDate = DateTime.UtcNow + timeUntilExipiration;
                this.LogDebug("Expiration date: {0} (UTC)", expirationDate);
            }

            TimeSpan? dailyStart = null;
            TimeSpan? dailyEnd = null;
            if (!string.IsNullOrEmpty(this.DailyAccessStart) && !string.IsNullOrEmpty(this.DailyAccessEnd))
            {
                var utcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
                var start = TimeSpan.Parse(this.DailyAccessStart) - utcOffset;
                var end = TimeSpan.Parse(this.DailyAccessEnd) - utcOffset;

                if (start < TimeSpan.Zero)
                    start += new TimeSpan(24, 0, 0);
                else if (start >= new TimeSpan(24, 0, 0))
                    start -= new TimeSpan(24, 0, 0);

                if (end < TimeSpan.Zero)
                    end += new TimeSpan(24, 0, 0);
                else if (end >= new TimeSpan(24, 0, 0))
                    end -= new TimeSpan(24, 0, 0);

                dailyStart = start;
                dailyEnd = end;

                this.LogDebug("Daily access window: {0} to {1} (UTC)", start, end);
            }

            this.LogInformation("Creating publish set...");
            var publishedSet = client.CreatePublishSet(
                configuration.Id,
                this.PublishedSetName,
                this.OneUrlPerVm,
                dailyStart,
                dailyEnd,
                this.Password,
                publishedVmRefs,
                runtimeLimit,
                expirationDate
            );

            this.LogInformation("Publish set created.");

            if (this.ExportVariables)
            {
                string desktopsUrl;

                if (this.OneUrlPerVm && publishedSet.Vms.Count > 1)
                {
                    desktopsUrl = string.Join(
                        Environment.NewLine,
                        publishedSet.Vms
                            .Join(
                                configVms,
                                p => p.Id,
                                v => v.Id,
                                (p, v) => new { v.Name, p.DesktopUrl })
                            .Select(v => v.Name + ": " + v.DesktopUrl)
                    );
                }
                else
                {
                    desktopsUrl = publishedSet.DesktopsUrl;
                }

                this.SetSkytapVariableValue("DesktopsUrl", desktopsUrl);
            }
        }
    }

    public sealed class PublishedVmRef
    {
        public PublishedVmRef(string name, string access)
        {
            this.Name = name;
            this.Access = access;
        }

        public string Name { get; set; }
        public string Access { get; set; }
    }

    public sealed class VmRefAdapter : ICustomPersistentSerializer, ICustomVariableReplacer
    {
        object ICustomPersistentSerializer.Deserialize(XElement element)
        {
            return element
                .Elements("VmRef")
                .Select(v => new PublishedVmRef((string)v.Attribute("Name"), (string)v.Attribute("Access")))
                .ToArray();
        }
        object ICustomPersistentSerializer.Serialize(object instance)
        {
            return ((PublishedVmRef[])instance)
                .Select(v => new XElement("VmRef", new XAttribute("Name", v.Name), new XAttribute("Access", v.Access)));
        }

        IEnumerable<VariableExpandingField> ICustomVariableReplacer.GetFieldsToExpand(object instance)
        {
            return ((PublishedVmRef[])instance)
                .Select(v => new VariableExpandingField(v.Name, n => v.Name = n));
        }
    }
}
