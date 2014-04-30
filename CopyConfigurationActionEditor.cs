using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal sealed class CopyConfigurationActionEditor : SkytapActionEditorBase
    {
        private ResourcePicker ddlConfiguration;
        private ValidatingTextBox txtNewConfigurationName;
        private CheckBox chkExportVariables;

        public override void BindToForm(ActionBase extension)
        {
            var action = (CopyConfigurationAction)extension;

            this.ddlConfiguration.SelectedId = action.ConfigurationId;
            this.ddlConfiguration.SelectedName = action.ConfigurationName;
            this.txtNewConfigurationName.Text = action.NewConfigurationName;
            this.chkExportVariables.Checked = action.ExportVariables;
        }
        public override ActionBase CreateFromForm()
        {
            return new CopyConfigurationAction
            {
                ConfigurationId = this.ddlConfiguration.SelectedId,
                ConfigurationName = this.ddlConfiguration.SelectedName,
                NewConfigurationName = this.txtNewConfigurationName.Text,
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

            this.txtNewConfigurationName = new ValidatingTextBox { MaxLength = 1000 };

            this.chkExportVariables = new CheckBox { Text = "Save ID to ${Skytap-ConfigurationId}", Checked = true };

            this.Controls.Add(
                new SlimFormField("Configuration to copy:", this.ddlConfiguration),
                new SlimFormField("New configuration name:", this.txtNewConfigurationName),
                new SlimFormField("Options:", this.chkExportVariables)
            );
        }
    }
}
