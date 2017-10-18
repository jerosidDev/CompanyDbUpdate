using System;

namespace ThirdPartyDbWebAPI.ModelsView
{

    public class BookingMini
    {

        // do not add methods or constructors because it will corrupt the API

        public string FullReference { get; set; }

        public string Status { get; set; }

        public string Consultant { get; set; }

        public DateTime Date_Entered { get; set; }

        public string Sales_Update { get; set; }
        public int? BHD_ID { get; set; }

    }
}
