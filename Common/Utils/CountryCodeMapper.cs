using System.Diagnostics.CodeAnalysis;

namespace OpenShock.Common.Utils;

/// <summary>
/// Country mapping for LCG node assignments
/// </summary>
public static class CountryCodeMapper
{
    public static readonly CountryInfo DefaultCountry = new()
    {
        Name = "Unknown",
        CfRegion = null,
        CountryCode = CountryInfo.Alpha2CountryCode.DefaultAlphaCode,
        Latitude = 0.0,
        Longitude = 0.0
    };
    
    public readonly struct CountryInfo : IEquatable<CountryInfo>
    {
        public required Alpha2CountryCode CountryCode { get; init; }
        public required string Name { get; init; }
        public required double Latitude { get; init; }
        public required double Longitude { get; init; }
        public required string? CfRegion { get; init; }
        
        public readonly struct Alpha2CountryCode : IEquatable<Alpha2CountryCode>
        {
            
            public static readonly Alpha2CountryCode DefaultAlphaCode = new() { Char1 = 'X', Char2 = 'X' };
            
            
            public required char Char1 { get; init; }
            public required char Char2 { get; init; }

            public override string ToString() => $"{Char1}{Char2}";

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
                
                return new Alpha2CountryCode
                {
                    Char1 = stringIn[0],
                    Char2 = stringIn[1]
                };
            }

            public bool Equals(Alpha2CountryCode other) => Char1 == other.Char1 && Char2 == other.Char2;
            public override bool Equals(object? obj) => obj is Alpha2CountryCode other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Char1, Char2);
            public static bool operator ==(Alpha2CountryCode left, Alpha2CountryCode right) => left.Equals(right);
            public static bool operator !=(Alpha2CountryCode left, Alpha2CountryCode right) => !(left == right);
            
        }
        
        public bool Equals(CountryInfo other) => CountryCode == other.CountryCode;
        public override bool Equals(object? obj) => obj is CountryInfo other && Equals(other);
        public override int GetHashCode() => CountryCode.GetHashCode();
        public static bool operator ==(CountryInfo left, CountryInfo right) => left.Equals(right);
        public static bool operator !=(CountryInfo left, CountryInfo right) => !(left == right);

        public double DistanceTo(double longitude, double latitude) => CountryCodeMapper.GetDistance(Longitude, Latitude, longitude, latitude);
        public double DistanceTo(CountryInfo otherCountry) => DistanceTo(otherCountry.Longitude, otherCountry.Latitude);
    }
    
    private static double GetDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
    {
        var d1 = latitude * (Math.PI / 180.0);
        var num1 = longitude * (Math.PI / 180.0);
        var d2 = otherLatitude * (Math.PI / 180.0);
        var num2 = otherLongitude * (Math.PI / 180.0) - num1;
        var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
    
        return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
    }

    static CountryCodeMapper()
    {
        Countries = new CountryInfo[]
        {
            new() { CountryCode = "AD", Name = "Andorra", Latitude = 42.546245, Longitude = 1.601554, CfRegion = "weur" },
            new() { CountryCode = "AE", Name = "United Arab Emirates", Latitude = 23.424076, Longitude = 53.847818, CfRegion = "apac" },
            new() { CountryCode = "AF", Name = "Afghanistan", Latitude = 33.93911, Longitude = 67.709953, CfRegion = "apac" },
            new() { CountryCode = "AG", Name = "Antigua and Barbuda", Latitude = 17.060816, Longitude = -61.796428, CfRegion = "apac" },
            new() { CountryCode = "AI", Name = "Anguilla", Latitude = 18.220554, Longitude = -63.068615, CfRegion = "apac" },
            new() { CountryCode = "AL", Name = "Albania", Latitude = 41.153332, Longitude = 20.168331, CfRegion = "weur" },
            new() { CountryCode = "AM", Name = "Armenia", Latitude = 40.069099, Longitude = 45.038189, CfRegion = "apac" },
            new() { CountryCode = "AN", Name = "Netherlands Antilles", Latitude = 12.226079, Longitude = -69.060087, CfRegion = null },
            new() { CountryCode = "AO", Name = "Angola", Latitude = -11.202692, Longitude = 17.873887, CfRegion = "apac" },
            new() { CountryCode = "AQ", Name = "Antarctica", Latitude = -75.250973, Longitude = -0.071389, CfRegion = "weur" },
            new() { CountryCode = "AR", Name = "Argentina", Latitude = -38.416097, Longitude = -63.616672, CfRegion = "apac" },
            new() { CountryCode = "AS", Name = "American Samoa", Latitude = -14.270972, Longitude = -170.132217, CfRegion = "apac" },
            new() { CountryCode = "AT", Name = "Austria", Latitude = 47.516231, Longitude = 14.550072, CfRegion = "weur" },
            new() { CountryCode = "AU", Name = "Australia", Latitude = -25.274398, Longitude = 133.775136, CfRegion = "apac" },
            new() { CountryCode = "AW", Name = "Aruba", Latitude = 12.52111, Longitude = -69.968338, CfRegion = "apac" },
            new() { CountryCode = "AZ", Name = "Azerbaijan", Latitude = 40.143105, Longitude = 47.576927, CfRegion = "apac" },
            new() { CountryCode = "BA", Name = "Bosnia and Herzegovina", Latitude = 43.915886, Longitude = 17.679076, CfRegion = "weur" },
            new() { CountryCode = "BB", Name = "Barbados", Latitude = 13.193887, Longitude = -59.543198, CfRegion = "apac" },
            new() { CountryCode = "BD", Name = "Bangladesh", Latitude = 23.684994, Longitude = 90.356331, CfRegion = "apac" },
            new() { CountryCode = "BE", Name = "Belgium", Latitude = 50.503887, Longitude = 4.469936, CfRegion = "weur" },
            new() { CountryCode = "BF", Name = "Burkina Faso", Latitude = 12.238333, Longitude = -1.561593, CfRegion = "apac" },
            new() { CountryCode = "BG", Name = "Bulgaria", Latitude = 42.733883, Longitude = 25.48583, CfRegion = "eeur" },
            new() { CountryCode = "BH", Name = "Bahrain", Latitude = 25.930414, Longitude = 50.637772, CfRegion = "apac" },
            new() { CountryCode = "BI", Name = "Burundi", Latitude = -3.373056, Longitude = 29.918886, CfRegion = "apac" },
            new() { CountryCode = "BJ", Name = "Benin", Latitude = 9.30769, Longitude = 2.315834, CfRegion = "apac" },
            new() { CountryCode = "BM", Name = "Bermuda", Latitude = 32.321384, Longitude = -64.75737, CfRegion = "enam" },
            new() { CountryCode = "BN", Name = "Brunei", Latitude = 4.535277, Longitude = 114.727669, CfRegion = "apac" },
            new() { CountryCode = "BO", Name = "Bolivia", Latitude = -16.290154, Longitude = -63.588653, CfRegion = "apac" },
            new() { CountryCode = "BR", Name = "Brazil", Latitude = -14.235004, Longitude = -51.92528, CfRegion = "apac" },
            new() { CountryCode = "BS", Name = "Bahamas", Latitude = 25.03428, Longitude = -77.39628, CfRegion = "apac" },
            new() { CountryCode = "BT", Name = "Bhutan", Latitude = 27.514162, Longitude = 90.433601, CfRegion = "apac" },
            new() { CountryCode = "BV", Name = "Bouvet Island", Latitude = -54.423199, Longitude = 3.413194, CfRegion = "apac" },
            new() { CountryCode = "BW", Name = "Botswana", Latitude = -22.328474, Longitude = 24.684866, CfRegion = "apac" },
            new() { CountryCode = "BY", Name = "Belarus", Latitude = 53.709807, Longitude = 27.953389, CfRegion = "eeur" },
            new() { CountryCode = "BZ", Name = "Belize", Latitude = 17.189877, Longitude = -88.49765, CfRegion = "apac" },
            new() { CountryCode = "CA", Name = "Canada", Latitude = 56.130366, Longitude = -106.346771, CfRegion = "enam" },
            new() { CountryCode = "CC", Name = "Cocos [Keeling] Islands", Latitude = -12.164165, Longitude = 96.870956, CfRegion = "apac" },
            new() { CountryCode = "CD", Name = "Congo [DRC]", Latitude = -4.038333, Longitude = 21.758664, CfRegion = "apac" },
            new() { CountryCode = "CF", Name = "Central African Republic", Latitude = 6.611111, Longitude = 20.939444, CfRegion = "apac" },
            new() { CountryCode = "CG", Name = "Congo [Republic]", Latitude = -0.228021, Longitude = 15.827659, CfRegion = "apac" },
            new() { CountryCode = "CH", Name = "Switzerland", Latitude = 46.818188, Longitude = 8.227512, CfRegion = "weur" },
            new() { CountryCode = "CI", Name = "Côte d'Ivoire", Latitude = 7.539989, Longitude = -5.54708, CfRegion = "apac" },
            new() { CountryCode = "CK", Name = "Cook Islands", Latitude = -21.236736, Longitude = -159.777671, CfRegion = "apac" },
            new() { CountryCode = "CL", Name = "Chile", Latitude = -35.675147, Longitude = -71.542969, CfRegion = "apac" },
            new() { CountryCode = "CM", Name = "Cameroon", Latitude = 7.369722, Longitude = 12.354722, CfRegion = "apac" },
            new() { CountryCode = "CN", Name = "China", Latitude = 35.86166, Longitude = 104.195397, CfRegion = "apac" },
            new() { CountryCode = "CO", Name = "Colombia", Latitude = 4.570868, Longitude = -74.297333, CfRegion = "apac" },
            new() { CountryCode = "CR", Name = "Costa Rica", Latitude = 9.748917, Longitude = -83.753428, CfRegion = "apac" },
            new() { CountryCode = "CU", Name = "Cuba", Latitude = 21.521757, Longitude = -77.781167, CfRegion = "apac" },
            new() { CountryCode = "CV", Name = "Cape Verde", Latitude = 16.002082, Longitude = -24.013197, CfRegion = "apac" },
            new() { CountryCode = "CX", Name = "Christmas Island", Latitude = -10.447525, Longitude = 105.690449, CfRegion = "apac" },
            new() { CountryCode = "CY", Name = "Cyprus", Latitude = 35.126413, Longitude = 33.429859, CfRegion = "apac" },
            new() { CountryCode = "CZ", Name = "Czech Republic", Latitude = 49.817492, Longitude = 15.472962, CfRegion = "eeur" },
            new() { CountryCode = "DE", Name = "Germany", Latitude = 51.165691, Longitude = 10.451526, CfRegion = "weur" },
            new() { CountryCode = "DJ", Name = "Djibouti", Latitude = 11.825138, Longitude = 42.590275, CfRegion = "apac" },
            new() { CountryCode = "DK", Name = "Denmark", Latitude = 56.26392, Longitude = 9.501785, CfRegion = "weur" },
            new() { CountryCode = "DM", Name = "Dominica", Latitude = 15.414999, Longitude = -61.370976, CfRegion = "apac" },
            new() { CountryCode = "DO", Name = "Dominican Republic", Latitude = 18.735693, Longitude = -70.162651, CfRegion = "apac" },
            new() { CountryCode = "DZ", Name = "Algeria", Latitude = 28.033886, Longitude = 1.659626, CfRegion = "apac" },
            new() { CountryCode = "EC", Name = "Ecuador", Latitude = -1.831239, Longitude = -78.183406, CfRegion = "apac" },
            new() { CountryCode = "EE", Name = "Estonia", Latitude = 58.595272, Longitude = 25.013607, CfRegion = "weur" },
            new() { CountryCode = "EG", Name = "Egypt", Latitude = 26.820553, Longitude = 30.802498, CfRegion = "apac" },
            new() { CountryCode = "EH", Name = "Western Sahara", Latitude = 24.215527, Longitude = -12.885834, CfRegion = "apac" },
            new() { CountryCode = "ER", Name = "Eritrea", Latitude = 15.179384, Longitude = 39.782334, CfRegion = "apac" },
            new() { CountryCode = "ES", Name = "Spain", Latitude = 40.463667, Longitude = -3.74922, CfRegion = "weur" },
            new() { CountryCode = "ET", Name = "Ethiopia", Latitude = 9.145, Longitude = 40.489673, CfRegion = "apac" },
            new() { CountryCode = "FI", Name = "Finland", Latitude = 61.92411, Longitude = 25.748151, CfRegion = "weur" },
            new() { CountryCode = "FJ", Name = "Fiji", Latitude = -16.578193, Longitude = 179.414413, CfRegion = "apac" },
            new() { CountryCode = "FK", Name = "Falkland Islands [Islas Malvinas]", Latitude = -51.796253, Longitude = -59.523613, CfRegion = "apac" },
            new() { CountryCode = "FM", Name = "Micronesia", Latitude = 7.425554, Longitude = 150.550812, CfRegion = "apac" },
            new() { CountryCode = "FO", Name = "Faroe Islands", Latitude = 61.892635, Longitude = -6.911806, CfRegion = "weur" },
            new() { CountryCode = "FR", Name = "France", Latitude = 46.227638, Longitude = 2.213749, CfRegion = "weur" },
            new() { CountryCode = "GA", Name = "Gabon", Latitude = -0.803689, Longitude = 11.609444, CfRegion = "apac" },
            new() { CountryCode = "GB", Name = "United Kingdom", Latitude = 55.378051, Longitude = -3.435973, CfRegion = "weur" },
            new() { CountryCode = "GD", Name = "Grenada", Latitude = 12.262776, Longitude = -61.604171, CfRegion = "apac" },
            new() { CountryCode = "GE", Name = "Georgia", Latitude = 42.315407, Longitude = 43.356892, CfRegion = "apac" },
            new() { CountryCode = "GF", Name = "French Guiana", Latitude = 3.933889, Longitude = -53.125782, CfRegion = "apac" },
            new() { CountryCode = "GG", Name = "Guernsey", Latitude = 49.465691, Longitude = -2.585278, CfRegion = "weur" },
            new() { CountryCode = "GH", Name = "Ghana", Latitude = 7.946527, Longitude = -1.023194, CfRegion = "apac" },
            new() { CountryCode = "GI", Name = "Gibraltar", Latitude = 36.137741, Longitude = -5.345374, CfRegion = "weur" },
            new() { CountryCode = "GL", Name = "Greenland", Latitude = 71.706936, Longitude = -42.604303, CfRegion = "enam" },
            new() { CountryCode = "GM", Name = "Gambia", Latitude = 13.443182, Longitude = -15.310139, CfRegion = "apac" },
            new() { CountryCode = "GN", Name = "Guinea", Latitude = 9.945587, Longitude = -9.696645, CfRegion = "apac" },
            new() { CountryCode = "GP", Name = "Guadeloupe", Latitude = 16.995971, Longitude = -62.067641, CfRegion = "apac" },
            new() { CountryCode = "GQ", Name = "Equatorial Guinea", Latitude = 1.650801, Longitude = 10.267895, CfRegion = "apac" },
            new() { CountryCode = "GR", Name = "Greece", Latitude = 39.074208, Longitude = 21.824312, CfRegion = "weur" },
            new() { CountryCode = "GS", Name = "South Georgia and the South Sandwich Islands", Latitude = -54.429579, Longitude = -36.587909, CfRegion = "apac" },
            new() { CountryCode = "GT", Name = "Guatemala", Latitude = 15.783471, Longitude = -90.230759, CfRegion = "apac" },
            new() { CountryCode = "GU", Name = "Guam", Latitude = 13.444304, Longitude = 144.793731, CfRegion = "apac" },
            new() { CountryCode = "GW", Name = "Guinea-Bissau", Latitude = 11.803749, Longitude = -15.180413, CfRegion = "apac" },
            new() { CountryCode = "GY", Name = "Guyana", Latitude = 4.860416, Longitude = -58.93018, CfRegion = "apac" },
            new() { CountryCode = "GZ", Name = "Gaza Strip", Latitude = 31.354676, Longitude = 34.308825, CfRegion = null },
            new() { CountryCode = "HK", Name = "Hong Kong", Latitude = 22.396428, Longitude = 114.109497, CfRegion = "apac" },
            new() { CountryCode = "HM", Name = "Heard Island and McDonald Islands", Latitude = -53.08181, Longitude = 73.504158, CfRegion = "apac" },
            new() { CountryCode = "HN", Name = "Honduras", Latitude = 15.199999, Longitude = -86.241905, CfRegion = "apac" },
            new() { CountryCode = "HR", Name = "Croatia", Latitude = 45.1, Longitude = 15.2, CfRegion = "weur" },
            new() { CountryCode = "HT", Name = "Haiti", Latitude = 18.971187, Longitude = -72.285215, CfRegion = "apac" },
            new() { CountryCode = "HU", Name = "Hungary", Latitude = 47.162494, Longitude = 19.503304, CfRegion = "eeur" },
            new() { CountryCode = "ID", Name = "Indonesia", Latitude = -0.789275, Longitude = 113.921327, CfRegion = "apac" },
            new() { CountryCode = "IE", Name = "Ireland", Latitude = 53.41291, Longitude = -8.24389, CfRegion = "weur" },
            new() { CountryCode = "IL", Name = "Israel", Latitude = 31.046051, Longitude = 34.851612, CfRegion = "apac" },
            new() { CountryCode = "IM", Name = "Isle of Man", Latitude = 54.236107, Longitude = -4.548056, CfRegion = "weur" },
            new() { CountryCode = "IN", Name = "India", Latitude = 20.593684, Longitude = 78.96288, CfRegion = "apac" },
            new() { CountryCode = "IO", Name = "British Indian Ocean Territory", Latitude = -6.343194, Longitude = 71.876519, CfRegion = "apac" },
            new() { CountryCode = "IQ", Name = "Iraq", Latitude = 33.223191, Longitude = 43.679291, CfRegion = "apac" },
            new() { CountryCode = "IR", Name = "Iran", Latitude = 32.427908, Longitude = 53.688046, CfRegion = "apac" },
            new() { CountryCode = "IS", Name = "Iceland", Latitude = 64.963051, Longitude = -19.020835, CfRegion = "weur" },
            new() { CountryCode = "IT", Name = "Italy", Latitude = 41.87194, Longitude = 12.56738, CfRegion = "weur" },
            new() { CountryCode = "JE", Name = "Jersey", Latitude = 49.214439, Longitude = -2.13125, CfRegion = "weur" },
            new() { CountryCode = "JM", Name = "Jamaica", Latitude = 18.109581, Longitude = -77.297508, CfRegion = "apac" },
            new() { CountryCode = "JO", Name = "Jordan", Latitude = 30.585164, Longitude = 36.238414, CfRegion = "apac" },
            new() { CountryCode = "JP", Name = "Japan", Latitude = 36.204824, Longitude = 138.252924, CfRegion = "apac" },
            new() { CountryCode = "KE", Name = "Kenya", Latitude = -0.023559, Longitude = 37.906193, CfRegion = "apac" },
            new() { CountryCode = "KG", Name = "Kyrgyzstan", Latitude = 41.20438, Longitude = 74.766098, CfRegion = "apac" },
            new() { CountryCode = "KH", Name = "Cambodia", Latitude = 12.565679, Longitude = 104.990963, CfRegion = "apac" },
            new() { CountryCode = "KI", Name = "Kiribati", Latitude = -3.370417, Longitude = -168.734039, CfRegion = "apac" },
            new() { CountryCode = "KM", Name = "Comoros", Latitude = -11.875001, Longitude = 43.872219, CfRegion = "apac" },
            new() { CountryCode = "KN", Name = "Saint Kitts and Nevis", Latitude = 17.357822, Longitude = -62.782998, CfRegion = "apac" },
            new() { CountryCode = "KP", Name = "North Korea", Latitude = 40.339852, Longitude = 127.510093, CfRegion = "apac" },
            new() { CountryCode = "KR", Name = "South Korea", Latitude = 35.907757, Longitude = 127.766922, CfRegion = "apac" },
            new() { CountryCode = "KW", Name = "Kuwait", Latitude = 29.31166, Longitude = 47.481766, CfRegion = "apac" },
            new() { CountryCode = "KY", Name = "Cayman Islands", Latitude = 19.513469, Longitude = -80.566956, CfRegion = "apac" },
            new() { CountryCode = "KZ", Name = "Kazakhstan", Latitude = 48.019573, Longitude = 66.923684, CfRegion = "apac" },
            new() { CountryCode = "LA", Name = "Laos", Latitude = 19.85627, Longitude = 102.495496, CfRegion = "apac" },
            new() { CountryCode = "LB", Name = "Lebanon", Latitude = 33.854721, Longitude = 35.862285, CfRegion = "apac" },
            new() { CountryCode = "LC", Name = "Saint Lucia", Latitude = 13.909444, Longitude = -60.978893, CfRegion = "apac" },
            new() { CountryCode = "LI", Name = "Liechtenstein", Latitude = 47.166, Longitude = 9.555373, CfRegion = "weur" },
            new() { CountryCode = "LK", Name = "Sri Lanka", Latitude = 7.873054, Longitude = 80.771797, CfRegion = "apac" },
            new() { CountryCode = "LR", Name = "Liberia", Latitude = 6.428055, Longitude = -9.429499, CfRegion = "apac" },
            new() { CountryCode = "LS", Name = "Lesotho", Latitude = -29.609988, Longitude = 28.233608, CfRegion = "apac" },
            new() { CountryCode = "LT", Name = "Lithuania", Latitude = 55.169438, Longitude = 23.881275, CfRegion = "weur" },
            new() { CountryCode = "LU", Name = "Luxembourg", Latitude = 49.815273, Longitude = 6.129583, CfRegion = "weur" },
            new() { CountryCode = "LV", Name = "Latvia", Latitude = 56.879635, Longitude = 24.603189, CfRegion = "weur" },
            new() { CountryCode = "LY", Name = "Libya", Latitude = 26.3351, Longitude = 17.228331, CfRegion = "apac" },
            new() { CountryCode = "MA", Name = "Morocco", Latitude = 31.791702, Longitude = -7.09262, CfRegion = "apac" },
            new() { CountryCode = "MC", Name = "Monaco", Latitude = 43.750298, Longitude = 7.412841, CfRegion = "weur" },
            new() { CountryCode = "MD", Name = "Moldova", Latitude = 47.411631, Longitude = 28.369885, CfRegion = "eeur" },
            new() { CountryCode = "ME", Name = "Montenegro", Latitude = 42.708678, Longitude = 19.37439, CfRegion = "weur" },
            new() { CountryCode = "MG", Name = "Madagascar", Latitude = -18.766947, Longitude = 46.869107, CfRegion = "apac" },
            new() { CountryCode = "MH", Name = "Marshall Islands", Latitude = 7.131474, Longitude = 171.184478, CfRegion = "apac" },
            new() { CountryCode = "MK", Name = "Macedonia [FYROM]", Latitude = 41.608635, Longitude = 21.745275, CfRegion = "weur" },
            new() { CountryCode = "ML", Name = "Mali", Latitude = 17.570692, Longitude = -3.996166, CfRegion = "apac" },
            new() { CountryCode = "MM", Name = "Myanmar [Burma]", Latitude = 21.913965, Longitude = 95.956223, CfRegion = "apac" },
            new() { CountryCode = "MN", Name = "Mongolia", Latitude = 46.862496, Longitude = 103.846656, CfRegion = "apac" },
            new() { CountryCode = "MO", Name = "Macau", Latitude = 22.198745, Longitude = 113.543873, CfRegion = "apac" },
            new() { CountryCode = "MP", Name = "Northern Mariana Islands", Latitude = 17.33083, Longitude = 145.38469, CfRegion = "apac" },
            new() { CountryCode = "MQ", Name = "Martinique", Latitude = 14.641528, Longitude = -61.024174, CfRegion = "apac" },
            new() { CountryCode = "MR", Name = "Mauritania", Latitude = 21.00789, Longitude = -10.940835, CfRegion = "apac" },
            new() { CountryCode = "MS", Name = "Montserrat", Latitude = 16.742498, Longitude = -62.187366, CfRegion = "apac" },
            new() { CountryCode = "MT", Name = "Malta", Latitude = 35.937496, Longitude = 14.375416, CfRegion = "weur" },
            new() { CountryCode = "MU", Name = "Mauritius", Latitude = -20.348404, Longitude = 57.552152, CfRegion = "apac" },
            new() { CountryCode = "MV", Name = "Maldives", Latitude = 3.202778, Longitude = 73.22068, CfRegion = "apac" },
            new() { CountryCode = "MW", Name = "Malawi", Latitude = -13.254308, Longitude = 34.301525, CfRegion = "apac" },
            new() { CountryCode = "MX", Name = "Mexico", Latitude = 23.634501, Longitude = -102.552784, CfRegion = "apac" },
            new() { CountryCode = "MY", Name = "Malaysia", Latitude = 4.210484, Longitude = 101.975766, CfRegion = "apac" },
            new() { CountryCode = "MZ", Name = "Mozambique", Latitude = -18.665695, Longitude = 35.529562, CfRegion = "apac" },
            new() { CountryCode = "NA", Name = "Namibia", Latitude = -22.95764, Longitude = 18.49041, CfRegion = "apac" },
            new() { CountryCode = "NC", Name = "New Caledonia", Latitude = -20.904305, Longitude = 165.618042, CfRegion = "apac" },
            new() { CountryCode = "NE", Name = "Niger", Latitude = 17.607789, Longitude = 8.081666, CfRegion = "apac" },
            new() { CountryCode = "NF", Name = "Norfolk Island", Latitude = -29.040835, Longitude = 167.954712, CfRegion = "apac" },
            new() { CountryCode = "NG", Name = "Nigeria", Latitude = 9.081999, Longitude = 8.675277, CfRegion = "apac" },
            new() { CountryCode = "NI", Name = "Nicaragua", Latitude = 12.865416, Longitude = -85.207229, CfRegion = "apac" },
            new() { CountryCode = "NL", Name = "Netherlands", Latitude = 52.132633, Longitude = 5.291266, CfRegion = "weur" },
            new() { CountryCode = "NO", Name = "Norway", Latitude = 60.472024, Longitude = 8.468946, CfRegion = "weur" },
            new() { CountryCode = "NP", Name = "Nepal", Latitude = 28.394857, Longitude = 84.124008, CfRegion = "apac" },
            new() { CountryCode = "NR", Name = "Nauru", Latitude = -0.522778, Longitude = 166.931503, CfRegion = "apac" },
            new() { CountryCode = "NU", Name = "Niue", Latitude = -19.054445, Longitude = -169.867233, CfRegion = "apac" },
            new() { CountryCode = "NZ", Name = "New Zealand", Latitude = -40.900557, Longitude = 174.885971, CfRegion = "apac" },
            new() { CountryCode = "OM", Name = "Oman", Latitude = 21.512583, Longitude = 55.923255, CfRegion = "apac" },
            new() { CountryCode = "PA", Name = "Panama", Latitude = 8.537981, Longitude = -80.782127, CfRegion = "apac" },
            new() { CountryCode = "PE", Name = "Peru", Latitude = -9.189967, Longitude = -75.015152, CfRegion = "apac" },
            new() { CountryCode = "PF", Name = "French Polynesia", Latitude = -17.679742, Longitude = -149.406843, CfRegion = "apac" },
            new() { CountryCode = "PG", Name = "Papua New Guinea", Latitude = -6.314993, Longitude = 143.95555, CfRegion = "apac" },
            new() { CountryCode = "PH", Name = "Philippines", Latitude = 12.879721, Longitude = 121.774017, CfRegion = "apac" },
            new() { CountryCode = "PK", Name = "Pakistan", Latitude = 30.375321, Longitude = 69.345116, CfRegion = "apac" },
            new() { CountryCode = "PL", Name = "Poland", Latitude = 51.919438, Longitude = 19.145136, CfRegion = "eeur" },
            new() { CountryCode = "PM", Name = "Saint Pierre and Miquelon", Latitude = 46.941936, Longitude = -56.27111, CfRegion = "enam" },
            new() { CountryCode = "PN", Name = "Pitcairn Islands", Latitude = -24.703615, Longitude = -127.439308, CfRegion = "apac" },
            new() { CountryCode = "PR", Name = "Puerto Rico", Latitude = 18.220833, Longitude = -66.590149, CfRegion = "apac" },
            new() { CountryCode = "PS", Name = "Palestinian Territories", Latitude = 31.952162, Longitude = 35.233154, CfRegion = "apac" },
            new() { CountryCode = "PT", Name = "Portugal", Latitude = 39.399872, Longitude = -8.224454, CfRegion = "weur" },
            new() { CountryCode = "PW", Name = "Palau", Latitude = 7.51498, Longitude = 134.58252, CfRegion = "apac" },
            new() { CountryCode = "PY", Name = "Paraguay", Latitude = -23.442503, Longitude = -58.443832, CfRegion = "apac" },
            new() { CountryCode = "QA", Name = "Qatar", Latitude = 25.354826, Longitude = 51.183884, CfRegion = "apac" },
            new() { CountryCode = "RE", Name = "Réunion", Latitude = -21.115141, Longitude = 55.536384, CfRegion = "apac" },
            new() { CountryCode = "RO", Name = "Romania", Latitude = 45.943161, Longitude = 24.96676, CfRegion = "eeur" },
            new() { CountryCode = "RS", Name = "Serbia", Latitude = 44.016521, Longitude = 21.005859, CfRegion = "weur" },
            new() { CountryCode = "RU", Name = "Russia", Latitude = 61.52401, Longitude = 105.318756, CfRegion = "eeur" },
            new() { CountryCode = "RW", Name = "Rwanda", Latitude = -1.940278, Longitude = 29.873888, CfRegion = "apac" },
            new() { CountryCode = "SA", Name = "Saudi Arabia", Latitude = 23.885942, Longitude = 45.079162, CfRegion = "apac" },
            new() { CountryCode = "SB", Name = "Solomon Islands", Latitude = -9.64571, Longitude = 160.156194, CfRegion = "apac" },
            new() { CountryCode = "SC", Name = "Seychelles", Latitude = -4.679574, Longitude = 55.491977, CfRegion = "apac" },
            new() { CountryCode = "SD", Name = "Sudan", Latitude = 12.862807, Longitude = 30.217636, CfRegion = "apac" },
            new() { CountryCode = "SE", Name = "Sweden", Latitude = 60.128161, Longitude = 18.643501, CfRegion = "weur" },
            new() { CountryCode = "SG", Name = "Singapore", Latitude = 1.352083, Longitude = 103.819836, CfRegion = "apac" },
            new() { CountryCode = "SH", Name = "Saint Helena", Latitude = -24.143474, Longitude = -10.030696, CfRegion = "apac" },
            new() { CountryCode = "SI", Name = "Slovenia", Latitude = 46.151241, Longitude = 14.995463, CfRegion = "weur" },
            new() { CountryCode = "SJ", Name = "Svalbard and Jan Mayen", Latitude = 77.553604, Longitude = 23.670272, CfRegion = "weur" },
            new() { CountryCode = "SK", Name = "Slovakia", Latitude = 48.669026, Longitude = 19.699024, CfRegion = "eeur" },
            new() { CountryCode = "SL", Name = "Sierra Leone", Latitude = 8.460555, Longitude = -11.779889, CfRegion = "apac" },
            new() { CountryCode = "SM", Name = "San Marino", Latitude = 43.94236, Longitude = 12.457777, CfRegion = "weur" },
            new() { CountryCode = "SN", Name = "Senegal", Latitude = 14.497401, Longitude = -14.452362, CfRegion = "apac" },
            new() { CountryCode = "SO", Name = "Somalia", Latitude = 5.152149, Longitude = 46.199616, CfRegion = "apac" },
            new() { CountryCode = "SR", Name = "Suriname", Latitude = 3.919305, Longitude = -56.027783, CfRegion = "apac" },
            new() { CountryCode = "ST", Name = "Sao Tomé and Príncipe", Latitude = 0.18636, Longitude = 6.613081, CfRegion = "apac" },
            new() { CountryCode = "SV", Name = "El Salvador", Latitude = 13.794185, Longitude = -88.89653, CfRegion = "apac" },
            new() { CountryCode = "SY", Name = "Syria", Latitude = 34.802075, Longitude = 38.996815, CfRegion = "apac" },
            new() { CountryCode = "SZ", Name = "Swaziland", Latitude = -26.522503, Longitude = 31.465866, CfRegion = "apac" },
            new() { CountryCode = "TC", Name = "Turks and Caicos Islands", Latitude = 21.694025, Longitude = -71.797928, CfRegion = "apac" },
            new() { CountryCode = "TD", Name = "Chad", Latitude = 15.454166, Longitude = 18.732207, CfRegion = "apac" },
            new() { CountryCode = "TF", Name = "French Southern Territories", Latitude = -49.280366, Longitude = 69.348557, CfRegion = "apac" },
            new() { CountryCode = "TG", Name = "Togo", Latitude = 8.619543, Longitude = 0.824782, CfRegion = "apac" },
            new() { CountryCode = "TH", Name = "Thailand", Latitude = 15.870032, Longitude = 100.992541, CfRegion = "apac" },
            new() { CountryCode = "TJ", Name = "Tajikistan", Latitude = 38.861034, Longitude = 71.276093, CfRegion = "apac" },
            new() { CountryCode = "TK", Name = "Tokelau", Latitude = -8.967363, Longitude = -171.855881, CfRegion = "apac" },
            new() { CountryCode = "TL", Name = "Timor-Leste", Latitude = -8.874217, Longitude = 125.727539, CfRegion = "apac" },
            new() { CountryCode = "TM", Name = "Turkmenistan", Latitude = 38.969719, Longitude = 59.556278, CfRegion = "apac" },
            new() { CountryCode = "TN", Name = "Tunisia", Latitude = 33.886917, Longitude = 9.537499, CfRegion = "apac" },
            new() { CountryCode = "TO", Name = "Tonga", Latitude = -21.178986, Longitude = -175.198242, CfRegion = "apac" },
            new() { CountryCode = "TR", Name = "Turkey", Latitude = 38.963745, Longitude = 35.243322, CfRegion = "apac" },
            new() { CountryCode = "TT", Name = "Trinidad and Tobago", Latitude = 10.691803, Longitude = -61.222503, CfRegion = "apac" },
            new() { CountryCode = "TV", Name = "Tuvalu", Latitude = -7.109535, Longitude = 177.64933, CfRegion = "apac" },
            new() { CountryCode = "TW", Name = "Taiwan", Latitude = 23.69781, Longitude = 120.960515, CfRegion = "apac" },
            new() { CountryCode = "TZ", Name = "Tanzania", Latitude = -6.369028, Longitude = 34.888822, CfRegion = "apac" },
            new() { CountryCode = "UA", Name = "Ukraine", Latitude = 48.379433, Longitude = 31.16558, CfRegion = "eeur" },
            new() { CountryCode = "UG", Name = "Uganda", Latitude = 1.373333, Longitude = 32.290275, CfRegion = "apac" },
            new() { CountryCode = "UM", Name = "U.S. Minor Outlying Islands", Latitude = 19.295374, Longitude = 166.6280441, CfRegion = "apac" },
            new() { CountryCode = "US", Name = "United States", Latitude = 37.09024, Longitude = -95.712891, CfRegion = "enam" },
            new() { CountryCode = "UY", Name = "Uruguay", Latitude = -32.522779, Longitude = -55.765835, CfRegion = "apac" },
            new() { CountryCode = "UZ", Name = "Uzbekistan", Latitude = 41.377491, Longitude = 64.585262, CfRegion = "apac" },
            new() { CountryCode = "VA", Name = "Vatican City", Latitude = 41.902916, Longitude = 12.453389, CfRegion = "weur" },
            new() { CountryCode = "VC", Name = "Saint Vincent and the Grenadines", Latitude = 12.984305, Longitude = -61.287228, CfRegion = "apac" },
            new() { CountryCode = "VE", Name = "Venezuela", Latitude = 6.42375, Longitude = -66.58973, CfRegion = "apac" },
            new() { CountryCode = "VG", Name = "British Virgin Islands", Latitude = 18.420695, Longitude = -64.639968, CfRegion = "apac" },
            new() { CountryCode = "VI", Name = "U.S. Virgin Islands", Latitude = 18.335765, Longitude = -64.896335, CfRegion = "apac" },
            new() { CountryCode = "VN", Name = "Vietnam", Latitude = 14.058324, Longitude = 108.277199, CfRegion = "apac" },
            new() { CountryCode = "VU", Name = "Vanuatu", Latitude = -15.376706, Longitude = 166.959158, CfRegion = "apac" },
            new() { CountryCode = "WF", Name = "Wallis and Futuna", Latitude = -13.768752, Longitude = -177.156097, CfRegion = "apac" },
            new() { CountryCode = "WS", Name = "Samoa", Latitude = -13.759029, Longitude = -172.104629, CfRegion = "apac" },
            new() { CountryCode = "XK", Name = "Kosovo", Latitude = 42.602636, Longitude = 20.902977, CfRegion = null },
            new() { CountryCode = "YE", Name = "Yemen", Latitude = 15.552727, Longitude = 48.516388, CfRegion = "apac" },
            new() { CountryCode = "YT", Name = "Mayotte", Latitude = -12.8275, Longitude = 45.166244, CfRegion = "apac" },
            new() { CountryCode = "ZA", Name = "South Africa", Latitude = -30.559482, Longitude = 22.937506, CfRegion = "apac" },
            new() { CountryCode = "ZM", Name = "Zambia", Latitude = -13.133897, Longitude = 27.849332, CfRegion = "apac" },
            new() { CountryCode = "ZW", Name = "Zimbabwe", Latitude = -19.015438, Longitude = 29.154857, CfRegion = "apac" },
        };
        
        CountryCodeToCountryInfo = Countries.ToDictionary(x => x.CountryCode, x => x);
    }

    public static readonly CountryInfo[] Countries;
    public static readonly IReadOnlyDictionary<CountryInfo.Alpha2CountryCode, CountryInfo> CountryCodeToCountryInfo;

    public static CountryInfo GetCountryOrDefault(CountryInfo.Alpha2CountryCode alpha2Country) => 
        CountryCodeToCountryInfo.TryGetValue(alpha2Country, out var countryInfo) ?
            countryInfo : DefaultCountry;
    
}