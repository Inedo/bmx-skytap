using System;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Delete Skytap Environment",
        "Deletes a Skytap environment.")]
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
                    " Skytap Environment"
                )
            );
        }

        internal override void Execute(SkytapClient client, SkytapConfiguration configuration)
        {
            if (configuration == null)
            {
                this.LogWarning("Environment {0} not found.", this.ConfigurationName);
                return;
            }

            this.LogInformation("Deleting {0} environment...", configuration.Name);
            try
            {
                client.DeleteConfiguration(configuration.Id);
                this.LogInformation("Environment deleted.");
            }
            catch(Exception ex)
            {
                this.LogError("The environment could not be deleted: " + ex.Message);
            }
        }
    }
}
