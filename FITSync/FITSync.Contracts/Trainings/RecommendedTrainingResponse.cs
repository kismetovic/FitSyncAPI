namespace FITSync.Contracts.Trainings;

/// <summary>
/// A recommended training plus the explanation the mobile app shows to the user.
/// See docs/RECOMMENDER.md for how Score and Reason are produced.
/// </summary>
public class RecommendedTrainingResponse : TrainingResponse
{
    /// <summary>Combined content-based + collaborative score. Higher is a stronger match.</summary>
    public double Score { get; set; }

    /// <summary>Which strategy contributed the most: ContentBased, Collaborative, Popular or Fallback.</summary>
    public string Strategy { get; set; } = "Fallback";

    /// <summary>Human-readable justification, e.g. "Because you often book Yoga trainings".</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Individual signals that fed the score, for transparency and debugging.</summary>
    public List<string> MatchedSignals { get; set; } = new();
}
