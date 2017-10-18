namespace CompanyDbWebAPI.ModelsDB
{
    using System;


    // the data base is created by code first and updated through successive migrations
    public class BStage
    {
        public int id { get; set; }

        public string FullReference { get; set; }

        public string Status { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string Consultant { get; set; }

        public string Sales_Update { get; set; }
        public int? BHD_ID { get; set; }

    }

    public class Log_Update
    {
        public int id { get; set; }
        public int? DayOfMonth { get; set; }
        public DateTime? Updated_date { get; set; }
        public int? BStages_Updated { get; set; }
        public int? BStages_Added { get; set; }
        public int? API_call_failure { get; set; }
    }



}
