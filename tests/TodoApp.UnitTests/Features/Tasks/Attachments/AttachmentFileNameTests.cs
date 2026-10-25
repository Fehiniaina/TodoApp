using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Domain.Tasks.Attachments;
using TodoApp.Features.Tasks.Attachments;

namespace TodoApp.UnitTests.Features.Tasks.Attachments;

public sealed class AttachmentFileNameTests
{
    [Theory]
    [InlineData("rapport.pdf", "rapport.pdf")]
    [InlineData(@"C:\fakepath\rapport.pdf", "rapport.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("  devis   final  .pdf", "devis final.pdf")]
    [InlineData("a<b>c:d\"e|f?g*.txt", "abcdefg.txt")]
    [InlineData(".pdf", "fichier.pdf")]
    [InlineData("", "fichier")]
    [InlineData(null, "fichier")]
    [InlineData("note.", "note")]
    public void Sanitize_produces_a_safe_display_name(string? input, string expected)
    {
        Assert.Equal(expected, AttachmentFileName.Sanitize(input));
    }

    [Fact]
    public void Right_to_left_override_is_removed_and_reveals_the_real_extension()
    {
        var disguised = "facture\u202Efdp.exe"; // s'affiche « facture exe.pdf »

        var sanitized = AttachmentFileName.Sanitize(disguised);

        Assert.Equal("facturefdp.exe", sanitized);
        Assert.Equal(".exe", Path.GetExtension(sanitized));
    }

    [Fact]
    public void Long_name_is_truncated_but_keeps_its_extension()
    {
        var sanitized = AttachmentFileName.Sanitize(new string('a', 400) + ".pdf");

        Assert.Equal(TaskAttachment.MaxFileNameLength, sanitized.Length);
        Assert.EndsWith(".pdf", sanitized, StringComparison.Ordinal);
    }
}
