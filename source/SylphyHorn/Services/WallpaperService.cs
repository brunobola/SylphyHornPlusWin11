﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using SylphyHorn.Interop;
using SylphyHorn.Properties;
using SylphyHorn.Serialization;
using WindowsDesktop;

namespace SylphyHorn.Services
{
	public class WallpaperService : IDisposable
	{
		private const WallpaperPosition _defaultPosition = WallpaperPosition.Fill;

		public static readonly string SupportedFormats =
			"Image File (*.jpg;*.jpeg;*.jfif;*.png;*.bmp)|*.jpg;*.jpeg;*.jfif;*.png;*.bmp|" +
			"JPEG (*.jpg;*.jpeg;*.jfif)|*.jpg;*.jpeg;*.jfif|" +
			"PNG (*.png)|*.png|" +
			"Bitmap (*.bmp)|*.bmp";

		public static WallpaperService Instance { get; } = new WallpaperService();

		private WallpaperService()
		{
			if (ProductInfo.IsWallpaperSupportBuild)
			{
				VirtualDesktop.CurrentChanged += this.VirtualDesktopOnCurrentChanged;
			}
			else
			{
				VirtualDesktop.CurrentChanged += this.VirtualDesktopOnCurrentChangedForWin10;
			}
		}

		private void VirtualDesktopOnCurrentChanged(object sender, VirtualDesktopChangedEventArgs e)
		{
			Task.Run(() => SetPosition(e.NewDesktop));
		}

		private void VirtualDesktopOnCurrentChangedForWin10(object sender, VirtualDesktopChangedEventArgs e)
		{
			Task.Run(() =>
			{
				if (!Settings.General.ChangeBackgroundEachDesktop) return;
				SetWallpaperAndPosition(e.NewDesktop);
			});
		}

		public void Dispose()
		{
			VirtualDesktop.CurrentChanged -= this.VirtualDesktopOnCurrentChanged;
		}

		public static void SetPosition(VirtualDesktop newDesktop)
		{
			var newIndex = newDesktop.Index;
			var positionSettings = Settings.General.DesktopBackgroundPositions;
			var positionCount = positionSettings.Count;

			if (positionCount == 0) return;

			var dw = DesktopWallpaperFactory.Create();
			var oldPosition = dw.GetPosition();
			var newPosition = newIndex < positionCount
				? (DesktopWallpaperPosition)positionSettings.Value[newIndex].Value
				: (DesktopWallpaperPosition)_defaultPosition;
			if (oldPosition != newPosition) dw.SetPosition(newPosition);
		}

		public static void SetWallpaperAndPosition(VirtualDesktop newDesktop)
		{
			var newIndex = newDesktop.Index;
			var pathSettings = Settings.General.DesktopBackgroundImagePaths;
			var positionSettings = Settings.General.DesktopBackgroundPositions;
			var pathCount = pathSettings.Count;
			var positionCount = positionSettings.Count;

			if (pathCount == 0 && positionCount == 0) return;

			var dw = DesktopWallpaperFactory.Create();
			var path = newIndex < pathCount
				? pathSettings.Value[newIndex].Value
				: pathSettings.Value.FirstOrDefault(p => p.Value.Length > 0);
			if (!string.IsNullOrEmpty(path)) dw.SetWallpaper(null, path);
			var oldPosition = dw.GetPosition();
			var newPosition = newIndex < positionCount
				? (DesktopWallpaperPosition)positionSettings.Value[newIndex].Value
				: (DesktopWallpaperPosition)_defaultPosition;
			if (oldPosition != newPosition) dw.SetPosition(newPosition);
		}

		public static void SetBackgroundColor(Color color)
		{
			var dw = DesktopWallpaperFactory.Create();
			dw.SetBackgroundColor(new COLORREF { R = color.R, G = color.G, B = color.B });
		}

		public static Tuple<Color, string> GetCurrentColorAndWallpaper()
		{
			var dw = DesktopWallpaperFactory.Create();
			var colorref = dw.GetBackgroundColor();

			string path = null;
			if (dw.GetMonitorDevicePathCount() >= 1)
			{
				var monitorId = dw.GetMonitorDevicePathAt(0);
				path = dw.GetWallpaper(monitorId);
			}

			return Tuple.Create(Color.FromRgb(colorref.R, colorref.G, colorref.B), path);
		}

		private static WallpaperPosition Parse(string options)
		{
			var options2 = options.ToLower();
			if (options2[0] == 'c') return WallpaperPosition.Center;
			if (options2[0] == 't') return WallpaperPosition.Tile;
			if (options2.StartsWith("sp")) return WallpaperPosition.Span;
			if (options2[0] == 's') return WallpaperPosition.Stretch;
			if (options2.StartsWith("fil")) return WallpaperPosition.Fill;
			if (options2[0] == 'f') return WallpaperPosition.Fit;
			return WallpaperPosition.Fill;
		}
	}

	public enum WallpaperPosition : byte
	{
		Center = 0,
		Tile,
		Stretch,
		Fit,
		Fill,
		Span,
	}
}
