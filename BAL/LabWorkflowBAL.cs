using RiceMillProject.DAL;
using RiceMillProject.Models;
using System.Data;
using System.Globalization;

namespace RiceMillProject.BAL;

public class LabWorkflowBAL(IConfiguration configuration)
{
    private readonly LabWorkflowDAL dal = new(configuration);
    public List<LabTestMaster> GetTests() => dal.GetTests();
    public DataTable GetUnits()
    {
        return dal.GetUnits();
    }
    public void SaveTest(LabTestMaster test, int userId) => dal.SaveTest(test, userId);
    public void RemoveTest(int id, int userId) => dal.RemoveTest(id, userId);
    public List<LabItemTestMapping> GetMappings() => dal.GetMappings();
    public void SaveMapping(LabItemTestMapping mapping, int userId) => dal.SaveMapping(mapping, userId);
    public void SaveMappings(LabItemTestSelection form, int userId) => dal.SaveMappings(form, userId);
    public void RemoveMapping(int id, int userId) => dal.RemoveMapping(id, userId);
    public List<LabItemOption> GetItems(string? rst = null) => dal.GetItems(rst);
    public List<LabRstSummary> GetRsts() => dal.GetRsts();
    public List<LabReport> GetReports(string? rst, DateTime? from, DateTime? to) => dal.GetReports(rst, from, to);
    public LabReport? GetReport(int id) => dal.GetReport(id);
    public int SaveReport(LabEntryViewModel model, int userId)
    {
        if (model.SubmissionId == Guid.Empty) throw new ArgumentException("Invalid submission. Reload this RST.");
        var eligible = dal.GetItems(model.RSTNumber);
        var selected = eligible.Where(i => model.SelectedItemIds.Contains(i.ItemId)).ToList();
        if (selected.Count == 0 || selected.Count != model.SelectedItemIds.Distinct().Count())
            throw new ArgumentException("Select only items unloaded against this RST.");
        if (selected.Any(i => !model.SelectedCategoryIds.Contains(i.CategoryId)))
            throw new ArgumentException("Select the categories of all selected items.");
        if (selected.Any(i => i.Tests.Count == 0)) throw new ArgumentException("Configure tests for every selected item first.");
        var expected = selected.SelectMany(i => i.Tests.Select(t => (i.CategoryId, i.ItemId, Test: t))).ToList();
        if (model.Results.Count != expected.Count || model.Results.Select(r => (r.CategoryId, r.ItemId, r.TestId)).Distinct().Count() != expected.Count)
            throw new ArgumentException("Fill every test for the selected items exactly once.");
        foreach (var e in expected)
        {
            var result = model.Results.SingleOrDefault(r => r.CategoryId == e.CategoryId && r.ItemId == e.ItemId && r.TestId == e.Test.TestId);
            if (result == null || string.IsNullOrWhiteSpace(result.Value) || result.Value.Length > 500)
                throw new ArgumentException($"Enter a result for {e.Test.TestName}.");
            if (e.Test.ResultType == "Number" && (!decimal.TryParse(result.Value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)
                || number <= -1000000000000m || number >= 1000000000000m || decimal.Round(number, 6) != number))
                throw new ArgumentException($"Enter a valid number with up to 6 decimal places for {e.Test.TestName}.");
        }
        return dal.SaveReport(model, userId);
    }
}
