using System;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Delete Skytap Configuration",
        "Deletes a Skytap configuration.")]
    [Tag("skytap")]
    [CustomEditor(typeof(DeleteConfigurationActionEditor))]
    public sealed class DeleteConfigurationAction : SkytapConfigurationActionBase
    {
        public DeleteConfigurationAction()
            : base(true)
        {
        }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    "Delete ",
                    new Hilite(this.ConfigurationName),
                    " Skytap Configuration"
                )
            );
        }

        internal override void Execute(SkytapClient client, SkytapConfiguration configuration)
        {
            if (configuration == null)
            {
                this.LogWarning("Configuration {0} not found.", this.ConfigurationName);
                return;
            }

            this.LogInformation("Deleting {0} configuration...", configuration.Name);
            try
            {
                client.DeleteConfiguration(configuration.Id);
                this.LogInformation("Configuration deleted.");
            }
            catch(Exception ex)
            {
                this.LogError("The configuration could not be deleted: " + ex.Message);
            }
        }
    }
}
