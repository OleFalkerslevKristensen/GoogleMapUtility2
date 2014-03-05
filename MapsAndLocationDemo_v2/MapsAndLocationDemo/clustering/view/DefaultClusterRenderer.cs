using System;
using System.Collections.Generic;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Animation;
using Android.Annotation;
using Android.Text.Style;
using Android.Graphics.Color;

using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Gms.Common;
using Com.Google.Maps.Android.Geometry;
using MapsAndLocationDemo.projection;
using MapsAndLocationDemo.ui;
using Android.Graphics.Drawables;
using Com.Google.Maps.Android.UI;
using Android.Graphics.Drawables.Shapes;

namespace  MapsAndLocationDemo.clustering.view
{

	/// <summary>
	/// The default view for a ClusterManager. Markers are animated in and out of clusters.
	/// </summary>
	public class DefaultClusterRenderer<T> : ClusterRenderer<T> where T : ClusterItem
	{
		const int MAX_DISTANCE_AT_ZOOM = 100;
        
        private bool InstanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			mViewModifier = new ViewModifier(this);
		}

		private static readonly bool SHOULD_ANIMATE = true;//Build.VERSION.SDK_INT >= Build.VERSION_CODES.HONEYCOMB;
		private readonly GoogleMap mMap;
		private readonly IconGenerator mIconGenerator;
		private readonly ClusterManager<T> mClusterManager;
		private readonly float mDensity;

		private static readonly int[] BUCKETS = new int[] {10, 20, 50, 100, 200, 500, 1000};
		private ShapeDrawable mColoredCircleBackground;

		/// <summary>
		/// Markers that are currently on the map.
		/// </summary>
		private HashSet<MarkerWithPosition> mMarkers = new HashSet<MarkerWithPosition>();

		/// <summary>
		/// Icons for each bucket.
		/// </summary>
		private SparseArray<BitmapDescriptor> mIcons = new SparseArray<BitmapDescriptor>();

		/// <summary>
		/// Markers for single ClusterItems.
		/// </summary>
		private MarkerCache<T> mMarkerCache = new MarkerCache<T>();

		/// <summary>
		/// If cluster size is less than this size, display individual markers.
		/// </summary>
		private const int MIN_CLUSTER_SIZE = 4;

		/// <summary>
		/// The currently displayed set of clusters.
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private java.util.Set<? extends com.google.maps.android.clustering.Cluster<T>> mClusters;
		private HashSet<Cluster<T>> mClusters;

  
		/// <summary>
		/// Lookup between markers and the associated cluster.
		/// </summary>
		private IDictionary<Marker, Cluster<T>> mMarkerToCluster = new Dictionary<Marker, Cluster<T>>();

		/// <summary>
		/// The target zoom level for the current set of clusters.
		/// </summary>
		private float mZoom;

		private ViewModifier mViewModifier;

		private ClusterManager.OnClusterClickListener<T> mClickListener;
		private ClusterManager.OnClusterInfoWindowClickListener<T> mInfoWindowClickListener;
		private ClusterManager.OnClusterItemClickListener<T> mItemClickListener;
		private ClusterManager.OnClusterItemInfoWindowClickListener<T> mItemInfoWindowClickListener;

