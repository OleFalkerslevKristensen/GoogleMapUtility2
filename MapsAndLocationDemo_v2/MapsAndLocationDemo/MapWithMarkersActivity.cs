namespace MapsAndLocationDemo
{
    using System;

    using Android.App;
    using Android.Content.PM;
    using Android.Gms.Maps;
    using Android.Gms.Maps.Model;
    using Android.OS;
    using Android.Support.V4.App;
    using Android.Widget;
    using System.Collections;
  

    [Activity(Label = "@string/activity_label_mapwithmarkers", ConfigurationChanges=ConfigChanges.Orientation)]
    public class MapWithMarkersActivity : FragmentActivity
    {
        private static readonly LatLng InMaui = new LatLng(20.72110, -156.44776);
        private static readonly LatLng LeaveFromHereToMaui = new LatLng(41.251696, -73.745667); //new LatLng(82.4986, -62.348);
        private static readonly LatLng[] LocationForCustomIconMarkers = new[]
                                                                            {
                                                                                new LatLng(40.741773, -74.004986),
                                                                                new LatLng(41.051696, -73.545667),
                                                                                new LatLng(41.311197, -72.902646)
                                                                            };
        private string _gotoMauiMarkerId;
        private GoogleMap _map;
        private Marker _polarBearMarker;
        private GroundOverlay _polarBearOverlay;

  //      private ClusterManager<MyItem> mClusterManager;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.MapWithOverlayLayout);
            InitMap();
        }

        protected override void OnPause()
        {
            base.OnPause();

            // Pause the GPS - we won't have to worry about showing the 
            // location.
            _map.MyLocationEnabled = false;
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (MapIsSetup())
            {
                // Enable the my-location layer.
                _map.MyLocationEnabled = true;

                AddMonkeyMarkersToMap();
                AddInitialPolarBarToMap();

                // Move the map so that it is showing the markers we added above.
                _map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(LocationForCustomIconMarkers[1], 2));

                // Setup a handler for when the user clicks on a marker.
                _map.MarkerClick += MapOnMarkerClick;
            }
        }

        private void AddInitialPolarBarToMap()
        {
            var markerOptions = new MarkerOptions()
                .SetSnippet("Click me to go on vacation.")
                .SetPosition(LeaveFromHereToMaui)
                .SetTitle("Goto Maui");
            _polarBearMarker = _map.AddMarker(markerOptions);
            _polarBearMarker.ShowInfoWindow();

            _gotoMauiMarkerId = _polarBearMarker.Id;

            PositionPolarBearGroundOverlay(LeaveFromHereToMaui);
        }

        /// <summary>
        ///   Add three markers to the map.
        /// </summary>
        private void AddMonkeyMarkersToMap()
        {
            for (var i = 0; i < LocationForCustomIconMarkers.Length; i++)
            {
                try
                {

                    var icon = BitmapDescriptorFactory.FromResource(Resource.Drawable.monkey);
                    var mapOption = new MarkerOptions()
                        .SetPosition(LocationForCustomIconMarkers[i])
                        .InvokeIcon(icon)
                        .SetSnippet(String.Format("This is marker #{0}.", i))
                        .SetTitle(String.Format("Marker {0}", i));
                    _map.AddMarker(mapOption);
                }
                catch (Exception exc)
                {
                    string test = exc.Message;
                }
            }
        }

        /// <summary>
        ///   All we do here is add a SupportMapFragment to the Activity.
        /// </summary>
        private void InitMap()
        {
            var mapOptions = new GoogleMapOptions()
                .InvokeMapType(GoogleMap.MapTypeSatellite)
                .InvokeZoomControlsEnabled(false)
                .InvokeCompassEnabled(true);

            var fragTx = SupportFragmentManager.BeginTransaction();
            var mapFragment = SupportMapFragment.NewInstance(mapOptions);
            fragTx.Add(Resource.Id.mapWithOverlay, mapFragment, "map");
            fragTx.Commit();
        }

        private bool MapIsSetup()
        {
            if (_map == null)
            {
                var fragment = SupportFragmentManager.FindFragmentByTag("map") as SupportMapFragment;
                if (fragment != null)
                {
                    _map = fragment.Map;
                }
            }
            return _map != null;
        }

        private void MapOnMarkerClick(object sender, GoogleMap.MarkerClickEventArgs markerClickEventArgs)
        {
            var marker = markerClickEventArgs.P0; // TODO [TO201212142221] Need to fix the name of this with MetaData.xml
            if (marker.Id.Equals(_gotoMauiMarkerId))
            {
                PositionPolarBearGroundOverlay(InMaui);
                _map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(InMaui,13));
                _gotoMauiMarkerId = null;
                _polarBearMarker.Remove();
                _polarBearMarker = null;
            }
            else
            {
                Toast.MakeText(this, String.Format("You clicked on Marker ID {0}", marker.Id), ToastLength.Short).Show();
            }
        }

        private void PositionPolarBearGroundOverlay(LatLng position)
        {
            if (_polarBearOverlay == null)
            {
                var image = BitmapDescriptorFactory.FromResource(Resource.Drawable.polarbear);
                var groundOverlayOptions = new GroundOverlayOptions()
                    .Position(position, 150, 200)
                    .InvokeImage(image);
                _polarBearOverlay = _map.AddGroundOverlay(groundOverlayOptions);
            }
            else
            {
                _polarBearOverlay.Position = InMaui;
            }
        }


       

       private void setUpClusterer()
       {
	// Declare a variable for the cluster manager.
 //  	   

	// Position the map.
	    _map.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(51.503186, -0.126446), 10));

	// Initialize the manager with the context and the map.
	// (Activity extends context, so we can pass 'this' in the constructor.)
//	     mClusterManager = new ClusterManager<MyItem>(this, IDictionary);

	// Point the map's listeners at the listeners implemented by the cluster
	// manager.
//	      IDictionary.OnCameraChangeListener = mClusterManager;
//	IDictionary.OnMarkerClickListener = mClusterManager;

	// Add cluster items (markers) to the cluster manager.
	    addItems();
     }

     private void addItems()
     {

	// Set some lat/lng coordinates to start with.
      	double lat = 51.5145160;
     	double lng = -0.1270060;

	// Add ten cluster items in close proximity, for purposes of this example.
/*	    for (int i = 0; i < 10; i++)
	    {
		    double offset = i / 60d;
		   lat = lat + offset;
		   lng = lng + offset;
		   MyItem offsetItem = new MyItem(lat, lng);
		   mClusterManager.addItem(offsetItem);
     	}*/
     } 
  


    }
}
