using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic_Mirror
{
    // Geolocation Class
    public class Geolocation
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
    }

    // LandSlides Class
    public class LandSlides
    {
        public string adminname1 { get; set; }
        public string adminname2 { get; set; }
        public string cat_id { get; set; }
        public string cat_src { get; set; }
        public string changeset_id { get; set; }
        public string continentcode { get; set; }
        public string countrycode { get; set; }
        public string countryname { get; set; }
        public string date { get; set; }
        public string distance { get; set; }
        public string fatalities { get; set; }
        public Geolocation geolocation { get; set; }
        public string hazard_type { get; set; }
        public string id { get; set; }
        public string injuries { get; set; }
        public string key { get; set; }
        public string landslide_size { get; set; }
        public string landslide_type { get; set; }
        public string latitude { get; set; }
        public string location_accuracy { get; set; }
        public string location_description { get; set; }
        public string longitude { get; set; }
        public string near { get; set; }
        public string nearest_places { get; set; }
        public string population { get; set; }
        public string source_link { get; set; }
        public string source_name { get; set; }
        public string storm_name { get; set; }
        public string trigger { get; set; }
        public string tstamp { get; set; }
        public string version { get; set; }
        public string country { get; set; }
        public string time { get; set; }
        public string photos_link { get; set; }
    }
}
