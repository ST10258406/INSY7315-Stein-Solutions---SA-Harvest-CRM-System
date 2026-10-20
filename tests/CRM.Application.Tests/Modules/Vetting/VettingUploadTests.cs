using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Vetting.Mappings;
using CRM.Application.Modules.Vetting.Uploads.Commands.UploadAssessmentFile;
using CRM.Application.Modules.Vetting.Uploads.Commands.UploadEvidencePhoto;
using CRM.Application.Modules.Vetting.Uploads.Queries.GetUploadDownloadUrl;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Vetting;

public class VettingUploadTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];
    private static readonly byte[] Pdf = "%PDF-1.7 test"u8.ToArray();

    private readonly IVettingSubmissionRepository _submissions = Substitute.For<IVettingSubmissionRepository>();
    private readonly IBlobStorageService _blob = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMapper _mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<VettingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    public VettingUploadTests()
    {
        _blob.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => new BlobUploadResult(ci.ArgAt<string>(1), "https://blob/" + ci.ArgAt<string>(1)));
        _blob.GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(ci => "https://blob/" + ci.ArgAt<string>(0) + "?sas");
    }

    private static UploadEvidencePhotoCommand Photo(byte[] bytes, string name, string type, Guid? id = null, int shot = 2) => new()
    {
        SubmissionId = id ?? Guid.NewGuid(), ShotIndex = shot, FileStream = new MemoryStream(bytes),
        OriginalFileName = name, ContentType = type, FileSizeBytes = bytes.Length
    };

    private static UploadAssessmentFileCommand File(byte[] bytes, string name, string type, string slot = "npo-certificate", Guid? id = null) => new()
    {
        SubmissionId = id ?? Guid.NewGuid(), Slot = slot, FileStream = new MemoryStream(bytes),
        OriginalFileName = name, ContentType = type, FileSizeBytes = bytes.Length
    };

    private VettingSubmission Existing(VettingApprovalState state = VettingApprovalState.New)
    {
        var s = new VettingSubmission
        {
            Id = Guid.NewGuid(),
            FormReference = "VET-2026-0001",
            ApprovalState = state,
            Details = VettingFixtures.CleanDetails(),
            Photos = Enum.GetValues<EvidencePhotoCategory>()
                .Select(c => new VettingEvidencePhoto { ShotIndex = (int)c, Category = c })
                .ToList()
        };
        _submissions.GetForUpdateAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s);
        return s;
    }

    // ---- validators

    [Fact]
    public void Photo_ValidJpeg_Passes()
        => Assert.True(new UploadEvidencePhotoCommandValidator().Validate(Photo(Jpeg, "kitchen.jpg", "image/jpeg")).IsValid);

    [Fact]
    public void File_PngRenamedToPdf_WithPdfContentType_IsRejected()
        => Assert.False(new UploadAssessmentFileCommandValidator().Validate(File(Png, "npo.pdf", "application/pdf")).IsValid);

    [Fact]
    public void Photo_Pdf_IsRejected()
        => Assert.False(new UploadEvidencePhotoCommandValidator().Validate(Photo(Pdf, "kitchen.pdf", "application/pdf")).IsValid);

    [Fact]
    public void ImageOnlyFileSlot_Pdf_IsRejected_ButPdfSlotAcceptsIt()
    {
        Assert.False(new UploadAssessmentFileCommandValidator().Validate(File(Pdf, "f.pdf", "application/pdf", "facility-photos")).IsValid);
        Assert.True(new UploadAssessmentFileCommandValidator().Validate(File(Pdf, "f.pdf", "application/pdf", "certificates")).IsValid);
    }

    [Fact]
    public void Photo_Over5MB_IsRejected()
    {
        var big = new byte[5 * 1024 * 1024 + 1];
        Jpeg.CopyTo(big, 0);
        var result = new UploadEvidencePhotoCommandValidator().Validate(Photo(big, "big.jpg", "image/jpeg"));
        Assert.Contains(result.Errors, e => e.ErrorMessage == "File exceeds 5MB limit.");
    }

    // ---- handlers

    [Fact]
    public async Task UploadPhoto_Slot2_CapturesIt_WithSniffedPathAndTruncatedName()
    {
        var s = Existing();
        var command = Photo(Png, new string('a', 150) + ".png", "image/png", s.Id, shot: 2);

        var dto = await new UploadEvidencePhotoCommandHandler(_submissions, _blob, _unitOfWork, _mapper).Handle(command, CancellationToken.None);

        var photo = s.Photos.Single(p => p.ShotIndex == 2);
        Assert.True(photo.IsCaptured);
        Assert.NotNull(photo.CapturedAt);
        Assert.Equal(100, photo.FileName!.Length);
        Assert.EndsWith(".png", photo.FileName);
        Assert.Matches($@"^vetting/{s.Id}/photos/2-[0-9a-f-]{{36}}\.png$", photo.BlobStoragePath);
        Assert.Equal("FridgeFreezer", dto.Category);
        Assert.All(s.Photos.Where(p => p.ShotIndex != 2), p => Assert.False(p.IsCaptured));
        Assert.Equal(s.Id, command.EntityId);
        Assert.DoesNotContain("http", command.NewValues!.ToString());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadPhoto_Twice_KeepsOldBlob_AndUsesANewPath()
    {
        var s = Existing();
        var handler = new UploadEvidencePhotoCommandHandler(_submissions, _blob, _unitOfWork, _mapper);

        await handler.Handle(Photo(Jpeg, "a.jpg", "image/jpeg", s.Id, 0), CancellationToken.None);
        var first = s.Photos.Single(p => p.ShotIndex == 0).BlobStoragePath;
        await handler.Handle(Photo(Jpeg, "b.jpg", "image/jpeg", s.Id, 0), CancellationToken.None);

        Assert.NotEqual(first, s.Photos.Single(p => p.ShotIndex == 0).BlobStoragePath);
        await _blob.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }

    [Theory]
    [InlineData(VettingApprovalState.Ready)]
    [InlineData(VettingApprovalState.Approved)]
    public async Task Uploads_WhenNotWithTheOfficer_Throw409_BeforeTouchingBlobStorage(VettingApprovalState state)
    {
        var s = Existing(state);

        await Assert.ThrowsAsync<ConflictException>(() => new UploadEvidencePhotoCommandHandler(_submissions, _blob, _unitOfWork, _mapper)
            .Handle(Photo(Jpeg, "k.jpg", "image/jpeg", s.Id), CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() => new UploadAssessmentFileCommandHandler(_submissions, _blob, _unitOfWork, _mapper)
            .Handle(File(Pdf, "npo.pdf", "application/pdf", id: s.Id), CancellationToken.None));

        await _blob.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UploadFile_SetsOnlyThatSlotsPath()
    {
        var s = Existing();
        s.Details!.NpoCertificatePath = null;

        var files = await new UploadAssessmentFileCommandHandler(_submissions, _blob, _unitOfWork, _mapper)
            .Handle(File(Pdf, "npo.pdf", "application/pdf", "npo-certificate", s.Id), CancellationToken.None);

        Assert.Matches($@"^vetting/{s.Id}/files/npo-certificate-[0-9a-f-]{{36}}\.pdf$", s.Details.NpoCertificatePath);
        Assert.True(files.NpoCertificate);
        Assert.False(files.PboCertificate);
    }

    [Fact]
    public async Task UploadFile_UnknownSlot_Throws404()
        => await Assert.ThrowsAsync<NotFoundException>(() => new UploadAssessmentFileCommandHandler(_submissions, _blob, _unitOfWork, _mapper)
            .Handle(File(Pdf, "x.pdf", "application/pdf", "tax-return"), CancellationToken.None));

    [Fact]
    public async Task Download_EmptySlot_Is404_FilledSlot_ReturnsSasInAnyState()
    {
        var s = Existing(VettingApprovalState.Approved);
        s.Photos.Single(p => p.ShotIndex == 1).BlobStoragePath = "vetting/x/photos/1-abc.jpg";
        s.Photos.Single(p => p.ShotIndex == 1).FileName = "dry.jpg";
        var handler = new GetUploadDownloadUrlQueryHandler(_submissions, _blob);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetUploadDownloadUrlQuery { SubmissionId = s.Id, ShotIndex = 0 }, CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetUploadDownloadUrlQuery { SubmissionId = s.Id, Slot = "pbo-certificate" }, CancellationToken.None));

        var photo = await handler.Handle(new GetUploadDownloadUrlQuery { SubmissionId = s.Id, ShotIndex = 1 }, CancellationToken.None);
        Assert.Equal("https://blob/vetting/x/photos/1-abc.jpg?sas", photo.DownloadUrl);
        Assert.Equal("dry.jpg", photo.OriginalFileName);
        await _blob.Received(1).GenerateSasUrlAsync("vetting/x/photos/1-abc.jpg", TimeSpan.FromMinutes(15));

        var file = await handler.Handle(new GetUploadDownloadUrlQuery { SubmissionId = s.Id, Slot = "npo-certificate" }, CancellationToken.None);
        Assert.Equal("npo-certificate.pdf", file.OriginalFileName);
    }
}
