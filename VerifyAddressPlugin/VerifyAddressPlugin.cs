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
using Microsoft.Xrm.Sdk.Query;

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
                    var mySerializer = new XmlSerializer(typeof(AddressValidateRequest), new XmlRootAttribute("AddressValidateRequest"));
                    var myDeserializer = new XmlSerializer(typeof(AddressValidateResponse), new XmlRootAttribute("AddressValidateResponse"));
                    
                    AddressValidateRequest addressData = new AddressValidateRequest();

                    logWithTimestamp(tracingService, "VerifyAddressPlugin: Business Logic start");

                    addressData.Address = new Address();

                    if (context.MessageName == "Update")
                    {
                        logWithTimestamp(tracingService, $"VerifyAddressPlugin: {entity.Id}");

                        var result = service.Retrieve("account", entity.Id,
                            new ColumnSet("address1_line1", "address1_line2", "address1_city", "address1_stateorprovince", "address1_postalcode"));
                        
                        if (entity.Contains("address1_line2") == false)
                        {
                            entity.Attributes.Add("address1_line2", result.Contains("address1_line2") ? result["address1_line2"] : "");                            
                        }
                        if (entity.Contains("address1_line1") == false)
                        {
                            entity.Attributes.Add("address1_line1", result["address1_line1"]);
                        }
                        if (entity.Contains("address1_city") == false)
                        {
                            entity.Attributes.Add("address1_city", result["address1_city"]);
                        }
                        if (entity.Contains("address1_stateorprovince") == false)
                        {
                            entity.Attributes.Add("address1_stateorprovince", result["address1_stateorprovince"]);
                        }
                        if (entity.Contains("address1_postalcode") == false)
                        {
                            entity.Attributes.Add("address1_postalcode", result["address1_postalcode"]);
                        }
                    }

                    addressData.Address.Address1 = entity.Contains("address1_line2") ? entity["address1_line2"] as string : "";
                    addressData.Address.Address2 = entity["address1_line1"] as string;
                    addressData.Address.City = entity["address1_city"] as string;
                    addressData.Address.State = entity["address1_stateorprovince"] as string;
                    addressData.Address.Zip5 = entity["address1_postalcode"] as string;
                    addressData.Address.Zip4 = "";
                    
                    var xml = "";

                    using (var sww = new StringWriter())
                    {
                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.OmitXmlDeclaration = true;

                        using (XmlWriter writer = XmlWriter.Create(sww, settings))
                        {
                            mySerializer.Serialize(writer, addressData);
                            
                            xml = sww.ToString(); // Your XML
                        }
                    }

                    logWithTimestamp(tracingService, $"VerifyAddressPlugin: {xml}");

                    Task<Stream> response = CallApi(xml);
                    response.Wait();

                    // Call the Deserialize method and cast to the object type.
                    var addressObject = myDeserializer.Deserialize(response.Result) as AddressValidateResponse;

                    if (addressObject.Address?.Error?.Description != null)
                    {
                        throw new InvalidPluginExecutionException($"Invalid Address. Please fix any errors and then try to save again.");
                    }

                    if (addressObject.Address?.ReturnText != null)
                    {
                        throw new InvalidPluginExecutionException($"{addressObject.Address.ReturnText}");
                    }

                    entity["address1_line1"] = addressObject.Address.Address2;
                    entity["address1_line2"] = addressObject.Address?.Address1;
                    entity["address1_city"] = addressObject.Address.City;
                    entity["address1_stateorprovince"] = addressObject.Address.State;
                    entity["address1_postalcode"] = $"{addressObject.Address.Zip5}-{addressObject.Address.Zip4}";

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
}
