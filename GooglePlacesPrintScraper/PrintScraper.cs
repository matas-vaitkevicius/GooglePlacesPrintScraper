using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Net.Http;

namespace GooglePlacesPrintScraper
{
    public class PrintScraper
    {
        static void Main(string[] args)
        {
            var keywords ="(" + string.Join(") OR (", ConfigurationManager.AppSettings.Get("keywords").Split(new[] {','}))+")";
            var googlePlacesApiKey = ConfigurationManager.AppSettings.Get("googlePlacesApiKey");
            var locationsToBeSearched = File.ReadAllLines("../../../data/us_postal_codes.csv");

            foreach (var locationTobeSearched in locationsToBeSearched)
            {
                try
                {
                    CallGooglePlacesAPIAndSetCallback(locationTobeSearched, keywords, googlePlacesApiKey);
                }
                catch (Exception e)
                {
                    File.AppendAllLines("log.txt", new[] { "\n{DateTime.Now}\n{e.Message},\n{e.StackTrace}\n" });
                }
            }

        }

        public static async void CallGooglePlacesAPIAndSetCallback(string location, string keywords, string googlePlacesApiKey)
        {
            var latitudeAndLongitude = location.Split(new[] { ',' }).Skip(5).ToList();

            decimal latitude = 0;
            decimal longitude = 0;

            if (decimal.TryParse(latitudeAndLongitude[0], out latitude) && decimal.TryParse(latitudeAndLongitude[1], out longitude))
            {
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync(string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={0},{1}&radius=500&keyword={2}&key={3}", latitude, longitude,keywords, googlePlacesApiKey));
                    dynamic result = response;
                }
            }
        }
    }

   
}
