using GitExtensions.Avalonia.Services;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;

namespace GitExtensions.Avalonia.Tests;

public sealed class CommitPresentationTests
{
    [Test]
    public void FromRevision_should_project_subject_author_date_and_short_id()
    {
        DateTime authorDate = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        GitRevision revision = new(ObjectId.Random())
        {
            Subject = "Fix crash on startup",
            Author = "Jane Doe",
            AuthorUnixTime = new DateTimeOffset(authorDate).ToUnixTimeSeconds()
        };

        CommitPresentation presentation = CommitPresentation.FromRevision(revision);

        presentation.ShortId.Should().Be(revision.ObjectId.ToShortString());
        presentation.Subject.Should().Be("Fix crash on startup");
        presentation.Author.Should().Be("Jane Doe");
        presentation.Date.Should().Be(revision.AuthorDate);
    }

    [Test]
    public void FromRevision_should_keep_only_the_subject_line_for_multiline_messages()
    {
        GitRevision revision = new(ObjectId.Random())
        {
            Subject = "Short subject",
            Author = "Jane Doe",
            HasMultiLineMessage = true,
            Body = "Short subject\n\nLonger body explaining the change in detail."
        };

        CommitPresentation presentation = CommitPresentation.FromRevision(revision);

        presentation.Subject.Should().Be("Short subject");
    }

    [Test]
    public void FromRevision_should_default_author_to_empty_when_missing()
    {
        GitRevision revision = new(ObjectId.Random())
        {
            Subject = "No author recorded",
            Author = null
        };

        CommitPresentation presentation = CommitPresentation.FromRevision(revision);

        presentation.Author.Should().BeEmpty();
    }
}
