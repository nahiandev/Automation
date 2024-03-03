namespace Automation.Helpers
{
    internal class Location
    {
        private Location() {}
        public static IDictionary<string, string> AvailableCountries()
        {
            IDictionary<string, string> country_code = new Dictionary<string, string>
            {
                { "All", string.Empty },
                { "Australia", "101452733" },
                { "Belgium", "100565514" },
                { "Brazil", "106057199" },
                { "Canada", "101174742" },
                { "China", "102890883" },
                { "Denmark", "104514075" },
                { "France", "105015875" },
                { "Finland", "100456013" },
                { "Germany", "101282230" },
                { "Israel", "101620260" },
                { "Italy", "103350119" },
                { "India", "102713980" },
                { "Japan", "101355337" },
                { "Netherlands", "102890719" },
                { "New Zealand", "105490917" },
                { "Norway", "103819153" },
                { "Poland", "105072130" },
                { "Russia", "101728296" },
                { "Romania", "106670623" },
                { "Sweden", "105117694" },
                { "Spain", "105646813" },
                { "Switzerland", "106693272" },
                { "United States", "103644278" },
                { "United Kingdom", "101165590" } 
            };

            return country_code;
        }
    }
}
