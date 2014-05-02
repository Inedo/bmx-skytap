using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal sealed class CreateBuildMasterServersActionEditor : ActionEditorBase
    {
        private ResourcePicker ddlConfiguration;

        public override void BindToForm(ActionBase extension)
        {
            var action = (CreateBuildMasterServersAction)extension;

            this.ddlConfiguration.SelectedId = action.ConfigurationId;
            this.ddlConfiguration.SelectedName = action.ConfigurationName;
        }
        public override ActionBase CreateFromForm()
        {
            return new CreateBuildMasterServersAction
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
                new SlimFormField("Configuration:", this.ddlConfiguration)
            );
        }
    }
}
