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

        public DateTime? AssignedDateTime { get; set; }

        public string VehicleNumber { get; set; } = "";

        public string PartyName { get; set; } = "";

        public string CompanyName { get; set; } = "";

        public string ItemName { get; set; } = "";

        public string LocationName { get; set; } = "";

        public List<int> AssignedLocationIds { get; set; } = new();

        public string MethName { get; set; } = "";


        // =====================================================
        // METH RESULT - READ ONLY
        // =====================================================

        public int MethTotalBags { get; set; }
        public bool IsWorkerWorkCompleted { get; set; }
        public bool IsFieldDetailsCompleted { get; set; }
        public List<SupervisorUnloadLocationRow> LocationRows { get; set; } = new();


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
        public int LocationId { get; set; }

        public int BagTypeId { get; set; }

        public int BagCount { get; set; }
    }

    public class SupervisorUnloadLocationRow
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public List<SupervisorUnloadLocationBagRow> BagRows { get; set; } = new();
    }

    public class SupervisorUnloadLocationBagRow
    {
        public int LocationId { get; set; }
        public int BagTypeId { get; set; }
        public int BagCount { get; set; }
    }
}
