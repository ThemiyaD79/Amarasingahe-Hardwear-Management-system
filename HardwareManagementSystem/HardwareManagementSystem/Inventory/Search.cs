using System.Collections.ObjectModel;
using System.Linq;
using HardwareManagementSystem.Models;

namespace HardwareManagementSystem.Inventory
{
    internal class Search
    {
        public static ObservableCollection<ProductModel> FilterProducts(
            ObservableCollection<ProductModel> products,
            string searchText,
            string categoryFilter)
        {
            searchText = searchText?.Trim().ToLower() ?? "";
            categoryFilter = categoryFilter?.Trim() ?? "";

            var filteredList = new ObservableCollection<ProductModel>(
                products.Where(p =>
                    (string.IsNullOrEmpty(searchText) ||
                     p.Name.ToLower().Contains(searchText) ||
                     p.Brand.ToLower().Contains(searchText)) &&
                    (string.IsNullOrEmpty(categoryFilter) ||
                     p.Category == categoryFilter)
                )
            );

            return filteredList;
        }
    }
}