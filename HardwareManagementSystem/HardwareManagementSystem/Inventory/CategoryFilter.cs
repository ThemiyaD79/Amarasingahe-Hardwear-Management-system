using System.Collections.ObjectModel;
using System.Linq;
using HardwareManagementSystem.Models;

namespace HardwareManagementSystem.Inventory
{
    public static class CategoryFilter
    {
        // Filter products by category
        public static ObservableCollection<ProductModel> FilterByCategory(ObservableCollection<ProductModel> products, string category)
        {
            if (string.IsNullOrEmpty(category))
                return new ObservableCollection<ProductModel>(products);

            var filtered = products.Where(p => p.Category.ToLower() == category.ToLower());
            return new ObservableCollection<ProductModel>(filtered);
        }
    }
}