using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Inedo.BuildMasterExtensions.Skytap.SkytapApi
{
    internal sealed class SkytapClient
    {
        private const string BaseUrl = "https://cloud.skytap.com";
        private string basicAuthHeader;
        private UTF8Encoding utf8 = new UTF8Encoding(false);

        public SkytapClient(string userName, string password)
        {
            this.basicAuthHeader = "Basic " + Convert.ToBase64String(this.utf8.GetBytes(userName + ":" + password));
        }

        public IEnumerable<SkytapResource> ListVms(string configurationId)
        {
            var xdoc = this.Get("/configurations/" + Uri.EscapeUriString(configurationId) + "/vms");
            return xdoc
                .Descendants("vm")
                .Select(v => new SkytapResource((string)v.Element("id"), (string)v.Element("name")));
        }
        public SkytapPublishedSet CreatePublishSet(string configurationId, string name, bool multiUrl, TimeSpan? startTime, TimeSpan? endTime, string password, IEnumerable<SkytapPublishedVmRef> vms, TimeSpan? runtimeLimit, DateTime? expirationDate)
        {
            var element = new XElement("publish_set");
            
            if (!string.IsNullOrEmpty(name))
                element.Add(new XElement("name", name));

            element.Add(new XElement("publish_set_type", multiUrl ? "multiple_url" : "single_url"));

            if (startTime != null && endTime != null)
            {
                element.Add(
                    new XElement("start_time", ((TimeSpan)startTime).ToString("hh':'mm")),
                    new XElement("end_time", ((TimeSpan)endTime).ToString("hh':'mm")),
                    new XElement("time_zone", "UTC")
                );
            }

            if (!string.IsNullOrEmpty(password))
                element.Add(new XElement("password", password));

            element.Add(
                new XElement("vms",
                    vms.Select(v => new XElement("vm",
                        new XElement("vm_ref", BaseUrl + "/vms/" + Uri.EscapeUriString(v.Id)),
                        new XElement("access", v.Access)
                    ))
                )
            );

            if (runtimeLimit != null)
                element.Add(new XElement("runtime_limit", (int)((TimeSpan)runtimeLimit).TotalMinutes));

            if (expirationDate != null)
            {
                element.Add(new XElement("expiration_date", ((DateTime)expirationDate).ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss")));
                element.Add(new XElement("expiration_date_tz", "UTC"));
            }

            var xdoc = this.Post(
                "/configurations/" + Uri.EscapeUriString(configurationId) + "/publish_sets",
                new XDocument(element)
            );

            var publishedSetElement = xdoc.Root;

            var vmIdRegex = new Regex("/(?<1>[^/]+)$", RegexOptions.ExplicitCapture);

            return new SkytapPublishedSet(
                (string)publishedSetElement.Element("id"),
                (string)publishedSetElement.Element("name"),
                (string)publishedSetElement.Element("desktops_url"),
                vms.Join(
                    publishedSetElement.Element("vms").Elements(),
                    v => v.Id,
                    e => vmIdRegex.Match((string)e.Element("vm_ref")).Groups[1].Value,
                    (v, e) => new SkytapPublishedVmRef(v.Id, v.Access, (string)e.Element("desktop_url"))
                )
            );
        }

        public SkytapConfiguration GetConfiguration(string configurationId)
        {
            try
            {
                var xdoc = this.Get("/configurations/" + Uri.EscapeUriString(configurationId));
                var configurationElement = xdoc.Descendants("configuration").FirstOrDefault();
                if (configurationElement == null)
                    return null;

                return new SkytapConfiguration(configurationElement);
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                    return null;
                else
                    throw;
            }
        }
        public SkytapConfiguration GetConfigurationFromName(string configurationName)
        {
            var xdoc = this.Get("/configurations?name=" + Uri.EscapeDataString(configurationName));
            var configurationElement = xdoc.Descendants("configuration").FirstOrDefault();
            if (configurationElement == null)
                return null;

            return this.GetConfiguration((string)configurationElement.Element("id"));
        }
        public IEnumerable<SkytapResource> ListConfigurations()
        {
            var xdoc = this.Get("/configurations");
            return xdoc
                .Descendants("configuration")
                .Select(c => new SkytapResource((string)c.Element("id"), (string)c.Element("name")));
        }
        public string CreateConfiguration(string templateId)
        {
            var xdoc = this.Post(
                "/configurations",
                new XDocument(
                    new XElement("template_id", templateId)
                )
            );

            return (string)xdoc.Descendants("id").FirstOrDefault();
        }
        public string CopyConfiguration(string configurationId)
        {
            var xdoc = this.Post(
                "/configurations",
                new XDocument(
                    new XElement("configuration_id", configurationId)
                )
            );

            return (string)xdoc.Descendants("id").FirstOrDefault();
        }
        public void RenameConfiguration(string configurationId, string name)
        {
            this.Put(
                "/configurations/" + Uri.EscapeUriString(configurationId),
                new XDocument(
                    new XElement("configuration",
                        new XElement("name", name)
                    )
                )
            );
        }
        public void DeleteConfiguration(string configurationId)
        {
            this.Delete("/configurations/" + Uri.EscapeUriString(configurationId));
        }

        public IEnumerable<SkytapResource> ListTemplates()
        {
            var xdoc = this.Get("/templates");
            return xdoc
                .Descendants("template")
                .Select(t => new SkytapResource((string)t.Element("id"), (string)t.Element("name")));
        }
        public SkytapResource GetTemplate(string templateId)
        {
            try
            {
                var xdoc = this.Get("/templates/" + Uri.EscapeUriString(templateId));
                var templateElement = xdoc.Descendants("template").FirstOrDefault();
                if (templateElement == null)
                    return null;

                return new SkytapResource((string)templateElement.Element("id"), (string)templateElement.Element("name"));
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                    return null;
                else
                    throw;
            }
        }
        public SkytapResource GetTemplateFromName(string templateName)
        {
            var xdoc = this.Get("/templates?name=" + Uri.EscapeDataString(templateName));
            var templateElement = xdoc.Descendants("template").FirstOrDefault();
            if (templateElement == null)
                return null;

            return new SkytapResource((string)templateElement.Element("id"), (string)templateElement.Element("name"));
        }
        public void SetConfigurationRunstate(string configurationId, string runstate)
        {
            this.Put(
                "/configurations/" + Uri.EscapeUriString(configurationId),
                new XDocument(
                    new XElement("configuration",
                        new XElement("runstate", runstate)
                    )
                )
            );
        }
        public string GetConfigurationRunstate(string configurationId)
        {
            var xdoc = this.Get("/configurations/" + configurationId);
            var runstateElement = xdoc.Descendants("runstate").FirstOrDefault();
            if(runstateElement == null)
                return null;

            return (string)runstateElement;
        }

        private XDocument Post(string resourceUrl, XDocument data)
        {
            return this.PerformRequest(resourceUrl, WebRequestMethods.Http.Post, data);
        }
        private void Put(string resourceUrl, XDocument data)
        {
            this.PerformRequest(resourceUrl, WebRequestMethods.Http.Put, data);
        }
        private void Delete(string resourceUrl)
        {
            this.PerformRequest(resourceUrl, "DELETE", null);
        }
        private XDocument Get(string resourceUrl)
        {
            return this.PerformRequest(resourceUrl, WebRequestMethods.Http.Get, null);
        }

        private XDocument PerformRequest(string resourceUrl, string method, XDocument data)
        {
            var request = (HttpWebRequest)WebRequest.Create(BaseUrl + resourceUrl);
            request.Method = method;
            request.Accept = "application/xml";
            if (data != null)
                request.ContentType = "application/xml";

            request.Headers[HttpRequestHeader.Authorization] = this.basicAuthHeader;

            if (data != null)
            {
                using (var requestStream = request.GetRequestStream())
                using (var writer = XmlWriter.Create(requestStream, new XmlWriterSettings { Indent = false, Encoding = this.utf8 }))
                {
                    data.Save(writer);
                }
            }

            using (var response = request.GetResponse())
            {
                if ((response.ContentType ?? string.Empty).IndexOf("application/xml", StringComparison.OrdinalIgnoreCase) >= 0 && response.ContentLength > 5)
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        return XDocument.Load(responseStream);
                    }
                }
            }

            return null;
        }
    }
}
