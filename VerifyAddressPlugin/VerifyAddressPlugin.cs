using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;

namespace VerifyAddressPlugin
{
    public class VerifyAddressPlugin : IPlugin
    {

        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity entity = (Entity)context.InputParameters["Target"];

                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    var mySerializer = new XmlSerializer(typeof(AddressValidateResponse), new XmlRootAttribute("AddressValidateResponse"));
                    AddressValidateResponse addressData = new AddressValidateResponse();

                    logWithTimestamp(tracingService, "VerifyAddressPlugin: Business Logic start");

                    addressData.Address = new Address();
                    addressData.Address.Error = new Error();

                    logWithTimestamp(tracingService, "VerifyAddressPlugin: 1");
                    addressData.Address.Address1 = entity["address1_line2"] as string;
                    logWithTimestamp(tracingService, "VerifyAddressPlugin: 2");
                    addressData.Address.Address2 = entity["address1_line1"] as string;
                    logWithTimestamp(tracingService, "VerifyAddressPlugin: 3");
                    addressData.Address.City = entity["address1_city"] as string;
                    logWithTimestamp(tracingService, "VerifyAddressPlugin: 4");
                    addressData.Address.State = entity["address1_stateorprovince"] as string;
                    logWithTimestamp(tracingService, "VerifyAddressPlugin: 5");
                    addressData.Address.Zip5 = entity["address1_postalcode"] as string;

                    var xml = "";

                    using (var sww = new StringWriter())
                    {
                        using (XmlWriter writer = XmlWriter.Create(sww))
                        {
                            mySerializer.Serialize(writer, addressData);
                            xml = sww.ToString(); // Your XML
                        }
                    }

                    logWithTimestamp(tracingService, $"VerifyAddressPlugin: {xml}");

                    Task<Stream> response = CallApi(xml);
                    response.Wait();

                    // Call the Deserialize method and cast to the object type.
                    var addressObject = mySerializer.Deserialize(response.Result) as AddressValidateResponse;

                    //if (addressObject.Address?.Error?.Description != null)
                    //{
                    //    throw new InvalidPluginExecutionException($"Invalid Address. Please fix any errors and then try to save again.");
                    //}

                    if (addressObject.Address?.ReturnText != null)
                    {
                        throw new InvalidPluginExecutionException($"{addressObject.Address.ReturnText}");
                    }

                    //if (entity.Contains("address1_stateorprovince"))
                    //{
                    //    string stateInput = entity["address1_stateorprovince"].ToString().ToLower();

                    //    if (stateInput == "tx" || stateInput == "texas")
                    //    {

                    //        logWithTimestamp(tracingService, "Account created with TX");
                    //    }
                    //    else
                    //    {
                    //        Entity culprit = service.Retrieve("systemuser", context.InitiatingUserId, new ColumnSet("fullname"));

                    //        logWithTimestamp(tracingService, $"{culprit["fullname"]} attempted account creation with state other than TX");
                    //        throw new InvalidPluginExecutionException("You can only create accounts with the state of Texas (TX)");
                    //    }
                    //}
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in VerifyAddressPlugin.", ex);
                }
                catch (Exception ex)
                {
                    tracingService.Trace($"VerifyAddressPlugin: {ex.ToString()}");
                    throw;
                }

            }
        }

        public async Task<Stream> CallApi(string xml)
        {

            var client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync("https://secure.shippingapis.com/ShippingAPI.dll?API=Verify&XML=" + $"{xml}");
            response.EnsureSuccessStatusCode();
            Stream responseStream = await response.Content.ReadAsStreamAsync();

            return responseStream;
        }

        private void logWithTimestamp(ITracingService tracingService, string v)
        {
            tracingService.Trace($"{DateTime.Now.ToString()} -- {v}");
        }

    }

    [XmlRoot("AddressValidateResponse")]
    public class AddressValidateResponse
    {
        [XmlElement("Address")]
        public Address Address { get; set; }
    }

    public class Address
    {
        
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip5 { get; set; }
        public string Zip4 { get; set; }
        public string ReturnText { get; set; }
        
        [XmlElement("Error")]
        public Error Error { get; set; }

    }

    public class Error
    {
        public string Number { get; set; }
        public string Source { get; set; }
        public string Description { get; set; }
        public string HelpFile { get; set; }
        public string HelpContext { get; set; }
    }
}
