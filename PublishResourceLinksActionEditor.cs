using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Web.ClientResources;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal sealed class PublishResourceLinksActionEditor : SkytapActionEditorBase
    {
        private ResourcePicker ddlConfiguration;
        private ValidatingTextBox txtPublishedSetName;
        private HiddenField ctlVmAccess;
        private CheckBox chkOneUrlPerVm;
        private PasswordTextBox txtPassword;
        private ValidatingTextBox txtRuntimeLimit;
        private ValidatingTextBox txtExpiration;
        private TimePicker txtDailyAccessStart;
        private TimePicker txtDailyAccessEnd;
        private CheckBox chkExportVariables;

        public override void BindToForm(ActionBase extension)
        {
            var action = (PublishResourceLinksAction)extension;

            this.ddlConfiguration.SelectedId = action.ConfigurationId;
            this.ddlConfiguration.SelectedName = action.ConfigurationName;
            this.txtPublishedSetName.Text = action.PublishedSetName;

            this.ctlVmAccess.Value = Uri.EscapeDataString(InedoLib.Util.CoalesceStr(action.DefaultAccess, "view_only"));
            if (action.VirtualMachines != null && action.VirtualMachines.Length > 0)
                this.ctlVmAccess.Value += "&" + string.Join("&", action.VirtualMachines.Select(v => Uri.EscapeDataString(v.Name) + "&" + Uri.EscapeDataString(v.Access)));

            this.chkOneUrlPerVm.Checked = action.OneUrlPerVm;
            this.txtPassword.Text = action.Password;
            this.txtRuntimeLimit.Text = action.RuntimeLimitHours;
            this.txtExpiration.Text = action.ExpirationHours;

            if (!string.IsNullOrEmpty(action.DailyAccessStart))
                this.txtDailyAccessStart.TimeFromMidnight = TimeSpan.Parse(action.DailyAccessStart);

            if (!string.IsNullOrEmpty(action.DailyAccessEnd))
                this.txtDailyAccessEnd.TimeFromMidnight = TimeSpan.Parse(action.DailyAccessEnd);

            this.chkExportVariables.Checked = action.ExportVariables;
        }
        public override ActionBase CreateFromForm()
        {
            var parts = this.ctlVmAccess
                .Value
                .Split('&')
                .Select(Uri.UnescapeDataString)
                .ToList();

            var defaultAccess = "view_only";
            PublishedVmRef[] vms = null;

            if (parts.Count > 0)
            {
                defaultAccess = parts[0];
                var vmList = new List<PublishedVmRef>();
                for (int i = 1; i < parts.Count - 1; i += 2)
                    vmList.Add(new PublishedVmRef(parts[i], parts[i + 1]));

                vms = vmList.ToArray();
            }

            return new PublishResourceLinksAction
            {
                ConfigurationId = this.ddlConfiguration.SelectedId,
                ConfigurationName = this.ddlConfiguration.SelectedName,
                PublishedSetName = this.txtPublishedSetName.Text,
                DefaultAccess = defaultAccess,
                VirtualMachines = vms,
                OneUrlPerVm = this.chkOneUrlPerVm.Checked,
                Password = this.txtPassword.Text,
                RuntimeLimitHours = this.txtRuntimeLimit.Text,
                ExpirationHours = this.txtExpiration.Text,
                DailyAccessStart = this.txtDailyAccessStart.TimeFromMidnight != null ? this.txtDailyAccessStart.TimeFromMidnight.ToString() : null,
                DailyAccessEnd = this.txtDailyAccessEnd.TimeFromMidnight != null ? this.txtDailyAccessEnd.TimeFromMidnight.ToString() : null,
                ExportVariables = this.chkExportVariables.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.ddlConfiguration = new ResourcePicker
            {
                ID = "ddlConfiguration",
                AjaxHandler = Ajax.GetConfigurations,
                Configurer = (SkytapExtensionConfigurer)this.GetExtensionConfigurer()
            };

            this.txtPublishedSetName = new ValidatingTextBox { DefaultText = "untitled" };

            this.ctlVmAccess = new HiddenField { ID = "ctlVmAccess" };

            this.chkOneUrlPerVm = new CheckBox { Text = "Generate a separate URL for each virtual machine" };

            this.txtPassword = new PasswordTextBox { DefaultText = "not required" };

            this.txtRuntimeLimit = new ValidatingTextBox { DefaultText = "no limit" };

            this.txtExpiration = new ValidatingTextBox { DefaultText = "never expires" };

            this.txtDailyAccessStart = new TimePicker();

            this.txtDailyAccessEnd = new TimePicker();

            this.chkExportVariables = new CheckBox { Text = "Save URLs to ${Skytap-DesktopsUrl}", Checked = true };

            Div ctlVmContainer;
            using (var templateStream = typeof(PublishResourceLinksActionEditor).Assembly.GetManifestResourceStream(typeof(PublishResourceLinksActionEditor).FullName + ".html"))
            using (var reader = new StreamReader(templateStream))
            {
                ctlVmContainer = new Div(
                    new LiteralControl(reader.ReadToEnd())
                ) { ID = "ctlVmContainer" };
            }

            this.Controls.Add(
                new SlimFormField("Environment:", this.ddlConfiguration),
                new SlimFormField("Published set name:", this.txtPublishedSetName),
                new SlimFormField("Virtual machines:", this.ctlVmAccess, ctlVmContainer),
                new SlimFormField("Password:", this.txtPassword),
                new SlimFormField("Runtime limit:", this.txtRuntimeLimit),
                new SlimFormField("URL expiration:", this.txtExpiration),
                new SlimFormField("Daily access window:", this.txtDailyAccessStart, " to ", this.txtDailyAccessEnd),
                new SlimFormField("Options:", new Div(this.chkOneUrlPerVm), new Div(this.chkExportVariables)),
                new RenderJQueryDocReadyDelegator(
                    w =>
                    {
                        w.Write("BmPublishResourceLinksActionEditor(");
                        InedoLib.Util.JavaScript.WriteJson(
                            w,
                            new
                            {
                                vmDataSelector = "#" + this.ctlVmAccess.ClientID,
                                containerSelector = "#" + ctlVmContainer.ClientID
                            }
                        );
                        w.Write(");");
                    }
                )
            );
        }
        protected override void OnPreRender(EventArgs e)
        {
            var dp = (IClientResource)Type
                .GetType("Inedo.BuildMaster.Web.WebApplication.Resources.WebResources,BuildMaster.Web.WebApplication")
                .GetProperty("knockout_js")
                .GetValue(null, null);

            this.IncludeClientResourceInPage(
                new JavascriptResource
                {
                    ResourcePath = "~/extension-resources/Skytap/PublishResourceLinksActionEditor.js?" + typeof(PublishResourceLinksActionEditor).Assembly.GetName().Version,
                    CompatibleVersions = { InedoLibCR.Versions.jq171 },
                    Dependencies = { dp }
                }
            );

            base.OnPreRender(e);
        }
    }
}
