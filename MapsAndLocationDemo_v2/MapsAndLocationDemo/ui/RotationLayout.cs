/*
 * Copyright 2013 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading;
using Android.Graphics;
using System.Collections.ObjectModel;
using Android.Util;

namespace MapsAndLocationDemo.ui
{

	
	/// <summary>
	/// RotationLayout rotates the contents of the layout by multiples of 90 degrees.
	/// <p/>
	/// May not work with padding.
	/// </summary>
	internal class RotationLayout : FrameLayout
	{
		private int mRotation;

		public RotationLayout(Context context) : base(context)
		{
		}

		public RotationLayout(Context context, IAttributeSet attrs) : base(context, attrs)
		{
		}

		public RotationLayout(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (mRotation == 1 || mRotation == 3)
			{
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
				SetMeasuredDimension(MeasuredHeight, MeasuredWidth);
			}
			else
			{
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			}
		}

		/// <param name="degrees"> the rotation, in degrees. </param>
		public virtual int ViewRotation
		{
			set
			{
				mRotation = ((value + 360) % 360) / 90;
			}
		}


        protected override void DispatchDraw(Canvas canvas)
		{
			if (mRotation == 0)
			{
				base.DispatchDraw(canvas);
				return;
			}

			if (mRotation == 1)
			{
				canvas.Translate(Width, 0);
				canvas.Rotate(90, Width / 2, 0);
				canvas.Translate(Height / 2, Width / 2);
			}
			else if (mRotation == 2)
			{
				canvas.Rotate(180, Width / 2, Height / 2);
			}
			else
			{
				canvas.Translate(0, Height);
				canvas.Rotate(270, Width / 2, 0);
				canvas.Translate(Height / 2, -Width / 2);
			}

			base.DispatchDraw(canvas);
		}
	}

}