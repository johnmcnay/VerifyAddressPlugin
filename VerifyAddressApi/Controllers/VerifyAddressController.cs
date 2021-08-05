using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VerifyAddressPlugin;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Net.Mime;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace VerifyAddressApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class VerifyAddressController : ControllerBase
    {

        private readonly ILogger<VerifyAddressController> _logger;

        public VerifyAddressController(ILogger<VerifyAddressController> logger)
        {
            _logger = logger;
        }

        [Route("test")]
        public void test()
        {
            AddressValidateRequest request = new AddressValidateRequest();
            request.Address = new Address();
            request.Address.Address2 = "5445 LEGACY DR";
            request.Address.City = "PLANO";
            request.Address.State = "TX";
            request.Address.Zip5 = "75024";

            return;
        }

        [HttpGet]
        public JsonResult Get(AddressValidateRequest request)
        {
            var mySerializer = new XmlSerializer(typeof(AddressValidateRequest), new XmlRootAttribute("AddressValidateRequest"));
            var myDeserializer = new XmlSerializer(typeof(AddressValidateResponse), new XmlRootAttribute("AddressValidateResponse"));

            var xml = "";

            if (request.Address.Address1 == null)
            {
                request.Address.Address1 = "";
            }
            if (request.Address.Zip4 == null)
            {
                request.Address.Zip4 = "";
            }

            using (var sww = new StringWriter())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true;

                using (XmlWriter writer = XmlWriter.Create(sww, settings))
                {
                    mySerializer.Serialize(writer, request);

                    xml = sww.ToString(); // Your XML
                }
            }

            Task<Stream> result = CallApi(xml);
            result.Wait();

            var addressObject = myDeserializer.Deserialize(result.Result) as AddressValidateResponse;
          
            return new JsonResult(addressObject);
        }

        public async Task<Stream> CallApi(string xmlPayload)
        {
            var client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync("https://secure.shippingapis.com/ShippingAPI.dll?API=Verify&XML=" + $"{xmlPayload}");
            response.EnsureSuccessStatusCode();
            var responseStream = await response.Content.ReadAsStreamAsync();

            return responseStream;
        }
    }
}
