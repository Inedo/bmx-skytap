using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Start Skytap Environment",
        "Sets a Skytap environment's runstate to start and optionally waits for the startup complete.")]
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
                    " Skytap Environment"
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
                this.LogInformation("Environment {0} is already running.", configuration.Name);
                return;
            }

            this.LogInformation("Starting environment...");
            client.SetConfigurationRunstate(configuration.Id, "running");

            if (this.WaitForStart)
            {
                this.LogInformation("Start command issued; waiting for environment to enter running state...");

                do
                {
                    this.Context.CancellationToken.WaitHandle.WaitOne(5000);
                    this.ThrowIfCanceledOrTimeoutExpired();
                    runstate = client.GetConfigurationRunstate(configuration.Id);
                }
                while (runstate == "busy");

                if (runstate == "running")
                    this.LogInformation("Environment is running.");
                else
                    this.LogError("Environment is {0}.", runstate);
            }
            else
            {
                this.LogInformation("Start command issued.");
            }
        }
    }
}
