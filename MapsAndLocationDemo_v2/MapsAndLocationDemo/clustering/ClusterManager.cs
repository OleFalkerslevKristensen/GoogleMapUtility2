using System.Collections.Generic;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MapsAndLocationDemo.utilities;
using MapsAndLocationDemo.clustering.algo;
using Java.Util.Concurrent.Locks;
using com.google.maps.android.clustering.view;
using Com.Google.Maps.Android.Clustering.Algo;
using MapsAndLocationDemo.clustering.view;
using System.Threading;
//using Com.Google.Maps.Android.Clustering.

namespace MapsAndLocationDemo.clustering
{

	

	/// <summary>
	/// Groups many items on a map based on zoom level.
	/// <p/>
	/// ClusterManager should be added to the map as an: <ul> <li><seealso cref="com.google.android.gms.maps.GoogleMap.OnCameraChangeListener"/></li>
	/// <li><seealso cref="com.google.android.gms.maps.GoogleMap.OnMarkerClickListener"/></li> </ul>
	/// </summary>
	public class ClusterManager<T> : GoogleMap.IOnCameraChangeListener, GoogleMap.IOnMarkerClickListener, GoogleMap.IOnInfoWindowClickListener where T : ClusterItem
	{
		private readonly MarkerManager mMarkerManager;
		private readonly MarkerManager.Collection mMarkers;
		private readonly MarkerManager.Collection mClusterMarkers;

		private Algorithm<T> mAlgorithm;
		private readonly ReaderWriterLock mAlgorithmLock = new ReaderWriterLock();
		private ClusterRenderer<T> mRenderer;

		private GoogleMap mMap;
		private CameraPosition mPreviousCameraPosition;
		private ClusterTask mClusterTask;
		private readonly ReaderWriterLock mClusterTaskLock = new ReaderWriterLock();

		private OnClusterItemClickListener<T> mOnClusterItemClickListener;
		private OnClusterInfoWindowClickListener<T> mOnClusterInfoWindowClickListener;
		private OnClusterItemInfoWindowClickListener<T> mOnClusterItemInfoWindowClickListener;
		private OnClusterClickListener<T> mOnClusterClickListener;

		public ClusterManager(Context context, GoogleMap map) : this(context, map, new MarkerManager(map))
		{
		}

		public ClusterManager(Context context, GoogleMap map, MarkerManager markerManager)
		{
			mMap = map;
			mMarkerManager = markerManager;
			mClusterMarkers = markerManager.newCollection();
			mMarkers = markerManager.newCollection();
			mRenderer = new DefaultClusterRenderer<T>(context, map, this);
			mAlgorithm = new PreCachingAlgorithmDecorator<T>(new NonHierarchicalDistanceBasedAlgorithm<T>());
			mClusterTask = new ClusterTask(this);
			mRenderer.onAdd();
		}

		public virtual MarkerManager.Collection MarkerCollection
		{
			get
			{
				return mMarkers;
			}
		}

		public virtual MarkerManager.Collection ClusterMarkerCollection
		{
			get
			{
				return mClusterMarkers;
			}
		}

		public virtual MarkerManager MarkerManager
		{
			get
			{
				return mMarkerManager;
			}
		}

		public virtual ClusterRenderer<T> Renderer
		{
			set
			{
				mRenderer.OnClusterClickListener = null;
				mRenderer.OnClusterItemClickListener = null;
				mClusterMarkers.clear();
				mMarkers.clear();
				mRenderer.onRemove();
				mRenderer = value;
				mRenderer.onAdd();
				mRenderer.OnClusterClickListener = mOnClusterClickListener;
				mRenderer.OnClusterInfoWindowClickListener = mOnClusterInfoWindowClickListener;
				mRenderer.OnClusterItemClickListener = mOnClusterItemClickListener;
				mRenderer.OnClusterItemInfoWindowClickListener = mOnClusterItemInfoWindowClickListener;
				cluster();
			}
		}

		public virtual Algorithm<T> Algorithm
		{
			set
			{
				mAlgorithmLock.AcquireWriterLock(5);
				try
				{
					if (mAlgorithm != null)
					{
						value.addItems(mAlgorithm.Items);
					}
					mAlgorithm = new PreCachingAlgorithmDecorator<T>(value);
				}
				finally
				{
					mAlgorithmLock.ReleaseWriterLock();
				}
				cluster();
			}
		}

		public virtual void clearItems()
		{
			mAlgorithmLock.AcquireWriterLock(5);
			try
			{
				mAlgorithm.clearItems();
			}
			finally
			{
				mAlgorithmLock.ReleaseWriterLock();
			}
		}

		public virtual void addItems(ICollection<T> items)
		{
			mAlgorithmLock.AcquireWriterLock(5);
			try
			{
				mAlgorithm.addItems(items);
			}
			finally
			{
				mAlgorithmLock.ReleaseWriterLock();
			}

		}

		public virtual void addItem(T myItem)
		{
			mAlgorithmLock.AcquireWriterLock(5);
			try
			{
				mAlgorithm.addItem(myItem);
			}
			finally
			{
				mAlgorithmLock.ReleaseWriterLock();
			}
		}

