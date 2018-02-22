using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cadenza.Collections;
using Xamarin.PropertyEditing.Drawing;
using Xamarin.PropertyEditing.Properties;

namespace Xamarin.PropertyEditing.ViewModels
{
	internal class BrushPropertyViewModel : PropertyViewModel<CommonBrush>
	{
		public BrushPropertyViewModel (TargetPlatform platform, IPropertyInfo property, IEnumerable<IObjectEditor> editors)
			: base (platform, property, editors)
		{
			if (property.Type.IsAssignableFrom (typeof (CommonSolidBrush))) {
				Solid = new SolidBrushViewModel (this,
					property is IColorSpaced colorSpacedPropertyInfo ? colorSpacedPropertyInfo.ColorSpaces :  null);
				if (platform.SupportsMaterialDesign) {
					MaterialDesign = new MaterialDesignColorViewModel (this);
				}
			}

			// TODO: we actually need to localize this for platforms really, "brush" doesn't make sense for some
			var types = new OrderedDictionary<string, CommonBrushType> {
				{ Resources.NoBrush, CommonBrushType.NoBrush },
				{ Resources.SolidBrush, CommonBrushType.Solid },
				{ Resources.ResourceBrush, CommonBrushType.Resource }
			};

			if (platform.SupportsMaterialDesign) {
				types.Insert (2, Resources.MaterialDesignColorBrush, CommonBrushType.MaterialDesign);
			}

			BrushTypes = types;
			RequestCurrentValueUpdate ();
		}

		public IReadOnlyDictionary<string, CommonBrushType> BrushTypes
		{
			get;
		}

		public CommonBrushType SelectedBrushType
		{
			get { return this.selectedBrushType; }
			set {
				if (this.selectedBrushType == value)
					return;

				this.selectedBrushType = value;
				SetBrushType (value);
				OnPropertyChanged();
			}
		}

		public SolidBrushViewModel Solid { get; }
		public MaterialDesignColorViewModel MaterialDesign { get; }

		public ResourceSelectorViewModel ResourceSelector
		{
			get {
				if (this.resourceSelector != null)
					return this.resourceSelector;

				if (ResourceProvider == null || Editors == null)
					return null;

				return this.resourceSelector = new ResourceSelectorViewModel (ResourceProvider, Editors.Select (ed => ed.Target), Property);
			}
		}
		
		// TODO: make this its own property view model so we can edit bindings, set to resources, etc.
		public double Opacity {
			get => Value == null ? 1.0 : Value.Opacity;
			set {
				switch (Value) {
				case CommonBrush brush when brush == null:
					return;
				case CommonSolidBrush solid:
					Value = new CommonSolidBrush (solid.Color, solid.ColorSpace, value);
					break;
				case CommonImageBrush img:
					Value = new CommonImageBrush (
						img.ImageSource, img.AlignmentX, img.AlignmentY, img.Stretch, img.TileMode,
						img.ViewBox, img.ViewBoxUnits, img.ViewPort, img.ViewPortUnits, value);
					break;
				case CommonLinearGradientBrush linear:
					Value = new CommonLinearGradientBrush (
						linear.StartPoint, linear.EndPoint, linear.GradientStops,
						linear.ColorInterpolationMode, linear.MappingMode, linear.SpreadMethod, value);
					break;
				case CommonRadialGradientBrush radial:
					Value = new CommonRadialGradientBrush (
						radial.Center, radial.GradientOrigin, radial.RadiusX, radial.RadiusY,
						radial.GradientStops, radial.ColorInterpolationMode, radial.MappingMode,
						radial.SpreadMethod, value);
					break;
				default:
					throw new InvalidOperationException ("Value is an unsupported brush type.");
				}
				OnPropertyChanged ();
			}
		}

		public void ResetResourceSelector ()
		{
			this.resourceSelector = null;
			OnPropertyChanged (nameof (ResourceSelector));
		}

		protected override async Task UpdateCurrentValueAsync ()
		{
			if (BrushTypes == null)
				return;

			await base.UpdateCurrentValueAsync ();

			if (MaterialDesign != null && (MaterialDesign.NormalColor.HasValue || MaterialDesign.AccentColor.HasValue))
				this.selectedBrushType = CommonBrushType.MaterialDesign;
			else if (Value == null)
				this.selectedBrushType = CommonBrushType.NoBrush;
			else {
				switch (ValueSource) {
				case ValueSource.Resource:
					this.selectedBrushType = CommonBrushType.Resource;
					break;
				default:
				case ValueSource.Local:
					this.selectedBrushType = CommonBrushType.Solid;
					break;
				}
			}

			OnPropertyChanged (nameof (SelectedBrushType));
		}

		protected override void OnValueChanged ()
		{
			base.OnValueChanged ();
			OnPropertyChanged (nameof (Opacity));
		}

		protected override void OnEditorsChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			base.OnEditorsChanged (sender, e);
			ResetResourceSelector ();
		}

		protected override void OnPropertyChanged ([CallerMemberName] string propertyName = null)
		{
			base.OnPropertyChanged (propertyName);
			if (propertyName == nameof (PropertyViewModel.ResourceProvider)) {
				ResetResourceSelector ();
			}
		}

		private ResourceSelectorViewModel resourceSelector;
		private CommonBrushType selectedBrushType;

		private void StorePreviousBrush ()
		{
			if (Value is CommonSolidBrush solid)
				Solid.PreviousSolidBrush = solid;
		}

		private void SetBrushType (CommonBrushType type)
		{
			StorePreviousBrush();

			switch(type) {
			case CommonBrushType.NoBrush:
				Value = null;
				break;
			case CommonBrushType.Solid:
				Value = Solid?.PreviousSolidBrush ?? new CommonSolidBrush (CommonColor.Black);
				Solid.CommitLastColor ();
				Solid.CommitHue ();
				break;
			case CommonBrushType.MaterialDesign:
				MaterialDesign.SetToClosest ();
				break;
			}
		}
	}
}
