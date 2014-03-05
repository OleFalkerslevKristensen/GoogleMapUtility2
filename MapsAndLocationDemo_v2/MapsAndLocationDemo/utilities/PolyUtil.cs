using System;
using System.Collections.Generic;
using System.Text;
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



	public class PolyUtil
	{

        
        const double EARTH_RADIUS = 6371009;
        double PI = Math.PI;

        MathUtil math = new MathUtil();
        
        private PolyUtil()
		{
		}

		/// <summary>
		/// Returns tan(latitude-at-lng3) on the great circle (lat1, lng1) to (lat2, lng2). lng1==0.
		/// See http://williams.best.vwh.net/avform.htm .
		/// </summary>
		private static double tanLatGC(double lat1, double lat2, double lng2, double lng3)
		{
			return (Math.Tan(lat1) * Math.Sin(lng2 - lng3) + Math.Tan(lat2) * Math.Sin(lng3)) / Math.Sin(lng2);
		}

		/// <summary>
		/// Returns mercator(latitude-at-lng3) on the Rhumb line (lat1, lng1) to (lat2, lng2). lng1==0.
		/// </summary>
		private double mercatorLatRhumb(double lat1, double lat2, double lng2, double lng3)
		{
			return (math.mercator(lat1) * (lng2 - lng3) + math.mercator(lat2) * lng3) / lng2;
		}

		/// <summary>
		/// Computes whether the vertical segment (lat3, lng3) to South Pole intersects the segment
		/// (lat1, lng1) to (lat2, lng2).
		/// Longitudes are offset by -lng1; the implicit lng1 becomes 0.
		/// </summary>
		private bool intersects(double lat1, double lat2, double lng2, double lat3, double lng3, bool geodesic)
		{
			// Both ends on the same side of lng3.
			if ((lng3 >= 0 && lng3 >= lng2) || (lng3 < 0 && lng3 < lng2))
			{
				return false;
			}
			// Point is South Pole.
			if (lat3 <= -PI / 2)
			{
				return false;
			}
			// Any segment end is a pole.
			if (lat1 <= -PI / 2 || lat2 <= -PI / 2 || lat1 >= PI / 2 || lat2 >= PI / 2)
			{
				return false;
			}
			if (lng2 <= -PI)
			{
				return false;
			}
			double linearLat = (lat1 * (lng2 - lng3) + lat2 * lng3) / lng2;
			// Northern hemisphere and point under lat-lng line.
			if (lat1 >= 0 && lat2 >= 0 && lat3 < linearLat)
			{
				return false;
			}
			// Southern hemisphere and point above lat-lng line.
			if (lat1 <= 0 && lat2 <= 0 && lat3 >= linearLat)
			{
				return true;
			}
			// North Pole.
			if (lat3 >= PI / 2)
			{
				return true;
			}
			// Compare lat3 with latitude on the GC/Rhumb segment corresponding to lng3.
			// Compare through a strictly-increasing function (tan() or mercator()) as convenient.
			return geodesic ? Math.Tan(lat3) >= tanLatGC(lat1, lat2, lng2, lng3) : math.mercator(lat3) >= mercatorLatRhumb(lat1, lat2, lng2, lng3);
		}

		/// <summary>
		/// Computes whether the given point lies inside the specified polygon.
		/// The polygon is always cosidered closed, regardless of whether the last point equals
		/// the first or not.
		/// Inside is defined as not containing the South Pole -- the South Pole is always outside.
		/// The polygon is formed of great circle segments if geodesic is true, and of rhumb
		/// (loxodromic) segments otherwise.
		/// </summary>
		public bool containsLocation(LatLng point, IList<LatLng> polygon, bool geodesic)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = polygon.size();
			int size = polygon.Count;
			if (size == 0)
			{
				return false;
			}
			double lat3 = math.ToRadians(point.Latitude);
			double lng3 = math.ToRadians(point.Longitude);
			LatLng prev = polygon[size - 1];
			double lat1 = math.ToRadians(prev.Latitude);
			double lng1 = math.ToRadians(prev.Longitude);
			int nIntersect = 0;
			foreach (LatLng point2 in polygon)
			{
				double dLng3 = math.wrap(lng3 - lng1, -PI, PI);
				// Special case: point equal to vertex is inside.
				if (lat3 == lat1 && dLng3 == 0)
				{
					return true;
				}
				double lat2 = math.ToRadians(point2.Latitude);
				double lng2 = math.ToRadians(point2.Longitude);
				// Offset longitudes by -lng1.
				if (intersects(lat1, lat2, math.wrap(lng2 - lng1, -PI, PI), lat3, dLng3, geodesic))
				{
					++nIntersect;
				}
				lat1 = lat2;
				lng1 = lng2;
			}
			return (nIntersect & 1) != 0;
		}

		private const double DEFAULT_TOLERANCE = 0.1; // meters.

		/// <summary>
		/// Computes whether the given point lies on or near the edge of a polygon, within a specified
		/// tolerance in meters. The polygon edge is composed of great circle segments if geodesic
		/// is true, and of Rhumb segments otherwise. The polygon edge is implicitly closed -- the
		/// closing segment between the first point and the last point is included.
		/// </summary>
		public bool isLocationOnEdge(LatLng point, IList<LatLng> polygon, bool geodesic, double tolerance)
		{
			return isLocationOnEdgeOrPath(point, polygon, true, geodesic, tolerance);
		}

		/// <summary>
		/// Same as <seealso cref="#isLocationOnEdge(LatLng, List, boolean, double)"/>
		/// with a default tolerance of 0.1 meters.
		/// </summary>
		public bool isLocationOnEdge(LatLng point, IList<LatLng> polygon, bool geodesic)
		{
			return isLocationOnEdge(point, polygon, geodesic, DEFAULT_TOLERANCE);
		}

		/// <summary>
		/// Computes whether the given point lies on or near a polyline, within a specified
		/// tolerance in meters. The polyline is composed of great circle segments if geodesic
		/// is true, and of Rhumb segments otherwise. The polyline is not closed -- the closing
		/// segment between the first point and the last point is not included.
		/// </summary>
		private bool isLocationOnPath(LatLng point, IList<LatLng> polyline, bool geodesic, double tolerance)
		{
			return isLocationOnEdgeOrPath(point, polyline, false, geodesic, tolerance);
		}

		/// <summary>
		/// Same as <seealso cref="#isLocationOnPath(LatLng, List, boolean, double)"/>
		/// 
		/// with a default tolerance of 0.1 meters.
		/// </summary>
		private bool isLocationOnPath(LatLng point, IList<LatLng> polyline, bool geodesic)
		{
			return isLocationOnPath(point, polyline, geodesic, DEFAULT_TOLERANCE);
		}

		private bool isLocationOnEdgeOrPath(LatLng point, IList<LatLng> poly, bool closed, bool geodesic, double toleranceEarth)
		{
			int size = poly.Count;
			if (size == 0)
			{
				return false;
			}
			double tolerance = toleranceEarth / EARTH_RADIUS;
			double havTolerance = math.hav(tolerance);
			double lat3 = math.ToRadians(point.Latitude);
			double lng3 = math.ToRadians(point.Longitude);
			LatLng prev = poly[closed ? size - 1 : 0];
			double lat1 = math.ToRadians(prev.Latitude);
			double lng1 = math.ToRadians(prev.Longitude);
			if (geodesic)
			{
				foreach (LatLng point2 in poly)
				{
					double lat2 = math.ToRadians(point2.Latitude);
					double lng2 = math.ToRadians(point2.Longitude);
					if (isOnSegmentGC(lat1, lng1, lat2, lng2, lat3, lng3, havTolerance))
					{
						return true;
					}
					lat1 = lat2;
					lng1 = lng2;
				}
			}
			else
			{
				// We project the points to mercator space, where the Rhumb segment is a straight line,
				// and compute the geodesic distance between point3 and the closest point on the
				// segment. This method is an approximation, because it uses "closest" in mercator
				// space which is not "closest" on the sphere -- but the error is small because
				// "tolerance" is small.
				double minAcceptable = lat3 - tolerance;
				double maxAcceptable = lat3 + tolerance;
				double y1 = math.mercator(lat1);
				double y3 = math.mercator(lat3);
				double[] xTry = new double[3];
				foreach (LatLng point2 in poly)
				{
					double lat2 = math.ToRadians(point2.Latitude);
					double y2 = math.mercator(lat2);
					double lng2 = math.ToRadians(point2.Longitude);
					if (Math.Max(lat1, lat2) >= minAcceptable && Math.Min(lat1, lat2) <= maxAcceptable)
					{
						// We offset longitudes by -lng1; the implicit x1 is 0.
						double x2 = math.wrap(lng2 - lng1, -PI, PI);
						double x3Base = math.wrap(lng3 - lng1, -PI, PI);
						xTry[0] = x3Base;
						// Also explore wrapping of x3Base around the world in both directions.
						xTry[1] = x3Base + 2 * PI;
						xTry[2] = x3Base - 2 * PI;
						foreach (double x3 in xTry)
						{
							double dy = y2 - y1;
							double len2 = x2 * x2 + dy * dy;
							double t = len2 <= 0 ? 0 : math.clamp((x3 * x2 + (y3 - y1) * dy) / len2, 0, 1);
							double xClosest = t * x2;
							double yClosest = y1 + t * dy;
							double latClosest = math.inverseMercator(yClosest);
							double havDist = math.havDistance(lat3, latClosest, x3 - xClosest);
							if (havDist < havTolerance)
							{
								return true;
							}
						}
					}
					lat1 = lat2;
					lng1 = lng2;
					y1 = y2;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns sin(initial bearing from (lat1,lng1) to (lat3,lng3) minus initial bearing
		/// from (lat1, lng1) to (lat2,lng2)).
		/// </summary>
		private double sinDeltaBearing(double lat1, double lng1, double lat2, double lng2, double lat3, double lng3)
		{
			double sinLat1 = Math.Sin(lat1);
			double cosLat2 = Math.Cos(lat2);
			double cosLat3 = Math.Cos(lat3);
			double lat31 = lat3 - lat1;
			double lng31 = lng3 - lng1;
			double lat21 = lat2 - lat1;
			double lng21 = lng2 - lng1;
			double a = Math.Sin(lng31) * cosLat3;
			double c = Math.Sin(lng21) * cosLat2;
			double b = Math.Sin(lat31) + 2 * sinLat1 * cosLat3 * math.hav(lng31);
			double d = Math.Sin(lat21) + 2 * sinLat1 * cosLat2 * math.hav(lng21);
			double denom = (a * a + b * b) * (c * c + d * d);
			return denom <= 0 ? 1 : (a * d - b * c) / Math.Sqrt(denom);
		}

		private bool isOnSegmentGC(double lat1, double lng1, double lat2, double lng2, double lat3, double lng3, double havTolerance)
		{
			double havDist13 = math.havDistance(lat1, lat3, lng1 - lng3);
			if (havDist13 <= havTolerance)
			{
				return true;
			}
			double havDist23 = math.havDistance(lat2, lat3, lng2 - lng3);
			if (havDist23 <= havTolerance)
			{
				return true;
			}
			double sinBearing = sinDeltaBearing(lat1, lng1, lat2, lng2, lat3, lng3);
			double sinDist13 = math.sinFromHav(havDist13);
			double havCrossTrack = math.havFromSin(sinDist13 * sinBearing);
			if (havCrossTrack > havTolerance)
			{
				return false;
			}
			double havDist12 = math.havDistance(lat1, lat2, lng1 - lng2);
			double term = havDist12 + havCrossTrack * (1 - 2 * havDist12);
			if (havDist13 > term || havDist23 > term)
			{
				return false;
			}
			if (havDist12 < 0.74)
			{
				return true;
			}
			double cosCrossTrack = 1 - 2 * havCrossTrack;
			double havAlongTrack13 = (havDist13 - havCrossTrack) / cosCrossTrack;
			double havAlongTrack23 = (havDist23 - havCrossTrack) / cosCrossTrack;
			double sinSumAlongTrack = math.sinSumFromHav(havAlongTrack13, havAlongTrack23);
			return sinSumAlongTrack > 0; // Compare with half-circle == PI using sign of sin().
		}

		/// <summary>
		/// Decodes an encoded path string into a sequence of LatLngs.
		/// </summary>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static java.util.List<com.google.android.gms.maps.model.LatLng> decode(final String encodedPath)
		public static IList<LatLng> decode(string encodedPath)
		{
			int len = encodedPath.Length;

			// For speed we preallocate to an upper bound on the final length, then
			// truncate the array before returning.
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<com.google.android.gms.maps.model.LatLng> path = new java.util.ArrayList<com.google.android.gms.maps.model.LatLng>();
			IList<LatLng> path = new List<LatLng>();
			int index = 0;
			int lat = 0;
			int lng = 0;

			while (index < len)
			{
				int result = 1;
				int shift = 0;
				int b;
				do
				{
					b = encodedPath[index++] - 63 - 1;
					result += b << shift;
					shift += 5;
				} while (b >= 0x1f);
				lat += (result & 1) != 0 ?~(result >> 1) : (result >> 1);

				result = 1;
				shift = 0;
				do
				{
					b = encodedPath[index++] - 63 - 1;
					result += b << shift;
					shift += 5;
				} while (b >= 0x1f);
				lng += (result & 1) != 0 ?~(result >> 1) : (result >> 1);

				path.Add(new LatLng(lat * 1e-5, lng * 1e-5));
			}

			return path;
		}

		/// <summary>
		/// Encodes a sequence of LatLngs into an encoded path string.
		/// </summary>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static String encode(final java.util.List<com.google.android.gms.maps.model.LatLng> path)
		private string encode(IList<LatLng> path)
		{
			long lastLat = 0;
			long lastLng = 0;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StringBuffer result = new StringBuffer();
			StringBuilder result = new StringBuilder();

			foreach (LatLng point in path)
			{
				long lat = (long)Math.Round(point.Latitude * 1e5);
				long lng = (long)Math.Round(point.Longitude * 1e5);

				long dLat = lat - lastLat;
				long dLng = lng - lastLng;

				encode(dLat, ref result);
				encode(dLng, ref result);

				lastLat = lat;
				lastLng = lng;
			}
			return result.ToString();
		}

		private void encode(long v, ref StringBuilder result)
		{
			v = v < 0 ?~(v << 1) : v << 1;
			while (v >= 0x20)
			{
                result.Append(Convert.ToChar((int)((0x20 | (v & 0x1f)) + 63)));
				v >>= 5;
			}
            result.Append(Convert.ToChar((int)(v + 63)));
		}
	}

}