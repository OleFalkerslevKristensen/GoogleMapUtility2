using System.Collections.Generic;

namespace MapsAndLocationDemo.clustering.algo
{


	/// <summary>
	/// Logic for computing clusters
	/// </summary>
	public interface Algorithm<T> where T : ClusterItem
	{
		void addItem(T item);

		void addItems(ICollection<T> items);

		void clearItems();

		void removeItem(T item);

//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: java.util.Set<? extends com.google.maps.android.clustering.Cluster<T>> getClusters(double zoom);
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: java.util.Set<? extends com.google.maps.android.clustering.Cluster<T>> getClusters(double zoom);
//		Set<?> getClusters(double zoom) where ? : Cluster<T>;

        HashSet<Cluster<T>> getClusters(double zoom);

		ICollection<T> Items {get;}
	}
}