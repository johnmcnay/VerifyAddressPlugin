using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace VerifyAddressApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VerifyAddressController : ControllerBase
    {

        private readonly ILogger<VerifyAddressController> _logger;

        public VerifyAddressController(ILogger<VerifyAddressController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public JsonResult Get()
        {

            return new JsonResult("");
        }
    }
}
