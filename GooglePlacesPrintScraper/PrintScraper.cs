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
        static  void Main(string[] args)
        {
            CallGooglePlacesAPIAndSetCallback();

        }



        public static async void CallGooglePlacesAPIAndSetCallback()
        {
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
                        var response = await client.GetStringAsync(string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={0},{1}&radius=500&keyword={2}&key={3}", locationTobeSearched.latitude, locationTobeSearched.longitude, keywords, googlePlacesApiKey));
        dynamic result = response;
    }
}
                catch (Exception e)
                {
                    File.AppendAllLines("log.txt", new[] { "\n{DateTime.Now}\n{e.Message},\n{e.StackTrace}\n" });
                }
            }
        }
    }

   
}
