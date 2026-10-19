using System.ComponentModel.DataAnnotations;

namespace GamingPlatform.Domain.Validation
{
    // Rejects dates of birth for people younger than the given age (checked on the server,
    // so it holds even if the browser's date picker limit is bypassed)
    [AttributeUsage(AttributeTargets.Property)]
    public class MinimumAgeAttribute : ValidationAttribute
    {
        public static readonly DateTime EarliestBirthDate = new(1900, 1, 1);

        public int Years { get; }

        public MinimumAgeAttribute(int years)
        {
            Years = years;
        }

        // The latest date of birth that is old enough today
        public static DateTime LatestBirthDate(int years) => DateTime.Today.AddYears(-years);

        // `today` is a parameter so tests can check any date. AddYears handles leap years:
        // someone born on 29 Feb turns 18 on 1 Mar in non-leap years.
        public static bool IsOldEnough(DateTime dateOfBirth, int years, DateTime today) =>
            dateOfBirth.Date <= today.Date.AddYears(-years);

        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is not DateTime dateOfBirth) return ValidationResult.Success; // [Required] reports a missing date

            if (dateOfBirth.Date < EarliestBirthDate)
                return new ValidationResult("Please enter a valid date of birth.");

            if (!IsOldEnough(dateOfBirth, Years, DateTime.Today))
                return new ValidationResult($"You must be at least {Years} years old to create an account.");

            return ValidationResult.Success;
        }
    }
}
