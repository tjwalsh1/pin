namespace Pinpoint_Quiz.Services
{
    public class ValidationService
    {
        public bool IsEmailValid(string email)
        {
            return !string.IsNullOrEmpty(email) && email.Contains("@");
        }

        public bool IsProficiencyValid(double proficiency)
        {
            return proficiency >= 1 && proficiency <= 10;
        }
    }
}
