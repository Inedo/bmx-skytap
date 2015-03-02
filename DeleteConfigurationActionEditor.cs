using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal sealed class DeleteConfigurationActionEditor : SkytapActionEditorBase
    {
        private ResourcePicker ddlConfiguration;

        public override void BindToForm(ActionBase extension)
        {
            var action = (DeleteConfigurationAction)extension;

            this.ddlConfiguration.SelectedId = action.ConfigurationId;
            this.ddlConfiguration.SelectedName = action.ConfigurationName;
        }
        public override ActionBase CreateFromForm()
        {
            return new DeleteConfigurationAction
            {
                ConfigurationId = this.ddlConfiguration.SelectedId,
                ConfigurationName = this.ddlConfiguration.SelectedName
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

            this.Controls.Add(
                new SlimFormField("Environment to delete:", this.ddlConfiguration)
            );
        }
    }
}