		public DefaultClusterRenderer(Context context, GoogleMap map, ClusterManager<T> clusterManager)
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
			mMap = map;
			mDensity = context.Resources.DisplayMetrics.Density;
			mIconGenerator = new IconGenerator(context);
			mIconGenerator.SetContentView(makeSquareTextView(context));//ContentView = makeSquareTextView(context);
			mIconGenerator.SetTextAppearance(Resource.Styleable.ClusterIcon.TextAppearance);//TextAppearance = R.style.ClusterIcon_TextAppearance;
			mIconGenerator.SetBackground(makeClusterBackground());    // Background = makeClusterBackground();
			mClusterManager = clusterManager;
		}

		public override void onAdd()
		{
			mClusterManager.MarkerCollection.OnMarkerClickListener = new OnMarkerClickListenerAnonymousInnerClassHelper(this);

			mClusterManager.MarkerCollection.OnInfoWindowClickListener = new OnInfoWindowClickListenerAnonymousInnerClassHelper(this);

			mClusterManager.ClusterMarkerCollection.OnMarkerClickListener = new OnMarkerClickListenerAnonymousInnerClassHelper2(this);

			mClusterManager.ClusterMarkerCollection.OnInfoWindowClickListener = new OnInfoWindowClickListenerAnonymousInnerClassHelper2(this);
		}

		private class OnMarkerClickListenerAnonymousInnerClassHelper : GoogleMap.IOnMarkerClickListener
		{
			private readonly DefaultClusterRenderer<T> outerInstance;

			public OnMarkerClickListenerAnonymousInnerClassHelper(DefaultClusterRenderer<T> outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public override bool onMarkerClick(Marker marker)
			{
				return outerInstance.mItemClickListener != null && outerInstance.mItemClickListener.onClusterItemClick(outerInstance.mMarkerCache.get(marker));
			}
		}

		private class OnInfoWindowClickListenerAnonymousInnerClassHelper : GoogleMap.IOnInfoWindowClickListener
		{
			private readonly DefaultClusterRenderer<T> outerInstance;

			public OnInfoWindowClickListenerAnonymousInnerClassHelper(DefaultClusterRenderer<T> outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public override void onInfoWindowClick(Marker marker)
			{
				if (outerInstance.mItemInfoWindowClickListener != null)
				{
					outerInstance.mItemInfoWindowClickListener.onClusterItemInfoWindowClick(outerInstance.mMarkerCache.get(marker));
				}
			}
		}

		private class OnMarkerClickListenerAnonymousInnerClassHelper2 : GoogleMap.IOnMarkerClickListener
		{
			private readonly DefaultClusterRenderer<T> outerInstance;

			public OnMarkerClickListenerAnonymousInnerClassHelper2(DefaultClusterRenderer<T> outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public override bool onMarkerClick(Marker marker)
			{
				return outerInstance.mClickListener != null && outerInstance.mClickListener.onClusterClick(outerInstance.mMarkerToCluster[marker]);
			}
		}

		private class OnInfoWindowClickListenerAnonymousInnerClassHelper2 : GoogleMap.IOnInfoWindowClickListener
		{
			private readonly DefaultClusterRenderer<T> outerInstance;

			public OnInfoWindowClickListenerAnonymousInnerClassHelper2(DefaultClusterRenderer<T> outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public override void onInfoWindowClick(Marker marker)
			{
				if (outerInstance.mInfoWindowClickListener != null)
				{
					outerInstance.mInfoWindowClickListener.onClusterInfoWindowClick(outerInstance.mMarkerToCluster[marker]);
				}
			}
		}

		public override void onRemove()
		{
			mClusterManager.MarkerCollection.OnMarkerClickListener = null;
			mClusterManager.ClusterMarkerCollection.OnMarkerClickListener = null;
		}

		private LayerDrawable makeClusterBackground()
		{
	Android.Graphics.Color color = Android.Graphics.Color.ParseColor("#80ffffff"); 
            
            mColoredCircleBackground = new ShapeDrawable(new OvalShape());
			ShapeDrawable outline = new ShapeDrawable(new OvalShape());
	/*		outline.Paint.Color = 0x80ffffff;  Transparent white.  */
            outline.Paint.Color =  Android.Graphics.Color.ParseColor("#80ffffff");
			LayerDrawable background = new LayerDrawable(new Drawable[]{outline, mColoredCircleBackground});
			int strokeWidth = (int)(mDensity * 3);
			background.SetLayerInset(1, strokeWidth, strokeWidth, strokeWidth, strokeWidth);
			return background;
		}

		private SquareTextView makeSquareTextView(Context context)
		{
			SquareTextView squareTextView = new SquareTextView(context);
			ViewGroup.LayoutParams layoutParams = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
			squareTextView.LayoutParameters = layoutParams;
			squareTextView.Id = Resource.Id.text;  
			int twelveDpi = (int)(12 * mDensity);
			squareTextView.SetPadding(twelveDpi, twelveDpi, twelveDpi, twelveDpi);
			return squareTextView;
		}

		private int getColor(int clusterSize)
		{
			const float hueRange = 220;
			const float sizeRange = 300;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float size = Math.min(clusterSize, sizeRange);
			float size = Math.Min(clusterSize, sizeRange);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float hue = (sizeRange - size) * (sizeRange - size) / (sizeRange * sizeRange) * hueRange;
			float hue = (sizeRange - size) * (sizeRange - size) / (sizeRange * sizeRange) * hueRange;
			return Android.Graphics.Color.HSVToColor(new float[]{hue, 1f,.6f});
		}

		protected internal virtual string getClusterText(int bucket)
		{
			if (bucket < BUCKETS[0])
			{
				return Convert.ToString(bucket);
			}
			return Convert.ToString(bucket) + "+";
		}

		/// <summary>
		/// Gets the "bucket" for a particular cluster. By default, uses the number of points within the
		/// cluster, bucketed to some set points.
		/// </summary>
		protected internal virtual int getBucket(Cluster<T> cluster)
		{
			int size = cluster.Size;
			if (size <= BUCKETS[0])
			{
				return size;
			}
			for (int i = 0; i < BUCKETS.Length - 1; i++)
			{
				if (size < BUCKETS[i + 1])
				{
					return BUCKETS[i];
				}
			}
			return BUCKETS[BUCKETS.Length - 1];
		}

		/// <summary>
		/// ViewModifier ensures only one re-rendering of the view occurs at a time, and schedules
		/// re-rendering, which is performed by the RenderTask.
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressLint("HandlerLeak") private class ViewModifier extends android.os.Handler
		private class ViewModifier : Handler
		{
			private readonly DefaultClusterRenderer<T> outerInstance;

			public ViewModifier(DefaultClusterRenderer<T> outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			internal const int RUN_TASK = 0;
			internal const int TASK_FINISHED = 1;
			internal bool mViewModificationInProgress = false;
			internal RenderTask mNextClusters = null;

			public override void handleMessage(Message msg)
			{
				if (msg.What == TASK_FINISHED)
				{
					mViewModificationInProgress = false;
					if (mNextClusters != null)
					{
						// Run the task that was queued up.
						SendEmptyMessage(RUN_TASK);
					}
					return;
				}
				RemoveMessages(RUN_TASK);

				if (mViewModificationInProgress)
				{
					// Busy - wait for the callback.
					return;
				}

				if (mNextClusters == null)
				{
					// Nothing to do.
					return;
				}

				RenderTask renderTask;
				lock (this)
				{
					renderTask = mNextClusters;
					mNextClusters = null;
					mViewModificationInProgress = true;
				}

				renderTask.Callback = new RunnableAnonymousInnerClassHelper(this);
				renderTask.Projection = outerInstance.mMap.Projection;
				renderTask.MapZoom = outerInstance.mMap.CameraPosition.Zoom;
				(new Thread(renderTask)).Start();
			}

			private class RunnableAnonymousInnerClassHelper : Runnable
			{
				private readonly ViewModifier outerInstance;

				public RunnableAnonymousInnerClassHelper(ViewModifier outerInstance)
				{
					this.outerInstance = outerInstance;
				}

				public override void run()
				{
					SendEmptyMessage(TASK_FINISHED);
				}
			}

			public virtual void queue<T1>(Set<T1> clusters) where T1 : Cluster<T>
			{
				lock (this)
				{
					// Overwrite any pending cluster tasks - we don't care about intermediate states.
					mNextClusters = new RenderTask(outerInstance, clusters);
				}
				SendEmptyMessage(RUN_TASK);
			}
		}

		/// <summary>
		/// Determine whether the cluster should be rendered as individual markers or a cluster.
		/// </summary>
		protected internal virtual bool shouldRenderAsCluster(Cluster<T> cluster)
		{
			return cluster.Size > MIN_CLUSTER_SIZE;
		}

		/// <summary>
		/// Transforms the current view (represented by DefaultClusterRenderer.mClusters and DefaultClusterRenderer.mZoom) to a
		/// new zoom level and set of clusters.
		/// <p/>
		/// This must be run off the UI thread. Work is coordinated in the RenderTask, then queued up to
		/// be executed by a MarkerModifier.
		/// <p/>
		/// There are three stages for the render:
		/// <p/>
		/// 1. Markers are added to the map
		/// <p/>
		/// 2. Markers are animated to their final position
		/// <p/>
		/// 3. Any old markers are removed from the map
		/// <p/>
		/// When zooming in, markers are animated out from the nearest existing cluster. When zooming
		/// out, existing clusters are animated to the nearest new cluster.
		/// </summary>
		private class RenderTask : Runnable
		{
			private readonly DefaultClusterRenderer<T> outerInstance;

//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: final java.util.Set<? extends com.google.maps.android.clustering.Cluster<T>> clusters;
			internal readonly Set<?> clusters;
			internal Runnable mCallback;
			internal Projection mProjection;
			internal SphericalMercatorProjection mSphericalMercatorProjection;
			internal float mMapZoom;

			internal RenderTask<T1>(DefaultClusterRenderer<T> outerInstance, Set<T1> clusters) where T1 : com.google.maps.android.clustering.Cluster<T>
			{
				this.outerInstance = outerInstance;
				this.clusters = clusters;
			}

			/// <summary>
			/// A callback to be run when all work has been completed.
			/// </summary>
			/// <param name="callback"> </param>
			public virtual Runnable Callback
			{
				set
				{
					mCallback = value;
				}
			}

			public virtual Projection Projection
			{
				set
				{
					this.mProjection = value;
				}
			}

			public virtual float MapZoom
			{
				set
				{
					this.mMapZoom = value;
					this.mSphericalMercatorProjection = new SphericalMercatorProjection(256 * Math.Pow(2, Math.Min(value, outerInstance.mZoom)));
				}
			}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressLint("NewApi") public void run()
			public virtual void run()
			{
				if (clusters.Equals(outerInstance.mClusters))
				{
					mCallback.run();
					return;
				}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final MarkerModifier markerModifier = new MarkerModifier();
				MarkerModifier markerModifier = new MarkerModifier(outerInstance);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float zoom = mMapZoom;
				float zoom = mMapZoom;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean zoomingIn = zoom > mZoom;
				bool zoomingIn = zoom > outerInstance.mZoom;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float zoomDelta = zoom - mZoom;
				float zoomDelta = zoom - outerInstance.mZoom;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Set<MarkerWithPosition> markersToRemove = mMarkers;
				Set<MarkerWithPosition> markersToRemove = outerInstance.mMarkers;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final com.google.android.gms.maps.model.LatLngBounds visibleBounds = mProjection.getVisibleRegion().latLngBounds;
				LatLngBounds visibleBounds = mProjection.VisibleRegion.latLngBounds;
				// TODO: Add some padding, so that markers can animate in from off-screen.

				// Find all of the existing clusters that are on-screen. These are candidates for
				// markers to animate from.
				IList<Point> existingClustersOnScreen = null;
				if (outerInstance.mClusters != null && SHOULD_ANIMATE)
				{
					existingClustersOnScreen = new List<Point>();
					foreach (Cluster<T> c in outerInstance.mClusters)
					{
						if (outerInstance.shouldRenderAsCluster(c) && visibleBounds.contains(c.Position))
						{
							Point point = mSphericalMercatorProjection.toPoint(c.Position);
							existingClustersOnScreen.Add(point);
						}
					}
				}

				// Create the new markers and animate them to their new positions.
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Set<MarkerWithPosition> newMarkers = new java.util.HashSet<MarkerWithPosition>();
				Set<MarkerWithPosition> newMarkers = new HashSet<MarkerWithPosition>();
				foreach (Cluster<T> c in clusters)
				{
					bool onScreen = visibleBounds.contains(c.Position);
					if (zoomingIn && onScreen && SHOULD_ANIMATE)
					{
						Point point = mSphericalMercatorProjection.toPoint(c.Position);
						Point closest = findClosestCluster(existingClustersOnScreen, point);
						if (closest != null)
						{
							LatLng animateTo = mSphericalMercatorProjection.toLatLng(closest);
							markerModifier.add(true, new CreateMarkerTask(outerInstance, c, newMarkers, animateTo));
						}
						else
						{
							markerModifier.add(true, new CreateMarkerTask(outerInstance, c, newMarkers, null));
						}
					}
					else
					{
						markerModifier.add(onScreen, new CreateMarkerTask(outerInstance, c, newMarkers, null));
					}
				}

				// Wait for all markers to be added.
				markerModifier.waitUntilFree();

				// Don't remove any markers that were just added. This is basically anything that had
				// a hit in the MarkerCache.
				markersToRemove.removeAll(newMarkers);

				// Find all of the new clusters that were added on-screen. These are candidates for
				// markers to animate from.
				IList<Point> newClustersOnScreen = null;
				if (SHOULD_ANIMATE)
				{
					newClustersOnScreen = new List<Point>();
					foreach (Cluster<T> c in clusters)
					{
						if (outerInstance.shouldRenderAsCluster(c) && visibleBounds.contains(c.Position))
						{
							Point p = mSphericalMercatorProjection.toPoint(c.Position);
							newClustersOnScreen.Add(p);
						}
					}
				}

				// Remove the old markers, animating them into clusters if zooming out.
				foreach (MarkerWithPosition marker in markersToRemove)
				{
					bool onScreen = visibleBounds.contains(marker.position);
					// Don't animate when zooming out more than 3 zoom levels.
					// TODO: drop animation based on speed of device & number of markers to animate.
					if (!zoomingIn && zoomDelta > -3 && onScreen && SHOULD_ANIMATE)
					{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final com.google.maps.android.geometry.Point point = mSphericalMercatorProjection.toPoint(marker.position);
						Point point = mSphericalMercatorProjection.toPoint(marker.position);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final com.google.maps.android.geometry.Point closest = findClosestCluster(newClustersOnScreen, point);
						Point closest = findClosestCluster(newClustersOnScreen, point);
						if (closest != null)
						{
							LatLng animateTo = mSphericalMercatorProjection.toLatLng(closest);
							markerModifier.animateThenRemove(marker, marker.position, animateTo);
						}
						else
						{
							markerModifier.remove(true, marker.marker);
						}
					}
					else
					{
						markerModifier.remove(onScreen, marker.marker);
					}
				}

				markerModifier.waitUntilFree();

				outerInstance.mMarkers = newMarkers;
				outerInstance.mClusters = clusters;
				outerInstance.mZoom = zoom;

				mCallback.run();
			}
		}

		public override void onClustersChanged<T1>(Set<T1> clusters) where T1 : com.google.maps.android.clustering.Cluster<T>
		{
			mViewModifier.queue(clusters);
		}

		public override ClusterManager.OnClusterClickListener<T> OnClusterClickListener
		{
			set
			{
				mClickListener = value;
			}
		}

		public override ClusterManager.OnClusterInfoWindowClickListener<T> OnClusterInfoWindowClickListener
		{
			set
			{
				mInfoWindowClickListener = value;
			}
		}

		public override ClusterManager.OnClusterItemClickListener<T> OnClusterItemClickListener
		{
			set
			{
				mItemClickListener = value;
			}
		}

		public override ClusterManager.OnClusterItemInfoWindowClickListener<T> OnClusterItemInfoWindowClickListener
		{
			set
			{
				mItemInfoWindowClickListener = value;
			}
		}

		private static double distanceSquared(Point a, Point b)
		{
			return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y);
		}

		private static Point findClosestCluster(IList<Point> markers, Point point)
		{
			if (markers == null || markers.Count == 0)
			{
				return null;
			}

			// TODO: make this configurable.
			double minDistSquared = MAX_DISTANCE_AT_ZOOM * MAX_DISTANCE_AT_ZOOM;
			Point closest = null;
			foreach (Point candidate in markers)
			{
				double dist = distanceSquared(candidate, point);
				if (dist < minDistSquared)
				{
					closest = candidate;
					minDistSquared = dist;
				}
			}
			return closest;
		}

		/// <summary>
		/// Handles all markerWithPosition manipulations on the map. Work (such as adding, removing, or
		/// animating a markerWithPosition) is performed while trying not to block the rest of the app's
		/// UI.
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressLint("HandlerLeak") private class MarkerModifier extends android.os.Handler implements android.os.MessageQueue.IdleHandler
		private class MarkerModifier : Handler, MessageQueue.IdleHandler
		{
			internal bool InstanceFieldsInitialized = false;

			internal virtual void InitializeInstanceFields()
			{
				busyCondition = @lock.newCondition();
			}

			private readonly DefaultClusterRenderer<T> outerInstance;

			internal const int BLANK = 0;

			internal readonly Lock @lock = new ReentrantLock();
			internal Condition busyCondition;

			internal LinkedList<CreateMarkerTask> mCreateMarkerTasks = new LinkedList<CreateMarkerTask>();
			internal LinkedList<CreateMarkerTask> mOnScreenCreateMarkerTasks = new LinkedList<CreateMarkerTask>();
			internal LinkedList<Marker> mRemoveMarkerTasks = new LinkedList<Marker>();
			internal LinkedList<Marker> mOnScreenRemoveMarkerTasks = new LinkedList<Marker>();
			internal LinkedList<AnimationTask> mAnimationTasks = new LinkedList<AnimationTask>();

			/// <summary>
			/// Whether the idle listener has been added to the UI thread's MessageQueue.
			/// </summary>
			internal bool mListenerAdded;

			internal MarkerModifier(DefaultClusterRenderer<T> outerInstance) : base(Looper.MainLooper)
			{
				this.outerInstance = outerInstance;

				if (!InstanceFieldsInitialized)
				{
					InitializeInstanceFields();
					InstanceFieldsInitialized = true;
				}
			}

			/// <summary>
			/// Creates markers for a cluster some time in the future.
			/// </summary>
			/// <param name="priority"> whether this operation should have priority. </param>
			public virtual void add(bool priority, CreateMarkerTask c)
			{
				@lock.@lock();
				sendEmptyMessage(BLANK);
				if (priority)
				{
					mOnScreenCreateMarkerTasks.AddLast(c);
				}
				else
				{
					mCreateMarkerTasks.AddLast(c);
				}
				@lock.unlock();
			}

			/// <summary>
			/// Removes a markerWithPosition some time in the future.
			/// </summary>
			/// <param name="priority"> whether this operation should have priority. </param>
			/// <param name="m">        the markerWithPosition to remove. </param>
			public virtual void remove(bool priority, Marker m)
			{
				@lock.@lock();
				sendEmptyMessage(BLANK);
				if (priority)
				{
					mOnScreenRemoveMarkerTasks.AddLast(m);
				}
				else
				{
					mRemoveMarkerTasks.AddLast(m);
				}
				@lock.unlock();
			}

			/// <summary>
			/// Animates a markerWithPosition some time in the future.
			/// </summary>
			/// <param name="marker"> the markerWithPosition to animate. </param>
			/// <param name="from">   the position to animate from. </param>
			/// <param name="to">     the position to animate to. </param>
			public virtual void animate(MarkerWithPosition marker, LatLng from, LatLng to)
			{
				@lock.@lock();
				mAnimationTasks.AddLast(new AnimationTask(outerInstance, marker, from, to));
				@lock.unlock();
			}

			/// <summary>
			/// Animates a markerWithPosition some time in the future, and removes it when the animation
			/// is complete.
			/// </summary>
			/// <param name="marker"> the markerWithPosition to animate. </param>
			/// <param name="from">   the position to animate from. </param>
			/// <param name="to">     the position to animate to. </param>
			public virtual void animateThenRemove(MarkerWithPosition marker, LatLng from, LatLng to)
			{
				@lock.@lock();
				AnimationTask animationTask = new AnimationTask(outerInstance, marker, from, to);
				animationTask.removeOnAnimationComplete(outerInstance.mClusterManager.MarkerManager);
				mAnimationTasks.AddLast(animationTask);
				@lock.unlock();
			}

			public override void handleMessage(Message msg)
			{
				if (!mListenerAdded)
				{
					Looper.myQueue().addIdleHandler(this);
					mListenerAdded = true;
				}
				removeMessages(BLANK);

				@lock.@lock();
				try
				{

					// Perform up to 10 tasks at once.
					// Consider only performing 10 remove tasks, not adds and animations.
					// Removes are relatively slow and are much better when batched.
					for (int i = 0; i < 10; i++)
					{
						performNextTask();
					}

					if (!Busy)
					{
						mListenerAdded = false;
						Looper.myQueue().removeIdleHandler(this);
						// Signal any other threads that are waiting.
						busyCondition.signalAll();
					}
					else
					{
						// Sometimes the idle queue may not be called - schedule up some work regardless
						// of whether the UI thread is busy or not.
						// TODO: try to remove this.
						sendEmptyMessageDelayed(BLANK, 10);
					}
				}
				finally
				{
					@lock.unlock();
				}
			}

			/// <summary>
			/// Perform the next task. Prioritise any on-screen work.
			/// </summary>
			internal virtual void performNextTask()
			{
				if (mOnScreenRemoveMarkerTasks.Count > 0)
				{
					removeMarker(mOnScreenRemoveMarkerTasks.RemoveFirst());
				}
				else if (mAnimationTasks.Count > 0)
				{
					mAnimationTasks.RemoveFirst().perform();
				}
				else if (mOnScreenCreateMarkerTasks.Count > 0)
				{
					mOnScreenCreateMarkerTasks.RemoveFirst().perform(this);
				}
				else if (mCreateMarkerTasks.Count > 0)
				{
					mCreateMarkerTasks.RemoveFirst().perform(this);
				}
				else if (mRemoveMarkerTasks.Count > 0)
				{
					removeMarker(mRemoveMarkerTasks.RemoveFirst());
				}
			}

			/*internal virtual*/ public  void removeMarker(Marker m)
			{
				outerInstance.mMarkerCache.remove(m);
				outerInstance.mMarkerToCluster.Remove(m);
				outerInstance.mClusterManager.MarkerManager.remove(m);
			}

			/// <returns> true if there is still work to be processed. </returns>
			public virtual bool Busy
			{
				get
				{
					return !(mCreateMarkerTasks.Count == 0 && mOnScreenCreateMarkerTasks.Count == 0 && mOnScreenRemoveMarkerTasks.Count == 0 && mRemoveMarkerTasks.Count == 0 && mAnimationTasks.Count == 0);
				}
			}

			/// <summary>
			/// Blocks the calling thread until all work has been processed.
			/// </summary>
			public virtual void waitUntilFree()
			{
				while (Busy)
				{
					// Sometimes the idle queue may not be called - schedule up some work regardless
					// of whether the UI thread is busy or not.
					// TODO: try to remove this.
					sendEmptyMessage(BLANK);
					@lock.@lock();
					try
					{
						if (Busy)
						{
							busyCondition.@await();
						}
					}
					catch (InterruptedException e)
					{
						throw new Exception(e);
					}
					finally
					{
						@lock.unlock();
					}
				}
			}

			public override bool queueIdle()
			{
				// When the UI is not busy, schedule some work.
				sendEmptyMessage(BLANK);
				return true;
			}
		}

		/// <summary>
		/// A cache of markers representing individual ClusterItems.
		/// </summary>
		private class MarkerCache<T>
		{
			internal IDictionary<T, Marker> mCache = new Dictionary<T, Marker>();
			internal IDictionary<Marker, T> mCacheReverse = new Dictionary<Marker, T>();

			public virtual Marker get(T item)
			{
				return mCache[item];
			}

			public virtual T get(Marker m)
			{
				return mCacheReverse[m];
			}

			public virtual void put(T item, Marker m)
			{
				mCache[item] = m;
				mCacheReverse[m] = item;
			}

			public virtual void remove(Marker m)
			{
				T item = mCacheReverse[m];
				mCacheReverse.Remove(m);
				mCache.Remove(item);
			}
		}

		/// <summary>
		/// Called before the marker for a ClusterItem is added to the map.
		/// </summary>
		protected internal virtual void onBeforeClusterItemRendered(T item, MarkerOptions markerOptions)
		{
		}

		/// <summary>
		/// Called before the marker for a Cluster is added to the map.
		/// The default implementation draws a circle with a rough count of the number of items.
		/// </summary>
		protected internal virtual void onBeforeClusterRendered(Cluster<T> cluster, MarkerOptions markerOptions)
		{
			int bucket = getBucket(cluster);
			BitmapDescriptor descriptor = mIcons.get(bucket);
			if (descriptor == null)
			{
				mColoredCircleBackground.Paint.Color = getColor(bucket);
				descriptor = BitmapDescriptorFactory.fromBitmap(mIconGenerator.makeIcon(getClusterText(bucket)));
				mIcons.put(bucket, descriptor);
			}
			// TODO: consider adding anchor(.5, .5) (Individual markers will overlap more often)
			markerOptions.icon(descriptor);
		}

		/// <summary>
		/// Called after the marker for a Cluster has been added to the map.
		/// </summary>
		protected internal virtual void onClusterRendered(Cluster<T> cluster, Marker marker)
		{
		}

		/// <summary>
		/// Called after the marker for a ClusterItem has been added to the map.
		/// </summary>
		protected internal virtual void onClusterItemRendered(T clusterItem, Marker marker)
		{
		}

		/// <summary>
		/// Creates markerWithPosition(s) for a particular cluster, animating it if necessary.
		/// </summary>
		private class CreateMarkerTask
		{
			private readonly DefaultClusterRenderer<T> outerInstance;

			internal readonly Cluster<T> cluster;
			internal readonly HashSet<MarkerWithPosition> newMarkers;
			internal readonly LatLng animateFrom;

			/// <param name="c">            the cluster to render. </param>
			/// <param name="markersAdded"> a collection of markers to append any created markers. </param>
			/// <param name="animateFrom">  the location to animate the markerWithPosition from, or null if no
			///                     animation is required. </param>
			public CreateMarkerTask(DefaultClusterRenderer<T> outerInstance, Cluster<T> c, HashSet<MarkerWithPosition> markersAdded, LatLng animateFrom)
			{
				this.outerInstance = outerInstance;
				this.cluster = c;
				this.newMarkers = markersAdded;
				this.animateFrom = animateFrom;
			}

			internal virtual void perform(MarkerModifier markerModifier)
			{
				// Don't show small clusters. Render the markers inside, instead.
				if (!outerInstance.shouldRenderAsCluster(cluster))
				{
					foreach (T item in cluster.Items)
					{
						Marker marker = outerInstance.mMarkerCache.get(item);
						MarkerWithPosition markerWithPosition;
						if (marker == null)
						{
							MarkerOptions markerOptions = new MarkerOptions();
							if (animateFrom != null)
							{
								markerOptions.position(animateFrom);
							}
							else
							{
								markerOptions.position(item.Position);
							}
							outerInstance.onBeforeClusterItemRendered(item, markerOptions);
							marker = outerInstance.mClusterManager.MarkerCollection.addMarker(markerOptions);
							markerWithPosition = new MarkerWithPosition(marker);
							outerInstance.mMarkerCache.put(item, marker);
							if (animateFrom != null)
							{
								markerModifier.animate(markerWithPosition, animateFrom, item.Position);
							}
						}
						else
						{
							markerWithPosition = new MarkerWithPosition(marker);
						}
						outerInstance.onClusterItemRendered(item, marker);
						newMarkers.add(markerWithPosition);
					}
					return;
				}

				MarkerOptions markerOptions = (new MarkerOptions()).position(animateFrom == null ? cluster.Position : animateFrom);

				outerInstance.onBeforeClusterRendered(cluster, markerOptions);

				Marker marker = outerInstance.mClusterManager.ClusterMarkerCollection.addMarker(markerOptions);
				outerInstance.mMarkerToCluster[marker] = cluster;
				MarkerWithPosition markerWithPosition = new MarkerWithPosition(marker);
				if (animateFrom != null)
				{
					markerModifier.animate(markerWithPosition, animateFrom, cluster.Position);
				}
				outerInstance.onClusterRendered(cluster, marker);
				newMarkers.add(markerWithPosition);
			}
		}

		/// <summary>
		/// A Marker and its position. Marker.getPosition() must be called from the UI thread, so this
		/// object allows lookup from other threads.
		/// </summary>
		private class MarkerWithPosition
		{
			internal readonly Marker marker;
			internal LatLng position;

			internal MarkerWithPosition(Marker marker)
			{
				this.marker = marker;
				position = marker.Position;
			}

			public override bool Equals(object other)
			{
				if (other is MarkerWithPosition)
				{
					return marker.Equals(((MarkerWithPosition) other).marker);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return marker.GetHashCode();
			}
		}

  
	    private TimeInterpolator ANIMATION_INTERP = new DecelerateInterpolator();

		/// <summary>
		/// Animates a markerWithPosition from one position to another. TODO: improve performance for
		/// slow devices (e.g. Nexus S).
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @TargetApi(Build.VERSION_CODES.HONEYCOMB_MR1) private class AnimationTask extends android.animation.AnimatorListenerAdapter implements android.animation.ValueAnimator.AnimatorUpdateListener
		public class AnimationTask : AnimatorListenerAdapter, ValueAnimator.AnimatorUpdateListener
		{
			private readonly DefaultClusterRenderer<T> outerInstance;

			internal readonly MarkerWithPosition markerWithPosition;
			internal readonly Marker marker;
			internal readonly LatLng from;
			internal readonly LatLng to;
			internal bool mRemoveOnComplete;
			internal MarkerManager mMarkerManager;

			internal AnimationTask(DefaultClusterRenderer<T> outerInstance, MarkerWithPosition markerWithPosition, LatLng from, LatLng to)
			{
				this.outerInstance = outerInstance;
				this.markerWithPosition = markerWithPosition;
				this.marker = markerWithPosition.marker;
				this.from = from;
				this.to = to;
			}

			public virtual void perform()
			{
				ValueAnimator valueAnimator = ValueAnimator.ofFloat(0, 1);
				valueAnimator.Interpolator = new TimeInterpolator();//ANIMATION_INTERP;
				valueAnimator.addUpdateListener(this);
				valueAnimator.addListener(this);
				valueAnimator.start();
			}

			public override void onAnimationEnd(Animator animation)
			{
				if (mRemoveOnComplete)
				{
					outerInstance.mMarkerCache.remove(marker);
					outerInstance.mMarkerToCluster.Remove(marker);
					mMarkerManager.remove(marker);
				}
				markerWithPosition.position = to;
			}

			public virtual void removeOnAnimationComplete(MarkerManager markerManager)
			{
				mMarkerManager = markerManager;
				mRemoveOnComplete = true;
			}

			public override void onAnimationUpdate(ValueAnimator valueAnimator)
			{
				float fraction = valueAnimator.AnimatedFraction;
				double lat = (to.latitude - from.latitude) * fraction + from.latitude;
				double lngDelta = to.longitude - from.longitude;

				// Take the shortest path across the 180th meridian.
				if (Math.Abs(lngDelta) > 180)
				{
					lngDelta -= Math.Sign(lngDelta) * 360;
				}
				double lng = lngDelta * fraction + from.longitude;
				LatLng position = new LatLng(lat, lng);
				marker.Position = position;
			}
		}
	}
}