using System.ComponentModel.DataAnnotations;
using GamingPlatform.Domain.Validation;

namespace GamingPlatform.Tests
{
    // Pure logic, no database
    public class MinimumAgeTests
    {
        private static ValidationResult? Validate(DateTime? dateOfBirth) =>
            new MinimumAgeAttribute(18).GetValidationResult(dateOfBirth, new ValidationContext(new object()));

        [Fact]
        public void Turning_18_today_is_accepted() =>
            Assert.Equal(ValidationResult.Success, Validate(DateTime.Today.AddYears(-18)));

        [Fact]
        public void One_day_short_of_18_is_rejected()
        {
            var result = Validate(DateTime.Today.AddYears(-18).AddDays(1));
            Assert.Contains("at least 18", result!.ErrorMessage);
        }

        [Fact]
        public void A_17_year_old_is_rejected() =>
            Assert.NotEqual(ValidationResult.Success, Validate(DateTime.Today.AddYears(-17)));

        [Fact]
        public void Dates_before_1900_are_rejected() =>
            Assert.Contains("valid date", Validate(new DateTime(1899, 12, 31))!.ErrorMessage);

        [Fact]
        public void A_missing_date_is_left_to_Required() =>
            Assert.Equal(ValidationResult.Success, Validate(null));

        // Born 29 Feb 2008; 2026 has no 29 Feb
        [Theory]
        [InlineData("2026-02-28", false)] // not 18 yet on 28 Feb...
        [InlineData("2026-03-01", true)]  // ...18 on 1 Mar
        public void Leap_day_birthdays(string today, bool oldEnough) =>
            Assert.Equal(oldEnough, MinimumAgeAttribute.IsOldEnough(new DateTime(2008, 2, 29), 18, DateTime.Parse(today)));

        [Theory]
        [InlineData("2026-10-16", false)] // the day before the 18th birthday
        [InlineData("2026-10-17", true)]  // the birthday itself
        public void Ordinary_birthdays(string today, bool oldEnough) =>
            Assert.Equal(oldEnough, MinimumAgeAttribute.IsOldEnough(new DateTime(2008, 10, 17), 18, DateTime.Parse(today)));
    }
}
