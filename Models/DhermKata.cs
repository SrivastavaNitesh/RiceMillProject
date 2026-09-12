using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace RiceMillProject.Models
{
    public class DhermKata
    {
        private readonly BusinessLayer _BusLayer;
        public DhermKata()
        {

        }

        public DhermKata(IConfiguration configuration)
        {
            _BusLayer = new BusinessLayer(configuration);
        }
        public int Mainofficeid { get; set; }
        public int? childofficeid { get; set; }
        public string? MainofficeName { get; set; }
        public string? childofficeName { get; set; } = null;
        public Boolean IsActive { get; set; } = true;

        public List<DhermKata>? MainLst { get; set; }
        public List<SelectListItem>? MainOfficelist { get; set; }

        internal DhermKata GetAlldhermkata(DhermKata obj)
        {
            DataTable dt = new DataTable();
            dt = _BusLayer.GetAlldhermkata();
            obj.MainLst=new List<DhermKata>();
            if (dt!=null && dt.Rows.Count>0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    obj.MainLst.Add(new DhermKata
                        {
                            Mainofficeid = Convert.ToInt32(dt.Rows[i]["Mainofficeid"]),
                            childofficeid = Convert.ToInt32(dt.Rows[i]["childofficeid"]),
                            MainofficeName = dt.Rows[i]["MainofficeName"].ToString(),
                            childofficeName = dt.Rows[i]["childofficeName"].ToString(),
                            IsActive = Convert.ToBoolean(dt.Rows[i]["IsActive"])
                        });                   

                }
            }
            return obj;
        }
    }
}
