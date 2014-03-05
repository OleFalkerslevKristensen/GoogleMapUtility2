using System.Collections.Generic;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;

namespace MapsAndLocationDemo.clustering.algo
{

	
	
	/// <summary>
	/// A cluster whose center is determined upon creation.
	/// </summary>
	public class StaticCluster<T> : Cluster<T> where T : ClusterItem
	{
		private readonly LatLng mCenter;
		private readonly IList<T> mItems = new List<T>();

		public StaticCluster(LatLng center)
		{
			mCenter = center;
		}

		public virtual bool add(T t)
		{
			 mItems.Add(t);

             return true;
		}

		public  LatLng Position
		{
			get
			{
				return mCenter;
			}
		}

		public virtual bool remove(T t)
		{
			return mItems.Remove(t);
		}

		public ICollection<T> Items
		{
			get
			{
				return mItems;
			}
		}

		public int Size
		{
			get
			{
				return mItems.Count;
			}
		}

		public override string ToString()
		{
			return "StaticCluster{" + "mCenter=" + mCenter + ", mItems.size=" + mItems.Count + '}';
		}
	}
}