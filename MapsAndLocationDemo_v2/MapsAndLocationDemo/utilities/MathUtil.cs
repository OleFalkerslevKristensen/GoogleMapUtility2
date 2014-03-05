using System;

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

	/// <summary>
	/// Utility functions that are used my both PolyUtil and SphericalUtil.
	/// </summary>
	public class MathUtil
	{
		/// <summary>
		/// The earth's radius, in meters.
		/// Mean radius as defined by IUGG.
		/// </summary>
		const double EARTH_RADIUS = 6371009;
        double PI = Math.PI;

        public double ToRadians(double angle)
        {
            return (PI / 180) * angle;
        }

        public double ToDegrees(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

		/// <summary>
		/// Restrict x to the range [low, high].
		/// </summary>
		public double clamp(double x, double low, double high)
		{
			return x < low ? low : (x > high ? high : x);
		}

		/// <summary>
		/// Wraps the given value into the inclusive-exclusive interval between min and max. </summary>
		/// <param name="n">   The value to wrap. </param>
		/// <param name="min"> The minimum. </param>
		/// <param name="max"> The maximum. </param>
		public double wrap(double n, double min, double max)
		{
			return (n >= min && n < max) ? n : (mod(n - min, max - min) + min);
		}

		/// <summary>
		/// Returns the non-negative remainder of x / m. </summary>
		/// <param name="x"> The operand. </param>
		/// <param name="m"> The modulus. </param>
		internal static double mod(double x, double m)
		{
			return ((x % m) + m) % m;
		}

		/// <summary>
		/// Returns mercator Y corresponding to latitude.
		/// See http://en.wikipedia.org/wiki/Mercator_projection .
		/// </summary>
		public double mercator(double lat)
		{
			return Math.Log(Math.Tan(lat * 0.5 + PI / 4));
		}

		/// <summary>
		/// Returns latitude from mercator Y.
		/// </summary>
		public double inverseMercator(double y)
		{
			return 2 * Math.Atan(Math.Exp(y)) - PI / 2;
		}

		/// <summary>
		/// Returns haversine(angle-in-radians).
		/// hav(x) == (1 - cos(x)) / 2 == sin(x / 2)^2.
		/// </summary>
		public double hav(double x)
		{
			double sinHalf = Math.Sin(x * 0.5);
			return sinHalf * sinHalf;
		}

		/// <summary>
		/// Computes inverse haversine. Has good numerical stability around 0.
		/// arcHav(x) == acos(1 - 2 * x) == 2 * asin(sqrt(x)).
		/// The argument must be in [0, 1], and the result is positive.
		/// </summary>
		public double arcHav(double x)
		{
			return 2 * Math.Asin(Math.Sqrt(x));
		}

		// Given h==hav(x), returns sin(abs(x)).
		public double sinFromHav(double h)
		{
			return 2 * Math.Sqrt(h * (1 - h));
		}

		// Returns hav(asin(x)).
		public double havFromSin(double x)
		{
			double x2 = x * x;
			return x2 / (1 + Math.Sqrt(1 - x2)) * .5;
		}

		// Returns sin(arcHav(x) + arcHav(y)).
		public double sinSumFromHav(double x, double y)
		{
			double a = Math.Sqrt(x * (1 - x));
			double b = Math.Sqrt(y * (1 - y));
			return 2 * (a + b - 2 * (a * y + b * x));
		}

		/// <summary>
		/// Returns hav() of distance from (lat1, lng1) to (lat2, lng2) on the unit sphere.
		/// </summary>
		public double havDistance(double lat1, double lat2, double dLng)
		{
			return hav(lat1 - lat2) + hav(dLng) * Math.Cos(lat1) * Math.Cos(lat2);
		}
	}

}