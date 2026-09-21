using System.Collections.Generic;

namespace RiceMillProject.Models
{
    public class SupervisorUnloadActionViewModel
    {
        // =====================================================
        // RST / UNLOAD DETAILS - READ ONLY
        // =====================================================

        public int UnloadId { get; set; }

        public string RSTNumber { get; set; } = "";

        public string VehicleNumber { get; set; } = "";

        public string PartyName { get; set; } = "";

        public string CompanyName { get; set; } = "";

        public string ItemName { get; set; } = "";

        public string LocationName { get; set; } = "";

        public string MethName { get; set; } = "";


        // =====================================================
        // METH RESULT - READ ONLY
        // =====================================================

        public int MethTotalBags { get; set; }


        // =====================================================
        // SUPERVISOR GOOGLE-FORM STYLE ENTRY
        // =====================================================

        public int StackPP { get; set; }

        public int StackJute { get; set; }

        public int HaudiPP { get; set; }

        public int HaudiJute { get; set; }


        // =====================================================
        // CATEGORY / VARIETY WISE BREAKUP
        // =====================================================

        public List<SupervisorUnloadCategoryRow> CategoryRows { get; set; }
            = new List<SupervisorUnloadCategoryRow>();
    }


    public class SupervisorUnloadCategoryRow
    {
        public int CategoryId { get; set; }

        public int ItemId { get; set; }

        public int BagTypeId { get; set; }

        public int BagCount { get; set; }
    }
}