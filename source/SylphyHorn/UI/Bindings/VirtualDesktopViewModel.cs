﻿using Livet;
using Livet.EventListeners;
using MetroTrilithon.Mvvm;
using SylphyHorn.Properties;
using SylphyHorn.Serialization;
using SylphyHorn.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using WindowsDesktop;

namespace SylphyHorn.UI.Bindings
{
	public class VirtualDesktopViewModel : ViewModel
	{
		private VirtualDesktop _desktop;
		private DesktopNameProperty _name;
		private WallpaperViewModel _wallpaper;
		private Action<string> _nameFunc;

		public int Index => this._name.Index;

		public string NumberText => this._name.NumberText;

		#region Model notification property

		public VirtualDesktop Model
		{
			get => this._desktop;
			private set
			{
				if (value != null && this._desktop != value)
				{
					this._desktop = value;
					this.RaisePropertyChanged(nameof(this.Model));
				}
			}
		}

		#endregion

		#region Name notification property

		public string Name
		{
			get => this._name.Value;
			set => this._nameFunc(value);
		}

		#endregion

		public bool IsWallpaperEnabled => ProductInfo.IsWallpaperSupportBuild || Settings.General.ChangeBackgroundEachDesktop;

		#region WallpaperPath notification property

		public string WallpaperPath
		{
			get => this._wallpaper.FilePath;
			set
			{
				if (this._wallpaper.FilePath != value)
				{
					this._wallpaper.FilePath = value;
				}
			}
		}

		#endregion

		public string WallpaperPathOrDefault => this._wallpaper.FilePathOrDefault;

		#region WallpaperPosition notification property

		public WallpaperPosition WallpaperPosition
		{
			get => this._wallpaper.Position;
			set
			{
				if (this._wallpaper.Position != value)
				{
					this._wallpaper.Position = value;
				}
			}
		}

		#endregion

		#region Wallpaper notification property

		public WallpaperViewModel Wallpaper
		{
			get => this._wallpaper;
			set
			{
				if (this._wallpaper != value)
				{
					this._wallpaper = value;
					this.RaisePropertyChanged(nameof(this.Wallpaper));
				}
			}
		}

		#endregion

		public bool HasWallpaper => !string.IsNullOrEmpty(this.WallpaperPath);
		public bool HasNoWallpaper => string.IsNullOrEmpty(this.WallpaperPath);

		private VirtualDesktopViewModel(int index, VirtualDesktop desktop)
		{
			this._desktop = desktop;

			var settings = Settings.General;
			var name = settings.DesktopNames.Value[index];
			this._name = name;

			var wallpaperPath = settings.DesktopBackgroundImagePaths.Value[index];
			var wallpaperPosition = settings.DesktopBackgroundPositions.Value[index];
			this._wallpaper = new WallpaperViewModel(desktop, wallpaperPath, wallpaperPosition);

			var listener = new PropertyChangedEventListener(this.Wallpaper);
			listener.Add(nameof(this.Wallpaper.FilePath), (sender, args) =>
				{
					this.RaisePropertyChanged(nameof(this.WallpaperPath));
					this.RaisePropertyChanged(nameof(this.HasWallpaper));
					this.RaisePropertyChanged(nameof(this.HasNoWallpaper));
				});
			listener.Add(nameof(this.Wallpaper.FilePathOrDefault), (sender, args) =>
				{
					this.RaisePropertyChanged(nameof(this.WallpaperPathOrDefault));
					this.RaisePropertyChanged(nameof(this.HasWallpaper));
					this.RaisePropertyChanged(nameof(this.HasNoWallpaper));
				});
			listener.Add(nameof(this.Wallpaper.Position), (sender, args) => this.RaisePropertyChanged(nameof(this.WallpaperPosition)));
			this.CompositeDisposable.Add(listener);

			// for Windows 10
			if (!ProductInfo.IsWallpaperSupportBuild)
			{
				if (ProductInfo.IsNameSupportBuild)
				{
					this._nameFunc = n =>
					{
						if (this._name.Value != n)
						{
							this._name.Value = n;
							this._desktop.Name = n;
						}
					};
				}
				else
				{
					this._nameFunc = n =>
					{
						if (this._name.Value != n) this._name.Value = n;
					};
				}
				name.Subscribe(_ => this.RaisePropertyChanged(nameof(this.Name))).AddTo(this);
				settings.ChangeBackgroundEachDesktop.Subscribe(_ => this.RaisePropertyChanged(nameof(this.IsWallpaperEnabled))).AddTo(this);
				return;
			}

			// for Windows 11 or later
			this._nameFunc = n =>
			{
				if (this._name.Value != n)
				{
					this._name.Value = n;
					this._desktop.Name = n;
				}
			};
			name.Subscribe(_ => this.RaisePropertyChanged(nameof(this.Name))).AddTo(this);
		}

