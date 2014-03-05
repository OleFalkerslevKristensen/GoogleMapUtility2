using System;
using System.Collections.Generic;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;

/*
 * Copyright 2013 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace MapsAndLocationDemo.utilities
{

	

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:


	public class SphericalUtil
	{

        const double EARTH_RADIUS = 6371009;
        double PI = Math.PI;
        MathUtil math = new MathUtil();
        
        private SphericalUtil()
		{
		}

		/// <summary>
		/// Returns the heading from one LatLng to another LatLng. Headings are
		/// expressed in degrees clockwise from North within the range [-180,180). </summary>
		/// <returns> The heading in degrees clockwise from north. </returns>
		public double computeHeading(LatLng from, LatLng to)
		{
			// http://williams.best.vwh.net/avform.htm#Crs
			double fromLat = math.ToRadians(from.Latitude);
			double fromLng = math.ToRadians(from.Longitude);
			double toLat = math.ToRadians(to.Latitude);
			double toLng = math.ToRadians(to.Longitude);
			double dLng = toLng - fromLng;
			double heading = Math.Atan2(Math.Sin(dLng) * Math.Cos(toLat), Math.Cos(fromLat) * Math.Sin(toLat) - Math.Sin(fromLat) * Math.Cos(toLat) * Math.Cos(dLng));
			return math.wrap(math.ToDegrees(heading), -180, 180);
		}

		/// <summary>
		/// Returns the LatLng resulting from moving a distance from an origin
		/// in the specified heading (expressed in degrees clockwise from north). </summary>
		/// <param name="from">     The LatLng from which to start. </param>
		/// <param name="distance"> The distance to travel. </param>
		/// <param name="heading">  The heading in degrees clockwise from north. </param>
		public LatLng computeOffset(LatLng from, double distance, double heading)
		{
			distance /= EARTH_RADIUS;
			heading = math.ToRadians(heading);
			// http://williams.best.vwh.net/avform.htm#LL
			double fromLat = math.ToRadians(from.Latitude);
			double fromLng = math.ToRadians(from.Longitude);
			double cosDistance = Math.Cos(distance);
			double sinDistance = Math.Sin(distance);
			double sinFromLat = Math.Sin(fromLat);
			double cosFromLat = Math.Cos(fromLat);
			double sinLat = cosDistance * sinFromLat + sinDistance * cosFromLat * Math.Cos(heading);
			double dLng = Math.Atan2(sinDistance * cosFromLat * Math.Sin(heading), cosDistance - sinFromLat * sinLat);
            return new LatLng(math.ToDegrees(Math.Asin(sinLat)), math.ToDegrees(fromLng + dLng));
		}

		/// <summary>
		/// Returns the location of origin when provided with a LatLng destination,
		/// meters travelled and original heading. Headings are expressed in degrees
		/// clockwise from North. This function returns null when no solution is
		/// available. </summary>
		/// <param name="to">       The destination LatLng. </param>
		/// <param name="distance"> The distance travelled, in meters. </param>
		/// <param name="heading">  The heading in degrees clockwise from north. </param>
		public LatLng computeOffsetOrigin(LatLng to, double distance, double heading)
		{
			heading = math.ToRadians(heading);
			distance /= EARTH_RADIUS;
			// http://lists.maptools.org/pipermail/proj/2008-October/003939.html
			double n1 = Math.Cos(distance);
			double n2 = Math.Sin(distance) * Math.Cos(heading);
			double n3 = Math.Sin(distance) * Math.Sin(heading);
			double n4 = Math.Sin(math.ToRadians(to.Latitude));
			// There are two solutions for b. b = n2 * n4 +/- sqrt(), one solution results
			// in the latitude outside the [-90, 90] range. We first try one solution and
			// back off to the other if we are outside that range.
			double n12 = n1 * n1;
			double discriminant = n2 * n2 * n12 + n12 * n12 - n12 * n4 * n4;
			if (discriminant < 0)
			{
				// No real solution which would make sense in LatLng-space.
				return null;
			}
			double b = n2 * n4 + Math.Sqrt(discriminant);
			b /= n1 * n1 + n2 * n2;
			double a = (n4 - n2 * b) / n1;
			double fromLatRadians = Math.Atan2(a, b);
			if (fromLatRadians < -PI / 2 || fromLatRadians > PI / 2)
			{
				b = n2 * n4 - Math.Sqrt(discriminant);
				b /= n1 * n1 + n2 * n2;
				fromLatRadians = Math.Atan2(a, b);
			}
			if (fromLatRadians < -PI / 2 || fromLatRadians > PI / 2)
			{
				// No solution which would make sense in LatLng-space.
				return null;
			}
			double fromLngRadians = math.ToRadians(to.Longitude) - Math.Atan2(n3, n1 * Math.Cos(fromLatRadians) - n2 * Math.Sin(fromLatRadians));
            return new LatLng(math.ToDegrees(fromLatRadians), math.ToDegrees(fromLngRadians));
		}

		/// <summary>
		/// Returns the LatLng which lies the given fraction of the way between the
		/// origin LatLng and the destination LatLng. </summary>
		/// <param name="from">     The LatLng from which to start. </param>
		/// <param name="to">       The LatLng toward which to travel. </param>
		/// <param name="fraction"> A fraction of the distance to travel. </param>
		/// <returns> The interpolated LatLng. </returns>
		public LatLng interpolate(LatLng from, LatLng to, double fraction)
		{
			// http://en.wikipedia.org/wiki/Slerp
			double fromLat = math.ToRadians(from.Latitude);
			double fromLng = math.ToRadians(from.Longitude);
			double toLat = math.ToRadians(to.Latitude);
			double toLng = math.ToRadians(to.Longitude);
			double cosFromLat = Math.Cos(fromLat);
			double cosToLat = Math.Cos(toLat);

			// Computes Spherical interpolation coefficients.
			double angle = computeAngleBetween(from, to);
			double sinAngle = Math.Sin(angle);
			if (sinAngle < 1E-6)
			{
				return from;
			}
			double a = Math.Sin((1 - fraction) * angle) / sinAngle;
			double b = Math.Sin(fraction * angle) / sinAngle;

			// Converts from polar to vector and interpolate.
			double x = a * cosFromLat * Math.Cos(fromLng) + b * cosToLat * Math.Cos(toLng);
			double y = a * cosFromLat * Math.Sin(fromLng) + b * cosToLat * Math.Sin(toLng);
			double z = a * Math.Sin(fromLat) + b * Math.Sin(toLat);

			// Converts interpolated vector back to polar.
			double lat = Math.Atan2(z, Math.Sqrt(x * x + y * y));
			double lng = Math.Atan2(y, x);
            return new LatLng(math.ToDegrees(lat), math.ToDegrees(lng));
		}

		/// <summary>
		/// Returns distance on the unit sphere; the arguments are in radians.
		/// </summary>
		private double distanceRadians(double lat1, double lng1, double lat2, double lng2)
		{
			return math.arcHav(math.havDistance(lat1, lat2, lng1 - lng2));
		}

		/// <summary>
		/// Returns the angle between two LatLngs, in radians. This is the same as the distance
		/// on the unit sphere.
		/// </summary>
		private double computeAngleBetween(LatLng from, LatLng to)
		{
			return distanceRadians(math.ToRadians(from.Latitude), math.ToRadians(from.Longitude), math.ToRadians(to.Latitude), math.ToRadians(to.Longitude));
		}

		/// <summary>
		/// Returns the distance between two LatLngs, in meters.
		/// </summary>
		private double computeDistanceBetween(LatLng from, LatLng to)
		{
			return computeAngleBetween(from, to) * EARTH_RADIUS;
		}

		/// <summary>
		/// Returns the length of the given path, in meters, on Earth.
		/// </summary>
		public double computeLength(IList<LatLng> path)
		{
			if (path.Count < 2)
			{
				return 0;
			}
			double length = 0;
			LatLng prev = path[0];
			double prevLat = math.ToRadians(prev.Latitude);
			double prevLng = math.ToRadians(prev.Longitude);
			foreach (LatLng point in path)
			{
				double lat = math.ToRadians(point.Latitude);
				double lng = math.ToRadians(point.Longitude);
				length += distanceRadians(prevLat, prevLng, lat, lng);
				prevLat = lat;
				prevLng = lng;
			}
			return length * EARTH_RADIUS;
		}

		/// <summary>
		/// Returns the area of a closed path on Earth. </summary>
		/// <param name="path"> A closed path. </param>
		/// <returns> The path's area in square meters. </returns>
		private double computeArea(IList<LatLng> path)
		{
			return Math.Abs(computeSignedArea(path));
		}

		/// <summary>
		/// Returns the signed area of a closed path on Earth. The sign of the area may be used to
		/// determine the orientation of the path.
		/// "inside" is the surface that does not contain the South Pole. </summary>
		/// <param name="path"> A closed path. </param>
		/// <returns> The loop's area in square meters. </returns>
		public double computeSignedArea(IList<LatLng> path)
		{
			return computeSignedArea(path, EARTH_RADIUS);
		}

		/// <summary>
		/// Returns the signed area of a closed path on a sphere of given radius.
		/// The computed area uses the same units as the radius squared.
		/// Used by SphericalUtilTest.
		/// </summary>
		public double computeSignedArea(IList<LatLng> path, double radius)
		{
			int size = path.Count;
			if (size < 3)
			{
				return 0;
			}
			double total = 0;
			LatLng prev = path[size - 1];
			double prevTanLat = Math.Tan((PI / 2 - math.ToRadians(prev.Latitude)) / 2);
			double prevLng = math.ToRadians(prev.Longitude);
			// For each edge, accumulate the signed area of the triangle formed by the North Pole
			// and that edge ("polar triangle").
			foreach (LatLng point in path)
			{
				double tanLat = Math.Tan((PI / 2 - math.ToRadians(point.Latitude)) / 2);
				double lng = math.ToRadians(point.Longitude);
				total += polarTriangleArea(tanLat, lng, prevTanLat, prevLng);
				prevTanLat = tanLat;
				prevLng = lng;
			}
			return total * (radius * radius);
		}

		/// <summary>
		/// Returns the signed area of a triangle which has North Pole as a vertex.
		/// Formula derived from "Area of a spherical triangle given two edges and the included angle"
		/// as per "Spherical Trigonometry" by Todhunter, page 71, section 103, point 2.
		/// See http://books.google.com/books?id=3uBHAAAAIAAJ&pg=PA71
		/// The arguments named "tan" are tan((pi/2 - latitude)/2).
		/// </summary>
		private static double polarTriangleArea(double tan1, double lng1, double tan2, double lng2)
		{
			double deltaLng = lng1 - lng2;
			double t = tan1 * tan2;
			return 2 * Math.Atan2(t * Math.Sin(deltaLng), 1 + t * Math.Cos(deltaLng));
		}
#if __ACTIVE__
		/// <summary>
		/// Wraps the given value into the inclusive-exclusive interval between min and max. </summary>
		/// <param name="n">   The value to wrap. </param>
		/// <param name="min"> The minimum. </param>
		/// <param name="max"> The maximum. </param>
		internal static double wrap(double n, double min, double max)
		{
			return (n >= Math.Min && n < Math.Max) ? n : (mod(n - Math.Min, Math.Max - Math.Min) + Math.Min);
		}
#endif
		/// <summary>
		/// Returns the non-negative remainder of x / m. </summary>
		/// <param name="x"> The operand. </param>
		/// <param name="m"> The modulus. </param>
		internal static double mod(double x, double m)
		{
			return ((x % m) + m) % m;
		}
	}

}