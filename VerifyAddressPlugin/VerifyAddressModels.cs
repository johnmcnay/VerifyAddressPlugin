using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VerifyAddressPlugin
{

    [DataContract]
    public class AddressValidateRequest
    {
        [DataMember(Name = "USERID")]
        [XmlAttribute]
        public string USERID = "479IMPRO8097";

        [DataMember(Name = "address")]
        public Address Address { get; set; }
    }

    [DataContract]
    public class AddressValidateResponse
    {
        [DataMember(Name = "address")]
        public Address Address { get; set; }
    }

    [DataContract]
    public class Address
    {
        [DataMember(Name = "address1")]
        public string Address1 { get; set; }
        [DataMember(Name = "address2")]
        public string Address2 { get; set; }
        [DataMember(Name = "city")]
        public string City { get; set; }
        [DataMember(Name = "state")]
        public string State { get; set; }
        [DataMember(Name = "zip5")]
        public string Zip5 { get; set; }
        [DataMember(Name = "zip4")]
        public string Zip4 { get; set; }
        [DataMember(Name = "returnText")]
        public string ReturnText { get; set; }
        [DataMember(Name = "error")]
        public Error Error { get; set; }
    }

    [DataContract]
    public class Error
    {
        [DataMember(Name = "number")]
        public string Number { get; set; }
        [DataMember(Name = "source")]
        public string Source { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "helpFile")]
        public string HelpFile { get; set; }
        [DataMember(Name = "helpContext")]
        public string HelpContext { get; set; }
    }
}
