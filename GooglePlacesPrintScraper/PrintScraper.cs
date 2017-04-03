using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace GooglePlacesPrintScraper
{
    public class PrintScraper
    {
        static void Main(string[] args)
        {
            var keywords = ConfigurationManager.AppSettings.Get("keywords").Split(new[] {','});

            var locationsToBeSearched = File.ReadAllLines("../../../data/us_postal_codes.csv");

            foreach (var locationTobeSearched in locationsToBeSearched)
            {
                try
                {
                    CallGooglePlacesAPIAndSetCallback(locationTobeSearched, keywords);
                }
                catch (Exception e)
                {
                    File.AppendAllLines("log.txt", new[] { "\n{DateTime.Now}\n{e.Message},\n{e.StackTrace}\n" });
                }
            }

        }

        public static void CallGooglePlacesAPIAndSetCallback(string location, string[] keywords)
        {
        }
    }

   
}
