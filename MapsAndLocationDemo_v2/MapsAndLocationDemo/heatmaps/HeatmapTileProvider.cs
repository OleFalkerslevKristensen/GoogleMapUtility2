
#if __ACTIVE__
using System;
using System.Collections.Generic;

using Android.Gms.Common;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Com.Google.Maps.Android.Heatmaps;
using com.google.maps.android.heatmaps;
using Com.Google.Maps.Android.Geometry;
using Java.Lang;
using Android.Graphics;
using Java.IO;
using System.IO;
using Com.Google.Maps.Android.Quadtree;
using MapsAndLocationDemo.utilities;

/*
 * Copyright 2014 Google Inc.
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



namespace MapsAndLocationDemo.heatmaps
{

	
	/// <summary>
	/// Tile provider that creates heatmap tiles.
	/// </summary>
	public class HeatmapTileProvider : TileProvider
	{
        MathUtil math = new MathUtil();
		/// <summary>
		/// Default radius for convolution
		/// </summary>
		public const int DEFAULT_RADIUS = 20;

		/// <summary>
		/// Default opacity of heatmap overlay
		/// </summary>
		public const double DEFAULT_OPACITY = 0.7;

		/// <summary>
		/// Colors for default gradient.
		/// Array of colors, represented by ints.
		/// </summary>
		private static readonly int[] DEFAULT_GRADIENT_COLORS = new int[] {Color.Rgb(102, 225, 0), Color.Rgb(255, 0, 0)};

		/// <summary>
		/// Starting fractions for default gradient.
		/// This defines which percentages the above colors represent.
		/// These should be a sorted array of floats in the interval [0, 1].
		/// </summary>
		private static readonly float[] DEFAULT_GRADIENT_START_POINTS = new float[] {0.2f, 1f};

		/// <summary>
		/// Default gradient for heatmap.
		/// </summary>
		public static readonly Gradient DEFAULT_GRADIENT = new Gradient(DEFAULT_GRADIENT_COLORS, DEFAULT_GRADIENT_START_POINTS);

		/// <summary>
		/// Size of the world (arbitrary).
		/// Used to measure distances relative to the total world size.
		/// Package access for WeightedLatLng.
		/// </summary>
		internal const double WORLD_WIDTH = 1;

		/// <summary>
		/// Tile dimension, in pixels.
		/// </summary>
		private const int TILE_DIM = 512;

		/// <summary>
		/// Assumed screen size (pixels)
		/// </summary>
		private const int SCREEN_SIZE = 1280;

		/// <summary>
		/// Default (and minimum possible) minimum zoom level at which to calculate maximum intensities
		/// </summary>
		private const int DEFAULT_MIN_ZOOM = 5;

		/// <summary>
		/// Default (and maximum possible) maximum zoom level at which to calculate maximum intensities
		/// </summary>
		private const int DEFAULT_MAX_ZOOM = 11;

		/// <summary>
		/// Maximum zoom level possible on a map.
		/// </summary>
		private const int MAX_ZOOM_LEVEL = 22;

		/// <summary>
		/// Minimum radius value.
		/// </summary>
		private const int MIN_RADIUS = 10;

		/// <summary>
		/// Maximum radius value.
		/// </summary>
		private const int MAX_RADIUS = 50;

		/// <summary>
		/// Quad tree of all the points to display in the heatmap
		/// </summary>
		private PointQuadTree<WeightedLatLng> mTree;
        

		/// <summary>
		/// Collection of all the data.
		/// </summary>
		private ICollection<WeightedLatLng> mData;

		/// <summary>
		/// Bounds of the quad tree
		/// </summary>
		private Bounds mBounds;

		/// <summary>
		/// Heatmap point radius.
		/// </summary>
		private int mRadius;

		/// <summary>
		/// Gradient of the color map
		/// </summary>
		private Gradient mGradient;

		/// <summary>
		/// Color map to use to color tiles
		/// </summary>
		private int[] mColorMap;

		/// <summary>
		/// Kernel to use for convolution
		/// </summary>
		private double[] mKernel;

		/// <summary>
		/// Opacity of the overall heatmap overlay [0...1]
		/// </summary>
		private double mOpacity;

		/// <summary>
		/// Maximum intensity estimates for heatmap
		/// </summary>
		private double[] mMaxIntensity;

		/// <summary>
		/// Builder class for the HeatmapTileProvider.
		/// </summary>
		public class Builder
		{
			// Required parameters - not final, as there are 2 ways to set it
			internal ICollection<WeightedLatLng> data_Renamed;

			// Optional, initialised to default values
			internal int radius_Renamed = DEFAULT_RADIUS;
			internal Gradient gradient_Renamed = DEFAULT_GRADIENT;
			internal double opacity_Renamed = DEFAULT_OPACITY;

			/// <summary>
			/// Constructor for builder.
			/// No required parameters here, but user must call either data() or weightedData().
			/// </summary>
			public Builder()
			{
			}

			/// <summary>
			/// Setter for data in builder. Must call this or weightedData
			/// </summary>
			/// <param name="val"> Collection of LatLngs to put into quadtree.
			///            Should be non-empty. </param>
			/// <returns> updated builder object </returns>
			public virtual Builder data(ICollection<LatLng> val)
			{
				return weightedData(wrapData(val));
			}

			/// <summary>
			/// Setter for data in builder. Must call this or data
			/// </summary>
			/// <param name="val"> Collection of WeightedLatLngs to put into quadtree.
			///            Should be non-empty. </param>
			/// <returns> updated builder object </returns>
			public virtual Builder weightedData(ICollection<WeightedLatLng> val)
			{
				this.data_Renamed = val;

				// Check that points is non empty
				if (this.data_Renamed.Count == 0)
				{
					throw new System.ArgumentException("No input points.");
				}
				return this;
			}

			/// <summary>
			/// Setter for radius in builder
			/// </summary>
			/// <param name="val"> Radius of convolution to use, in terms of pixels.
			///            Must be within minimum and maximum values of 10 to 50 inclusive. </param>
			/// <returns> updated builder object </returns>
			public virtual Builder radius(int val)
			{
				radius_Renamed = val;
				// Check that radius is within bounds.
				if (radius_Renamed < MIN_RADIUS || radius_Renamed > MAX_RADIUS)
				{
					throw new System.ArgumentException("Radius not within bounds.");
				}
				return this;
			}

			/// <summary>
			/// Setter for gradient in builder
			/// </summary>
			/// <param name="val"> Gradient to color heatmap with. </param>
			/// <returns> updated builder object </returns>
			public virtual Builder gradient(Gradient val)
			{
				gradient_Renamed = val;
				return this;
			}

			/// <summary>
			/// Setter for opacity in builder
			/// </summary>
			/// <param name="val"> Opacity of the entire heatmap in range [0, 1] </param>
			/// <returns> updated builder object </returns>
			public virtual Builder opacity(double val)
			{
				opacity_Renamed = val;
				// Check that opacity is in range
				if (opacity_Renamed < 0 || opacity_Renamed > 1)
				{
					throw new System.ArgumentException("Opacity must be in range [0, 1]");
				}
				return this;
			}

			/// <summary>
			/// Call when all desired options have been set.
			/// Note: you must set data using data or weightedData before this!
			/// </summary>
			/// <returns> HeatmapTileProvider created with desired options. </returns>
			public virtual HeatmapTileProvider build()
			{
				// Check if data or weightedData has been called
				if (data_Renamed == null)
				{
					throw new IllegalStateException("No input data: you must use either .data or " + ".weightedData before building");
				}

				return new HeatmapTileProvider(this);
			}
		}

        private HeatmapTileProvider()
        {

        }

		private HeatmapTileProvider(Builder builder)
		{
			// Get parameters from builder
			mData = builder.data_Renamed;

			mRadius = builder.radius_Renamed;
			mGradient = builder.gradient_Renamed;
			mOpacity = builder.opacity_Renamed;

			// Compute kernel density function (sd = 1/3rd of radius)
			mKernel = generateKernel(mRadius, mRadius / 3.0);

			// Generate color map
			Gradient = mGradient;

			// Set the data
			WeightedData = mData;
		}

		/// <summary>
		/// Changes the dataset the heatmap is portraying. Weighted.
		/// User should clear overlay's tile cache (using clearTileCache()) after calling this.
		/// </summary>
		/// <param name="data"> Data set of points to use in the heatmap, as LatLngs.
		///             Note: Editing data without calling setWeightedData again will not update the data
		///             displayed on the map, but will impact calculation of max intensity values,
		///             as the collection you pass in is stored.
		///             Outside of changing the data, max intensity values are calculated only upon
		///             changing the radius. </param>
		public virtual ICollection<WeightedLatLng> WeightedData
		{
			set
			{
				// Change point set
				mData = value;
    
				// Check point set is OK
				if (mData.Count == 0)
				{
					throw new System.ArgumentException("No input points.");
				}
    
				// Because quadtree bounds are final once the quadtree is created, we cannot add
				// points outside of those bounds to the quadtree after creation.
				// As quadtree creation is actually quite lightweight/fast as compared to other functions
				// called in heatmap creation, re-creating the quadtree is an acceptable solution here.
    
				// Make the quad tree
				mBounds = getBounds(mData);
    
				mTree = new PointQuadTree<WeightedLatLng>(mBounds);
    
				// Add points to quad tree
				foreach (WeightedLatLng l in mData)
				{
					mTree.add(l);
				}
    
				// Calculate reasonable maximum intensity for color scale (user can also specify)
				// Get max intensities
				mMaxIntensity = getMaxIntensities(mRadius);
			}
		}

		/// <summary>
		/// Changes the dataset the heatmap is portraying. Unweighted.
		/// User should clear overlay's tile cache (using clearTileCache()) after calling this.
		/// </summary>
		/// <param name="data"> Data set of points to use in the heatmap, as LatLngs. </param>
		public virtual ICollection<LatLng> Data
		{
			set
			{
				// Turn them into WeightedLatLngs and delegate.
				WeightedData = wrapData(value);
			}
		}

		/// <summary>
		/// Helper function - wraps LatLngs into WeightedLatLngs.
		/// </summary>
		/// <param name="data"> Data to wrap (LatLng) </param>
		/// <returns> Data, in WeightedLatLng form </returns>
		private static ICollection<WeightedLatLng> wrapData(ICollection<LatLng> data)
		{
			// Use an ArrayList as it is a nice collection
			List<WeightedLatLng> weightedData = new List<WeightedLatLng>();

			foreach (LatLng l in data)
			{
				weightedData.Add(new WeightedLatLng(l));
			}

			return weightedData;
		}

		/// <summary>
		/// Creates tile.
		/// </summary>
		/// <param name="x">    X coordinate of tile. </param>
		/// <param name="y">    Y coordinate of tile. </param>
		/// <param name="zoom"> Zoom level. </param>
		/// <returns> image in Tile format </returns>
		public virtual Tile getTile(int x, int y, int zoom)
		{
			// Convert tile coordinates and zoom into Point/Bounds format
			// Know that at zoom level 0, there is one tile: (0, 0) (arbitrary width 512)
			// Each zoom level multiplies number of tiles by 2
			// Width of the world = WORLD_WIDTH = 1
			// x = [0, 1) corresponds to [-180, 180)

			// calculate width of one tile, given there are 2 ^ zoom tiles in that zoom level
			// In terms of world width units
			double tileWidth = WORLD_WIDTH / Math.Pow(2, zoom);

			// how much padding to include in search
			// is to tileWidth as mRadius (padding in terms of pixels) is to TILE_DIM
			// In terms of world width units
			double padding = tileWidth * mRadius / TILE_DIM;

			// padded tile width
			// In terms of world width units
			double tileWidthPadded = tileWidth + 2 * padding;

			// padded bucket width - divided by number of buckets
			// In terms of world width units
			double bucketWidth = tileWidthPadded / (TILE_DIM + mRadius * 2);

			// Make bounds: minX, maxX, minY, maxY
			double minX = x * tileWidth - padding;
			double maxX = (x + 1) * tileWidth + padding;
			double minY = y * tileWidth - padding;
			double maxY = (y + 1) * tileWidth + padding;

			// Deal with overlap across lat = 180
			// Need to make it wrap around both ways
			// However, maximum tile size is such that you wont ever have to deal with both, so
			// hence, the else
			// Note: Tile must remain square, so cant optimise by editing bounds
			double xOffset = 0;
			ICollection<WeightedLatLng> wrappedPoints = new List<WeightedLatLng>();
			if (minX < 0)
			{
				// Need to consider "negative" points
				// (minX to 0) ->  (512+minX to 512) ie +512
				// add 512 to search bounds and subtract 512 from actual points
				Bounds overlapBounds = new Bounds(minX + WORLD_WIDTH, WORLD_WIDTH, minY, maxY);
				xOffset = -WORLD_WIDTH;
				wrappedPoints = mTree.search(overlapBounds);
			}
			else if (maxX > WORLD_WIDTH)
			{
				// Cant both be true as then tile covers whole world
				// Need to consider "overflow" points
				// (512 to maxX) -> (0 to maxX-512) ie -512
				// subtract 512 from search bounds and add 512 to actual points
				Bounds overlapBounds = new Bounds(0, maxX - WORLD_WIDTH, minY, maxY);
				xOffset = WORLD_WIDTH;
				wrappedPoints = mTree.search(overlapBounds);
			}

			// Main tile bounds to search
			Bounds tileBounds = new Bounds(minX, maxX, minY, maxY);

			// If outside of *padded* quadtree bounds, return blank tile
			// This is comparing our bounds to the padded bounds of all points in the quadtree
			// ie tiles that don't touch the heatmap at all
			Bounds paddedBounds = new Bounds(mBounds.MinX - padding, mBounds.MaxX + padding, mBounds.MinY - padding, mBounds.MaxY + padding);
			if (!tileBounds.Intersects(paddedBounds))
			{
				return TileProvider.NoTile;
			}

			// Search for all points within tile bounds
			ICollection<WeightedLatLng> points = mTree.search(tileBounds);

			// If no points, return blank tile
			if (points.Count == 0)
			{
                return TileProvider.NoTile;
			}

			// Quantize points
//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: double[][] intensity = new double[TILE_DIM + mRadius * 2][TILE_DIM + mRadius * 2];
			double[][] intensity = RectangularArrays.ReturnRectangularDoubleArray(TILE_DIM + mRadius * 2, TILE_DIM + mRadius * 2);
			foreach (WeightedLatLng w in points)
			{
				Com.Google.Maps.Android.Projection.Point p = w.Point;
				int bucketX = (int)((p.X - minX) / bucketWidth);
				int bucketY = (int)((p.X - minY) / bucketWidth);
				intensity[bucketX][bucketY] += w.Intensity;
			}
			// Quantize wraparound points (taking xOffset into account)
			foreach (WeightedLatLng w in wrappedPoints)
			{
				Com.Google.Maps.Android.Projection.Point p = w.Point;
				int bucketX = (int)((p.X + xOffset - minX) / bucketWidth);
				int bucketY = (int)((p.Y - minY) / bucketWidth);
				intensity[bucketX][bucketY] += w.Intensity;
			}

			// Convolve it ("smoothen" it out)
			double[][] convolved = convolve(intensity, mKernel);

			// Color it into a bitmap
			Bitmap bitmap = colorize(convolved, mColorMap, mMaxIntensity[zoom]);

			// Convert bitmap to tile and return
			return convertBitmap(bitmap);
		}

		/// <summary>
		/// Setter for gradient/color map.
		/// User should clear overlay's tile cache (using clearTileCache()) after calling this.
		/// </summary>
		/// <param name="gradient"> Gradient to set </param>
		public virtual Gradient Gradient
		{
			set
			{
				mGradient = value;
				mColorMap = value.generateColorMap(mOpacity);
			}
		}

		/// <summary>
		/// Setter for radius.
		/// User should clear overlay's tile cache (using clearTileCache()) after calling this.
		/// </summary>
		/// <param name="radius"> Radius to set </param>
		public virtual int Radius
		{
			set
			{
				mRadius = value;
				// need to recompute kernel
				mKernel = generateKernel(mRadius, mRadius / 3.0);
				// need to recalculate max intensity
				mMaxIntensity = getMaxIntensities(mRadius);
			}
		}

		/// <summary>
		/// Setter for opacity
		/// User should clear overlay's tile cache (using clearTileCache()) after calling this.
		/// </summary>
		/// <param name="opacity"> opacity to set </param>
		public virtual double Opacity
		{
			set
			{
				mOpacity = value;
				// need to recompute kernel color map
				Gradient = mGradient;
			}
		}

		/// <summary>
		/// Gets array of maximum intensity values to use with the heatmap for each zoom level
		/// This is the value that the highest color on the color map corresponds to
		/// </summary>
		/// <param name="radius"> radius of the heatmap </param>
		/// <returns> array of maximum intensities </returns>
		private double[] getMaxIntensities(int radius)
		{
			// Can go from zoom level 3 to zoom level 22
			double[] maxIntensityArray = new double[MAX_ZOOM_LEVEL];

			// Calculate max intensity for each zoom level
			for (int i = DEFAULT_MIN_ZOOM; i < DEFAULT_MAX_ZOOM; i++)
			{
				// Each zoom level multiplies viewable size by 2
				maxIntensityArray[i] = getMaxValue(mData, mBounds, radius, (int)(SCREEN_SIZE * Math.Pow(2, i - 3)));
				if (i == DEFAULT_MIN_ZOOM)
				{
					for (int j = 0; j < i; j++)
					{
						maxIntensityArray[j] = maxIntensityArray[i];
					}
				}
			}
			for (int i = DEFAULT_MAX_ZOOM; i < MAX_ZOOM_LEVEL; i++)
			{
				maxIntensityArray[i] = maxIntensityArray[DEFAULT_MAX_ZOOM - 1];
			}

			return maxIntensityArray;
		}

		/// <summary>
		/// helper function - convert a bitmap into a tile
		/// </summary>
		/// <param name="bitmap"> bitmap to convert into a tile </param>
		/// <returns> the tile </returns>
		private static Tile convertBitmap(Bitmap bitmap)
		{
			// Convert it into byte array (required for tile creation)
	//		ByteArrayOutputStream stream = new ByteArrayOutputStream();

            MemoryStream stream = new MemoryStream();
			bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
			sbyte[] bitmapdata = stream.ToArray;
			return new Tile(TILE_DIM, TILE_DIM, bitmapdata);
		}

		/* Utility functions below */

		/// <summary>
		/// Helper function for quadtree creation
		/// </summary>
		/// <param name="points"> Collection of WeightedLatLng to calculate bounds for </param>
		/// <returns> Bounds that enclose the listed WeightedLatLng points </returns>
		internal static Bounds getBounds(ICollection<WeightedLatLng> points)
		{

			// Use an iterator, need to access any one point of the collection for starting bounds
			IEnumerator<WeightedLatLng> iter = points.GetEnumerator();

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			WeightedLatLng first = iter.next();

			double minX = first.Point.X;
			double maxX = first.Point.X;
			double minY = first.Point.Y;
			double maxY = first.Point.Y;

			while (iter.MoveNext())
			{
				WeightedLatLng l = iter.Current;
				double x = l.Point.X;
				double y = l.Point.Y;
				// Extend bounds if necessary
				if (x < minX)
				{
					minX = x;
				}
				if (x > maxX)
				{
					maxX = x;
				}
				if (y < minY)
				{
					minY = y;
				}
				if (y > maxY)
				{
					maxY = y;
				}
			}

			return new Bounds(minX, maxX, minY, maxY);
		}

		/// <summary>
		/// Generates 1D Gaussian kernel density function, as a double array of size radius * 2  + 1
		/// Normalised with central value of 1.
		/// </summary>
		/// <param name="radius"> radius of the kernel </param>
		/// <param name="sd">     standard deviation of the Gaussian function </param>
		/// <returns> generated Gaussian kernel </returns>
		private double[] generateKernel(int radius, double sd)
		{
			double[] kernel = new double[radius * 2 + 1];
			for (int i = -radius; i <= radius; i++)
			{
				kernel[i + radius] = (System.Math.Exp(-i * i / (2 * sd * sd)));
			}
			return kernel;
		}

		/// <summary>
		/// Applies a 2D Gaussian convolution to the input grid, returning a 2D grid cropped of padding.
		/// </summary>
		/// <param name="grid">   Raw input grid to convolve: dimension (dim + 2 * radius) x (dim + 2 * radius)
		///               ie dim * dim with padding of size radius </param>
		/// <param name="kernel"> Pre-computed Gaussian kernel of size radius * 2 + 1 </param>
		/// <returns> the smoothened grid </returns>
	//	private double[][] convolve(double[][] grid, double[] kernel)
        private double[,] convolve(double[][] grid, double[] kernel)
		{
			// Calculate radius size
			int radius = (int) System.Math.Floor((double) kernel.Length / 2.0);
			// Padded dimension
			int dimOld = grid.Length;
			// Calculate final (non padded) dimension
			int dim = dimOld - 2 * radius;

			// Upper and lower limits of non padded (inclusive)
			int lowerLimit = radius;
			int upperLimit = radius + dim - 1;

			// Convolve horizontally
//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: double[][] intermediate = new double[dimOld][dimOld];
	//		double[][] intermediate = RectangularArrays.ReturnRectangularDoubleArray(dimOld, dimOld);
            double[,] intermediate = new double[10, 10];   //HACK OFK

			// Need to convolve every point (including those outside of non-padded area)
			// but only need to add to points within non-padded area
			int x, y, x2, xUpperLimit, initial;
			double val;
			for (x = 0; x < dimOld; x++)
			{
				for (y = 0; y < dimOld; y++)
				{
					// for each point (x, y)
					val = grid[x][y];
					// only bother if something there
					if (val != 0)
					{
						// need to "apply" convolution from that point to every point in
						// (max(lowerLimit, x - radius), y) to (min(upperLimit, x + radius), y)
						xUpperLimit = ((upperLimit < x + radius) ? upperLimit : x + radius) + 1;
						// Replace Math.max
						initial = (lowerLimit > x - radius) ? lowerLimit : x - radius;
						for (x2 = initial; x2 < xUpperLimit; x2++)
						{
							// multiplier for x2 = x - radius is kernel[0]
							// x2 = x + radius is kernel[radius * 2]
							// so multiplier for x2 in general is kernel[x2 - (x - radius)]
	//HACK						intermediate[x2][y] += val * kernel[x2 - (x - radius)];
						}
					}
				}
			}

			// Convolve vertically
//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: double[][] outputGrid = new double[dim][dim];
	//		double[][] outputGrid = RectangularArrays.ReturnRectangularDoubleArray(dim, dim);
            double[,] outputGrid = new double[10, 10];   //HACK OFK

			// Similarly, need to convolve every point, but only add to points within non-padded area
			// However, we are adding to a smaller grid here (previously, was to a grid of same size)
			int y2, yUpperLimit;

			// Don't care about convolving parts in horizontal padding - wont impact inner
			for (x = lowerLimit; x < upperLimit + 1; x++)
			{
				for (y = 0; y < dimOld; y++)
				{
					// for each point (x, y)
                    		//		val = intermediate[x][y];
                    val = intermediate[9,9];   //Hack
					// only bother if something there
					if (val != 0)
					{
						// need to "apply" convolution from that point to every point in
						// (x, max(lowerLimit, y - radius) to (x, min(upperLimit, y + radius))
						// Don't care about
						yUpperLimit = ((upperLimit < y + radius) ? upperLimit : y + radius) + 1;
						// replace math.max
						initial = (lowerLimit > y - radius) ? lowerLimit : y - radius;
						for (y2 = initial; y2 < yUpperLimit; y2++)
						{
							// Similar logic to above
							// subtract, as adding to a smaller grid
                            //HACK						outputGrid[x - radius][y2 - radius] += val * kernel[y2 - (y - radius)];
						}
					}
				}
			}

			return outputGrid;
		}

		/// <summary>
		/// Converts a grid of intensity values to a colored Bitmap, using a given color map
		/// </summary>
		/// <param name="grid">     the input grid (assumed to be square) </param>
		/// <param name="colorMap"> color map (created by generateColorMap) </param>
		/// <param name="max">      Maximum intensity value: maps to 100% on gradient </param>
		/// <returns> the colorized grid in Bitmap form, with same dimensions as grid </returns>
		internal static Bitmap colorize(double[][] grid, int[] colorMap, double max)
		{
			// Maximum color value
			int maxColor = colorMap[colorMap.Length - 1];
			// Multiplier to "scale" intensity values with, to map to appropriate color
			double colorMapScaling = (colorMap.Length - 1) / max;
			// Dimension of the input grid (and dimension of output bitmap)
			int dim = grid.Length;

			int i, j, index, col;
			double val;
			// Array of colors
			int[] colors = new int[dim * dim];
			for (i = 0; i < dim; i++)
			{
				for (j = 0; j < dim; j++)
				{
					// [x][y]
					// need to enter each row of x coordinates sequentially (x first)
					// -> [j][i]
					val = grid[j][i];
					index = i * dim + j;
					col = (int)(val * colorMapScaling);

					if (val != 0)
					{
						// Make it more resilient: cant go outside colorMap
						if (col < colorMap.Length)
						{
							colors[index] = colorMap[col];
						}
						else
						{
							colors[index] = maxColor;
						}
					}
					else
					{
						colors[index] = Color.TRANSPARENT;
					}
				}
			}

			// Now turn these colors into a bitmap
			Bitmap tile = Bitmap.createBitmap(dim, dim, Bitmap.Config.ARGB_8888);
			// (int[] pixels, int offset, int stride, int x, int y, int width, int height)
			tile.setPixels(colors, 0, dim, 0, 0, dim, dim);
			return tile;
		}

		/// <summary>
		/// Calculate a reasonable maximum intensity value to map to maximum color intensity
		/// </summary>
		/// <param name="points">    Collection of LatLngs to put into buckets </param>
		/// <param name="bounds">    Bucket boundaries </param>
		/// <param name="radius">    radius of convolution </param>
		/// <param name="screenDim"> larger dimension of screen in pixels (for scale) </param>
		/// <returns> Approximate max value </returns>
		internal static double getMaxValue(ICollection<WeightedLatLng> points, Bounds bounds, int radius, int screenDim)
		{
			// Approximate scale as if entire heatmap is on the screen
			// ie scale dimensions to larger of width or height (screenDim)
			double minX = bounds.minX;
			double maxX = bounds.maxX;
			double minY = bounds.minY;
			double maxY = bounds.maxY;
			double boundsDim = (maxX - minX > maxY - minY) ? maxX - minX : maxY - minY;

			// Number of buckets: have diameter sized buckets
			int nBuckets = (int)(screenDim / (2 * radius) + 0.5);
			// Scaling factor to convert width in terms of point distance, to which bucket
			double scale = nBuckets / boundsDim;

			// Make buckets
			// Use a sparse array - use LongSparseArray just in case
			LongSparseArray<LongSparseArray<double?>> buckets = new LongSparseArray<LongSparseArray<double?>>();
			//double[][] buckets = new double[nBuckets][nBuckets];

			// Assign into buckets + find max value as we go along
			double x, y;
			double max = 0;
			foreach (WeightedLatLng l in points)
			{
				x = l.Point.x;
				y = l.Point.y;

				int xBucket = (int)((x - minX) * scale);
				int yBucket = (int)((y - minY) * scale);

				// Check if x bucket exists, if not make it
				LongSparseArray<double?> column = buckets.get(xBucket);
				if (column == null)
				{
					column = new LongSparseArray<double?>();
					buckets.put(xBucket, column);
				}
				// Check if there is already a y value there
				double? value = column.get(yBucket);
				if (value == null)
				{
					value = 0.0;
				}
				value += l.Intensity;
				// Yes, do need to update it, despite it being a Double.
				column.put(yBucket, value);

				if (value > max)
				{
					max = value;
				}
			}

			return max;
		}
	}

}

#endif