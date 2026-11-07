namespace PhotoIt.Data
{
    public class RouteLeg
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public RoutePoint From { get; set; } = new();
        public RoutePoint To { get; set; } = new();
        public TransportType Transport { get; set; }
        public double? DistanceKm { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsEditing { get; set; } = false;

        public double? CalculateDistanceKm()
        {
            if (!From.HasGeoPoint() || !To.HasGeoPoint()
                || From.Latitude is not double fromLatitude
                || From.Longitude is not double fromLongitude
                || To.Latitude is not double toLatitude
                || To.Longitude is not double toLongitude)
            {
                return null;
            }

            const double earthRadiusKm = 6371.0088;
            var latitudeDifference = DegreesToRadians(toLatitude - fromLatitude);
            var longitudeDifference = DegreesToRadians(toLongitude - fromLongitude);
            var haversine = Math.Pow(Math.Sin(latitudeDifference / 2), 2)
                + Math.Cos(DegreesToRadians(fromLatitude))
                * Math.Cos(DegreesToRadians(toLatitude))
                * Math.Pow(Math.Sin(longitudeDifference / 2), 2);

            return earthRadiusKm * 2 * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0, 1)));
        }

        public bool IsValid()
        {
            return From.IsValid() && To.IsValid();
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    }

    
  
}
