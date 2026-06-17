namespace ServiceDeskPro.Models;

// This model stores one SLA rule for a ticket priority.
// Admins can edit these values from Settings, so the system is not hardcoded.
public class SlaRule
{
    public int SlaRuleId { get; set; }
    public string Priority { get; set; } = "";
    public int ResponseHours { get; set; }
    public int ResolutionHours { get; set; }
}
