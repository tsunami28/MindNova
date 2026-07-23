using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Infrastructure;

public class TreatmentNoteConfigurationTests
{
    private readonly MindNovaDbContext _context;

    public TreatmentNoteConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<MindNovaDbContext>()
            .UseSqlServer("Server=localhost;Database=fake;TrustServerCertificate=true")
            .Options;

        _context = new MindNovaDbContext(options);
    }

    [Fact]
    [Trait("Story", "MN-32")]
    [Trait("AC", "AC-1")]
    public void TreatmentNote_HasExpectedProperties()
    {
        var note = new TreatmentNote
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            TherapistUserId = "user-123",
            PresentingIssue = "Anxiety",
            Interventions = "CBT techniques",
            Homework = "Breathing exercises",
            ProgressRating = 7,
            FreeText = "Client showed improvement",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            DeletedAt = null,
            DeletedByUserId = null
        };

        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.NotEqual(Guid.Empty, note.SessionId);
        Assert.Equal("user-123", note.TherapistUserId);
        Assert.Equal("Anxiety", note.PresentingIssue);
        Assert.Equal("CBT techniques", note.Interventions);
        Assert.Equal("Breathing exercises", note.Homework);
        Assert.Equal(7, note.ProgressRating);
        Assert.Equal("Client showed improvement", note.FreeText);
        Assert.NotEqual(default, note.CreatedAt);
        Assert.NotEqual(default, note.UpdatedAt);
        Assert.False(note.IsDeleted);
        Assert.Null(note.DeletedAt);
        Assert.Null(note.DeletedByUserId);
    }

    [Fact]
    [Trait("Story", "MN-32")]
    [Trait("AC", "AC-2")]
    public void TreatmentNoteConfiguration_HasForeignKey_ToSession()
    {
        var entityType = _context.Model.FindEntityType(typeof(TreatmentNote));
        var fks = entityType.GetForeignKeys().ToList();

        Assert.Contains(fks, fk => fk.Properties.Any(p => p.Name == nameof(TreatmentNote.SessionId)));
    }

    [Fact]
    [Trait("Story", "MN-32")]
    [Trait("AC", "AC-2")]
    public void TreatmentNoteConfiguration_HasIndex_OnSessionId()
    {
        var entityType = _context.Model.FindEntityType(typeof(TreatmentNote));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(TreatmentNote.SessionId)));
    }

    [Fact]
    [Trait("Story", "MN-32")]
    [Trait("AC", "AC-2")]
    public void TreatmentNoteConfiguration_HasIndex_OnTherapistUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(TreatmentNote));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(TreatmentNote.TherapistUserId)));
    }

    [Fact]
    [Trait("Story", "MN-32")]
    [Trait("AC", "AC-4")]
    public void DbContext_HasDbSetForTreatmentNote()
    {
        Assert.NotNull(_context.TreatmentNotes);
    }

    [Fact]
    [Trait("Story", "MN-32")]
    [Trait("AC", "AC-5")]
    public void TreatmentNote_SoftDeleteDefaults_ToNotDeleted()
    {
        var note = new TreatmentNote();

        Assert.False(note.IsDeleted);
        Assert.Null(note.DeletedAt);
        Assert.Null(note.DeletedByUserId);
    }
}
