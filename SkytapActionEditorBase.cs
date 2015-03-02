using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal abstract class SkytapActionEditorBase : ActionEditorBase
    {
        protected SkytapActionEditorBase()
        {
            this.ValidateBeforeCreate += this.SkytapActionEditorBase_ValidateBeforeCreate;
        }

        private void SkytapActionEditorBase_ValidateBeforeCreate(object sender, ValidationEventArgs<ActionBase> e)
        {
            var configurer = (SkytapExtensionConfigurer)this.GetExtensionConfigurer();
            if (configurer == null || string.IsNullOrEmpty(configurer.UserName) || string.IsNullOrEmpty(configurer.Password))
            {
                e.ValidLevel = ValidationLevel.Warning;
                e.Message = "This action requires a Skytap user name and API key to be provided on the Skytap extension configuration page.";
            }
        }
    }
}
