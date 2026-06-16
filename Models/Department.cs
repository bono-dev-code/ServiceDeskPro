namespace ServiceDeskPro.Models;

// Departments help the company organise tickets by business area.
public class Department
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
