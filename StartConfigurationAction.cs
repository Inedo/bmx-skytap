using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Start Skytap Configuration",
        "Sets a Skytap configuration's runstate to start and optionally waits for the startup complete.")]
    [Tag("skytap")]
    [CustomEditor(typeof(StartConfigurationActionEditor))]
    public sealed class StartConfigurationAction : SkytapConfigurationActionBase
    {
        [Persistent]
        public bool WaitForStart { get; set; }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    "Start ",
                    new Hilite(this.ConfigurationName),
                    " Skytap Configuration"
                ),
                new LongActionDescription(
                    this.WaitForStart ? "and wait for it to enter running state" : string.Empty
                )
            );
        }

        internal override void Execute(SkytapClient client, SkytapConfiguration configuration)
        {
            var runstate = client.GetConfigurationRunstate(configuration.Id);
            if (runstate == "running")
            {
                this.LogInformation("Configuration {0} is already running.", configuration.Name);
                return;
            }

            this.LogInformation("Starting configuration...");
            client.SetConfigurationRunstate(configuration.Id, "running");

            if (this.WaitForStart)
            {
                this.LogInformation("Start command issued; waiting for configuration to enter running state...");

                do
                {
                    this.Context.CancellationToken.WaitHandle.WaitOne(5000);
                    this.ThrowIfCanceledOrTimeoutExpired();
                    runstate = client.GetConfigurationRunstate(configuration.Id);
                }
                while (runstate == "busy");

                if (runstate == "running")
                    this.LogInformation("Configuration is running.");
                else
                    this.LogError("Configuration is {0}.", runstate);
            }
            else
            {
                this.LogInformation("Start command issued.");
            }
        }
    }
}
