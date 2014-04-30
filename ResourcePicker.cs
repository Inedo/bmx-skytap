using System;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Inedo.Web.ClientResources;
using Inedo.Web.Controls;
using Inedo.Web.Handlers;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal sealed class ResourcePicker : HiddenField
    {
        public Func<string, object> AjaxHandler { get; set; }
        public SkytapExtensionConfigurer Configurer { get; set; }
        public string SelectedId
        {
            get
            {
                var value = this.Value;
                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                var parts = value.Split(new[] { '&' }, 2);
                return Uri.UnescapeDataString(parts[0]);
            }
            set
            {
                if (string.IsNullOrEmpty(this.Value))
                {
                    this.Value = Uri.EscapeDataString(value ?? string.Empty) + "&" + Uri.EscapeDataString(value ?? string.Empty);
                }
                else
                {
                    var parts = this.Value.Split(new[] { '&' }, 2);
                    this.Value = Uri.EscapeDataString(value ?? string.Empty) + "&" + parts[1];
                }
            }
        }
        public string SelectedName
        {
            get
            {
                var value = this.Value;
                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                var parts = value.Split(new[] { '&' }, 2);
                return Uri.UnescapeDataString(parts[1]);
            }
            set
            {
                if (string.IsNullOrEmpty(this.Value))
                {
                    this.Value = Uri.EscapeDataString(value ?? string.Empty) + "&" + Uri.EscapeDataString(value ?? string.Empty);
                }
                else
                {
                    var parts = this.Value.Split(new[] { '&' }, 2);
                    this.Value = parts[0] + "&" + Uri.EscapeDataString(value ?? string.Empty);
                }
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            this.IncludeClientResourceInPage(
                new JavascriptResource
                {
                    ResourcePath = "~/extension-resources/Skytap/ResourcePicker.js?" + typeof(ResourcePicker).Assembly.GetName().Version,
                    Dependencies = { InedoLibCR.select2.select2_js },
                    CompatibleVersions = { InedoLibCR.Versions.jq171 }
                }
            );

            base.OnPreRender(e);
        }
        protected override void Render(HtmlTextWriter writer)
        {
            base.Render(writer);

            writer.Write("<script type=\"text/javascript\">$(function(){");

            writer.Write("BmSkytapResourcePicker(");
            InedoLib.Util.JavaScript.WriteJson(
                writer,
                new
                {
                    ajaxUrl = DynamicHttpHandling.GetJavascriptDataUrl(this.AjaxHandler),
                    token = GetToken(this.Configurer),
                    selector = "#" + this.ClientID
                }
            );
            writer.Write(");");

            writer.Write("});</script>");
        }

        private static string GetToken(SkytapExtensionConfigurer configurer)
        {
            if (configurer == null || string.IsNullOrEmpty(configurer.UserName) || string.IsNullOrEmpty(configurer.Password))
                return string.Empty;

            return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N") + ":" + configurer.UserName + ":" + configurer.Password), null, DataProtectionScope.LocalMachine));
        }
    }
}
