using System.Collections.Generic;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Views;


namespace MapsAndLocationDemo.utilities
{


	/// <summary>
	/// Keeps track of collections of markers on the map. Delegates all Marker-related events to each
	/// collection's individually managed listeners.
	/// <p/>
	/// All marker operations (adds and removes) should occur via its collection class. That is, don't
	/// add a marker via a collection, then remove it via Marker.remove()
	/// </summary>
    public class MarkerManager : Java.Lang.Object, GoogleMap.IOnInfoWindowClickListener, GoogleMap.IOnMarkerClickListener, GoogleMap.IOnMarkerDragListener, GoogleMap.IInfoWindowAdapter
	{
		private readonly GoogleMap mMap;

		private readonly IDictionary<string, Collection> mNamedCollections = new Dictionary<string, Collection>();
		private readonly IDictionary<Marker, Collection> mAllMarkers = new Dictionary<Marker, Collection>();

		public MarkerManager(GoogleMap map)
		{
			this.mMap = map;
            this.mMap.SetOnMarkerClickListener(this);
            this.mMap.MarkerClick += mMap_MarkerClick;


		}

        void mMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            throw new System.NotImplementedException();
        }

		public virtual Collection newCollection()
		{
			return new Collection(this);
		}

		/// <summary>
		/// Create a new named collection, which can later be looked up by <seealso cref="#getCollection(String)"/> </summary>
		/// <param name="id"> a unique id for this collection. </param>
		public virtual Collection newCollection(string id)
		{
			if (mNamedCollections[id] != null)
			{
				throw new System.ArgumentException("collection id is not unique: " + id);
			}
			Collection collection = new Collection(this);
			mNamedCollections[id] = collection;
			return collection;
		}

		/// <summary>
		/// Gets a named collection that was created by <seealso cref="#newCollection(String)"/> </summary>
		/// <param name="id"> the unique id for this collection. </param>
		public virtual Collection getCollection(string id)
		{
			return mNamedCollections[id];
		}

		public View GetInfoWindow(Marker marker)
		{
			Collection collection = mAllMarkers[marker];
			if (collection != null && collection.mInfoWindowAdapter != null)
			{
				return collection.mInfoWindowAdapter.GetInfoWindow(marker);
			}
			return null;
		}

		public View GetInfoContents(Marker marker)
		{
			Collection collection = mAllMarkers[marker];
			if (collection != null && collection.mInfoWindowAdapter != null)
			{
				return collection.mInfoWindowAdapter.GetInfoContents(marker);
			}
			return null;
		}

		public void OnInfoWindowClick(Marker marker)
		{
			Collection collection = mAllMarkers[marker];
			if (collection != null && collection.mInfoWindowClickListener != null)
			{
				collection.mInfoWindowClickListener.OnInfoWindowClick(marker);
			}
		}

		public bool OnMarkerClick(Marker marker)
		{
			Collection collection = mAllMarkers[marker];
			if (collection != null && collection.mMarkerClickListener != null)
			{
				return collection.mMarkerClickListener.OnMarkerClick(marker);
			}
			return false;
		}

		public void OnMarkerDragStart(Marker marker)
		{
			Collection collection = mAllMarkers[marker];
			if (collection != null && collection.mMarkerDragListener != null)
			{
				collection.mMarkerDragListener.OnMarkerDragStart(marker);
			}
		}

		public void OnMarkerDrag(Marker marker)
		{
			Collection collection = mAllMarkers[marker];
			if (collection != null && collection.mMarkerDragListener != null)
			{
				collection.mMarkerDragListener.OnMarkerDrag(marker);
			}
		}

		public void OnMarkerDragEnd(Marker marker)
		{
			Collection collection = mAllMarkers[marker];
			if (collection != null && collection.mMarkerDragListener != null)
			{
				collection.mMarkerDragListener.OnMarkerDragEnd(marker);
			}
		}

		/// <summary>
		/// Removes a marker from its collection.
		/// </summary>
		/// <param name="marker"> the marker to remove. </param>
		/// <returns> true if the marker was removed. </returns>
		public virtual bool remove(Marker marker)
		{
			Collection collection = mAllMarkers[marker];
			return collection != null && collection.remove(marker);
		}

		public class Collection
		{
			private readonly MarkerManager outerInstance;

            internal readonly HashSet<Marker> mMarkers = new HashSet<Marker>();
			internal GoogleMap.IOnInfoWindowClickListener mInfoWindowClickListener;
			internal GoogleMap.IOnMarkerClickListener mMarkerClickListener;
			internal GoogleMap.IOnMarkerDragListener mMarkerDragListener;
			internal GoogleMap.IInfoWindowAdapter mInfoWindowAdapter;

			public Collection(MarkerManager outerInstance)
			{
				this.outerInstance = outerInstance;
			}

  //          private readonly IDictionary<Marker, Collection> mAllMarkers = new Dictionary<Marker, Collection>();

			public virtual Marker addMarker(MarkerOptions opts)
			{
				Marker marker = outerInstance.mMap.AddMarker(opts);
				mMarkers.Add(marker);
//				outerInstance.mAllMarkers[marker] = Collection.this;
				return marker;
			}

			public virtual bool remove(Marker marker)
			{
				if (mMarkers.Remove(marker))
				{
					outerInstance.mAllMarkers.Remove(marker);
					marker.Remove();
					return true;
				}
				return false;
			}

			public virtual void clear()
			{
				foreach (Marker marker in mMarkers)
				{
					marker.Remove();
					outerInstance.mAllMarkers.Remove(marker);
				}
				mMarkers.Clear();
			}

		//	public virtual ICollection<Marker> Markers
            public virtual HashSet<Marker> Markers
			{
				get
				{
		//			return Collections.unmodifiableCollection(mMarkers);
                    return mMarkers;
				}
			}

			public virtual GoogleMap.IOnInfoWindowClickListener OnInfoWindowClickListener
			{
				set
				{
					mInfoWindowClickListener = value;
				}
			}

			public virtual GoogleMap.IOnMarkerClickListener OnMarkerClickListener
			{
				set
				{
					mMarkerClickListener = value;
				}
			}

			public virtual GoogleMap.IOnMarkerDragListener OnMarkerDragListener
			{
				set
				{
					mMarkerDragListener = value;
				}
			}

			public virtual GoogleMap.IInfoWindowAdapter OnInfoWindowAdapter
			{
				set
				{
					mInfoWindowAdapter = value;
				}
			}
		}
	}

}