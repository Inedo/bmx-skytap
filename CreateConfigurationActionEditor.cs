using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal sealed class CreateConfigurationActionEditor : SkytapActionEditorBase
    {
        private ResourcePicker ddlTemplate;
        private ValidatingTextBox txtConfigurationName;
        private CheckBox chkExportVariables;

        public override void BindToForm(ActionBase extension)
        {
            var action = (CreateConfigurationAction)extension;

            this.ddlTemplate.SelectedId = action.TemplateId;
            this.ddlTemplate.SelectedName = action.TemplateName;
            this.txtConfigurationName.Text = action.ConfigurationName;
            this.chkExportVariables.Checked = action.ExportVariables;
        }
        public override ActionBase CreateFromForm()
        {
            return new CreateConfigurationAction
            {
                TemplateId = this.ddlTemplate.SelectedId,
                TemplateName = this.ddlTemplate.SelectedName,
                ConfigurationName = this.txtConfigurationName.Text,
                ExportVariables = this.chkExportVariables.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.ddlTemplate = new ResourcePicker
            {
                ID = "ddlTemplate",
                AjaxHandler = Ajax.GetTemplates,
                Configurer = (SkytapExtensionConfigurer)this.GetExtensionConfigurer()
            };

            this.txtConfigurationName = new ValidatingTextBox { MaxLength = 1000 };

            this.chkExportVariables = new CheckBox { Text = "Save ID to ${Skytap-EnvironmentId}", Checked = true };

            this.Controls.Add(
                new SlimFormField("Template:", this.ddlTemplate),
                new SlimFormField("New environment name:", this.txtConfigurationName),
                new SlimFormField("Options:", this.chkExportVariables)
            );
        }
    }
}
