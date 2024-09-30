using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace OpenShock.Common.Utils;

/// <summary>
/// Country mapping for LCG node assignments
/// </summary>
public static class CountryCodeMapper
{
    public static readonly CountryInfo DefaultCountry = new(Alpha2CountryCode.DefaultAlphaCode, "Unknown", 0.0, 0.0, null);

    public readonly record struct Alpha2CountryCode(char Char1, char Char2) : IEquatable<Alpha2CountryCode>
    {

        public static readonly Alpha2CountryCode DefaultAlphaCode = new('X', 'X');

        public static bool TryParseAndValidate(string stringIn, [MaybeNullWhen(false)] out Alpha2CountryCode code)
        {
            if (stringIn.Length != 2 || !char.IsAsciiLetterUpper(stringIn[0]) || !char.IsAsciiLetterUpper(stringIn[1]))
            {
                code = default;
                return false;
            }

            code = new Alpha2CountryCode
            {
                Char1 = stringIn[0],
                Char2 = stringIn[1]
            };
            return true;
        }

        public static Alpha2CountryCode ParseOrDefault(string stringIn) => TryParseAndValidate(stringIn, out var code) ? code : DefaultAlphaCode;

        public static implicit operator Alpha2CountryCode(string stringIn)
        {
            if (stringIn.Length != 2) throw new ArgumentOutOfRangeException(nameof(stringIn), "String input must be exactly 2 chars");
            if (!char.IsAsciiLetterUpper(stringIn[0]) || !char.IsAsciiLetterUpper(stringIn[1])) throw new ArgumentOutOfRangeException(nameof(stringIn), "String input must be upper characters only");

            return new Alpha2CountryCode(stringIn[0], stringIn[1]);
        }

        public bool IsUnknown() => this == DefaultAlphaCode;

        public override string ToString() => $"{Char1}{Char2}";
    }

    public sealed record CountryInfo(Alpha2CountryCode CountryCode, string Name, double Latitude, double Longitude, string? CfRegion);

    /// <summary>
    /// Sorts the codes and concatenates them to create a unique ID that represents the distance between two countries
    /// </summary>
    /// <param name="code1"></param>
    /// <param name="code2"></param>
    /// <returns></returns>
    private static string CreateId(Alpha2CountryCode code1, Alpha2CountryCode code2)
    {
        if (code1.Char1 > code2.Char1 || code1.Char2 > code2.Char2) (code2, code1) = (code1, code2);

        return new string([code1.Char1, code1.Char2, code2.Char1, code2.Char2]);
    }

    /// <summary>
    /// Calculates the distance between two countries using the Haversine formula
    /// </summary>
    /// <param name="lat1"></param>
    /// <param name="lon1"></param>
    /// <param name="lat2"></param>
    /// <param name="lon2"></param>
    /// <returns></returns>
    private static double GetDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadius = 6371.0;
        const double DegToRad = Math.PI / 180.0;

        double latDist = (lat2 - lat1) * DegToRad;
        double lonDist = (lon2 - lon1) * DegToRad;

        double latVal = Math.Sin(latDist / 2.0);
        double lonVal = Math.Sin(lonDist / 2.0);
        double otherVal = Math.Cos(lat1 * DegToRad) * Math.Cos(lat2 * DegToRad);

