using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VerifyAddressPlugin
{

    [XmlRoot("AddressValidateRequest")]
    public class AddressValidateRequest
    {
        [XmlAttribute]
        public string USERID = "479IMPRO8097";

        public Address Address { get; set; }
    }

    [XmlRoot("AddressValidateResponse")]
    public class AddressValidateResponse
    {
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
