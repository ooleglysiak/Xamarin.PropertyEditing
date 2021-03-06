using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Windows
{
	internal partial class TypeSelectorWindow
		: WindowEx
	{
		internal TypeSelectorWindow (IEnumerable<ResourceDictionary> mergedResources, AsyncValue<IReadOnlyDictionary<IAssemblyInfo, ILookup<string, ITypeInfo>>> assignableTypes)
		{
			DataContext = new TypeSelectorViewModel (assignableTypes);
			InitializeComponent ();
			Resources.MergedDictionaries.AddItems (mergedResources);
		}

		internal static ITypeInfo RequestType (FrameworkElement owner, AsyncValue<IReadOnlyDictionary<IAssemblyInfo, ILookup<string, ITypeInfo>>> assignableTypes)
		{
			Window hostWindow = Window.GetWindow (owner);

			var w = new TypeSelectorWindow (owner.Resources.MergedDictionaries, assignableTypes) {
				Owner = hostWindow,
			};

			if (!w.ShowDialog () ?? false)
				return null;

			return ((TypeSelectorViewModel)w.DataContext).SelectedType;
		}

		private void OnSelectedItemChanged (object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			this.ok.IsEnabled = (e.NewValue as ITypeInfo) != null;
		}

		private void OnItemActivated (object sender, System.EventArgs e)
		{
			DialogResult = true;
		}

		private void OnOkClicked (object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
