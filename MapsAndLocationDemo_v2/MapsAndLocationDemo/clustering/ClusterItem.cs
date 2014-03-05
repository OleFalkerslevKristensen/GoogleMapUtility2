using Android.Gms.Maps.Model;

namespace MapsAndLocationDemo.clustering
{
 
	/// <summary>
	/// ClusterItem represents a marker on the map.
	/// </summary>
	public interface ClusterItem
	{

		/// <summary>
		/// The position of this marker. This must always return the same value.
		/// </summary>
		LatLng Position {get;}
	}
}