        double a = (latVal * latVal) + (otherVal * (lonVal * lonVal));
        double b = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return EarthRadius * b;
    }
    private static double GetDistance(CountryInfo country1, CountryInfo country2) => GetDistance(country1.Latitude, country1.Longitude, country2.Latitude, country2.Longitude);

    static CountryCodeMapper()
    {
        Countries =
        [
            new("AD", "Andorra", 42.546245, 1.601554, "weur"),
            new("AE", "United Arab Emirates", 23.424076, 53.847818, "apac"),
            new("AF", "Afghanistan", 33.93911, 67.709953, "apac"),
            new("AG", "Antigua and Barbuda", 17.060816, -61.796428, "apac"),
            new("AI", "Anguilla", 18.220554, -63.068615, "apac"),
            new("AL", "Albania", 41.153332, 20.168331, "weur"),
            new("AM", "Armenia", 40.069099, 45.038189, "apac"),
            new("AN", "Netherlands Antilles", 12.226079, -69.060087, null),
            new("AO", "Angola", -11.202692, 17.873887, "apac"),
            new("AQ", "Antarctica", -75.250973, -0.071389, "weur"),
            new("AR", "Argentina", -38.416097, -63.616672, "apac"),
            new("AS", "American Samoa", -14.270972, -170.132217, "apac"),
            new("AT", "Austria", 47.516231, 14.550072, "weur"),
            new("AU", "Australia", -25.274398, 133.775136, "apac"),
            new("AW", "Aruba", 12.52111, -69.968338, "apac"),
            new("AZ", "Azerbaijan", 40.143105, 47.576927, "apac"),
            new("BA", "Bosnia and Herzegovina", 43.915886, 17.679076, "weur"),
            new("BB", "Barbados", 13.193887, -59.543198, "apac"),
            new("BD", "Bangladesh", 23.684994, 90.356331, "apac"),
            new("BE", "Belgium", 50.503887, 4.469936, "weur"),
            new("BF", "Burkina Faso", 12.238333, -1.561593, "apac"),
            new("BG", "Bulgaria", 42.733883, 25.48583, "eeur"),
            new("BH", "Bahrain", 25.930414, 50.637772, "apac"),
            new("BI", "Burundi", -3.373056, 29.918886, "apac"),
            new("BJ", "Benin", 9.30769, 2.315834, "apac"),
            new("BM", "Bermuda", 32.321384, -64.75737, "enam"),
            new("BN", "Brunei", 4.535277, 114.727669, "apac"),
            new("BO", "Bolivia", -16.290154, -63.588653, "apac"),
            new("BR", "Brazil", -14.235004, -51.92528, "apac"),
            new("BS", "Bahamas", 25.03428, -77.39628, "apac"),
            new("BT", "Bhutan", 27.514162, 90.433601, "apac"),
            new("BV", "Bouvet Island", -54.423199, 3.413194, "apac"),
            new("BW", "Botswana", -22.328474, 24.684866, "apac"),
            new("BY", "Belarus", 53.709807, 27.953389, "eeur"),
            new("BZ", "Belize", 17.189877, -88.49765, "apac"),
            new("CA", "Canada", 56.130366, -106.346771, "enam"),
            new("CC", "Cocos [Keeling] Islands", -12.164165, 96.870956, "apac"),
            new("CD", "Congo [DRC]", -4.038333, 21.758664, "apac"),
            new("CF", "Central African Republic", 6.611111, 20.939444, "apac"),
            new("CG", "Congo [Republic]", -0.228021, 15.827659, "apac"),
            new("CH", "Switzerland", 46.818188, 8.227512, "weur"),
            new("CI", "Côte d'Ivoire", 7.539989, -5.54708, "apac"),
            new("CK", "Cook Islands", -21.236736, -159.777671, "apac"),
            new("CL", "Chile", -35.675147, -71.542969, "apac"),
            new("CM", "Cameroon", 7.369722, 12.354722, "apac"),
            new("CN", "China", 35.86166, 104.195397, "apac"),
            new("CO", "Colombia", 4.570868, -74.297333, "apac"),
            new("CR", "Costa Rica", 9.748917, -83.753428, "apac"),
            new("CU", "Cuba", 21.521757, -77.781167, "apac"),
            new("CV", "Cape Verde", 16.002082, -24.013197, "apac"),
            new("CX", "Christmas Island", -10.447525, 105.690449, "apac"),
            new("CY", "Cyprus", 35.126413, 33.429859, "apac"),
            new("CZ", "Czech Republic", 49.817492, 15.472962, "eeur"),
            new("DE", "Germany", 51.165691, 10.451526, "weur"),
            new("DJ", "Djibouti", 11.825138, 42.590275, "apac"),
            new("DK", "Denmark", 56.26392, 9.501785, "weur"),
            new("DM", "Dominica", 15.414999, -61.370976, "apac"),
            new("DO", "Dominican Republic", 18.735693, -70.162651, "apac"),
            new("DZ", "Algeria", 28.033886, 1.659626, "apac"),
            new("EC", "Ecuador", -1.831239, -78.183406, "apac"),
            new("EE", "Estonia", 58.595272, 25.013607, "weur"),
            new("EG", "Egypt", 26.820553, 30.802498, "apac"),
            new("EH", "Western Sahara", 24.215527, -12.885834, "apac"),
            new("ER", "Eritrea", 15.179384, 39.782334, "apac"),
            new("ES", "Spain", 40.463667, -3.74922, "weur"),
            new("ET", "Ethiopia", 9.145, 40.489673, "apac"),
            new("FI", "Finland", 61.92411, 25.748151, "weur"),
            new("FJ", "Fiji", -16.578193, 179.414413, "apac"),
            new("FK", "Falkland Islands [Islas Malvinas]", -51.796253, -59.523613, "apac"),
            new("FM", "Micronesia", 7.425554, 150.550812, "apac"),
            new("FO", "Faroe Islands", 61.892635, -6.911806, "weur"),
            new("FR", "France", 46.227638, 2.213749, "weur"),
            new("GA", "Gabon", -0.803689, 11.609444, "apac"),
            new("GB", "United Kingdom", 55.378051, -3.435973, "weur"),
            new("GD", "Grenada", 12.262776, -61.604171, "apac"),
            new("GE", "Georgia", 42.315407, 43.356892, "apac"),
            new("GF", "French Guiana", 3.933889, -53.125782, "apac"),
            new("GG", "Guernsey", 49.465691, -2.585278, "weur"),
            new("GH", "Ghana", 7.946527, -1.023194, "apac"),
            new("GI", "Gibraltar", 36.137741, -5.345374, "weur"),
            new("GL", "Greenland", 71.706936, -42.604303, "enam"),
            new("GM", "Gambia", 13.443182, -15.310139, "apac"),
            new("GN", "Guinea", 9.945587, -9.696645, "apac"),
            new("GP", "Guadeloupe", 16.995971, -62.067641, "apac"),
            new("GQ", "Equatorial Guinea", 1.650801, 10.267895, "apac"),
            new("GR", "Greece", 39.074208, 21.824312, "weur"),
            new("GS", "South Georgia and the South Sandwich Islands", -54.429579, -36.587909, "apac"),
            new("GT", "Guatemala", 15.783471, -90.230759, "apac"),
            new("GU", "Guam", 13.444304, 144.793731, "apac"),
            new("GW", "Guinea-Bissau", 11.803749, -15.180413, "apac"),
            new("GY", "Guyana", 4.860416, -58.93018, "apac"),
            new("GZ", "Gaza Strip", 31.354676, 34.308825, null),
            new("HK", "Hong Kong", 22.396428, 114.109497, "apac"),
            new("HM", "Heard Island and McDonald Islands", -53.08181, 73.504158, "apac"),
            new("HN", "Honduras", 15.199999, -86.241905, "apac"),
            new("HR", "Croatia", 45.1, 15.2, "weur"),
            new("HT", "Haiti", 18.971187, -72.285215, "apac"),
            new("HU", "Hungary", 47.162494, 19.503304, "eeur"),
            new("ID", "Indonesia", -0.789275, 113.921327, "apac"),
            new("IE", "Ireland", 53.41291, -8.24389, "weur"),
            new("IL", "Israel", 31.046051, 34.851612, "apac"),
            new("IM", "Isle of Man", 54.236107, -4.548056, "weur"),
            new("IN", "India", 20.593684, 78.96288, "apac"),
            new("IO", "British Indian Ocean Territory", -6.343194, 71.876519, "apac"),
            new("IQ", "Iraq", 33.223191, 43.679291, "apac"),
            new("IR", "Iran", 32.427908, 53.688046, "apac"),
            new("IS", "Iceland", 64.963051, -19.020835, "weur"),
            new("IT", "Italy", 41.87194, 12.56738, "weur"),
            new("JE", "Jersey", 49.214439, -2.13125, "weur"),
            new("JM", "Jamaica", 18.109581, -77.297508, "apac"),
            new("JO", "Jordan", 30.585164, 36.238414, "apac"),
            new("JP", "Japan", 36.204824, 138.252924, "apac"),
            new("KE", "Kenya", -0.023559, 37.906193, "apac"),
            new("KG", "Kyrgyzstan", 41.20438, 74.766098, "apac"),
            new("KH", "Cambodia", 12.565679, 104.990963, "apac"),
            new("KI", "Kiribati", -3.370417, -168.734039, "apac"),
            new("KM", "Comoros", -11.875001, 43.872219, "apac"),
            new("KN", "Saint Kitts and Nevis", 17.357822, -62.782998, "apac"),
            new("KP", "North Korea", 40.339852, 127.510093, "apac"),
            new("KR", "South Korea", 35.907757, 127.766922, "apac"),
            new("KW", "Kuwait", 29.31166, 47.481766, "apac"),
            new("KY", "Cayman Islands", 19.513469, -80.566956, "apac"),
            new("KZ", "Kazakhstan", 48.019573, 66.923684, "apac"),
            new("LA", "Laos", 19.85627, 102.495496, "apac"),
            new("LB", "Lebanon", 33.854721, 35.862285, "apac"),
            new("LC", "Saint Lucia", 13.909444, -60.978893, "apac"),
            new("LI", "Liechtenstein", 47.166, 9.555373, "weur"),
            new("LK", "Sri Lanka", 7.873054, 80.771797, "apac"),
            new("LR", "Liberia", 6.428055, -9.429499, "apac"),
            new("LS", "Lesotho", -29.609988, 28.233608, "apac"),
            new("LT", "Lithuania", 55.169438, 23.881275, "weur"),
            new("LU", "Luxembourg", 49.815273, 6.129583, "weur"),
            new("LV", "Latvia", 56.879635, 24.603189, "weur"),
            new("LY", "Libya", 26.3351, 17.228331, "apac"),
            new("MA", "Morocco", 31.791702, -7.09262, "apac"),
            new("MC", "Monaco", 43.750298, 7.412841, "weur"),
            new("MD", "Moldova", 47.411631, 28.369885, "eeur"),
            new("ME", "Montenegro", 42.708678, 19.37439, "weur"),
            new("MG", "Madagascar", -18.766947, 46.869107, "apac"),
            new("MH", "Marshall Islands", 7.131474, 171.184478, "apac"),
            new("MK", "Macedonia [FYROM]", 41.608635, 21.745275, "weur"),
            new("ML", "Mali", 17.570692, -3.996166, "apac"),
            new("MM", "Myanmar [Burma]", 21.913965, 95.956223, "apac"),
            new("MN", "Mongolia", 46.862496, 103.846656, "apac"),
            new("MO", "Macau", 22.198745, 113.543873, "apac"),
            new("MP", "Northern Mariana Islands", 17.33083, 145.38469, "apac"),
            new("MQ", "Martinique", 14.641528, -61.024174, "apac"),
            new("MR", "Mauritania", 21.00789, -10.940835, "apac"),
            new("MS", "Montserrat", 16.742498, -62.187366, "apac"),
            new("MT", "Malta", 35.937496, 14.375416, "weur"),
            new("MU", "Mauritius", -20.348404, 57.552152, "apac"),
            new("MV", "Maldives", 3.202778, 73.22068, "apac"),
            new("MW", "Malawi", -13.254308, 34.301525, "apac"),
            new("MX", "Mexico", 23.634501, -102.552784, "apac"),
            new("MY", "Malaysia", 4.210484, 101.975766, "apac"),
            new("MZ", "Mozambique", -18.665695, 35.529562, "apac"),
            new("NA", "Namibia", -22.95764, 18.49041, "apac"),
            new("NC", "New Caledonia", -20.904305, 165.618042, "apac"),
            new("NE", "Niger", 17.607789, 8.081666, "apac"),
            new("NF", "Norfolk Island", -29.040835, 167.954712, "apac"),
            new("NG", "Nigeria", 9.081999, 8.675277, "apac"),
            new("NI", "Nicaragua", 12.865416, -85.207229, "apac"),
            new("NL", "Netherlands", 52.132633, 5.291266, "weur"),
            new("NO", "Norway", 60.472024, 8.468946, "weur"),
            new("NP", "Nepal", 28.394857, 84.124008, "apac"),
            new("NR", "Nauru", -0.522778, 166.931503, "apac"),
            new("NU", "Niue", -19.054445, -169.867233, "apac"),
            new("NZ", "New Zealand", -40.900557, 174.885971, "apac"),
            new("OM", "Oman", 21.512583, 55.923255, "apac"),
            new("PA", "Panama", 8.537981, -80.782127, "apac"),
            new("PE", "Peru", -9.189967, -75.015152, "apac"),
            new("PF", "French Polynesia", -17.679742, -149.406843, "apac"),
            new("PG", "Papua New Guinea", -6.314993, 143.95555, "apac"),
            new("PH", "Philippines", 12.879721, 121.774017, "apac"),
            new("PK", "Pakistan", 30.375321, 69.345116, "apac"),
            new("PL", "Poland", 51.919438, 19.145136, "eeur"),
            new("PM", "Saint Pierre and Miquelon", 46.941936, -56.27111, "enam"),
            new("PN", "Pitcairn Islands", -24.703615, -127.439308, "apac"),
            new("PR", "Puerto Rico", 18.220833, -66.590149, "apac"),
            new("PS", "Palestinian Territories", 31.952162, 35.233154, "apac"),
            new("PT", "Portugal", 39.399872, -8.224454, "weur"),
            new("PW", "Palau", 7.51498, 134.58252, "apac"),
            new("PY", "Paraguay", -23.442503, -58.443832, "apac"),
            new("QA", "Qatar", 25.354826, 51.183884, "apac"),
            new("RE", "Réunion", -21.115141, 55.536384, "apac"),
            new("RO", "Romania", 45.943161, 24.96676, "eeur"),
            new("RS", "Serbia", 44.016521, 21.005859, "weur"),
            new("RU", "Russia", 61.52401, 105.318756, "eeur"),
            new("RW", "Rwanda", -1.940278, 29.873888, "apac"),
            new("SA", "Saudi Arabia", 23.885942, 45.079162, "apac"),
            new("SB", "Solomon Islands", -9.64571, 160.156194, "apac"),
            new("SC", "Seychelles", -4.679574, 55.491977, "apac"),
            new("SD", "Sudan", 12.862807, 30.217636, "apac"),
            new("SE", "Sweden", 60.128161, 18.643501, "weur"),
            new("SG", "Singapore", 1.352083, 103.819836, "apac"),
            new("SH", "Saint Helena", -24.143474, -10.030696, "apac"),
            new("SI", "Slovenia", 46.151241, 14.995463, "weur"),
            new("SJ", "Svalbard and Jan Mayen", 77.553604, 23.670272, "weur"),
            new("SK", "Slovakia", 48.669026, 19.699024, "eeur"),
            new("SL", "Sierra Leone", 8.460555, -11.779889, "apac"),
            new("SM", "San Marino", 43.94236, 12.457777, "weur"),
            new("SN", "Senegal", 14.497401, -14.452362, "apac"),
            new("SO", "Somalia", 5.152149, 46.199616, "apac"),
            new("SR", "Suriname", 3.919305, -56.027783, "apac"),
            new("ST", "Sao Tomé and Príncipe", 0.18636, 6.613081, "apac"),
            new("SV", "El Salvador", 13.794185, -88.89653, "apac"),
            new("SY", "Syria", 34.802075, 38.996815, "apac"),
            new("SZ", "Swaziland", -26.522503, 31.465866, "apac"),
            new("TC", "Turks and Caicos Islands", 21.694025, -71.797928, "apac"),
            new("TD", "Chad", 15.454166, 18.732207, "apac"),
            new("TF", "French Southern Territories", -49.280366, 69.348557, "apac"),
            new("TG", "Togo", 8.619543, 0.824782, "apac"),
            new("TH", "Thailand", 15.870032, 100.992541, "apac"),
            new("TJ", "Tajikistan", 38.861034, 71.276093, "apac"),
            new("TK", "Tokelau", -8.967363, -171.855881, "apac"),
            new("TL", "Timor-Leste", -8.874217, 125.727539, "apac"),
            new("TM", "Turkmenistan", 38.969719, 59.556278, "apac"),
            new("TN", "Tunisia", 33.886917, 9.537499, "apac"),
            new("TO", "Tonga", -21.178986, -175.198242, "apac"),
            new("TR", "Turkey", 38.963745, 35.243322, "apac"),
            new("TT", "Trinidad and Tobago", 10.691803, -61.222503, "apac"),
            new("TV", "Tuvalu", -7.109535, 177.64933, "apac"),
            new("TW", "Taiwan", 23.69781, 120.960515, "apac"),
            new("TZ", "Tanzania", -6.369028, 34.888822, "apac"),
            new("UA", "Ukraine", 48.379433, 31.16558, "eeur"),
            new("UG", "Uganda", 1.373333, 32.290275, "apac"),
            new("UM", "U.S. Minor Outlying Islands", 19.295374, 166.6280441, "apac"),
            new("US", "United States", 37.09024, -95.712891, "enam"),
            new("UY", "Uruguay", -32.522779, -55.765835, "apac"),
            new("UZ", "Uzbekistan", 41.377491, 64.585262, "apac"),
            new("VA", "Vatican City", 41.902916, 12.453389, "weur"),
            new("VC", "Saint Vincent and the Grenadines", 12.984305, -61.287228, "apac"),
            new("VE", "Venezuela", 6.42375, -66.58973, "apac"),
            new("VG", "British Virgin Islands", 18.420695, -64.639968, "apac"),
            new("VI", "U.S. Virgin Islands", 18.335765, -64.896335, "apac"),
            new("VN", "Vietnam", 14.058324, 108.277199, "apac"),
            new("VU", "Vanuatu", -15.376706, 166.959158, "apac"),
            new("WF", "Wallis and Futuna", -13.768752, -177.156097, "apac"),
            new("WS", "Samoa", -13.759029, -172.104629, "apac"),
            new("XK", "Kosovo", 42.602636, 20.902977, null),
            new("YE", "Yemen", 15.552727, 48.516388, "apac"),
            new("YT", "Mayotte", -12.8275, 45.166244, "apac"),
            new("ZA", "South Africa", -30.559482, 22.937506, "apac"),
            new("ZM", "Zambia", -13.133897, 27.849332, "apac"),
            new("ZW", "Zimbabwe", -19.015438, 29.154857, "apac"),
        ];

        CountryCodeToCountryInfo = Countries.ToFrozenDictionary(x => x.CountryCode, x => x); // Create a frozen dictionary for fast lookups

        // Calculate all distances (43k+ entries, allows for really fast lookups tho)
        Distances = Countries
                .SelectMany((a, i) =>
                    Countries
                        .Skip(i) // Skip the countries we've already calculated
                        .Select(b => new KeyValuePair<string, double>(CreateId(a.CountryCode, b.CountryCode), GetDistance(a, b)))
                )
                .ToFrozenDictionary(); // Create a frozen dictionary for fast lookups
    }

    public static readonly CountryInfo[] Countries;
    public static readonly IReadOnlyDictionary<Alpha2CountryCode, CountryInfo> CountryCodeToCountryInfo;
    private static readonly IReadOnlyDictionary<string, double> Distances;

    public static CountryInfo GetCountryOrDefault(Alpha2CountryCode alpha2Country) =>
        CountryCodeToCountryInfo.TryGetValue(alpha2Country, out var countryInfo) ?
            countryInfo : DefaultCountry;

    public static bool TryGetDistanceBetween(Alpha2CountryCode alpha2CountryA, Alpha2CountryCode alpha2CountryB, out double distance) =>
        Distances.TryGetValue(CreateId(alpha2CountryA, alpha2CountryB), out distance);

}