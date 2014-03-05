
using MapsAndLocationDemo.clustering;

namespace MapsAndLocationDemo.clustering.view
{

	
  
	/// <summary>
	/// Renders clusters.
	/// </summary>
	public interface ClusterRenderer<T> where T : ClusterItem
	{

		/// <summary>
		/// Called when the view needs to be updated because new clusters need to be displayed. </summary>
		/// <param name="clusters"> the clusters to be displayed. </param>
		void onClustersChanged<T1>(Set<T1> clusters) where T1 : com.google.maps.android.clustering.Cluster<T>;

		ClusterManager.OnClusterClickListener<T> OnClusterClickListener {set;}

		ClusterManager.OnClusterInfoWindowClickListener<T> OnClusterInfoWindowClickListener {set;}

		ClusterManager.OnClusterItemClickListener<T> OnClusterItemClickListener {set;}

		ClusterManager.OnClusterItemInfoWindowClickListener<T> OnClusterItemInfoWindowClickListener {set;}

		/// <summary>
		/// Called when the view is added.
		/// </summary>
		void onAdd();

		/// <summary>
		/// Called when the view is removed.
		/// </summary>
		void onRemove();
	}
}