using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder;

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
            var radius = ConfigurationManager.AppSettings.Get("radius");
            string filename = ConfigurationManager.AppSettings.Get("coordinateSource");
            var locationsToBeSearched = File.ReadAllLines(filename).Where(o => !o.Contains("Processed")).Select(o =>
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
                    dynamic res = null;
                    using (var client = new HttpClient())
                    {
                        while (res == null || HasProperty(res, "next_page_token"))
                        {
                            var url =  string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={0},{1}&radius={4}&keyword={2}&key={3}", locationTobeSearched.latitude, locationTobeSearched.longitude, keywords, googlePlacesApiKey, radius);
                            if (res != null && HasProperty(res, "next_page_token"))
                             url += "&pagetoken=" + res["next_page_token"];
                            var response = client.GetStringAsync(url).Result;
                            JavaScriptSerializer json = new JavaScriptSerializer();
                            res = json.Deserialize<dynamic>(response);


                            if (res["status"] == "OK")
                            {
                                foreach (var match in res["results"])
                                {
                                    if (!File.ReadAllText("results.csv").Contains(match["place_id"]))
                                    {
                                        var placeResponse = client.GetStringAsync(string.Format("https://maps.googleapis.com/maps/api/place/details/json?placeid={0}&key={1}", match["place_id"], googlePlacesApiKey)).Result;
                                        WriteResponse(placeResponse);

                                    }
                                }
                            }
                            else if (res["status"] == "OVER_QUERY_LIMIT")
                            {
                                return;
                            }

                           
                        }

                        lineChanger($"{locationTobeSearched.latitude},{locationTobeSearched.longitude}", filename);
                    }
                }
                catch (Exception e)
                {
                    File.AppendAllLines("log.txt", new[] { "\n{DateTime.Now}\n{e.Message},\n{e.StackTrace}\n" });
                }
            }
        }

        static void lineChanger(string coordinates, string filename)
        {

            string[] arrLine = File.ReadAllLines(filename);
            for (int i = 0; i < arrLine.Length; i++)
            {
                if (arrLine[i].Contains(coordinates))
                {
                    arrLine[i] = arrLine[i].Insert(arrLine[i].Length - 1, @",""Processed""");
                }
            }

            File.WriteAllLines(filename, arrLine);
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

                var address = HasProperty(res["result"], "vicinity") ? res["result"]["vicinity"] : string.Empty;
                var phone = HasProperty(res["result"], "international_phone_number") ? res["result"]["international_phone_number"] : string.Empty;
                var website = HasProperty(res["result"], "website") ? res["result"]["website"] : string.Empty;
                var placeid = res["result"]["place_id"];

                File.AppendAllLines("results.csv", new[] { $@"""{name}"", ""{types}"",""{city}"",""{state}"",""{address}"",""{phone}"",""{website}"",""{placeid}""" });

            }
        }

        public static bool HasProperty(dynamic obj, string name)
        {
            try
            {
                var value = obj[name];
                return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }
    }
}
