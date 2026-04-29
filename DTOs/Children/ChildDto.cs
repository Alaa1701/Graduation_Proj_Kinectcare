namespace KinectCare.API.DTOs.Children;

public class ChildDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age => DateTime.Today.Year - DateOfBirth.Year;
    public string Gender { get; set; } = string.Empty;
    public string DiagnosisType { get; set; } = string.Empty;
    public string? DiagnosisDetails { get; set; }
    public string? ProfileImagePath { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // بيانات الأخصائي وولي الأمر
    public int SpecialistId { get; set; }
    public string SpecialistName { get; set; } = string.Empty;
    public int ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
}