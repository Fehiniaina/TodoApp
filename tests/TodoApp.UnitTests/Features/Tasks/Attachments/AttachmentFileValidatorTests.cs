using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Domain.Tasks.Attachments;
using TodoApp.Features.Tasks.Attachments;

namespace TodoApp.UnitTests.Features.Tasks.Attachments;

public sealed class AttachmentFileValidatorTests
{
    private static readonly byte[] Pdf = "%PDF-1.7\n"u8.ToArray();
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00];
    private static readonly byte[] WindowsExecutable = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00]; // « MZ »

    public static TheoryData<string, byte[], string> ValidFiles => new()
    {
        { "rapport.pdf", Pdf, "application/pdf" },
        { "capture.PNG", Png, "image/png" },
        { "devis.docx", Zip, "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { "notes.txt", "Bonjour 👋"u8.ToArray(), "text/plain; charset=utf-8" },
    };

    [Theory]
    [MemberData(nameof(ValidFiles))]
    public void Valid_files_are_accepted_with_the_server_side_content_type(string name, byte[] header, string contentType)
    {
        ArgumentNullException.ThrowIfNull(header);
        var result = AttachmentFileValidator.Validate(name, header.Length, header);

        Assert.True(result.IsSuccess);
        Assert.Equal(contentType, result.Value.ContentType);
    }

    [Fact]
    public void Executable_renamed_as_pdf_is_rejected()
    {
        var result = AttachmentFileValidator.Validate("facture.pdf", WindowsExecutable.Length, WindowsExecutable);

        Assert.Equal("Attachment.ContentMismatch", result.Error.Code);
    }

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("page.html")]
    [InlineData("logo.svg")]
    [InlineData("macro.xlsm")]
    [InlineData("sans-extension")]
    public void Dangerous_or_unknown_types_are_rejected(string name)
    {
        var result = AttachmentFileValidator.Validate(name, Pdf.Length, Pdf);

        Assert.Equal("Attachment.TypeNotAllowed", result.Error.Code);
    }

    [Fact]
    public void Binary_content_in_a_text_file_is_rejected()
    {
        var result = AttachmentFileValidator.Validate("notes.txt", WindowsExecutable.Length, WindowsExecutable);

        Assert.Equal("Attachment.ContentMismatch", result.Error.Code);
    }

    [Fact]
    public void Empty_and_oversized_files_are_rejected()
    {
        Assert.Equal("Attachment.Empty", AttachmentFileValidator.Validate("a.pdf", 0, []).Error.Code);
        Assert.Equal("Attachment.TooLarge", AttachmentFileValidator.Validate("a.pdf", TaskAttachment.MaxSizeBytes + 1, Pdf).Error.Code);
    }
}