		public void MoveToPrevious()
		{
			this._desktop?.MoveToLeft();
		}

		public void MoveToNext()
		{
			this._desktop?.MoveToRight();
		}

		public void MoveToFirst()
		{
			this._desktop?.MoveToFirst();
		}

		public void MoveToLast()
		{
			this._desktop?.MoveToLast();
		}

		public void Switch()
		{
			this._desktop?.Switch();
		}

		public void Close()
		{
			this._desktop?.Remove();
		}

		public static VirtualDesktopViewModel[] CreateAll()
		{
			var desktops = VirtualDesktop.AllDesktops;
			return desktops.Select((d, i) => new VirtualDesktopViewModel(i, d)).ToArray();
		}

		public static VirtualDesktopViewModel Create(VirtualDesktop desktop)
		{
			var desktopIndex = desktop.Index;
			return new VirtualDesktopViewModel(desktopIndex, desktop);
		}

		public static void UpdateModel(VirtualDesktopViewModel[] viewModels)
		{
			var desktops = VirtualDesktop.AllDesktops;

			if (desktops.Length != viewModels.Length) throw new ArgumentException("ViewModel count does not match virtual desktop count.");

			for (var i = 0; i < viewModels.Length; ++i)
			{
				viewModels[i].Model = desktops[i];
			}
		}
	}

	public class WallpaperViewModel : ViewModel
	{
		private WallpaperPathProperty _path;
		private WallpaperPositionsProperty _position;
		private Action<string> _pathFunc;
		private Action<byte> _positionFunc;

		public int DesktopIndex => this._path.Index;

		#region FilePath notification property

		public string FilePath
		{
			get => this._path.Value;
			set => this._pathFunc(value);
		}

		#endregion

		public string FilePathOrDefault => this._path.GetOrDefault();

		#region Position notification property

		public WallpaperPosition Position
		{
			get => (WallpaperPosition)this._position.Value;
			set => this._positionFunc((byte)value);
		}

		#endregion

		public Color Color
		{
			get => WallpaperService.GetCurrentColorAndWallpaper().Item1;
			set => WallpaperService.SetBackgroundColor(value);
		}

		public WallpaperViewModel(VirtualDesktop desktop, WallpaperPathProperty path, WallpaperPositionsProperty position)
		{
			this._path = path;
			this._position = position;

			this._positionFunc = p =>
			{
				if (this._position.Value != p)
				{
					this._position.Value = p;
					var currentDesktop = VirtualDesktop.Current;
					if (desktop == currentDesktop)
					{
						WallpaperService.SetPosition(currentDesktop);
					}
				}
			};
			position.Subscribe(_ => this.RaisePropertyChanged(nameof(this.Position))).AddTo(this);

			// for Windows 10
			if (!ProductInfo.IsWallpaperSupportBuild)
			{
				var generalSettings = Settings.General;
				this._pathFunc = p =>
				{
					if (p == null) p = "";
					if (this._path.Value != p)
					{
						this._path.Value = p;
						if (generalSettings.ChangeBackgroundEachDesktop && desktop == VirtualDesktop.Current)
						{
							WallpaperService.SetWallpaperAndPosition(desktop);
						}
					}
				};
				path.Subscribe(_ =>
					{
						this.RaisePropertyChanged(nameof(this.FilePath));
						this.RaisePropertyChanged(nameof(this.FilePathOrDefault));
					}).AddTo(this);
				return;
			}

			// for Windows 11 or later
			if (string.IsNullOrEmpty(desktop.WallpaperPath))
			{
				desktop.WallpaperPath = path.GetOrDefault();
			}
			else if (path.Value != desktop.WallpaperPath)
			{
				path.Value = desktop.WallpaperPath;
			}

			this._pathFunc = p =>
			{
				if (string.IsNullOrEmpty(p)) return;
				if (this._path.Value != p)
				{
					this._path.Value = p;
					WallpaperService.SetWallpaperEnabled(!string.IsNullOrEmpty(p));
					desktop.WallpaperPath = p;
				}
			};
			path.Subscribe(_ =>
				{
					this.RaisePropertyChanged(nameof(this.FilePath));
					this.RaisePropertyChanged(nameof(this.FilePathOrDefault));
				}).AddTo(this);
		}
	}
}
