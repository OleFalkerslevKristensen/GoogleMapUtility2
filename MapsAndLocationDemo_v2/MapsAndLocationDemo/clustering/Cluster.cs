using System.Collections.Generic;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;

namespace MapsAndLocationDemo.clustering
{
  // com.google.android.gms.maps.model
   
	/// <summary>
	/// A collection of ClusterItems that are nearby each other.
	/// </summary>
	public interface Cluster<T> where T : ClusterItem
	{
		LatLng Position {get;}

		ICollection<T> Items {get;}

		int Size {get;}
	}
}