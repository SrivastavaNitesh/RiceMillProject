using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace RiceMillProject.Models
{
    public static class Allclass
    {
        public static List<SelectListItem> CreateDropdown(DataTable dt)
        {
            var items = new List<SelectListItem>();

            // Always add Select at top
            items.Add(new SelectListItem
            {
                Text = "--Select--",
                Value = "0"
            });

            // If DataTable is null or empty, return only Select
            if (dt == null || dt.Rows.Count == 0)
                return items;

            // Add DataTable records
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                items.Add(new SelectListItem
                {
                    Text = dt.Rows[i][1].ToString(),
                    Value = dt.Rows[i][0].ToString()
                });
            }

            return items;
        }
    }
}
