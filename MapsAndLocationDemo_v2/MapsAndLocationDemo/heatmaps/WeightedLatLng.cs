/*
 * Copyright 2014 Google Inc.
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

#if __ACTIVE__

using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Com.Google.Maps.Android.Projection;
using MapsAndLocationDemo.projection;
using MapsAndLocationDemo.heatmaps;
using Com.Google.Maps.Android.Quadtree;


namespace com.google.maps.android.heatmaps
{

	
	/// <summary>
	/// A wrapper class that can be used in a PointQuadTree
	/// Created from a LatLng and optional intensity: point coordinates of the LatLng and the intensity
	/// value can be accessed from it later.
	/// </summary>
	public class WeightedLatLng : PointQuadTree  //.Item
	{

        public interface Item
        {
            public Point getPoint();
        }

		/// <summary>
		/// Default intensity to use when intensity not specified
		/// </summary>
		public const double DEFAULT_INTENSITY = 1;

		/// <summary>
		/// Projection to use for points
		/// Converts LatLng to (x, y) coordinates using a SphericalMercatorProjection
		/// </summary>
		private static readonly SphericalMercatorProjection sProjection = new SphericalMercatorProjection(HeatmapTileProvider.WORLD_WIDTH);

		private Point mPoint;

		private double mIntensity;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="latLng">    LatLng to add to wrapper </param>
		/// <param name="intensity"> Intensity to use: should be greater than 0
		///                  Default value is 1.
		///                  This represents the "importance" or "value" of this particular point
		///                  Higher intensity values map to higher colours.
		///                  Intensity is additive: having two points of intensity 1 at the same
		///                  location is identical to having one of intensity 2. </param>
		public WeightedLatLng(LatLng latLng, double intensity)
		{
			mPoint = sProjection.toPoint(latLng);
			if (intensity >= 0)
			{
				mIntensity = intensity;
			}
			else
			{
				mIntensity = DEFAULT_INTENSITY;
			}
		}

		/// <summary>
		/// Constructor that uses default value for intensity
		/// </summary>
		/// <param name="latLng"> LatLng to add to wrapper </param>
		public WeightedLatLng(LatLng latLng) : this(latLng, DEFAULT_INTENSITY)
		{
		}

		public virtual Point Point
		{
			get
			{
				return mPoint;
			}
		}

		public virtual double Intensity
		{
			get
			{
				return mIntensity;
			}
		}

	}

}
#endif