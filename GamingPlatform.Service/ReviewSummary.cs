namespace GamingPlatform.Service
{
    // Steam-style review summary. Steam reviews are thumbs up/down; ours are 1-5 stars,
    // so 4 and 5 stars count as positive and 1-3 as not positive.
    public static class ReviewSummary
    {
        public static bool IsPositive(int score) => score >= 4;

        // The same bands Steam uses: the share of positive reviews picks the wording,
        // and the number of reviews picks how strong it is ("Very", "Overwhelmingly")
        public static string Label(int positive, int total)
        {
            if (total == 0) return "No user reviews";

            int percent = PositivePercent(positive, total);
            return percent switch
            {
                >= 95 when total >= 500 => "Overwhelmingly Positive",
                >= 80 when total >= 50 => "Very Positive",
                >= 80 => "Positive",
                >= 70 => "Mostly Positive",
                >= 40 => "Mixed",
                >= 20 => "Mostly Negative",
                _ when total >= 500 => "Overwhelmingly Negative",
                _ when total >= 50 => "Very Negative",
                _ => "Negative"
            };
        }

        // Rounded down, like Steam: 79.9% is "Mostly Positive", not "Positive"
        public static int PositivePercent(int positive, int total) =>
            total == 0 ? 0 : positive * 100 / total;

        // A CSS class for colouring the label (blue-ish positive, orange mixed, red negative)
        public static string Tone(string label) =>
            label.Contains("Positive") ? "positive" : label == "Mixed" ? "mixed" : label.Contains("Negative") ? "negative" : "none";
    }
}
