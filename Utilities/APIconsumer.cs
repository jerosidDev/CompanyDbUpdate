using ThirdPartyDbWebAPI.ModelsView;
using CompanyDbWebAPI.ModelsDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CompanyDbUpdate.Utilities
{
    public static class APIconsumer
    {

        public static IEnumerable<T> ExtractFromAPI<T>(string API_url, string API_Controller)
        {
            IEnumerable<T> listItems;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(API_url);
                //HTTP GET
                Task<HttpResponseMessage> responseTask = client.GetAsync(API_Controller);
                responseTask.Wait();

                HttpResponseMessage result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    Task<IList<T>> readTask = result.Content.ReadAsAsync<IList<T>>();
                    readTask.Wait();
                    listItems = readTask.Result;
                }
                else //web api sent error response 
                {
                    listItems = Enumerable.Empty<T>();
                }
            }

            return listItems;
        }


        public static HttpResponseMessage Apply_Log_Update(int _BStages_Updated, int _BStages_Added, int _API_call_failure)
        {
            //  update the table "Log_Update" in the company db 
            Log_Update lu = new Log_Update()
            {
                id = DateTime.Today.Day,
                DayOfMonth = DateTime.Today.Day,
                Updated_date = DateTime.Now,
                BStages_Updated = _BStages_Updated,
                BStages_Added = _BStages_Added,
                API_call_failure = _API_call_failure
            };

            HttpResponseMessage result;
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://111.111.11.11:13/api/");
                Task<HttpResponseMessage> putTask = client.PutAsJsonAsync($"log_update/{lu.id}", lu);
                putTask.Wait();
                result = putTask.Result;
            }

            return result;
        }


    }



    public static class CompanyDBManipulation
    {
        public static Dictionary<string, List<BStage>> UpdateBStages(IEnumerable<BookingMini> listBookingMini, string APIcompanyDB, IEnumerable<BStage> listBStage)
        {
            //      the BStage in the company db have to be continous in order not to add days in between in case of a booking reverted to a previous status
            //      relationship between listBookingMini and listBStage is of One to 0-Many
            //  comparison between a BookingMini and its last(to keep the continuity) BStage
            //      2 possible cases :
            //          1) existing BStage
            //            -> update the field "ToDate" of the corresponding BStage in the company db
            //                 API PUT
            //           2) new BStage
            //           -> create the new BStage in the company db  
            //                API POST 


            // 26/04/2017: APItraceSuccess
            //      will have following information available for the homepage
            //          list of booking where the API was successful:
            //              list of BStages updated in the company db
            //              list of BStages added in the company db
            //          list of the booking where the API was unsuccessful



            //  keep a trace of the succesfull or unsuccessful POST/PUT
            Dictionary<string, List<BStage>> APItraceSuccess = new Dictionary<string, List<BStage>>();
            APItraceSuccess.Add("updatedBookingStages", new List<BStage>());
            APItraceSuccess.Add("addedBookingStages", new List<BStage>());
            APItraceSuccess.Add("unsuccessfulAPICall", new List<BStage>());


            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(APIcompanyDB);

                foreach (BookingMini bm in listBookingMini)
                {
                    bool NewBStage = false;

                    BStage bs = listBStage.Where(_bs => _bs.BHD_ID == bm.BHD_ID).OrderBy(_bs => _bs.ToDate).LastOrDefault();

                    BStage APItransferedBS = new BStage() { FullReference = bm.FullReference, Consultant = bm.Consultant, Sales_Update = bm.Sales_Update, Status = bm.Status, ToDate = DateTime.Today, BHD_ID = bm.BHD_ID };

                    if (bs == null)
                    {
                        // when the full reference appears for the first time
                        APItransferedBS.FromDate = bm.Date_Entered;
                        NewBStage = true;
                    }
                    else
                    {
                        // equality evaluation:
                        foreach (PropertyInfo p in typeof(BookingMini).GetProperties())
                        {
                            // if one value of the string properties is different NewBStage = true and break the for loop
                            if (p.PropertyType.Name == "String")
                            {
                                if (typeof(BStage).GetProperty(p.Name).GetValue(bs).ToString().Trim() != p.GetValue(bm).ToString().Trim())
                                {
                                    NewBStage = true;
                                    APItransferedBS.FromDate = DateTime.Today;
                                    break;
                                }
                            }
                        }
                    }

                    Task<HttpResponseMessage> apiTask;
                    if (NewBStage)
                    {                        // POST
                        apiTask = client.PostAsJsonAsync<BStage>("BStages", APItransferedBS);
                    }
                    else
                    {                        // PUT

                        // the only change is the last date of the booking stage
                        APItransferedBS = bs;
                        APItransferedBS.ToDate = DateTime.Today;
                        apiTask = client.PutAsJsonAsync<BStage>($"BStages/{APItransferedBS.id}", APItransferedBS);
                    }

                    try
                    {
                        apiTask.Wait();
                        HttpResponseMessage result = apiTask.Result;
                        if (result.IsSuccessStatusCode)
                        {
                            if (NewBStage)
                            {
                                APItraceSuccess["addedBookingStages"].Add(APItransferedBS);
                            }
                            else
                            {
                                APItraceSuccess["updatedBookingStages"].Add(APItransferedBS);
                            }
                        }
                        else
                        {
                            APItraceSuccess["unsuccessfulAPICall"].Add(APItransferedBS);
                        }
                    }
                    catch (System.AggregateException)
                    {

                        APItraceSuccess["unsuccessfulAPICall"].Add(APItransferedBS);
                    }

                }

            }

            return APItraceSuccess;
        }


        public static void WriteXMLtrace(IEnumerable<BookingMini> listBookingMini, IEnumerable<BStage> listBStage, Dictionary<string, List<BStage>> APItraceSuccess, string APItraceFolder)
        {




            // write the API trace log file


            string FileName = DateTime.Today.ToString("dddd") + ".xml";
            string FilePath = APItraceFolder + $"\\{FileName}";



            using (StreamWriter sw = new StreamWriter(FilePath, false))
            {
                string[] initialInfo = new string[2];
                initialInfo[0] = $"Number of Bookings extracted from Thirdparty system: {listBookingMini.Count()}";
                initialInfo[1] = $"Number of Booking Stages extracted from the company database: {listBStage.Count()}";
                XElement xeInfo1 = new XElement("From_Thirdparty", initialInfo[0]);
                XElement xeInfo2 = new XElement("From_Company_Database", initialInfo[1]);


                //XElement xeAPIresult = new XElement("API_Result", APItraceSuccess.Select(kvp => new XElement($"API_Success_{kvp.Key.ToString()}", kvp.Value.Select(o => new XElement("Booking_Stage", $"Full reference:{o.FullReference} Status:{o.Status} From date:{o.FromDate} To date:{o.ToDate} Consultant:{o.Consultant}  Sales update:{o.Sales_Update}")))));


                XElement xeAPIresult = new XElement("API_Result", APItraceSuccess.Select(kvp => new XElement(kvp.Key.ToString(), kvp.Value.Select(o => new XElement("Booking_Stage", $"Full reference:{o.FullReference} Status:{o.Status} From date:{o.FromDate} To date:{o.ToDate} Consultant:{o.Consultant}  Sales update:{o.Sales_Update}")))));





                XElement xeSaved = new XElement("root", xeInfo1, xeInfo2, xeAPIresult);
                xeSaved.Save(sw);
            }
        }


        public static void FullProcessUpdateBStages(out IEnumerable<BookingMini> listBookingMini, out IEnumerable<BStage> listBStage, out Dictionary<string, List<BStage>> APItraceSuccess, string APItraceFolder)
        {


            // compare data extracted from Thirdparty booking system and data extracted from the company db 
            //      apply the update in the company db 

            string API_url = "http://111.111.11.11:14/API/";
            string API_Controller = "bookingsinfo";
            listBookingMini = APIconsumer.ExtractFromAPI<BookingMini>(API_url, API_Controller);
            string APIcompanyDB = "http://111.111.11.11:13/API/";
            API_Controller = "bstages";
            listBStage = APIconsumer.ExtractFromAPI<BStage>(APIcompanyDB, API_Controller);
            APItraceSuccess = CompanyDBManipulation.UpdateBStages(listBookingMini, APIcompanyDB, listBStage);


            // write the API trace log file
    
            CompanyDBManipulation.WriteXMLtrace(listBookingMini, listBStage, APItraceSuccess, APItraceFolder);

            // update in the table "Log_Update" in the company db 
            //APIconsumer.Apply_Log_Update(_BStages_Updated, _BStages_Added, _API_call_failure);
            HttpResponseMessage response = APIconsumer.Apply_Log_Update(APItraceSuccess["updatedBookingStages"].Count, APItraceSuccess["addedBookingStages"].Count, APItraceSuccess["unsuccessfulAPICall"].Count);


        }

    }

}
