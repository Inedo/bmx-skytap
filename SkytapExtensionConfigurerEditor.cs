using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal sealed class SkytapExtensionConfigurerEditor : ExtensionConfigurerEditorBase
    {
        private ValidatingTextBox txtUserName;
        private PasswordTextBox txtPassword;

        public override void InitializeDefaultValues()
        {
        }
        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            var configurer = (SkytapExtensionConfigurer)extension;

            this.txtUserName.Text = configurer.UserName;
            this.txtPassword.Text = configurer.Password;
        }
        public override ExtensionConfigurerBase CreateFromForm()
        {
            return new SkytapExtensionConfigurer
            {
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtUserName = new ValidatingTextBox { Required = true };
            this.txtPassword = new PasswordTextBox { Required = true };

            this.Controls.Add(
                new SlimFormField("User name:", this.txtUserName),
                new SlimFormField("Password:", this.txtPassword)
            );
        }
    }
}
