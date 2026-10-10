namespace Raphael.Shared.Routing
{
    /// <summary>
    /// How far a vehicle still has to go to a stop, measured at home: never a request to Google.
    /// </summary>
    /// <remarks>
    /// Run on every position a driver reports, for the trips a clinic is watching, so it must be
    /// free. The road comes from the shape the routing cache already bought for that leg
    /// (<c>RouteLegCacheEntry.EncodedPolyline</c>): the vehicle is projected onto it and the rest of
    /// the line is measured. Without a shape, or with the vehicle off it, the straight line times a
    /// road factor stands in. Decision of 2026-10-10, see BACKLOG §17 <c>live-eta</c>.
    /// </remarks>
    public static class RemainingDistance
    {
        private const double EarthRadiusMeters = 6_371_008.8;

        /// <summary>
        /// Streets are longer than the straight line: in South Florida a road trip runs about 20–40 %
        /// over it. 1.3 is the middle, and it only applies when there is no shape to measure on.
        /// </summary>
        public const double RoadFactor = 1.3;

        /// <summary>
        /// Farther than this from the shape, the vehicle is not on that road (a detour, or still
        /// coming from another stop), and measuring along the line would be wrong.
        /// </summary>
        public const double MaxOffRouteMeters = 150;

        public const double MetersPerMile = 1609.344;

        /// <summary>
        /// Meters still to go: along <paramref name="encodedPolyline"/> when the vehicle is on it,
        /// otherwise the straight line to the stop times <see cref="RoadFactor"/>.
        /// </summary>
        public static double MetersToGo(
            double vehicleLat, double vehicleLng,
            double stopLat, double stopLng,
            string? encodedPolyline)
        {
            if (!string.IsNullOrEmpty(encodedPolyline))
            {
                var along = AlongLine(Decode(encodedPolyline), vehicleLat, vehicleLng, MaxOffRouteMeters);
                if (along.HasValue) return along.Value;
            }

            return HaversineMeters(vehicleLat, vehicleLng, stopLat, stopLng) * RoadFactor;
        }

        /// <summary>
        /// The length of the line from the point on it nearest the vehicle to its end, or null when
        /// the vehicle is more than <paramref name="maxOffMeters"/> away from every segment.
        /// </summary>
        public static double? AlongLine(IReadOnlyList<(double Lat, double Lng)> line, double lat, double lng, double maxOffMeters)
        {
            if (line.Count == 0) return null;
            if (line.Count == 1) return HaversineMeters(lat, lng, line[0].Lat, line[0].Lng);

            var bestOff = double.MaxValue;
            var bestIndex = -1;
            var bestT = 0.0;

            for (var i = 0; i < line.Count - 1; i++)
            {
                var (off, t) = OffsetToSegment(line[i], line[i + 1], lat, lng);
                if (off < bestOff)
                {
                    bestOff = off;
                    bestIndex = i;
                    bestT = t;
                }
            }

            if (bestOff > maxOffMeters) return null;

            // The unfinished part of the segment the vehicle is on, then every segment after it.
            var a = line[bestIndex];
            var b = line[bestIndex + 1];
            var rest = HaversineMeters(a.Lat, a.Lng, b.Lat, b.Lng) * (1 - bestT);
            for (var i = bestIndex + 1; i < line.Count - 1; i++)
                rest += HaversineMeters(line[i].Lat, line[i].Lng, line[i + 1].Lat, line[i + 1].Lng);

            return rest;
        }

        /// <summary>
        /// Distance from a point to a segment, and how far along the segment (0–1) its foot falls.
        /// A flat projection around the segment is exact enough at city scale: a few metres over a
        /// segment, against a GPS that already wanders by ten.
        /// </summary>
        private static (double OffMeters, double T) OffsetToSegment((double Lat, double Lng) a, (double Lat, double Lng) b, double lat, double lng)
        {
            var cosLat = Math.Cos(ToRadians((a.Lat + b.Lat) / 2));
            double X(double lngDeg) => ToRadians(lngDeg) * cosLat * EarthRadiusMeters;
            double Y(double latDeg) => ToRadians(latDeg) * EarthRadiusMeters;

            double ax = X(a.Lng), ay = Y(a.Lat), bx = X(b.Lng), by = Y(b.Lat), px = X(lng), py = Y(lat);
            double dx = bx - ax, dy = by - ay;
            var lengthSq = dx * dx + dy * dy;
            var t = lengthSq == 0 ? 0 : Math.Clamp(((px - ax) * dx + (py - ay) * dy) / lengthSq, 0, 1);
            double fx = ax + t * dx - px, fy = ay + t * dy - py;
            return (Math.Sqrt(fx * fx + fy * fy), t);
        }

        public static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);
            var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }

        /// <summary>Google's encoded polyline format (precision 5) into points.</summary>
        public static List<(double Lat, double Lng)> Decode(string encoded)
        {
            var points = new List<(double, double)>();
            int index = 0, lat = 0, lng = 0;

            while (index < encoded.Length)
            {
                if (!Next(ref index, out var dLat) || !Next(ref index, out var dLng)) break;
                lat += dLat;
                lng += dLng;
                points.Add((lat / 1e5, lng / 1e5));
            }

            return points;

            bool Next(ref int i, out int value)
            {
                int result = 0, shift = 0, b;
                value = 0;
                do
                {
                    if (i >= encoded.Length) return false;
                    b = encoded[i++] - 63;
                    result |= (b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20);
                value = (result & 1) != 0 ? ~(result >> 1) : result >> 1;
                return true;
            }
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
