namespace KinectCare.API.DTOs.Children;

public class CreateChildDto
{
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string DiagnosisType { get; set; } = string.Empty;
    public string? DiagnosisDetails { get; set; }
    public int SpecialistId { get; set; }
    public int ParentId { get; set; }
}