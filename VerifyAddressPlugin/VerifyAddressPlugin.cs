using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
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

                    string serializedResult;

                    using (MemoryStream serializeMemoryStream = new MemoryStream())
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AddressValidateRequest)); 
                        //write newly created object(NewStudent) into memory stream
                        serializer.WriteObject(serializeMemoryStream, addressData);

                        serializeMemoryStream.Position = 0;

                        //use stream reader to read serialized data from memory stream
                        StreamReader sr = new StreamReader(serializeMemoryStream);

                        //get JSON data serialized in string format in string variable 
                        serializedResult = sr.ReadToEnd();

                        logWithTimestamp(tracingService, $"VerifyAddressPlugin: {serializedResult}");                        
                    }
                    Task<Stream> response = CallApi(serializedResult);
                    response.Wait();

                    StreamReader reader = new StreamReader(response.Result);
                    string json = reader.ReadToEnd();

                    AddressValidateResponse addressObject;

                    using (MemoryStream deserializeMemoryStream = new MemoryStream())
                    {                       
                        //initialize DataContractJsonSerializer object and pass Address class type to it
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AddressValidateResponse), new DataContractJsonSerializerSettings
                        {
                            UseSimpleDictionaryFormat = true
                        });

                        deserializeMemoryStream.Position = 0;

                        //user stream writer to write JSON string data to memory stream
                        StreamWriter writer = new StreamWriter(deserializeMemoryStream);
                        writer.Write(json);
                        writer.Flush();

                        deserializeMemoryStream.Position = 0;
                        //get the Deserialized data in object of type Address
                        addressObject = serializer.ReadObject(deserializeMemoryStream) as AddressValidateResponse;
                    }

                    logWithTimestamp(tracingService, $"VerifyAddressPlugin: {json} - {addressObject.Address.Address2}");

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

        public async Task<Stream> CallApi(string jsonPayload)
        {

            var client = new HttpClient();
            var requestMessage = new HttpRequestMessage();

            requestMessage.RequestUri = new Uri("https://verifyaddressapi.azurewebsites.net/VerifyAddress");
            requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            requestMessage.Method = HttpMethod.Post;

            HttpResponseMessage response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return responseStream;
        }

        private void logWithTimestamp(ITracingService tracingService, string v)
        {
            tracingService.Trace($"{DateTime.Now.ToString()} -- {v}");
        }

    }
}
