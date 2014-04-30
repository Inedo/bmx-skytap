using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal sealed class StartConfigurationActionEditor : SkytapActionEditorBase
    {
        private ResourcePicker ddlConfigurations;
        private CheckBox chkWaitForStart;

        public override void BindToForm(ActionBase extension)
        {
            var action = (StartConfigurationAction)extension;

            this.ddlConfigurations.SelectedId = action.ConfigurationId;
            this.ddlConfigurations.SelectedName = action.ConfigurationName;
            this.chkWaitForStart.Checked = action.WaitForStart;
        }
        public override ActionBase CreateFromForm()
        {
            return new StartConfigurationAction
            {
                ConfigurationId = this.ddlConfigurations.SelectedId,
                ConfigurationName = this.ddlConfigurations.SelectedName,
                WaitForStart = this.chkWaitForStart.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.ddlConfigurations = new ResourcePicker
            {
                ID = "ddlConfigurations",
                AjaxHandler = Ajax.GetConfigurations,
                Configurer = (SkytapExtensionConfigurer)this.GetExtensionConfigurer()
            };

            this.chkWaitForStart = new CheckBox
            {
                Text = "Wait for configuration to enter running state",
                Checked = true
            };

            this.Controls.Add(
                new SlimFormField("Configuration:", this.ddlConfigurations),
                new SlimFormField("Options:", this.chkWaitForStart)
            );
        }
    }
}
