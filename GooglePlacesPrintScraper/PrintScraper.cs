using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Web.Script.Serialization;

namespace GooglePlacesPrintScraper
{
    public class PrintScraper
    {
        static void Main(string[] args)
        {
            CallGooglePlacesAPIAndSetCallback();

        }

        public static void CallGooglePlacesAPIAndSetCallback()
        {
            if (!File.Exists("results.csv")) { File.CreateText("results.csv"); }
            var keywords = "(" + string.Join(") OR (", ConfigurationManager.AppSettings.Get("keywords").Split(new[] { ',' })) + ")";
            var googlePlacesApiKey = ConfigurationManager.AppSettings.Get("googlePlacesApiKey");
            var locationsToBeSearched = File.ReadAllLines("../../../data/us_postal_codes.csv").Select(o =>
            {
                var latitudeAndLongitude = o.Split(new[] { ',' }).Skip(5).ToList();
                decimal latitude = 0;
                decimal longitude = 0;
                return decimal.TryParse(latitudeAndLongitude[0], out latitude) && decimal.TryParse(latitudeAndLongitude[1], out longitude) ?
                     new { latitude, longitude } : null;
            }).Where(o => o != null).Distinct();

            foreach (var locationTobeSearched in locationsToBeSearched)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var response = client.GetStringAsync(string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={0},{1}&radius=5000&keyword={2}&key={3}", locationTobeSearched.latitude, locationTobeSearched.longitude, keywords, googlePlacesApiKey)).Result;
                        JavaScriptSerializer json = new JavaScriptSerializer();
                        var res = json.Deserialize<dynamic>(response);
                        if (res["status"] == "OK")
                            foreach (var match in res["results"])
                            {
                                if (!File.ReadAllText("results.csv").Contains(match["place_id"]))
                                {
                                    var placeResponse = client.GetStringAsync(string.Format("https://maps.googleapis.com/maps/api/place/details/json?placeid={0}&key={1}", match["place_id"], googlePlacesApiKey)).Result;
                                    WriteResponse(placeResponse);
                                }
                            }
                    }
                }
                catch (Exception e)
                {
                    File.AppendAllLines("log.txt", new[] { "\n{DateTime.Now}\n{e.Message},\n{e.StackTrace}\n" });
                }
            }
        }

        public static void WriteResponse(string response)
        {
            JavaScriptSerializer json = new JavaScriptSerializer();
            var res = json.Deserialize<dynamic>(response);
            if (res["status"] == "OK")
            {
                var name = res["result"]["name"];
                var types = string.Join(",", res["result"]["types"]);
                string city = string.Empty;
                string state = string.Empty;
                foreach (var addressComponent in res["result"]["address_components"])
                {
                    foreach (var t in addressComponent["types"])
                    {
                        if (t == "locality")
                        {
                            city = addressComponent["long_name"];
                        }
                        if (t == "administrative_area_level_1")
                        {
                            state = addressComponent["long_name"];
                        }
                    }
                }

                var address = res["result"]["vicinity"];
                var phone = res["result"]["international_phone_number"];
                var website = res["result"]["website"];
                var placeid = res["result"]["place_id"];

                File.AppendAllLines("results.csv", new[] { $@"""{name}"", ""{types}"",""{city}"",""{state}"",""{address}"",""{phone}"",""{website}"",""{placeid}""" });

            }
        }
    }
}
