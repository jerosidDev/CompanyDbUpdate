using CompanyDbUpdate.Utilities;
using ThirdPartyDbWebAPI.ModelsView;
using CompanyDbWebAPI.ModelsDB;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace CompanyDbUpdate.Controllers
{
    public class UpdateFromTPController : Controller
    {
        // GET: show bookings from Thirdparty booking system using the API
        public ActionResult Index()
        {

            string API_url = "http://111.111.11.11:14/API/";
            string API_Controller = "bookingsinfo";
            IEnumerable<BookingMini> listBookingMini = APIconsumer.ExtractFromAPI<BookingMini>(API_url, API_Controller);

            return View(listBookingMini);
        }


        // show the company db elements
        public ActionResult ShowDbElements()
        {

            string API_url = "http://111.111.11.11:13/API/";
            string API_Controller = "bstages";
            IEnumerable<BStage> listBStage = APIconsumer.ExtractFromAPI<BStage>(API_url, API_Controller);


            // test
            var t = listBStage.Where(bs => bs.id == 5011).ToList();
            return View(t);
        }


        public ActionResult Updater()
        {


            IEnumerable<BookingMini> listBookingMini;
            IEnumerable<BStage> listBStage;
            Dictionary<string, List<BStage>> APItraceSuccess;
            string APItraceFolder = Server.MapPath("~/Content/APItrace2");  // here is the difference with the unit testing
            CompanyDBManipulation.FullProcessUpdateBStages(out listBookingMini, out listBStage, out APItraceSuccess, APItraceFolder);

            return View(APItraceSuccess["updatedBookingStages"].Concat(APItraceSuccess["addedBookingStages"]));
        }


    }
}
