using System;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Com.Google.Maps.Android.Projection;
using MapsAndLocationDemo.utilities;

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

namespace MapsAndLocationDemo.projection
{

	
	public class SphericalMercatorProjection
	{
		internal readonly double mWorldWidth;

        MathUtil math = new MathUtil();

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public SphericalMercatorProjection(final double worldWidth)
		public SphericalMercatorProjection(double worldWidth)
		{
			mWorldWidth = worldWidth;
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("deprecation") public Point toPoint(final com.google.android.gms.maps.model.LatLng latLng)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public virtual Point toPoint(LatLng latLng)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double x = latLng.longitude / 360 +.5;
			double x = latLng.Longitude / 360 + .5;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double siny = Math.sin(Math.toRadians(latLng.latitude));
			double siny = Math.Sin(math.ToRadians(latLng.Latitude));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double y = 0.5 * Math.log((1 + siny) / (1 - siny)) / -(2 * Math.PI) +.5;
			double y = 0.5 * Math.Log((1 + siny) / (1 - siny)) / -(2 * Math.PI) + .5;

			return new Point(x * mWorldWidth, y * mWorldWidth);
		}

		public virtual LatLng toLatLng(Point point)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double x = point.x / mWorldWidth - 0.5;
			double x = point.X / mWorldWidth - 0.5;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double lng = x * 360;
			double lng = x * 360;

			double y = .5 - (point.Y / mWorldWidth);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double lat = 90 - Math.toDegrees(Math.atan(Math.exp(-y * 2 * Math.PI)) * 2);
			double lat = 90 - math.ToDegrees(Math.Atan(Math.Exp(-y * 2 * Math.PI)) * 2);

			return new LatLng(lat, lng);
		}
	}

}