		public virtual void removeItem(T item)
		{
			mAlgorithmLock.AcquireWriterLock(5);
			try
			{
				mAlgorithm.removeItem(item);
			}
			finally
			{
				mAlgorithmLock.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// Force a re-cluster. You may want to call this after adding new item(s).
		/// </summary>
		public virtual void cluster()
		{
			mClusterTaskLock.AcquireWriterLock(5);
			try
			{
				// Attempt to cancel the in-flight request.
				mClusterTask.Cancel(true);
				mClusterTask = new ClusterTask(this);
				mClusterTask.Execute(mMap.CameraPosition.Zoom);
			}
			finally
			{
				mClusterTaskLock.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// Might re-cluster.
		/// </summary>
		/// <param name="cameraPosition"> </param>
		public override void OnCameraChange(CameraPosition cameraPosition)
		{
			if (mRenderer is GoogleMap.IOnCameraChangeListener)
			{
				((GoogleMap.IOnCameraChangeListener) mRenderer).OnCameraChange(cameraPosition);
			}

			// Don't re-compute clusters if the map has just been panned/tilted/rotated.
			CameraPosition position = mMap.CameraPosition;
			if (mPreviousCameraPosition != null && mPreviousCameraPosition.Zoom == position.Zoom)
			{
				return;
			}
			mPreviousCameraPosition = mMap.CameraPosition;

			cluster();
		}

		public override bool OnMarkerClick(Marker marker)
		{
			return MarkerManager.OnMarkerClick(marker);
		}

		public override void OnInfoWindowClick(Marker marker)
		{
			MarkerManager.OnInfoWindowClick(marker);
		}

		/// <summary>
		/// Runs the clustering algorithm in a background thread, then re-paints when results come back.
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private class ClusterTask extends android.os.AsyncTask<Float, Void, java.util.Set<? extends Cluster<T>>>
		private class ClusterTask : AsyncTask<float?, Void, Set<JavaToDotNetGenericWildcard>> where ? : Cluster<T>
		{
			private readonly ClusterManager<T> outerInstance;

			public ClusterTask(ClusterManager<T> outerInstance)
			{
				this.outerInstance = outerInstance;
			}

//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: @Override protected java.util.Set<? extends Cluster<T>> doInBackground(Float... zoom)
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: @Override protected java.util.Set<? extends Cluster<T>> doInBackground(Float... zoom)
			protected internal override Set<?> doInBackground(params float?[] zoom) where ? : Cluster<T>
			{
				outerInstance.mAlgorithmLock.AcquireReaderLock(5);
				try
				{
					return outerInstance.mAlgorithm.getClusters(zoom[0]);
				}
				finally
				{
					outerInstance.mAlgorithmLock.ReleaseReaderLock();
				}
			}

			protected internal override void onPostExecute<T1>(Set<T1> clusters) where T1 : Cluster<T>
			{
				outerInstance.mRenderer.onClustersChanged(clusters);
			}
		}

		/// <summary>
		/// Sets a callback that's invoked when a Cluster is tapped. Note: For this listener to function,
		/// the ClusterManager must be added as a click listener to the map.
		/// </summary>
		public virtual OnClusterClickListener<T> OnClusterClickListener
		{
			set
			{
				mOnClusterClickListener = value;
				mRenderer.OnClusterClickListener = value;
			}
		}

		/// <summary>
		/// Sets a callback that's invoked when a Cluster is tapped. Note: For this listener to function,
		/// the ClusterManager must be added as a info window click listener to the map.
		/// </summary>
		public virtual OnClusterInfoWindowClickListener<T> OnClusterInfoWindowClickListener
		{
			set
			{
				mOnClusterInfoWindowClickListener = value;
				mRenderer.OnClusterInfoWindowClickListener = value;
			}
		}

		/// <summary>
		/// Sets a callback that's invoked when an individual ClusterItem is tapped. Note: For this
		/// listener to function, the ClusterManager must be added as a click listener to the map.
		/// </summary>
		public virtual OnClusterItemClickListener<T> OnClusterItemClickListener
		{
			set
			{
				mOnClusterItemClickListener = value;
				mRenderer.OnClusterItemClickListener = value;
			}
		}

		/// <summary>
		/// Sets a callback that's invoked when an individual ClusterItem's Info Window is tapped. Note: For this
		/// listener to function, the ClusterManager must be added as a info window click listener to the map.
		/// </summary>
		public virtual OnClusterItemInfoWindowClickListener<T> OnClusterItemInfoWindowClickListener
		{
			set
			{
				mOnClusterItemInfoWindowClickListener = value;
				mRenderer.OnClusterItemInfoWindowClickListener = value;
			}
		}

		/// <summary>
		/// Called when a Cluster is clicked.
		/// </summary>
		public interface OnClusterClickListener<T> where T : ClusterItem
		{
			bool onClusterClick(Cluster<T> cluster);
		}

		/// <summary>
		/// Called when a Cluster's Info Window is clicked.
		/// </summary>
		public interface OnClusterInfoWindowClickListener<T> where T : ClusterItem
		{
			void onClusterInfoWindowClick(Cluster<T> cluster);
		}

		/// <summary>
		/// Called when an individual ClusterItem is clicked.
		/// </summary>
		public interface OnClusterItemClickListener<T> where T : ClusterItem
		{
			bool onClusterItemClick(T item);
		}

		/// <summary>
		/// Called when an individual ClusterItem's Info Window is clicked.
		/// </summary>
		public interface OnClusterItemInfoWindowClickListener<T> where T : ClusterItem
		{
			void onClusterItemInfoWindowClick(T item);
		}
	}

}