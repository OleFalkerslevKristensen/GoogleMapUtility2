#if __ACTIVE__
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Runtime;
using System;

//namespace MapsAndLocationDemo
namespace com.google.maps.android.clustering.ClusterManager
{
   [Register(OnCameraChangeListenerImplementor.IOnCameraChangeListener_JniName, DoNotGenerateAcw = true)]
 	public interface IOnCameraChangeListener : IJavaObject {
   //     [Register("onCameraChange", "([III)V", "GetOnCameraListener:MapsAndLocationDemo.IOnCameraChangeListenerInvoker, SanityTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
       [Register("onCameraChange", "(Lcom/google/android/gms/maps/model/CameraPosition;)V", "GetOnCameraChange_Lcom_google_android_gms_maps_model_CameraPosition_Handler:Android.Gms.Maps.GoogleMap/IOnCameraChangeListenerInvoker, SanityTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
 		void OnCameraChange (CameraPosition position);

    }
   #region IOnCameraChangeListener

   class OnCameraChangeListenerImplementor : Java.Lang.Object, IOnCameraChangeListener
   {
       public const string IOnCameraChangeListener_JniName = "com/google/android/gms/maps/GoogleMap$OnCameraChangeListener";    
                                                                                         
     
       static IntPtr java_class_ref = JNIEnv.FindClass(IOnCameraChangeListener_JniName);

       IntPtr class_ref;

       public OnCameraChangeListenerImplementor(IntPtr handle, JniHandleOwnership transfer)
			: base (handle, transfer)
		{
			IntPtr lref = JNIEnv.GetObjectClass (Handle);
			class_ref = JNIEnv.NewGlobalRef (lref);
			JNIEnv.DeleteLocalRef (lref);
		}

       protected override void Dispose(bool disposing)
       {
           if (this.class_ref != IntPtr.Zero)
               JNIEnv.DeleteGlobalRef(this.class_ref);
           this.class_ref = IntPtr.Zero;
           base.Dispose(disposing);
       }

       protected override Type ThresholdType
       {
           get { return typeof(OnCameraChangeListenerImplementor); }
       }

       protected override IntPtr ThresholdClass
       {
           get { return class_ref; }
       }

       public static IOnCameraChangeListener GetObject(IntPtr handle, JniHandleOwnership transfer)
       {
           return new OnCameraChangeListenerImplementor(handle, transfer);
       }


       IntPtr id_onCameraChange;
       public void OnCameraChange(CameraPosition position)
       {
           if (id_onCameraChange == IntPtr.Zero)
               id_onCameraChange = JNIEnv.GetMethodID(class_ref, "onCameraChange", "([III)V");
           JNIEnv.CallVoidMethod(Handle, id_onCameraChange,
                   new JValue(JNIEnv.ToJniHandle(position)));
       }

#pragma warning disable 0169
       static Delegate cb_onCameraChange;
       static Delegate GetOnCameraChangeHandler()
       {
           if (cb_onCameraChange == null)
               cb_onCameraChange = JNINativeWrapper.CreateDelegate((Action<IntPtr, IntPtr, CameraPosition>)n_OnCameraChange);
           return cb_onCameraChange;
       }

       static void n_OnCameraChange(IntPtr jnienv, IntPtr lrefThis, CameraPosition position)
       {
           IOnCameraChangeListener __this = Java.Lang.Object.GetObject<IOnCameraChangeListener>(lrefThis, JniHandleOwnership.DoNotTransfer);
           var _position = position;
           __this.OnCameraChange((CameraPosition)position);
        
       }
#pragma warning restore 0169

   #endregion

   }
}
#endif