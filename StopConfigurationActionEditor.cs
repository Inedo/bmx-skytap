using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal sealed class StopConfigurationActionEditor : SkytapActionEditorBase
    {
        private ResourcePicker ddlConfigurations;
        private DropDownList ddlTargetRunstate;
        private CheckBox chkWaitForStop;

        public override void BindToForm(ActionBase extension)
        {
            var action = (StopConfigurationAction)extension;

            this.ddlConfigurations.SelectedId = action.ConfigurationId;
            this.ddlConfigurations.SelectedName = action.ConfigurationName;
            this.ddlTargetRunstate.SelectedValue = ((int)action.Runstate).ToString();
            this.chkWaitForStop.Checked = action.WaitForStop;
        }
        public override ActionBase CreateFromForm()
        {
            return new StopConfigurationAction
            {
                ConfigurationId = this.ddlConfigurations.SelectedId,
                ConfigurationName = this.ddlConfigurations.SelectedName,
                Runstate = (StopConfigurationMode)int.Parse(this.ddlTargetRunstate.SelectedValue),
                WaitForStop = this.chkWaitForStop.Checked
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

            this.ddlTargetRunstate = new DropDownList
            {
                ID = "ddlTargetRunstate",
                Items =
                {
                    new ListItem("Suspend", ((int)StopConfigurationMode.Suspend).ToString()),
                    new ListItem("Shut down", ((int)StopConfigurationMode.ShutDown).ToString()),
                    new ListItem("Power off", ((int)StopConfigurationMode.PowerOff).ToString())
                }
            };

            this.chkWaitForStop = new CheckBox
            {
                Text = "Wait for environment to enter target state",
                Checked = true
            };

            this.Controls.Add(
                new SlimFormField("Environment:", this.ddlConfigurations),
                new SlimFormField("Stop mode:", this.ddlTargetRunstate),
                new SlimFormField("Options:", this.chkWaitForStop)
            );
        }
    }
}
